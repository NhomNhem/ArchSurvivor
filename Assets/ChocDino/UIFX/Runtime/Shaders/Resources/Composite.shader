Shader "Hidden/ChocDino/UIFX/Composite"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	#include "UnityUI.cginc"
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
		// NOTE: Since the UI element was rendered to a black target, the RGB values are now effectively pre-multiplied by alpha
		float4 col = tex2D(_MainTex, i.uv);

		// NOTE: We have to sqrt the alpha channel to reverse the effects of alpha blending because alpha blending causes the written to a zero dst alpha channel to be squared.
		// Luckily this is only an issue before Unity 2020.1.0 where they changed UI rendering to use premultiplied alpha.
		// However still not sure how this would play with custom UI rendering if other blend modes are used.
		col.a = sqrt(col.a);

		return col;
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
		Blend One OneMinusSrcAlpha // Premultiplied transparency

		Pass
		{
			Name "Composite-FromAlphaBlended-ToPremultiplied"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
