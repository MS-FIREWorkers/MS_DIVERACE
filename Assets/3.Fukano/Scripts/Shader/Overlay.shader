
Shader "MS41/PostProcess/Overlay"
{
    Properties
    {
        _OverlayColor("Overlay Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Range(0, 1)) = 0.5
        _BlendMode("Blend Mode", Float) = 0
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
            Name "Overlay"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            // ==================================================
            // Includes
            // ==================================================

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // ==================================================
            // Variables
            // ==================================================

            CBUFFER_START(UnityPerMaterial)
                float4 _OverlayColor;
                float _Intensity;
                float _BlendMode;
            CBUFFER_END

            // ==================================================
            // Fragment Shader
            // ==================================================

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 sourceColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                half3 result = sourceColor.rgb;
                half3 overlay = _OverlayColor.rgb;

                // Normal
                if (_BlendMode < 0.5)
                {
                    result = lerp(
                        sourceColor.rgb,
                        overlay,
                        _Intensity
                    );
                }
                // Additive
                else if (_BlendMode < 1.5)
                {
                    result = sourceColor.rgb +
                        overlay * _Intensity;
                }
                // Multiply
                else
                {
                    result = lerp(
                        sourceColor.rgb,
                        sourceColor.rgb * overlay,
                        _Intensity
                    );
                }

                return half4(result, sourceColor.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
