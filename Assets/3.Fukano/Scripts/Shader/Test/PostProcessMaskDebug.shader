// ==================================================
// File: PostProcessMaskDebug.shader
// Created: 2026/10/29
// Author:
// Summary: 除外マスクのデバッグ表示
// ==================================================

Shader "MS41/PostProcess/MaskDebug"
{
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

        // ==================================================
        // Fragment Shader
        // ==================================================

        half4 FragMaskDebug(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;

            half mask = SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv
            ).r;

            return half4(mask, mask, mask, 1.0);
        }

        ENDHLSL

        Pass
        {
            Name "MaskDebug"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragMaskDebug

            ENDHLSL
        }
    }

    FallBack Off
}