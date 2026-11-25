/*
    BoostRing_Laser.shader
    20261125  Teki
    加速リング本体のレーザー（加算合成）
    ・帯状の円環メッシュ（UV.x = 円周 0〜1 / UV.y = 内側 0 → 外側 1）に貼る
    ・帯の中央に白く強い芯、外側へ色付きのグローが減衰する
    ・円周方向にエネルギーのうねりが流れる
    ・_Intensity / _Flash はスクリプト（BoostRing_Visual）から制御
*/

Shader "Custom/BoostRing_Laser"
{
    Properties
    {
        [Header(Color)]
        [HDR] _CoreColor ("Core Color", Color) = (4.0, 5.0, 6.0, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.3, 1.2, 3.0, 1)

        [Header(Shape)]
        _CoreWidth ("Core Width", Range(0.01, 1.0)) = 0.24
        _GlowFalloff ("Glow Falloff", Range(0.5, 20.0)) = 5.0

        [Header(Flow)]
        _FlowCount ("Flow Count (Integer)", Float) = 6
        _FlowSpeed ("Flow Speed", Float) = 3.0
        _FlowAmount ("Flow Amount", Range(0.0, 1.0)) = 0.35
        _ShimmerSpeed ("Shimmer Speed", Float) = 20.0
        _ShimmerAmount ("Shimmer Amount", Range(0.0, 1.0)) = 0.1

        [Header(Script Control)]
        _Intensity ("Intensity", Float) = 1.0
        _Flash ("Flash", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                float _CoreWidth;
                float _GlowFalloff;
                float _FlowCount;
                float _FlowSpeed;
                float _FlowAmount;
                float _ShimmerSpeed;
                float _ShimmerAmount;
                float _Intensity;
                float _Flash;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 帯の中心 0 / 端 1
                float d = abs(input.uv.y * 2.0 - 1.0);

                // 白い芯
                float core = 1.0 - smoothstep(_CoreWidth * 0.5, _CoreWidth, d);

                // 外へ減衰するグロー（端で 0 になるようにフェード）
                float glow = exp(-d * _GlowFalloff) * (1.0 - smoothstep(0.85, 1.0, d));

                // 円周方向のうねり（整数回なので継ぎ目が出ない）
                float angle = input.uv.x * 6.2832;
                float flow = 1.0 + _FlowAmount * sin(angle * round(_FlowCount) - _Time.y * _FlowSpeed);
                float shimmer = 1.0 + _ShimmerAmount
                              * sin(angle * 53.0 + _Time.y * _ShimmerSpeed)
                              * sin(angle * 17.0 - _Time.y * _ShimmerSpeed * 0.7);

                float power = _Intensity + _Flash * 2.0;
                half3 color = (_CoreColor.rgb * core + _GlowColor.rgb * glow) * flow * shimmer * power;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
