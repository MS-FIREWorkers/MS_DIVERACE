// ==================================================
// File: LensDistortion.shader
// Created: 2026/10/22
// Author: MS41
// Summary: 範囲・強度・画面端保護に対応したレンズ歪み
// ==================================================

Shader "MS41/PostProcess/LensDistortion"
{
    Properties
    {
        _DistortionIntensity("Intensity", Range(-1, 1)) = 0
        _DistortionCenter("Center", Vector) = (0.5, 0.5, 0, 0)
        _DistortionScale("Scale", Range(0.1, 2)) = 1

        _RangeMode("Range Mode", Float) = 0
        _RangeRadius("Range Radius", Range(0, 2)) = 0.5
        _InnerRadius("Inner Radius", Range(0, 2)) = 0.2
        _EdgeSoftness("Edge Softness", Range(0, 0.5)) = 0.05

        _DistortionPower("Distortion Power", Range(0.5, 6)) = 2
        _EdgeProtection("Edge Protection", Range(0, 0.5)) = 0.1
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

                float _DistortionPower;
                float _EdgeProtection;
            CBUFFER_END

            // ==================================================
            // Custom Functions
            // ==================================================

            // ----- Range Mask -----

            float CalculateRangeMask(float distanceFromCenter)
            {
                float softness = max(_EdgeSoftness, 0.0001);

                float outerMask = 1.0 - smoothstep(
                    _RangeRadius - softness,
                    _RangeRadius + softness,
                    distanceFromCenter
                );

                float innerMask = smoothstep(
                    _InnerRadius - softness,
                    _InnerRadius + softness,
                    distanceFromCenter
                );

                if (_RangeMode < 0.5)
                    return 1.0;

                if (_RangeMode < 1.5)
                    return outerMask;

                if (_RangeMode < 2.5)
                    return 1.0 - outerMask;

                return innerMask * outerMask;
            }

            // ----- Edge Protection -----

            float CalculateEdgeMask(float2 uv)
            {
                if (_EdgeProtection <= 0.0001)
                    return 1.0;

                float2 edgeDistance = min(uv, 1.0 - uv);

                float nearestEdge = min(
                    edgeDistance.x,
                    edgeDistance.y
                );

                return smoothstep(
                    0.0,
                    _EdgeProtection,
                    nearestEdge
                );
            }

            // ==================================================
            // Fragment
            // ==================================================

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 center = _DistortionCenter.xy;

                // ----- Aspect Ratio -----

                float aspect = _BlitTexture_TexelSize.z /
                               max(_BlitTexture_TexelSize.w, 1.0);

                float2 delta = uv - center;
                delta.x *= aspect;

                float distanceFromCenter = length(delta);

                // ----- Distortion -----

                // 距離を正規化
                float maxRadius = length(
                    float2(aspect, 1.0)
                );

                float normalizedRadius = saturate(
                    distanceFromCenter / maxRadius
                );

                // 歪みの分布を調整
                float distortionFactor = pow(
                    normalizedRadius,
                    _DistortionPower
                );

                // レンズ歪み
                float distortion =
                    1.0 +
                    _DistortionIntensity *
                    distortionFactor;

                float2 distorted = delta * distortion;

                distorted.x /= max(aspect, 0.0001);

                float2 distortedUV =
                    center + distorted * _DistortionScale;

                // ----- Range Mask -----

                float rangeMask = CalculateRangeMask(
                    distanceFromCenter
                );

                // ----- Edge Protection -----

                float edgeMask = CalculateEdgeMask(uv);

                float finalMask = rangeMask * edgeMask;

                // ----- Final UV -----

                float2 finalUV = lerp(
                    uv,
                    distortedUV,
                    finalMask
                );

                // 画面外への参照を防止
                float2 texel = _BlitTexture_TexelSize.xy;

                finalUV = clamp(
                    finalUV,
                    texel * 0.5,
                    1.0 - texel * 0.5
                );

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