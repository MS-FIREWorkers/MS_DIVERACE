
Shader "MS_DIVERACE/BoostRing"
{
    Properties
    {
        _RingColor ("Ring Color", Color) = (0.7, 0.9, 1, 1)
        _Brightness ("Brightness", Range(0, 8)) = 2
        _Thickness ("Ring Thickness", Range(0.02, 0.4)) = 0.15
        _Softness ("Edge Softness", Range(0.005, 0.1)) = 0.035
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "BoostRing"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _RingColor;
                float _Brightness;
                float _Thickness;
                float _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            // ==================================================
            // ----- Vertex Shader -----
            // ==================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            // ==================================================
            // ----- Fragment Shader -----
            // ==================================================

            half4 Frag(Varyings input) : SV_Target
            {
                // UV座標から円の中心までの距離を計算する。
                float2 center = input.uv - float2(0.5, 0.5);
                float distanceToCenter = length(center) * 2.0;

                // 円の外周と内周からリング形状を生成する。
                float innerRadius = 1.0 - _Thickness;

                float innerAlpha = smoothstep(
                    innerRadius - _Softness,
                    innerRadius + _Softness,
                    distanceToCenter
                );

                float outerAlpha = 1.0 - smoothstep(
                    1.0 - _Softness,
                    1.0,
                    distanceToCenter
                );

                float alpha = saturate(innerAlpha * outerAlpha);

                // パーティクルの透明度を反映する。
                alpha *= _RingColor.a * input.color.a;

                // 発光色を計算する。
                float3 color = _RingColor.rgb *
                               input.color.rgb *
                               _Brightness;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
