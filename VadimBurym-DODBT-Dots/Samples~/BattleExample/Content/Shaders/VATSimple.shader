Shader "DODBT/Examples/Battle/VAT"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Clipping", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2

        [NoScaleOffset] _VATPositionTex("VAT Position", 2D) = "black" {}
        [NoScaleOffset] _VATNormalTex("VAT Normal", 2D) = "gray" {}
        [NoScaleOffset] _VATMetaTex("VAT Meta", 2D) = "black" {}
        _VATBoundsMin("VAT Bounds Min", Vector) = (0,0,0,0)
        _VATBoundsExtent("VAT Bounds Extent", Vector) = (1,1,1,0)
        _VATPosTexInfo("VAT Pos Tex Info", Vector) = (1,1,1,1)
        _VATMetaTexInfo("VAT Meta Tex Info", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_local _ DODBT_SAMPLE_ANIMATION
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "VATCommon.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 animatedPosOS;
                float3 animatedNormalOS;
                VATSampleBlended(input.vertexID, _VATPosTexInfo, _VATMetaTexInfo, _VATBoundsMin, _VATBoundsExtent, animatedPosOS, animatedNormalOS);

                VertexPositionInputs posInputs = GetVertexPositionInputs(animatedPosOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(animatedNormalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.shadowCoord = GetShadowCoord(posInputs);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half3 AccumulateAdditionalLights(InputData inputData, half3 albedo)
            {
                half3 lighting = 0;

                #if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint lightIndex = 0u; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    half atten = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                    lighting += LightingLambert(additionalLight.color * atten, additionalLight.direction, inputData.normalWS) * albedo;
                }
                #endif

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    half atten = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                    lighting += LightingLambert(additionalLight.color * atten, additionalLight.direction, inputData.normalWS) * albedo;
                LIGHT_LOOP_END

                return lighting;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(albedoSample.a - _Cutoff);
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.shadowMask = half4(1,1,1,1);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0,0,0);

                half3 color = inputData.bakedGI * albedoSample.rgb;

                Light mainLight = GetMainLight(inputData.shadowCoord);
                half mainAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                color += LightingLambert(mainLight.color * mainAtten, mainLight.direction, inputData.normalWS) * albedoSample.rgb;

                #if defined(_ADDITIONAL_LIGHTS)
                    color += AccumulateAdditionalLights(inputData, albedoSample.rgb);
                #endif

                color = MixFog(color, inputData.fogCoord);
                return half4(color, albedoSample.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_local _ DODBT_SAMPLE_ANIMATION
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "VATCommon.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 animatedPosOS;
                float3 animatedNormalOS;
                VATSampleBlended(input.vertexID, _VATPosTexInfo, _VATMetaTexInfo, _VATBoundsMin, _VATBoundsExtent, animatedPosOS, animatedNormalOS);
                output.positionCS = TransformObjectToHClip(animatedPosOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_local _ DODBT_SAMPLE_ANIMATION
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "VATCommon.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 animatedPosOS;
                float3 animatedNormalOS;
                VATSampleBlended(input.vertexID, _VATPosTexInfo, _VATMetaTexInfo, _VATBoundsMin, _VATBoundsExtent, animatedPosOS, animatedNormalOS);
                output.positionCS = TransformObjectToHClip(animatedPosOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_local _ DODBT_SAMPLE_ANIMATION
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "VATCommon.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 animatedPosOS;
                float3 animatedNormalOS;
                VATSampleBlended(input.vertexID, _VATPosTexInfo, _VATMetaTexInfo, _VATBoundsMin, _VATBoundsExtent, animatedPosOS, animatedNormalOS);

                output.positionCS = TransformObjectToHClip(animatedPosOS);
                output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(animatedNormalOS));
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                half3 packedNormal = input.normalWS * 0.5h + 0.5h;
                return half4(packedNormal, 1.0h);
            }
            ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_local _ DODBT_SAMPLE_ANIMATION
            #pragma editor_sync_compilation
            #pragma vertex SelectionVert
            #pragma fragment SelectionFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #define SCENESELECTIONPASS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "VATSelection.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_local _ DODBT_SAMPLE_ANIMATION
            #pragma editor_sync_compilation
            #pragma vertex SelectionVert
            #pragma fragment SelectionFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #define SCENEPICKINGPASS
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "VATSelection.hlsl"
            ENDHLSL
        }
    }
}
