// ==================================================
// File: LensDistortion.shader
// Created: 2026/10/22
// Author: MS41
// Summary: 適用範囲を制御できるレンズ歪み
// ==================================================

Shader "MS41/PostProcess/LensDistortion"
{
    Properties
    {
        _DistortionIntensity("Intensity", Range(-1, 1)) = 0
        _DistortionCenter("Center", Vector) = (0.5, 0.5, 0, 0)
        _DistortionScale("Scale", Range(0.1, 2)) = 1

        // ==================================================
        // Range Settings
        // ==================================================

        _RangeMode("Range Mode", Float) = 0
        _RangeRadius("Range Radius", Range(0, 2)) = 0.5
        _InnerRadius("Inner Radius", Range(0, 2)) = 0.2
        _EdgeSoftness("Edge Softness", Range(0, 0.5)) = 0.05
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

        Pass
        {
            Name "LensDistortion"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _DistortionIntensity;
                float4 _DistortionCenter;
                float _DistortionScale;

                float _RangeMode;
                float _RangeRadius;
                float _InnerRadius;
                float _EdgeSoftness;
            CBUFFER_END

            // ==================================================
            // Custom Functions
            // ==================================================

            // ----- Range Mask -----

            float CalculateRangeMask(float2 uv, float aspect)
            {
                float2 delta = uv - _DistortionCenter.xy;

                // 画面の縦横比を補正
                delta.x *= aspect;

                float distanceFromCenter = length(delta);

                float softness = max(_EdgeSoftness, 0.0001);

                // 外径の内側を1、外側を0とする
                float outerMask = 1.0 - smoothstep(
                    _RangeRadius - softness,
                    _RangeRadius + softness,
                    distanceFromCenter
                );

                // 内径の内側を0、外側を1とする
                float innerMask = smoothstep(
                    _InnerRadius - softness,
                    _InnerRadius + softness,
                    distanceFromCenter
                );

                // Full
                if (_RangeMode < 0.5)
                {
                    return 1.0;
                }

                // Center
                if (_RangeMode < 1.5)
                {
                    return outerMask;
                }

                // Outer
                if (_RangeMode < 2.5)
                {
                    return 1.0 - outerMask;
                }

                // Ring
                return innerMask * outerMask;
            }

            // ==================================================
            // Fragment
            // ==================================================

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 center = _DistortionCenter.xy;

                // 画面の縦横比を補正
                float aspect = _BlitTexture_TexelSize.z / max(_BlitTexture_TexelSize.w, 1.0);

                float2 delta = uv - center;
                delta.x *= aspect;

                // 中心からの距離
                float radiusSquared = dot(delta, delta);

                // レンズ歪み
                float distortion = 1.0 + _DistortionIntensity * radiusSquared;

                float2 distorted = delta * distortion;
                distorted.x /= max(aspect, 0.0001);

                float2 distortedUV = center + distorted * _DistortionScale;

                // ==================================================
                // Range Mask
                // ==================================================

                float rangeMask = CalculateRangeMask(uv, aspect);

                // 元のUVと歪んだUVを補間
                float2 finalUV = lerp(uv, distortedUV, rangeMask);

                // 画面外は黒く表示
                if (any(finalUV < 0.0) || any(finalUV > 1.0))
                {
                    return half4(0, 0, 0, 1);
                }

                return SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    finalUV
                );
            }

            ENDHLSL
        }
    }
}