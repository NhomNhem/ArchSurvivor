// Curved World <http://u3d.as/1W8h>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
#ifndef CURVEDWORLD_SCENE_SELECTION_CGINC
#define CURVEDWORLD_SCENE_SELECTION_CGINC

#if !defined (SELECTION_PASS_USES_VARIABLES_FROM_URP_CBUFFER)
#include "UnityCG.cginc"
#endif

#include "CurvedWorldTransform.cginc" 


//Variables/////////////////////////////////////////////////////////////
#if !defined (SELECTION_PASS_USES_VARIABLES_FROM_URP_CBUFFER)
half        _Cutoff;
sampler2D   _MainTex;
float4      _MainTex_ST;
#endif

float _ObjectId;
float _PassValue;
float4 _SelectionID;
//Structs///////////////////////////////////////////////////////////////
struct VertexInput
{
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 texcoords : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 texcoord : TEXCOORD0;
    float4 color : TEXCOORD2;
};

//Vertex////////////////////////////////////////////////////////////////
void vertEditorPass(VertexInput v, out VertexOutput o, out float4 opos : SV_POSITION)
{
	UNITY_SETUP_INSTANCE_ID(v);


    #if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
	    CURVEDWORLD_TRANSFORM_VERTEX(v.vertex);
    #endif

    #if defined(SELECTION_PASS_USES_VARIABLES_FROM_URP_CBUFFER)
        opos = TransformObjectToHClip(v.vertex.xyz);
    #else
        opos = UnityObjectToClipPos(v.vertex);
    #endif

    #if defined(SELECTION_PASS_USES_VARIABLES_FROM_URP_CBUFFER)
        o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _BaseMap);
    #else
        o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
    #endif
    o.color = v.color;
}

//Fragment//////////////////////////////////////////////////////////////
void fragSceneClip(VertexOutput i)
{
    #if defined(SELECTION_PASS_USES_VARIABLES_FROM_URP_CBUFFER)
        half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.texcoord).a;
    #else
        half alpha = tex2D(_MainTex, i.texcoord).a;
    #endif

    alpha *= i.color.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _Cutoff);
#elif defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
    clip(alpha - 0.2);
#elif defined(CUTOUT_0_3)
    clip(alpha-0.33);
#endif
}

half4 fragSceneHighlightPass(VertexOutput i) : SV_Target
{
    fragSceneClip(i);
    return float4(_ObjectId, _PassValue, 1, 1);
}

half4 fragScenePickingPass(VertexOutput i) : SV_Target
{
    fragSceneClip(i);
    return _SelectionID;
}

#endif
