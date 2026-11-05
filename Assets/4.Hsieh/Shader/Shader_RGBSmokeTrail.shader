
Shader "MS_DIVERACE/RGBSmokeTrail"
{
    Properties
    {
        [Header(RGB Color)]
        _ColorSpeed ("RGB Cycle Speed", Range(0, 2)) = 0.2
        _ColorSpread ("RGB Color Spread", Range(0, 4)) = 1.5
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 3.0

        [Header(Smoke)]
        _Opacity ("Smoke Opacity", Range(0, 1)) = 0.7
        _NoiseTiling ("Noise Tiling", Range(1, 30)) = 8
        _NoiseSpeed ("Noise Flow Speed", Range(0, 4)) = 0.6
        _EdgeSoftness ("Edge Softness", Range(0.5, 5)) = 2
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
            Name "RGBSmokeTrail"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _ColorSpeed;
            float _ColorSpread;
            float _EmissionStrength;
            float _Opacity;
            float _NoiseTiling;
            float _NoiseSpeed;
            float _EdgeSoftness;

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
            // ----- Noise Functions -----
            // ==================================================

            // 座標に応じた疑似乱数を生成する。
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // 滑らかなノイズを生成する。
            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash(cell);
                float b = Hash(cell + float2(1.0, 0.0));
                float c = Hash(cell + float2(0.0, 1.0));
                float d = Hash(cell + float2(1.0, 1.0));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            // ==================================================
            // ----- Rainbow Color -----
            // ==================================================

            // 色相からRGBカラーを生成する。
            float3 HueToRGB(float hue)
            {
                float3 color = saturate(
                    abs(
                        frac(hue + float3(0.0, 2.0 / 3.0, 1.0 / 3.0))
                        * 6.0 - 3.0
                    ) - 1.0
                );

                return color * color * (3.0 - 2.0 * color);
            }

            // ==================================================
            // ----- Fragment Shader -----
            // ==================================================

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // 時間とトレイルの位置によってRGBを循環させる。
                float hue = frac(
                    time * _ColorSpeed +
                    uv.x * _ColorSpread
                );

                float3 rainbowColor = HueToRGB(hue);

                // 煙の模様が時間とともに流れるようにする。
                float2 noiseUV = float2(
                    uv.x * _NoiseTiling - time * _NoiseSpeed,
                    uv.y * 3.0 +
                    sin(uv.x * 11.0 - time * 1.5) * 0.2
                );

                float noise1 = ValueNoise(noiseUV);
                float noise2 = ValueNoise(noiseUV * 2.3 + 8.7);
                float smokeNoise = noise1 * 0.7 + noise2 * 0.3;

                // ノイズから煙の濃淡を生成する。
                float smokeAlpha = smoothstep(
                    0.2, 0.75, smokeNoise
                );

                // 帯の中心は濃く、左右の端は透明にする。
                float center = 1.0 - abs(uv.y * 2.0 - 1.0);
                float edgeAlpha = pow(
                    saturate(center), _EdgeSoftness
                );

                // トレイルの頂点カラーによる透明度も反映する。
                float alpha = saturate(
                    _Opacity *
                    input.color.a *
                    smokeAlpha *
                    edgeAlpha
                );

                // HDRカラーを使用して発光感を表現する。
                float3 finalColor =
                    rainbowColor *
                    input.color.rgb *
                    _EmissionStrength;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
