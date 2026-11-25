/*
    BoostTunnel_Panel.shader
    20261124  Teki
    加速トンネル内壁の発光パネル
    ・UV.x = 円周方向 / UV.y = 進行方向（0 = 入口, 1 = 出口）
    ・パネルを格子状に区切り、隙間を暗くする
    ・光の帯が入口 → 出口（下方向）へ流れる
    ・_Intensity / _Flash はスクリプト（BoostTunnel_Visual）から制御
*/

Shader "Custom/BoostTunnel_Panel"
{
    Properties
    {
        [Header(Color)]
        [HDR] _PanelColor ("Panel Color", Color) = (2.0, 0.9, 0.15, 1)
        [HDR] _FlowColor ("Flow Color", Color) = (6.0, 3.0, 0.8, 1)
        _GapColor ("Gap Color", Color) = (0.04, 0.03, 0.025, 1)

        [Header(Panel)]
        _PanelCountU ("Panel Count (Around)", Float) = 24
        _PanelCountV ("Panel Count (Length)", Float) = 4
        _GapWidth ("Gap Width", Range(0.0, 0.5)) = 0.08
        _PanelShade ("Panel Center Shade", Range(0.0, 1.0)) = 0.4
        _PanelFlicker ("Panel Flicker", Range(0.0, 1.0)) = 0.15
        _FlickerSpeed ("Flicker Speed", Float) = 6.0

        [Header(Flow)]
        _FlowSpeed ("Flow Speed", Float) = 1.5
        _FlowFrequency ("Flow Frequency", Float) = 1.0
        _FlowSharpness ("Flow Sharpness", Range(1.0, 16.0)) = 5.0

        [Header(Script Control)]
        _Intensity ("Intensity", Float) = 1.0
        _Flash ("Flash", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _PanelColor;
            half4 _FlowColor;
            half4 _GapColor;
            float _PanelCountU;
            float _PanelCountV;
            float _GapWidth;
            float _PanelShade;
            float _PanelFlicker;
            float _FlickerSpeed;
            float _FlowSpeed;
            float _FlowFrequency;
            float _FlowSharpness;
            float _Intensity;
            float _Flash;
        CBUFFER_END
        ENDHLSL

        // ==================================================
        // ----- Forward Pass -----
        // ==================================================
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            // 筒の内側・外側どちらからでも見えるようにする
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogFactor  : TEXCOORD1;
            };

            // 0〜1 の疑似乱数
            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // ----- パネル格子 -----
                float2 cell = float2(input.uv.x * _PanelCountU, input.uv.y * _PanelCountV);
                float2 cellId = floor(cell);
                float2 local = frac(cell);

                // セル端までの距離で隙間を作る（fwidth でアンチエイリアス）
                float2 edge = min(local, 1.0 - local);
                float2 aa = fwidth(cell);
                float halfGap = _GapWidth * 0.5;
                float2 mask2 = smoothstep(halfGap - aa, halfGap + aa, edge);
                float panelMask = mask2.x * mask2.y;

                // パネル中央ほど明るく
                float shade = lerp(1.0, saturate(1.0 - length(local - 0.5) * 1.4), _PanelShade);

                // パネルごとのゆらぎ
                float phaseOffset = Hash11(cellId.x * 17.0 + cellId.y * 131.0) * 6.2832;
                float flicker = 1.0 - _PanelFlicker * (0.5 + 0.5 * sin(_Time.y * _FlickerSpeed + phaseOffset));

                // ----- 流れる光（入口 → 出口へ移動、先頭が明るく尾を引く） -----
                float flowPhase = input.uv.y * _FlowFrequency - _Time.y * _FlowSpeed;
                float flow = pow(frac(flowPhase), _FlowSharpness);

                // ----- 合成 -----
                half3 panel = (_PanelColor.rgb * shade * flicker + _FlowColor.rgb * flow) * _Intensity
                            + _FlowColor.rgb * _Flash;
                half3 color = lerp(_GapColor.rgb, panel, panelMask);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ==================================================
        // ----- Depth Pass -----
        // 深度テクスチャ / 深度プリパス用
        // ==================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
}
