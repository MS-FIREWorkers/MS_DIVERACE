/*
    BoostRing_Hologram.shader
    20261125  Teki
    加速リング周りのホログラム装飾（加算合成）
    ・UV : 各パーツ内の座標（帯なら UV.x = 円周 / UV.y = 幅方向）
    ・頂点カラー R : パーツごとの乱数（ちらつき・アクセント色の判定用）
    ・頂点カラー G : 並び順 0〜1（スイープ演出用、矢印が後ろ → 前へ順に光る）
    ・頂点カラー B : パネルの 横 / 縦 ÷ 4（パネル模様の縦横比補正用）
    ・_PanelDetail > 0 でパネルに模様（ブラケット・文字列風の線・棒グラフ・照準）と角の面取りを追加
    ・1 つのシェーダーで「破線」「ふち光り」「走査線」「ちらつき」「スイープ」を
      マテリアルの数値で切り替える
    ・_Intensity / _Flash はスクリプト（BoostRing_Visual）から制御
*/

Shader "Custom/BoostRing_Hologram"
{
    Properties
    {
        [Header(Color)]
        [HDR] _Color ("Color", Color) = (0.4, 1.0, 2.5, 1)
        [HDR] _AccentColor ("Accent Color", Color) = (2.0, 0.5, 2.5, 1)
        _AccentAmount ("Accent Amount", Range(0.0, 1.0)) = 0.0
        _AccentAlternate ("Accent Alternate (1 = Dash Alternate)", Range(0.0, 1.0)) = 0.0
        _Opacity ("Opacity", Range(0.0, 2.0)) = 1.0

        [Header(Fill and Edge)]
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 1.0
        _EdgeWidth ("Edge Width", Range(0.001, 0.5)) = 0.15
        _EdgeGlow ("Edge Glow", Range(0.0, 3.0)) = 0.0
        _EdgeAxisX ("Edge On UV.x (0 = Y only, 1 = Both)", Range(0.0, 1.0)) = 0.0

        [Header(Dash)]
        _DashCount ("Dash Count (0 = Off)", Float) = 0
        _DashDuty ("Dash Duty", Range(0.01, 1.0)) = 0.6

        [Header(Scanline)]
        _ScanlineDensity ("Scanline Density", Float) = 20.0
        _ScanlineSpeed ("Scanline Speed", Float) = 1.0
        _ScanlineAmount ("Scanline Amount", Range(0.0, 1.0)) = 0.0

        [Header(Flicker)]
        _FlickerAmount ("Flicker Amount", Range(0.0, 1.0)) = 0.2
        _FlickerSpeed ("Flicker Speed", Float) = 8.0

        [Header(Sweep)]
        _SweepAmount ("Sweep Amount", Range(0.0, 3.0)) = 0.0
        _SweepSpeed ("Sweep Speed", Float) = 1.5
        _SweepSharpness ("Sweep Sharpness", Range(1.0, 16.0)) = 4.0

        [Header(Panel Detail)]
        _PanelDetail ("Panel Detail (0 = Off)", Range(0.0, 2.0)) = 0.0
        _PanelChamfer ("Panel Chamfer", Range(0.0, 0.4)) = 0.12

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
                half4 _Color;
                half4 _AccentColor;
                float _AccentAmount;
                float _AccentAlternate;
                float _Opacity;
                float _FillAmount;
                float _EdgeWidth;
                float _EdgeGlow;
                float _EdgeAxisX;
                float _DashCount;
                float _DashDuty;
                float _ScanlineDensity;
                float _ScanlineSpeed;
                float _ScanlineAmount;
                float _FlickerAmount;
                float _FlickerSpeed;
                float _SweepAmount;
                float _SweepSpeed;
                float _SweepSharpness;
                float _PanelDetail;
                float _PanelChamfer;
                float _Intensity;
                float _Flash;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : TEXCOORD1;
            };

            // 0〜1 の疑似乱数
            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            // 範囲内なら 1（min 以上 max 以下）
            float InRange(float v, float minValue, float maxValue)
            {
                return step(minValue, v) * step(v, maxValue);
            }

            // ホログラムパネルの模様
            // ・四隅のブラケット（全パネル共通）
            // ・中身は id で 3 種類：文字列風の線 / 動く棒グラフ / 回転する照準
            // aspect = パネルの 横 / 縦（線の太さと円を縦横で揃えるため）
            float PanelDetail(float2 uv, float id, float aspect)
            {
                float2 c = min(uv, 1.0 - uv) - 0.05;

                // ----- 四隅のブラケット -----
                float thickness = 0.03;
                float bracketLength = 0.22;
                float vertical = InRange(c.x, 0.0, thickness / aspect) * InRange(c.y, 0.0, bracketLength);
                float horizontal = InRange(c.y, 0.0, thickness) * InRange(c.x, 0.0, bracketLength / aspect);
                float bracket = saturate(vertical + horizontal);

                // ----- 中身（0〜1 の内側領域） -----
                float2 a = (uv - 0.15) / 0.7;
                float inside = InRange(a.x, 0.0, 1.0) * InRange(a.y, 0.0, 1.0);
                float motif = floor(id * 3.0);
                float content = 0.0;

                if (motif < 0.5)
                {
                    // 文字列風の横線（ときどき書き換わる）
                    float rows = 5.0;
                    float row = floor(a.y * rows);
                    float lineLength = 0.25 + 0.7 * Hash11(row * 7.1 + id * 53.0 + floor(_Time.y * 1.5 + id * 4.0));
                    content = step(frac(a.y * rows), 0.45) * step(a.x, lineLength);
                }
                else if (motif < 1.5)
                {
                    // 動く棒グラフ
                    float bars = 7.0;
                    float barIndex = floor(a.x * bars);
                    float barHeight = 0.2 + 0.75 * (0.5 + 0.5 * sin(_Time.y * 2.5 + barIndex * 1.3 + id * 20.0));
                    content = step(frac(a.x * bars), 0.6) * step(a.y, barHeight);
                }
                else
                {
                    // 回転する照準 + 右側に短い線
                    float2 q = a - float2(0.3, 0.5);
                    q.x *= aspect;
                    float r = length(q);
                    float ring = 1.0 - smoothstep(0.015, 0.035, abs(r - 0.3));
                    float ringAngle = atan2(q.y, q.x) / 6.2832 + 0.5;
                    float ringGap = step(0.25, frac(ringAngle * 4.0 + _Time.y * 0.5));
                    float centerDot = 1.0 - smoothstep(0.04, 0.06, r);
                    float sideRow = floor(a.y * 4.0);
                    float sideLines = InRange(a.x, 0.68, 0.68 + 0.3 * Hash11(sideRow + id * 9.0))
                                    * step(frac(a.y * 4.0), 0.35);
                    content = saturate(ring * ringGap + centerDot + sideLines);
                }

                return saturate(bracket + content * inside * 0.8);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // ----- 破線（UV.x 方向） -----
                float dash = 1.0;
                float dashIndex = 0.0;
                if (_DashCount >= 1.0)
                {
                    float dashCoord = uv.x * round(_DashCount);
                    float dashPhase = frac(dashCoord);
                    float aa = max(fwidth(dashCoord), 1e-4);
                    dash = smoothstep(0.0, aa, dashPhase) * (1.0 - smoothstep(_DashDuty - aa, _DashDuty, dashPhase));
                    dashIndex = floor(dashCoord);
                }

                // パーツ（または破線 1 本）ごとの乱数
                float id = Hash11(input.color.r * 97.13 + dashIndex * 12.7);

                // ----- 塗り + ふち光り -----
                float edgeY = min(uv.y, 1.0 - uv.y);
                float edgeX = min(uv.x, 1.0 - uv.x);
                float edgeDistance = lerp(edgeY, min(edgeX, edgeY), _EdgeAxisX);
                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, edgeDistance);
                float shape = _FillAmount + edge * _EdgeGlow;

                // ----- 走査線 -----
                float scan = 1.0 - _ScanlineAmount
                           * (0.5 + 0.5 * sin((uv.y * _ScanlineDensity + _Time.y * _ScanlineSpeed) * 6.2832));

                // ----- ちらつき（ときどき暗くなる + 小さなゆらぎ） -----
                float flickerStep = floor(_Time.y * _FlickerSpeed);
                float dropout = step(0.85, Hash11(id * 91.3 + flickerStep));
                float wobble = 0.5 + 0.5 * sin(_Time.y * _FlickerSpeed * 1.7 + id * 6.2832);
                float flicker = 1.0 - _FlickerAmount * (dropout * 0.8 + wobble * 0.2);

                // ----- スイープ（並び順に光が走る） -----
                float sweep = pow(frac(input.color.g - _Time.y * _SweepSpeed), _SweepSharpness) * _SweepAmount;

                // ----- パネルの模様 + 角の面取り -----
                float chamferMask = 1.0;
                if (_PanelDetail > 0.001)
                {
                    float aspect = max(input.color.b * 4.0, 0.1);
                    shape += PanelDetail(uv, id, aspect) * _PanelDetail;

                    // 角を斜めに切り落とし、切り口も光らせる
                    float cornerDistance = edgeX + edgeY - _PanelChamfer;
                    chamferMask = step(0.0, cornerDistance);
                    shape += (1.0 - smoothstep(0.0, 0.03, cornerDistance)) * _EdgeGlow;
                }

                // ----- 合成 -----
                // アクセント色：ランダム or 破線 1 本ごとに交互（偶数本なら対称になる）
                float accentRandom = step(1.0 - _AccentAmount + 1e-4, id);
                float accentAlternate = fmod(dashIndex, 2.0);
                float accent = lerp(accentRandom, accentAlternate, _AccentAlternate);
                half3 baseColor = lerp(_Color.rgb, _AccentColor.rgb, accent);
                float amount = shape * chamferMask * dash * scan * flicker * (1.0 + sweep) * _Opacity;
                float power = _Intensity + _Flash * 2.0;

                return half4(baseColor * amount * power, 1.0);
            }
            ENDHLSL
        }
    }
}
