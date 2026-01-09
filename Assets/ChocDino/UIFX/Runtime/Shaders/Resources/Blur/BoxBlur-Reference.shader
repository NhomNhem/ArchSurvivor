Shader "Hidden/ChocDino/UIFX/BoxBlur-Reference"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_KernelRadius ("Kernel Radius", Float) = 0
	}

	CGINCLUDE
	#include "UnityCG.cginc"

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

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _MainTex_TexelSize;
	float _KernelRadius;

	v2f vert (appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}

	float4 Blur1D(float2 uv, float2 texelSize)
	{
		float4 accum = tex2D(_MainTex, uv);

		float2 step = texelSize;
		int m = round(_KernelRadius);
		float2 offset = step;
		for (int i = 0; i < m; i++)
		{
			accum += tex2D(_MainTex, uv + offset);
			accum += tex2D(_MainTex, uv - offset);
			offset += step;
		}

		return accum / (m + m + 1.0);
	}

	float4 Blur1DFrac(float2 uv, float2 texelSize)
	{
		float4 accum = tex2D(_MainTex, uv);

		float2 step = texelSize;
		int m = floor(_KernelRadius);
		float2 offset = step;
		for (int i = 0; i < m; i++)
		{
			accum += tex2D(_MainTex, uv + offset);
			accum += tex2D(_MainTex, uv - offset);
			offset += step;
		}

		float remainder = _KernelRadius - m;
		{
			float2 correction = step * remainder;
			accum += tex2D(_MainTex, uv + (step * m + correction)) * remainder;
			accum += tex2D(_MainTex, uv - (step * m + correction)) * remainder;
		}

		return accum / (_KernelRadius + _KernelRadius + 1.0);
	}

	float4 fragH(v2f i) : SV_Target
	{
		return Blur1D(i.uv, float2(_MainTex_TexelSize.x, 0.0));
	}

	float4 fragV(v2f i) : SV_Target
	{
		return Blur1D(i.uv, float2(0.0, _MainTex_TexelSize.y));
	}

	float4 fragHFrac(v2f i) : SV_Target
	{
		return Blur1DFrac(i.uv, float2(_MainTex_TexelSize.x, 0.0));
	}

	float4 fragVFrac(v2f i) : SV_Target
	{
		return Blur1DFrac(i.uv, float2(0.0, _MainTex_TexelSize.y));
	}

	float4 Blur2D(float2 uv, float2 texelSize)
	{
		float4 accum = 0.0;
		float weights = 0.0;
		int m = round(_KernelRadius);
		for (int i = -m; i <= m; i++)
		{
			for (int j = -m; j <= m; j++)
			{
				accum += tex2D(_MainTex, uv + texelSize * float2(i, j));
			}
		}

		weights = (m*2+1) * (m*2+1);

		return accum / weights;
	}

	float4 Blur2DFrac(float2 uv, float2 texelSize)
	{
		float4 accum = 0.0;
		float weights = 0.0;
		int m = ceil(_KernelRadius);
		for (int i = -m; i <= m; i++)
		{
			for (int j = -m; j <= m; j++)
			{
				// Calculate weight for fractional edges of kernel
				float weightx = 1.0 - max(0.0, abs(i) - _KernelRadius);
				float weighty = 1.0 - max(0.0, abs(j) - _KernelRadius);
				float weight = min(weightx, weighty);
				weights += weight;

				accum += tex2D(_MainTex, uv + texelSize * float2(i, j)) * weight;
			}
		}

		return accum / weights;
	}

	float4 fragBox(v2f i) : SV_Target
	{
		return Blur2D(i.uv, _MainTex_TexelSize);
	}

	float4 fragBoxFrac(v2f i) : SV_Target
	{
		return Blur2DFrac(i.uv, _MainTex_TexelSize);
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
			"OutputsPremultipliedAlpha"="True"
		}

		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			Name "BlurHorizontal"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragH
			ENDCG
		}

		Pass
		{
			Name "BlurVertical"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragV
			ENDCG
		}

		Pass
		{
			Name "BlurHorizontal-Frac"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragHFrac
			ENDCG
		}

		Pass
		{
			Name "BlurVertical-Frac"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragVFrac
			ENDCG
		}

		Pass
		{
			Name "BlurBox"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragBox
			ENDCG
		}

		Pass
		{
			Name "BlurBox-Frac"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragBoxFrac
			ENDCG
		}
	}
}