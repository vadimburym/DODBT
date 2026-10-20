#ifndef VAT_DOTS_COMMON_INCLUDED
#define VAT_DOTS_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_VATPositionTex);
SAMPLER(sampler_VATPositionTex);
TEXTURE2D(_VATNormalTex);
SAMPLER(sampler_VATNormalTex);
TEXTURE2D(_VATMetaTex);
SAMPLER(sampler_VATMetaTex);

struct VATAnimStateData
{
    float animIndex;
    float animTime;
    float prevAnimIndex;
    float prevAnimTime;
    float blend;
    float pad0;
    float pad1;
    float pad2;
};

#if defined(DODBT_SAMPLE_ANIMATION)
StructuredBuffer<VATAnimStateData> _DODBTExampleVATAnimStateBuffer;
#endif

struct VATClipMeta
{
    uint rowStart;
    uint frameCount;
    float clipLength;
    uint meshIndex;
};

float2 VATPixelCenter(uint x, uint y, float4 texInfo)
{
    return float2((x + 0.5) * texInfo.x, (y + 0.5) * texInfo.y);
}

VATAnimStateData VATLoadAnimState()
{
    #if defined(DODBT_SAMPLE_ANIMATION)
        return _DODBTExampleVATAnimStateBuffer[unity_RendererUserValue];
    #else
        // Material previews do not need a global GPU buffer or a running battle.
        VATAnimStateData state = (VATAnimStateData)0;
        state.prevAnimIndex = -1;
        state.blend = 1;
        return state;
    #endif
}

VATClipMeta VATLoadClipMeta(float clipIndex, float4 metaTexInfo)
{
    VATClipMeta meta;
    float safeIndex = max(clipIndex, 0.0);
    float2 uv = float2((safeIndex + 0.5) * metaTexInfo.x, 0.5 * metaTexInfo.y);
    float4 data = SAMPLE_TEXTURE2D_LOD(_VATMetaTex, sampler_VATMetaTex, uv, 0);

    meta.rowStart = (uint)max(data.x, 0.0);
    meta.frameCount = max((uint)max(data.y, 1.0), 1u);
    meta.clipLength = max(data.z, 1e-5);
    meta.meshIndex = (uint)max(data.w, 0.0);
    return meta;
}

float3 VATDecodePosition(float4 encoded, float4 boundsMin, float4 boundsExtent)
{
    return boundsMin.xyz + encoded.xyz * boundsExtent.xyz;
}

float3 VATDecodeNormal(float4 encoded)
{
    return normalize(encoded.xyz * 2.0 - 1.0);
}

void VATGetRows(VATClipMeta meta, float time01, out uint rowA, out uint rowB, out float frameT)
{
    float wrapped = frac(max(time01, 0.0));
    float frame = wrapped * meta.frameCount;
    uint frameIndex = (uint)floor(frame);
    frameT = frac(frame);

    rowA = meta.rowStart + (frameIndex % meta.frameCount);
    rowB = meta.rowStart + ((frameIndex + 1u) % meta.frameCount);
}

void VATSampleFrame(uint vertexID, uint row, float4 posTexInfo, float4 boundsMin, float4 boundsExtent, out float3 positionOS, out float3 normalOS)
{
    float2 uv = VATPixelCenter(vertexID, row, posTexInfo);
    float4 encodedPosition = SAMPLE_TEXTURE2D_LOD(_VATPositionTex, sampler_VATPositionTex, uv, 0);
    float4 encodedNormal = SAMPLE_TEXTURE2D_LOD(_VATNormalTex, sampler_VATNormalTex, uv, 0);

    positionOS = VATDecodePosition(encodedPosition, boundsMin, boundsExtent);
    normalOS = VATDecodeNormal(encodedNormal);
}

void VATSampleClip(uint vertexID, float clipIndex, float time01, float4 posTexInfo, float4 metaTexInfo, float4 boundsMin, float4 boundsExtent, out float3 positionOS, out float3 normalOS)
{
    VATClipMeta meta = VATLoadClipMeta(clipIndex, metaTexInfo);

    uint rowA;
    uint rowB;
    float frameT;
    VATGetRows(meta, time01, rowA, rowB, frameT);

    float3 posA;
    float3 posB;
    float3 nrmA;
    float3 nrmB;
    VATSampleFrame(vertexID, rowA, posTexInfo, boundsMin, boundsExtent, posA, nrmA);
    VATSampleFrame(vertexID, rowB, posTexInfo, boundsMin, boundsExtent, posB, nrmB);

    positionOS = lerp(posA, posB, frameT);
    normalOS = normalize(lerp(nrmA, nrmB, frameT));
}

float3 VATFallbackPerpendicular(float3 normalOS)
{
    float3 axis = abs(normalOS.y) < 0.99 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
    return normalize(cross(axis, normalOS));
}

float4 VATRebuildTangent(float4 tangentOS, float3 normalOS)
{
    float3 tangent = tangentOS.xyz - normalOS * dot(normalOS, tangentOS.xyz);
    float tangentLenSq = dot(tangent, tangent);
    tangent = tangentLenSq > 1e-6 ? tangent * rsqrt(tangentLenSq) : VATFallbackPerpendicular(normalOS);
    return float4(tangent, tangentOS.w);
}

void VATSampleBlended(uint vertexID, float4 posTexInfo, float4 metaTexInfo, float4 boundsMin, float4 boundsExtent, out float3 positionOS, out float3 normalOS)
{
    VATAnimStateData state = VATLoadAnimState();

    float3 currentPos;
    float3 currentNrm;
    VATSampleClip(vertexID, state.animIndex, state.animTime, posTexInfo, metaTexInfo, boundsMin, boundsExtent, currentPos, currentNrm);

    if (state.prevAnimIndex >= 0.0 && state.blend < 0.9999)
    {
        float3 previousPos;
        float3 previousNrm;
        VATSampleClip(vertexID, state.prevAnimIndex, state.prevAnimTime, posTexInfo, metaTexInfo, boundsMin, boundsExtent, previousPos, previousNrm);
        positionOS = lerp(previousPos, currentPos, saturate(state.blend));
        normalOS = normalize(lerp(previousNrm, currentNrm, saturate(state.blend)));
    }
    else
    {
        positionOS = currentPos;
        normalOS = currentNrm;
    }
}

#endif
