// ==================================================
// File Name   : ToonShader_7.shader
// Created     : 2026/10/14
// Author      : MS41
// Description : Unity 6 URP Forward+ 3段階トゥーンシェーダー。
//               法線、アルファクリップ、発光、影、アウトラインに対応。
// ==================================================
Shader "Custom/ToonShader_7"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Toon Colors)]
        _ShadowColor("Shadow Color", Color) = (0.3, 0.3, 0.4, 1)
        _MidColor("Mid Color", Color) = (0.7, 0.7, 0.7, 1)
        _LightColor("Light Color", Color) = (1, 1, 1, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.3
        _LightThreshold("Light Threshold", Range(0, 1)) = 0.6
        _SmoothWidth("Smooth Width", Range(0, 0.3)) = 0.05
        _ShadowStrength("Shadow Strength", Range(0, 2)) = 1
        _MidStrength("Mid Strength", Range(0, 2)) = 1
        _LightStrength("Light Strength", Range(0, 2)) = 1
        _LightColorStrength("Light Color Influence", Range(0, 2)) = 1
        _LuminanceStrength("Luminance Influence", Range(0, 2)) = 1

        [Header(Normal Map)]
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 1

        [Header(Alpha Clip)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Emission)]
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        _EmissionMap("Emission Map", 2D) = "white" {}
        _EmissionStrength("Emission Strength", Range(0, 5)) = 1

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        // ==================================================
        // 全Pass共通: SRP Batcher向けMaterial定数とTexture
        // ==================================================
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _ShadowColor;
            float4 _MidColor;
            float4 _LightColor;
            float _ShadowThreshold;
            float _LightThreshold;
            float _SmoothWidth;
            float _ShadowStrength;
            float _MidStrength;
            float _LightStrength;
            float _LightColorStrength;
            float _LuminanceStrength;
            float _BumpScale;
            float _AlphaClip;
            float _Cutoff;
            float4 _EmissionColor;
            float _EmissionStrength;
            float4 _OutlineColor;
            float _OutlineWidth;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_EmissionMap);
        SAMPLER(sampler_EmissionMap);

        // 3つの描画Passで同じUV変換とAlpha Clip判定を使う。
        float2 GetToonUV(float2 uv)
        {
            return TRANSFORM_TEX(uv, _BaseMap);
        }

        void ApplyToonAlphaClip(float2 uv)
        {
            #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
            #endif
        }
        ENDHLSL

        // ==================================================
        // 通常描画: 3段階トゥーン + メインライト影
        // ==================================================
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VertToon
            #pragma fragment FragToon
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            Varyings VertToon(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w * GetOddNegativeScale();
                output.uv = GetToonUV(input.uv);
                return output;
            }

            half4 FragToon(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(baseColor.a - _Cutoff);
                #endif

                // Tangent Spaceの法線をWorld Spaceに変換する。
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3x3 tangentToWorld = float3x3(
                    normalize(input.tangentWS),
                    normalize(input.bitangentWS),
                    normalize(input.normalWS));

                float3 normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float mainNdotL = saturate(dot(normalWS, mainLight.direction));
                float mainLuminance = dot(mainLight.color.rgb, float3(0.2126, 0.7152, 0.0722));
                float strongestLighting = mainNdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation * mainLuminance;
                float3 strongestLightColor = mainLight.color.rgb;

                // LIGHT_LOOP_BEGINが参照するInputDataを用意する。
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionHCS);

                // Forward+ではGetAdditionalLightsCount()が0でも
                // LIGHT_LOOP_BEGINがクラスター内ライトを列挙できる。
                uint pixelLightCount = GetAdditionalLightsCount();

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                    float ndotL = saturate(dot(normalWS, additionalLight.direction));
                    float luminance = dot(additionalLight.color.rgb, float3(0.2126, 0.7152, 0.0722));
                    float amount = ndotL * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation * luminance;
                    if (amount > strongestLighting)
                    {
                        strongestLighting = amount;
                        strongestLightColor = additionalLight.color.rgb;
                    }
                LIGHT_LOOP_END

                float lighting = saturate(strongestLighting * _LuminanceStrength);
                float3 lightTint = lerp(float3(1, 1, 1), strongestLightColor, _LightColorStrength);

                half3 shadowColor = baseColor.rgb * _ShadowColor.rgb * lightTint * _ShadowStrength;
                half3 midColor = baseColor.rgb * _MidColor.rgb * lightTint * _MidStrength;
                half3 lightColor = baseColor.rgb * _LightColor.rgb * lightTint * _LightStrength;

                float shadowToMid = smoothstep(
                    _ShadowThreshold - _SmoothWidth,
                    _ShadowThreshold + _SmoothWidth, lighting);

                float midToLight = smoothstep(
                    _LightThreshold - _SmoothWidth,
                    _LightThreshold + _SmoothWidth, lighting);

                half3 finalColor = shadowColor * (1 - shadowToMid)
                    + midColor * shadowToMid * (1 - midToLight)
                    + lightColor * midToLight;

                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                finalColor += emission * _EmissionColor.rgb * _EmissionStrength;

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // ==================================================
        // 輪郭線: OutlineRendererFeatureからToonOutlineを指定して描画
        // ==================================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "ToonOutline" }
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct OutlineVaryings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            OutlineVaryings VertOutline(OutlineAttributes input)
            {
                OutlineVaryings output;
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22));

                float maxScale = max(scale.x, max(scale.y, scale.z));
                positionWS += normalWS * _OutlineWidth * maxScale;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = GetToonUV(input.uv);
                return output;
            }

            half4 FragOutline(OutlineVaryings input) : SV_Target
            {
                clip(_OutlineWidth - 0.000001);
                ApplyToonAlphaClip(input.uv);
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ==================================================
        // Shadow Map描画: Alpha Clipにも対応
        // ==================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex VertShadow
            #pragma fragment FragShadow
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // URPがShadowCaster描画時に設定するライト方向/位置。
            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            ShadowVaryings VertShadow(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 lightDirectionWS = _LightDirection;

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    lightDirectionWS = normalize(_LightPosition - positionWS);
                #endif

                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                output.positionHCS = TransformWorldToHClip(biasedPositionWS);

                #if UNITY_REVERSED_Z
                    output.positionHCS.z = min(output.positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionHCS.z = max(output.positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.uv = GetToonUV(input.uv);
                return output;
            }

            half4 FragShadow(ShadowVaryings input) : SV_Target
            {
                ApplyToonAlphaClip(input.uv);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
