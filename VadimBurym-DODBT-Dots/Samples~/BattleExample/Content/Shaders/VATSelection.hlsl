#ifndef VAT_SELECTION_INCLUDED
#define VAT_SELECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "VATCommon.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
// Keep the material layout identical to the visible VAT passes (SRP Batcher).
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float _Cutoff;
    float3 _VATPadding0;
    float4 _VATBoundsMin;
    float4 _VATBoundsExtent;
    float4 _VATPosTexInfo;
    float4 _VATMetaTexInfo;
CBUFFER_END

float4 _SelectionID;
int _ObjectId;
int _PassValue;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings SelectionVert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 positionOS, normalOS;
    VATSampleBlended(input.vertexID, _VATPosTexInfo, _VATMetaTexInfo,
        _VATBoundsMin, _VATBoundsExtent, positionOS, normalOS);
    output.positionCS = TransformObjectToHClip(positionOS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 SelectionFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    #if defined(_ALPHATEST_ON)
        half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
        clip(alpha - _Cutoff);
    #endif
    #if defined(SCENEPICKINGPASS)
        #if defined(UNITY_DOTS_INSTANCING_ENABLED)
            return unity_SelectionID;
        #else
            return _SelectionID;
        #endif
    #else
        return half4(_ObjectId, _PassValue, 1, 1);
    #endif
}
#endif
