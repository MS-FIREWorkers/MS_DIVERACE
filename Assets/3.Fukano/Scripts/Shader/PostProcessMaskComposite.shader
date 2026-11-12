// ==================================================
// File: PostProcessMaskComposite.shader
// Created: 2026/10/29
// Author:
// Summary: 除外マスクを利用して元画像とエフェクト結果を合成
// ==================================================

Shader "MS41/PostProcess/MaskComposite"
{
    Properties
    {
        _MaskThreshold("Mask Threshold", Range(0, 1)) = 0.5
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

        TEXTURE2D_X(_OriginalTexture);
        TEXTURE2D_X(_ExclusionMask);

        CBUFFER_START(UnityPerMaterial)
            float _MaskThreshold;
        CBUFFER_END

        // ==================================================
        // Fragment Shader
        // ==================================================

        half4 FragComposite(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;

            half4 effectColor = SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv
            );

            half4 originalColor = SAMPLE_TEXTURE2D_X(
                _OriginalTexture,
                sampler_LinearClamp,
                uv
            );

            half mask = SAMPLE_TEXTURE2D_X(
                _ExclusionMask,
                sampler_LinearClamp,
                uv
            ).r;

            mask = step(_MaskThreshold, mask);

            return lerp(effectColor, originalColor, mask);
        }

        ENDHLSL

        Pass
        {
            Name "MaskComposite"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragComposite

            ENDHLSL
        }
    }

    FallBack Off
}