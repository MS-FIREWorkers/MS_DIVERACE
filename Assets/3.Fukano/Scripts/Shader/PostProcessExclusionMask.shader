// ==================================================
// File: PostProcessExclusionMask.shader
// Created: 2026/10/29
// Author:
// Summary: ポストプロセス除外対象を白色で描画する
// ==================================================

Shader "MS41/PostProcess/ExclusionMask"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ExclusionMask"

            ZTest LEqual
            ZWrite Off
            Cull Back

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ==================================================
            // Structures
            // ==================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            // ==================================================
            // Vertex Shader
            // ==================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz);

                return output;
            }

            // ==================================================
            // Fragment Shader
            // ==================================================

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(1.0, 1.0, 1.0, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}