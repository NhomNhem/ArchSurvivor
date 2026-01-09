Shader "Hidden/ChocDino/UIFX/DistanceMap"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _InsideTex ("Inside Texture", 2D) = "white" {}
		_StepSize ("Step Size", Vector) = (1.0, 1.0, 0.0, 0.0)
		_DownSample ("Down Sample", Int) = 1
		[KeywordEnum(Square,Diamond,Circle)] Dist("Dist",int) = 0
	}

	CGINCLUDE

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
	};	

	uniform sampler2D _MainTex;
	uniform sampler2D _InsideTex;
	uniform float4 _MainTex_TexelSize;
	uniform float2 _StepSize;
	uniform int _DownSample;

	v2f vert (appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	// Manhattan distance
	float distanceDiamond(float2 a, float2 b)
	{
		return abs(a.x - b.x) + abs(a.y - b.y);
	}
	// Chebyshev distance
	float distanceSquare(float2 a, float2 b)
	{
		return max(abs(a.x - b.x), abs(a.y - b.y));
	}

	#define NULL_PIXEL -1.0
	
	float2 fragAlphaToUV(v2f i) : SV_Target
	{
		float2 offset = _MainTex_TexelSize.xy;

		float2 result = i.uv;

		float alpha = tex2D(_MainTex, i.uv).a;
		if (alpha > 0.99)
		{
			return result;
		}
		// Changed this from 0.01 to 0.016 to filter out some very tiny values introduced in some textues when using HIGH compression setting
		if (alpha < 0.016)
		{
			return NULL_PIXEL;
		}

		// For intermediate alpha values use sobel filter to estimate the fractional offset of the subpixel
		// Credit for this idea to Ben Golus "The Quest for Very Wide Outlines".

		float c00 = tex2D(_MainTex, i.uv - offset.xy).a;
		float c10 = tex2D(_MainTex, i.uv + float2(0.0, -offset.y)).a;
		float c20 = tex2D(_MainTex, i.uv + float2(offset.x, -offset.y)).a;

		float c01 = tex2D(_MainTex, i.uv + float2(-offset.x, 0.0)).a;
		float c21 = tex2D(_MainTex, i.uv + float2(offset.x, 0.0)).a;

		float c02 = tex2D(_MainTex, i.uv + float2(-offset.x, offset.y)).a;
		float c12 = tex2D(_MainTex, i.uv + float2(0.0, offset.y)).a;
		float c22 = tex2D(_MainTex, i.uv + float2(offset.x, offset.y)).a;

		// Sobel gradient to estimate edge direction
		float sobelX = c00 + c01 * 2.0 + c02 - c20 - c21 * 2.0 - c22;
		float sobelY = c00 + c10 * 2.0 + c20 - c02 - c12 * 2.0 - c22;
		float2 dir = -float2(sobelX, sobelY);
		
		// If dir length is small, this is either a sub pixel dot or line
		// no way to estimate sub pixel edge, so output position
		if (abs(dir.x) <= 0.005 && abs(dir.y) <= 0.005)
		{
			return result;
		}

		// normalize direction
		dir = normalize(dir);

		// sub pixel offset
		offset *= dir * (1.0 - alpha);
		
		result = (i.uv + offset);
		
		return result;
	}

	float2 fragInvAlphaToUV(v2f i) : SV_Target
	{
		float2 offset = _MainTex_TexelSize.xy;

		float2 result = i.uv;

		float alpha = 1.0-tex2D(_MainTex, i.uv).a;
		if (alpha > 0.99)
		{
			return result;
		}
		if (alpha < 0.01)
		{
			return NULL_PIXEL;
		}

		// For intermediate alpha values use sobel filter to estimate the fractional offset of the subpixel
		// Credit for this idea to Ben Golus "The Quest for Very Wide Outlines".

		float c00 = tex2D(_MainTex, i.uv - offset.xy).a;
		float c10 = tex2D(_MainTex, i.uv + float2(0.0, -offset.y)).a;
		float c20 = tex2D(_MainTex, i.uv + float2(offset.x, -offset.y)).a;

		float c01 = tex2D(_MainTex, i.uv + float2(-offset.x, 0.0)).a;
		float c21 = tex2D(_MainTex, i.uv + float2(offset.x, 0.0)).a;

		float c02 = tex2D(_MainTex, i.uv + float2(-offset.x, offset.y)).a;
		float c12 = tex2D(_MainTex, i.uv + float2(0.0, offset.y)).a;
		float c22 = tex2D(_MainTex, i.uv + float2(offset.x, offset.y)).a;

		// Sobel gradient to estimate edge direction
		float sobelX = c00 + c01 * 2.0 + c02 - c20 - c21 * 2.0 - c22;
		float sobelY = c00 + c10 * 2.0 + c20 - c02 - c12 * 2.0 - c22;
		float2 dir = float2(sobelX, sobelY);
		
		// If dir length is small, this is either a sub pixel dot or line
		// no way to estimate sub pixel edge, so output position
		if (abs(dir.x) <= 0.005 && abs(dir.y) <= 0.005)
		{
			return result;
		}

		// normalize direction
		dir = normalize(dir);

		// sub pixel offset
		offset *= dir * (1.0 - alpha);
		
		result = (i.uv + offset);
		
		return result;
	}

	float2 fragJumpFlood(v2f i) : SV_Target
	{
		float2 pixelScale = _MainTex_TexelSize.zw;
		float2 offset = _MainTex_TexelSize.xy * _StepSize;
		float2 closest = NULL_PIXEL;
		// NOTE: this value should be big enough, as using 1.#INF was causing problems with some console compilers
		#if SHADER_API_GLES || SHADER_API_MOBILE || SHADER_API_SWITCH 
		float minDist = 32768.0;
		#else
		float minDist = 16777216.0;
		#endif

		float2 p0 = tex2D(_MainTex, i.uv).xy;
		if (p0.x > NULL_PIXEL)
		{
			float2 v3 = (p0 - i.uv) * pixelScale;
			float dd = dot(v3, v3);
			if (dd < 2)//(abs(p0.x - i.uv.x) < _MainTex_TexelSize.x*1.5) && (abs(p0.y - i.uv.y) < _MainTex_TexelSize.y*1.5))
			{
				return p0;
			}
			
			closest = p0;
			#if DIST_CIRCLE
			minDist = dd;//dot(v2, v2);
			//float sd = distance(p * pixelScale, i.uv * pixelScale);
			#elif DIST_DIAMOND
			minDist = distanceDiamond(p0 * pixelScale, i.uv * pixelScale);
			#elif DIST_SQUARE
			minDist = distanceSquare(p0 * pixelScale, i.uv * pixelScale);
			#endif
		}
		
		UNITY_UNROLL
		for (int y = -1; y <= 1; y++)
		{
			UNITY_UNROLL
			for (int x = -1; x <= 1; x++)
			{
				if (x == 0 && y == 0) continue;

				float2 p = tex2D(_MainTex, i.uv + offset * float2(x, y)).xy;
				if (p.x > NULL_PIXEL)
				{
					float sd = 0.0;
					#if DIST_CIRCLE
					float2 v = (p - i.uv) * pixelScale;
					sd = dot(v, v);
					//float sd = distance(p * pixelScale, i.uv * pixelScale);
					#elif DIST_DIAMOND
					sd = distanceDiamond(p * pixelScale, i.uv * pixelScale);
					#elif DIST_SQUARE
					sd = distanceSquare(p * pixelScale, i.uv * pixelScale);
					#endif
					if (sd < minDist)
					{
						minDist = sd;
						closest = p;
					}
				}
			}
		}
		return closest;
	}

	float2 fragJumpFloodSingleAxis(v2f i) : SV_Target
	{
		float2 pixelScale =  _MainTex_TexelSize.zw;
		float2 offset = _MainTex_TexelSize.xy * _StepSize;
		float2 closest = NULL_PIXEL;
		// NOTE: this value should be big enough, as using 1.#INF was causing problems with some console compilers
		#if SHADER_API_GLES || SHADER_API_MOBILE || SHADER_API_SWITCH 
		float minDist = 32768.0;
		#else
		float minDist = 16777216.0;
		#endif

		float2 p0 = tex2D(_MainTex, i.uv).xy;
		if (p0.x > NULL_PIXEL)
		{
			float2 v3 = (p0 - i.uv) * pixelScale;
			float dd = dot(v3, v3);
			if (dd < 2)//(abs(p0.x - i.uv.x) < _MainTex_TexelSize.x*1.5) && (abs(p0.y - i.uv.y) < _MainTex_TexelSize.y*1.5))
			{
				return p0;
			}
			
			closest = p0;
			#if DIST_CIRCLE
			//float2 v2 = (p0 - i.uv) * pixelScale;
			minDist = dd;//dot(v2, v2);
			//float sd = distance(p * pixelScale, i.uv * pixelScale);
			#elif DIST_DIAMOND
			minDist = distanceDiamond(p0 * pixelScale, i.uv * pixelScale);
			#elif DIST_SQUARE
			minDist = distanceSquare(p0 * pixelScale, i.uv * pixelScale);
			#endif
		}
		
		UNITY_UNROLL
		for (int x = -1; x <= 1; x++)
		{
			if (x == 0) continue;
			float2 p = tex2D(_MainTex, i.uv + offset * float2(x, x)).xy;
			if (p.x > NULL_PIXEL)
			{
				float sd = 0.0;
				#if DIST_CIRCLE
				float2 v = (p - i.uv) * pixelScale;
				sd = dot(v, v);
				//float sd = distance(p * pixelScale, i.uv * pixelScale);
				#elif DIST_DIAMOND
				sd = distanceDiamond(p * pixelScale, i.uv * pixelScale);
				#elif DIST_SQUARE
				sd = distanceSquare(p * pixelScale, i.uv * pixelScale);
				#endif
				if (sd < minDist)
				{
					minDist = sd;
					closest = p;
				}
			}
		}
		return closest;
	}

	// NOTE: In GLES2.0 if you return float it gives an error "Type mismatch, cannot convert from 'float' to 'vec4'", so just return float4
	float4 fragResolveDistance(v2f i) : SV_Target
	{
		float2 pixelScale = _MainTex_TexelSize.zw;

		float2 p0 = i.uv * pixelScale;
		float2 p1 = tex2D(_MainTex, i.uv).xy * pixelScale;

		float d = 0.0;
		//if (p1.x > NULL_PIXEL)
		{
			#if DIST_CIRCLE
			d = distance(p0, p1);
			#elif DIST_DIAMOND
			d = distanceDiamond(p0, p1);
			#elif DIST_SQUARE
			d = distanceSquare(p0, p1);
			#endif

			// Subtract to eliminate noise where distances are almost equal
			// TODO: ideally shouldn't even write these pixels, just zero them
			//d = max(0, d - 2.0);
		}

		return d * _DownSample;
	}

	// NOTE: In GLES2.0 if you return float it gives an error "Type mismatch, cannot convert from 'float' to 'vec4'", so just return float4
	float4 fragResolveDistanceIOnOutMax(v2f i) : SV_Target
	{
		float2 pixelScale = _MainTex_TexelSize.zw;

		float2 p0 = i.uv * pixelScale;
		float2 p1 = tex2D(_MainTex, i.uv).xy * pixelScale;

		float dOut = 0.0;
		#if DIST_CIRCLE
		dOut = distance(p0, p1);
		#elif DIST_DIAMOND
		dOut = distanceDiamond(p0, p1);
		#elif DIST_SQUARE
		dOut = distanceSquare(p0, p1);
		#endif

		p1 = tex2D(_InsideTex, i.uv).xy * pixelScale;

		float dIn = 0.0;
		#if DIST_CIRCLE
		dIn = distance(p0, p1);
		#elif DIST_DIAMOND
		dIn = distanceDiamond(p0, p1);
		#elif DIST_SQUARE
		dIn = distanceSquare(p0, p1);
		#endif

		float d = max(dOut, dIn);

		return (d * _DownSample);
	}

	// NOTE: In GLES2.0 if you return float it gives an error "Type mismatch, cannot convert from 'float' to 'vec4'", so just return float4
	float4 fragResolveDistanceSDF(v2f i) : SV_Target
	{
		float2 pixelScale = _MainTex_TexelSize.zw;

		float2 p0 = i.uv * pixelScale;
		float2 p1 = tex2D(_MainTex, i.uv).xy * pixelScale;

		float dOut = 0.0;
		#if DIST_CIRCLE
		dOut = distance(p0, p1);
		#elif DIST_DIAMOND
		dOut = distanceDiamond(p0, p1);
		#elif DIST_SQUARE
		dOut = distanceSquare(p0, p1);
		#endif

		p1 = tex2D(_InsideTex, i.uv).xy * pixelScale;

		float dIn = 0.0;
		#if DIST_CIRCLE
		dIn = distance(p0, p1);
		#elif DIST_DIAMOND
		dIn = distanceDiamond(p0, p1);
		#elif DIST_SQUARE
		dIn = distanceSquare(p0, p1);
		#endif

		float d = dOut - dIn;

		return (d * _DownSample);
	}

	ENDCG

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			Name "AlphaToUV"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragAlphaToUV
			ENDCG
		}
		Pass
		{
			Name "InvAlphaToUV"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragInvAlphaToUV
			ENDCG
		}
		Pass
		{
			Name "JumpFlood"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragJumpFlood
			#pragma multi_compile_local_fragment DIST_SQUARE DIST_DIAMOND DIST_CIRCLE
			ENDCG
		}
		Pass
		{
			Name "JumpFloodSingleAxis"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragJumpFloodSingleAxis
			#pragma multi_compile_local_fragment DIST_SQUARE DIST_DIAMOND DIST_CIRCLE
			ENDCG
		}
		Pass
		{
			Name "ResolveDistance"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragResolveDistance
			#pragma multi_compile_local_fragment DIST_SQUARE DIST_DIAMOND DIST_CIRCLE
			ENDCG
		}
		Pass
		{
			Name "ResolveDistanceInOutMax"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragResolveDistanceIOnOutMax
			#pragma multi_compile_local_fragment DIST_SQUARE DIST_DIAMOND DIST_CIRCLE
			ENDCG
		}
		Pass
		{
			Name "ResolveDistanceSDF"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragResolveDistanceSDF
			#pragma multi_compile_local_fragment DIST_SQUARE DIST_DIAMOND DIST_CIRCLE
			ENDCG
		}
	}
}