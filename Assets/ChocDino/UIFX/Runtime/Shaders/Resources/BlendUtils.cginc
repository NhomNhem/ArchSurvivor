#include "UnityCG.cginc"
#include "UnityUI.cginc"

// Actually 2019.4.18 but UNITY_VERSION doesn't support double digits
#if UNITY_VERSION < 201949
	#define MASK_SOFTNESS_OLD
#endif

struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	float4 color : COLOR;

	UNITY_VERTEX_INPUT_INSTANCE_ID // VR SUPPORT
};

struct v2f
{
	float4 vertex : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 color : TEXCOORD1;
	#ifdef UNITY_UI_CLIP_RECT
	float4 mask : TEXCOORD2;
	#endif

	UNITY_VERTEX_OUTPUT_STEREO // VR SUPPORT
};

sampler2D _MainTex;
sampler2D _ResultTex;
sampler2D _SourceTex;
float4 _MainTex_ST;
float4 _ResultTex_ST;
float4 _ResultTex_TexelSize;

#ifdef UNITY_UI_CLIP_RECT
float4 _ClipRect;
#ifdef MASK_SOFTNESS_OLD
float _MaskSoftnessX;
float _MaskSoftnessY;
#else
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
#endif
#endif

v2f vert (appdata v)
{
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v); // VR SUPPORT
	UNITY_INITIALIZE_OUTPUT(v2f, o); // VR SUPPORT
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); // VR SUPPORT

	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.uv, _ResultTex);
	o.color = v.color;

	// 2D rect clipping
	#ifdef UNITY_UI_CLIP_RECT
	{
		float2 pixelSize = o.vertex.w;
		pixelSize /= float2(1.0, 1.0) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

		float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
		#ifdef MASK_SOFTNESS_OLD
		half2 maskSoftness = half2(_MaskSoftnessX, _MaskSoftnessY);
		#else
		half2 maskSoftness = half2(_UIMaskSoftnessX, _UIMaskSoftnessY);
		#endif
		o.mask = float4(v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * maskSoftness + abs(pixelSize.xy)));
	}
	#endif
	
	return o;
}

float4 ApplyClipRect(float4 color, float4 mask)
{
	#ifdef UNITY_UI_CLIP_RECT
	half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(mask.xy)) * mask.zw);
	color.a *= m.x * m.y;
	color.rgb *= m.x * m.y;
	#endif
	return color;
}