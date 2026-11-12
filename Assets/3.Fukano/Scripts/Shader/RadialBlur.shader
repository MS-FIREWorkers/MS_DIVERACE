// ==================================================
// File: RadialBlur.shader
// Created: 2026/10/28
// Author: MS41
// Summary: 中心座標から放射方向に適用するブラー
// ==================================================

Shader "MS41/PostProcess/RadialBlur"
{
    Properties
    {
        _BlurIntensity("Blur Intensity", Range(0, 1)) = 0.3
        _BlurCenter("Blur Center", Vector) = (0.5, 0.5, 0, 0)
        _InnerRadius("Inner Radius", Range(0, 1)) = 0.1
        _OuterRadius("Outer Radius", Range(0, 2)) = 1.0
        _SampleCount("Sample Count", Range(4, 32)) = 12
        _Falloff("Falloff", Range(0.001, 0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZTest Always
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _BlurIntensity;
            float4 _BlurCenter;
            float _InnerRadius;
            float _OuterRadius;
            int _SampleCount;
            float _Falloff;
        CBUFFER_END

        // ==================================================
        // Custom Functions
        // ==================================================

        // ----- Radial Blur -----

        half4 ApplyRadialBlur(float2 uv)
        {
            float2 center = _BlurCenter.xy;

            // 中心からの方向
            float2 direction = uv - center;

            // アスペクト比を考慮した距離
            float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
            float2 aspectDirection = direction * float2(aspect, 1.0);
            float distanceToCenter = length(aspectDirection);

            // 中央から外側へ強度を増加
            float innerFade = smoothstep(
                _InnerRadius,
                _InnerRadius + _Falloff,
                distanceToCenter);

            // 外側の適用終了範囲
            float outerFade = 1.0 - smoothstep(
                max(_InnerRadius, _OuterRadius - _Falloff),
                _OuterRadius,
                distanceToCenter);

            float mask = saturate(innerFade * outerFade);
            float blurAmount = _BlurIntensity * mask;

            // 強度0の場合は元画像を返す
            if (blurAmount <= 0.0001)
            {
                return SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv);
            }

            half4 accumulatedColor = 0;
            int sampleCount = clamp(_SampleCount, 4, 32);

            // 中心方向にサンプリング
            [loop]
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                float2 sampleUV = uv - direction * t * blurAmount;
                sampleUV = saturate(sampleUV);

                half4 sampleColor =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        sampleUV);

                accumulatedColor += sampleColor;
            }

            return accumulatedColor / sampleCount;
        }

        // ----- Fragment Shader -----

        half4 FragRadialBlur(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;

            return ApplyRadialBlur(uv);
        }

        ENDHLSL

        // ==================================================
        // Pass
        // ==================================================

        Pass
        {
            Name "RadialBlur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragRadialBlur

            ENDHLSL
        }
    }

    FallBack Off
}