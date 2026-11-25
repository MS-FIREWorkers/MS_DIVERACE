/*
    BoostTunnel_CoreGlow.shader
    20261124  Teki
    加速トンネル中心の光（加算合成）
    ・円盤メッシュ（UV 0〜1 を平面投影）に貼る
    ・中心ほど明るい径方向グラデーション
    ・同心円の波紋が外側 → 中心へ吸い込まれるように流れる
    ・_Intensity / _Flash はスクリプト（BoostTunnel_Visual）から制御
*/

Shader "Custom/BoostTunnel_CoreGlow"
{
    Properties
    {
        [Header(Color)]
        [HDR] _GlowColor ("Glow Color", Color) = (3.0, 1.3, 0.2, 1)
        _Opacity ("Opacity", Range(0.0, 2.0)) = 0.5

        [Header(Glow)]
        _GlowFalloff ("Glow Falloff", Range(0.5, 8.0)) = 2.5
        _EdgeFade ("Edge Fade", Range(0.01, 0.5)) = 0.15

        [Header(Ripple)]
        _RingCount ("Ring Count", Float) = 4.0
        _RingSpeed ("Ring Speed", Float) = 0.8
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.12
        _RingStrength ("Ring Strength", Range(0.0, 2.0)) = 0.6

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
                half4 _GlowColor;
                float _Opacity;
                float _GlowFalloff;
                float _EdgeFade;
                float _RingCount;
                float _RingSpeed;
                float _RingWidth;
                float _RingStrength;
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
                // 中心 0 / 外周 1
                float r = length(input.uv * 2.0 - 1.0);

                // 中心ほど明るい光
                float glow = pow(saturate(1.0 - r), _GlowFalloff);

                // 中心へ吸い込まれる波紋
                float ringPhase = frac(r * _RingCount + _Time.y * _RingSpeed);
                float ring = 1.0 - smoothstep(0.0, _RingWidth, abs(ringPhase - 0.5));
                ring *= saturate(1.0 - r);

                // 外周でフェードアウト
                float edge = smoothstep(1.0, 1.0 - _EdgeFade, r);

                float amount = (glow + ring * _RingStrength) * edge * _Opacity;
                float power = _Intensity + _Flash * 3.0;

                return half4(_GlowColor.rgb * amount * power, 1.0);
            }
            ENDHLSL
        }
    }
}
