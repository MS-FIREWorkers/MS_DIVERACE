// ==================================================
// File: GaussianBlur.shader
// Created: 2026/10/21
// Author: MS41
// Summary: 横方向・縦方向のガウシアンブラー
// ==================================================

Shader "MS41/PostProcess/GaussianBlur"
{
    Properties
    {
        _BlurRadius("Blur Radius", Range(0, 10)) = 2
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
            float _BlurRadius;
        CBUFFER_END

        // ==================================================
        // Gaussian Blur
        // ==================================================

        half4 GaussianBlur(float2 uv, float2 direction)
        {
            float2 texelSize = _BlitTexture_TexelSize.xy;

            float2 offset = texelSize * direction * _BlurRadius;

            // 5-tap Gaussian weights
            const half w0 = 0.4026h;
            const half w1 = 0.2442h;
            const half w2 = 0.0545h;

            half4 color =
                SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                ) * w0;

            color += SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv + offset
            ) * w1;

            color += SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv - offset
            ) * w1;

            color += SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv + offset * 2.0
            ) * w2;

            color += SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv - offset * 2.0
            ) * w2;

            return color;
        }

        // ==================================================
        // Horizontal Fragment
        // ==================================================

        half4 FragHorizontal(Varyings input) : SV_Target
        {
            return GaussianBlur(
                input.texcoord,
                float2(1.0, 0.0)
            );
        }

        // ==================================================
        // Vertical Fragment
        // ==================================================

        half4 FragVertical(Varyings input) : SV_Target
        {
            return GaussianBlur(
                input.texcoord,
                float2(0.0, 1.0)
            );
        }

        ENDHLSL

        // ==================================================
        // Pass 0 : Horizontal
        // ==================================================

        Pass
        {
            Name "HorizontalBlur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragHorizontal

            ENDHLSL
        }

        // ==================================================
        // Pass 1 : Vertical
        // ==================================================

        Pass
        {
            Name "VerticalBlur"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragVertical

            ENDHLSL
        }
    }
}