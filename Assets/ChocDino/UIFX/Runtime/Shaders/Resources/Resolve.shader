Shader "Hidden/ChocDino/UIFX/Resolve"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"
	#include "ColorUtils.cginc"

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

	v2f vert (appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		return o;
	}

	float4 frag(v2f i) : SV_Target
	{
		// Note: This is already pre-multiplied alpha
		float4 color = tex2D(_MainTex, i.uv);

		// Remove premultiplied-alpha
		color = ToStraight(color);

		// The conversion to sRGB space from linear will happen during the read to sRGB texture so no need to do it in the shader

		return color;
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
			"OutputsPremultipliedAlpha"="False"
		}

		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			Name "Normal"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}