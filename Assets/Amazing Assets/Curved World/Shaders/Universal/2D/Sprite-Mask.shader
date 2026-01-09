// Curved World <http://u3d.as/1W8h>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
Shader "Amazing Assets/Curved World/2D/Sprite-Mask"
{
    Properties
    {
        [HideInInspector][CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1", Vector) = (0, 0, 0, 0)

        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Cutoff ("Mask alpha cutoff", Range(0.0, 1.0)) = 0.0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend Off
        ColorMask 0

        Pass
        {
            Tags{ "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex MaskRenderingVertex
            #pragma fragment MaskRenderingFragment


#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#include "../../Core/CurvedWorldTransform.cginc"


            #include "SpriteMaskShared.hlsl"
            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "NormalsRendering" }
            HLSLPROGRAM
            #pragma vertex MaskRenderingVertex
            #pragma fragment MaskRenderingFragment


#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#include "../../Core/CurvedWorldTransform.cginc"
 

            #include "SpriteMaskShared.hlsl"
            ENDHLSL
        }
        Pass
        {
            Tags{ "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex MaskRenderingVertex
            #pragma fragment MaskRenderingFragment


#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
#define CURVEDWORLD_BEND_ID_1
#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
#include "../../Core/CurvedWorldTransform.cginc"


            #include "SpriteMaskShared.hlsl"
            ENDHLSL
        }

    }


    CustomEditor "AmazingAssets.CurvedWorld.Editor.DefaultShaderGUI"
}
