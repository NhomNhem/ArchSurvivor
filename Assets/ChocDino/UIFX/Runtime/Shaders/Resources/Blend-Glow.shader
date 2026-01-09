Shader "Hidden/ChocDino/UIFX/Blend-Glow"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _ResultTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _SourceTex ("Source Texture", 2D) = "white" {}
		[PerRendererData] _FalloffTex ("Falloff Texture", 2D) = "white" {}
		[PerRendererData] _GradientTex ("Gradient Texture", 2D) = "white" {}
		//[PerRendererData] _FillTex ("Fill Texture", 2D) = "white" {}

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_MaxDistance ("Max Distance", Float) = 128
		_FalloffParams ("Falloff Params", Vector) = (4, 2, 0, 2.2)
		_GlowColor ("Glow Color", Vector) = (1, 1, 1, 1)
		_GradientParams ("Gradient Params", Vector) = (1, 1, 1, 1)
		_SourceAlpha ("Source Alpha", Float) = 1
		_AdditiveFactor ("Additive Factor", Float) = 1
		_NoiseScale ("Noise Scale", Float) = 1
		_MaskSide ("Mask Side", Vector) = (1, 0, 0, 0)
		
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_ColorMask			("Color Mask", Float) = 15
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
	#pragma multi_compile_local_fragment _ USE_NOISE
	#pragma multi_compile_local_fragment _ USE_GRADIENT_TEXTURE
	#pragma multi_compile_local_fragment _ USE_CURVE_FALLOFF
		
	#include "BlendUtils.cginc"
	#include "CompUtils.cginc"
	#include "ColorUtils.cginc"

	uniform float _MaxDistance;
	uniform float4 _FalloffParams; // Energy, Falloff, Offset, Gamma
	uniform sampler2D _FalloffTex;
	uniform float4 _GlowColor;
	uniform Texture2D _GradientTex;
	SamplerState my_linear_clamp_sampler;
	uniform float4 _GradientParams; // Offset, Gamma, Reverse, 0.0
	uniform float _AdditiveFactor;
	uniform float _SourceAlpha;
	uniform float _NoiseScale;
	uniform float2 _MaskSide;

	float hash13(float3 p3)
	{
		p3 = frac(p3 * .1031);
		p3 += dot(p3, p3.yzx + 19.19);
		return frac((p3.x + p3.y) * p3.z);
	}

	float4 fragGlow(v2f i) : SV_Target
	{
		// Note: This is already pre-multiplied alpha
		float4 source = tex2D(_SourceTex, i.uv.xy);

		float distance = tex2D(_ResultTex, i.uv.xy).x;

		// Optionally limit the distance by making sure it falls off to zero by _MaxDistance
		float distanceFalloff = 1.0 - saturate(abs(distance) / _MaxDistance);

		#if USE_CURVE_FALLOFF
		float glowMask = pow(saturate(tex2D(_FalloffTex, float2(1.0-pow(distanceFalloff, _FalloffParams.w), 0.0)).r), 1.0);
		#else
		// Create exponential glow from distance (the 0.2 clamp prevents sparkles when using offset)
		float glowMask = pow(_FalloffParams.x/(max(0.2, abs(distance - _FalloffParams.z))), _FalloffParams.y);

		#ifdef UNITY_COLORSPACE_GAMMA
		// TODO: add better support here for gamma mode?
		//glowMask = pow(glowMask, 1.0/2.2);
		#endif
		
		// Optionally limit the distance by making sure it falls off to zero by _MaxDistance
		glowMask *= distanceFalloff;
		#endif

		// Add noise for dithering
		#if USE_NOISE
		float noise = (hash13(float3(i.uv.xy * 2566.0, _Time.x)) - 0.5) * max(0, glowMask) * 0.1;
		glowMask += noise * _NoiseScale;
		#endif

		// Get the glow color
		#if USE_GRADIENT_TEXTURE

		// Gradient coordinate
		float gradt = distanceFalloff;
		gradt = pow(gradt, _GradientParams.y);
		gradt -= _GradientParams.x;
		gradt = saturate(gradt);
		// Reverse the fill or not
		gradt = 1.0 - abs(_GradientParams.z - gradt);

		float4 glowColor = _GradientTex.Sample(my_linear_clamp_sampler, float2(gradt, 0.0));
		glowColor = ToPremultiplied(glowColor);
		#else
		float4 glowColor = _GlowColor;
		#endif

		// Modulate color by mask
		glowColor *= glowMask;

		// Exponential tonemap to correct overflows
		glowColor = 1.0 - exp(-glowColor);

		// Used to mask the glow based on whether glow is inside/outside/both
		float sourceMask = _MaskSide.x + (source.a * _MaskSide.y);

		glowColor *= sourceMask;

		// Optionally fade out the source graphic
		// Note: Need to do this explicit swizzling for console compilers
		float zero = 0.0;
		float4 color = lerp(zero.xxxx, source, _SourceAlpha.xxxx);

		// NOTE: We only need to use the source alpha and not consider the glow alpha,
		// because due to premultiplied alpha, the glow over alpha == 0 areas will be additively
		// blended, which is what we want.
		float4 additive = float4(saturate(color + glowColor).rgb, color.a);

		// Switch between alpha blended output or additive output
		// Additive is what we usually want, but sometimes alpha blend
		// can look better artistically.
		float4 alphaBlended = AlphaComp_Over(glowColor, color);
		color = lerp(alphaBlended, additive, _AdditiveFactor);

		// 2D rect clipping
		#ifdef UNITY_UI_CLIP_RECT
		color = ApplyClipRect(color, i.mask);
		#endif

		// Alpha clipping
		#ifdef UNITY_UI_ALPHACLIP
		clip (color.a - 0.001);
		#endif

		color.rgb *= i.color.a;
		color *= i.color;

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
			"OutputsPremultipliedAlpha"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend One OneMinusSrcAlpha // Premultiplied transparency
		ColorMask [_ColorMask]

		Pass
		{
			Name "Blend-Glow"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragGlow
			ENDCG
		}
	}
}
