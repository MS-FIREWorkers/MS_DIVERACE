/*
    UI_Parallelogram.shader
    20261117  hanaue sho
    Unity 6 / URP
*/

Shader "Custom/UI/Parallelogram"
{
    // インスペクターに表示するパラメータ
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("UI Tint", Color) = (1.0, 1.0, 1.0, 1.0)

        [Header(Shape)]
        _SkewAngle ("Skew Angle", Range(-45.0, 45.0)) = 20.0
        _EdgeInset ("Edge Inset", Range(0.0, 20.0)) = 2.0
        _EdgeSoftness ("Edge Softness", Range(0.5, 4.0)) = 1.0

        [Header(Fill)]
        _FillColor ("Fill Color", Color) = (0.35, 0.8, 0.3, 0.5)

        [Header(Border)]
        _BorderColor ("Border Color", Color) = (0.75, 1.0, 0.6, 1.0)
        _BorderWidth ("Border Width", Range(0.0, 20.0)) = 4.0

        [Header(Accent Lines)]
        _AccentColor ("Accent Color", Color) = (0.85, 1.0, 0.7, 1.0)
        _AccentWidth ("Accent Width", Range(0.0, 20.0)) = 5.0
        _AccentSpacing ("Accent Spacing", Range(0.0, 100.0)) = 14.0
        _AccentOffset ("Accent Offset", Range(0.0, 100.0)) = 16.0

        [Header(Noise)]
        _NoiseStrength ("Noise Strength", Range(0.0, 1.0)) = 0.08   // ノイズによる明暗変化の強さ
        _NoiseScale ("Noise Scale", Range(1.0, 10.0)) = 2.0         // ノイズの粒の細かさ
        _NoiseSpeed ("Noise Speed", Range(0.0, 60.0)) = 20.0        // ノイズパターンの更新回数

        [Header(Horizontal Glitch)]
        _GlitchInterval ("Glitch Interval", Float) = 4.0            // グリッチの発生間隔
        _GlitchDuration ("Glitch Duration", Float) = 0.15           // グリッチの継続時間
        _GlitchBandCount ("Glitch Band Count", Range(1, 8)) = 3     // 同時に発生する帯の本数
        _GlitchBandWidth ("Glitch Band Width", Float) = 6.0         // 帯の基本的な太さ
        _GlitchStrength ("Glitch Strength", Range(0.0, 1.0)) = 0.4  // 帯による明るさの変化量

        [Header(Border Light)]
        [HDR] _BorderLightColor ("Border Light Color", Color) = (0.5, 1.0, 0.4, 1.0)
        _BorderLightLength ("Border Light Length", Float) = 40.0
        _BorderLightDuration ("Border Light Duration", Float) = 0.6
        [HideInInspector] _BorderLightStartTime ("Border Light Start Time", Float) = -1.0

        [Header(Highlight)]
        [HDR] _HighlightColor ("Highlight Color", Color) = (0.6, 1.0, 0.5, 1.0)
        _HighlightWidth ("Highlight Width", Float) = 60.0
        _HighlightDuration ("Highlight Duration", Float) = 0.5
        [HideInInspector] _HighlightStartTime ("Highlight Start Time", Float) = -1.0


        // Unity UI Mask
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        // 描画パイプラインや描画順序など
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        // 一組の描画処理
        Pass
        {
            Name "UI Parallelogram"

            // HLSLで記述するGPU側の処理
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ==================================================
            // ----- Properties -----
            // ==================================================
            // Propertiesの値をHLSLで使用するための宣言
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;

                // ----- Shape -----
                float _SkewAngle;
                float _EdgeInset;
                float _EdgeSoftness;

                // ----- Fill -----
                float4 _FillColor;

                // ----- Border -----
                float4 _BorderColor;
                float _BorderWidth;

                // ----- Accent Lines -----
                float4 _AccentColor;
                float _AccentWidth;
                float _AccentSpacing;
                float _AccentOffset;

                // ----- Noise -----
                float _NoiseStrength;
                float _NoiseScale;
                float _NoiseSpeed;

                // ----- Horizontal Glitch -----
                float _GlitchInterval;
                float _GlitchDuration;
                float _GlitchBandCount;
                float _GlitchBandWidth;
                float _GlitchStrength;

                // ----- Border Light -----
                float4 _BorderLightColor;
                float _BorderLightLength;
                float _BorderLightDuration;
                float _BorderLightStartTime;

                // ----- Highlight -----
                float4 _HighlightColor;
                float _HighlightWidth;
                float _HighlightDuration;
                float _HighlightStartTime;

                // ----- UI Mask -----
                float4 _ClipRect;
            CBUFFER_END

            // ==================================================
            // ----- Structures -----
            // ==================================================
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
                float2 localPos : TEXCOORD1;
            };

            // ==================================================
            // ----- Rect Size -----
            // UVとローカル座標からUIの幅・高さを算出する
            // ==================================================
            float2 GetRectSize(float2 uv, float2 localPos)
            {
                // UVの画面上での変化量
                float2 du = float2(ddx(uv.x), ddy(uv.x));
                float2 dv = float2(ddx(uv.y), ddy(uv.y));

                // ローカル座標の画面上での変化量
                float2 dx = float2(ddx(localPos.x), ddy(localPos.x));
                float2 dy = float2(ddx(localPos.y), ddy(localPos.y));

                // UV変化量とローカル座標変化量から幅と高さを復元
                float width = abs(dot(dx, du) / max(dot(du, du), 1e-10));
                float height = abs(dot(dy, dv) / max(dot(dv, dv), 1e-10));

                return max(float2(width, height), 1.0);
            }

            // ==================================================
            // ----- Shape Distance -----
            // 平行四辺形の境界までの距離を計算する
            // 内側が負、外側が正
            // ==================================================
            float GetShapeDistance(float2 p, float2 size, float skew)
            {
                float2 halfSize = size * 0.5;

                // 傾斜してもRectの内部に収まるように幅を調整
                float halfWidth = max(halfSize.x - abs(skew) * halfSize.y - _EdgeInset, 0.0);
                float halfHeight = max(halfSize.y - _EdgeInset, 0.0);

                // 傾斜した座標を補正
                float skewedX = p.x - p.y * skew;

                // 左右の斜辺までの距離
                float distanceX = (abs(skewedX) - halfWidth) / sqrt(1.0 + skew * skew);

                // 上下の辺までの距離
                float distanceY = abs(p.y) - halfHeight;

                // 最も外側にある境界距離を返す
                return max(distanceX, distanceY);
            }

            // ==================================================
            // ----- Line Mask -----
            // 指定位置を中心に線のマスクを生成する
            // ==================================================
            float GetLineMask(float distanceValue, float center, float width, float aa)
            {
                if (width <= 0.0)
                {
                    return 0.0;
                }

                // 線の中心からの距離
                float d = abs(distanceValue - center) - width * 0.5;

                // 境界を滑らかにする
                return 1.0 - smoothstep(-aa, aa, d);
            }

            // ==================================================
            // ----- Segment Distance -----
            // 線分上の最近接点と、線分内の進行率を求める
            // ==================================================
            float CalculateSegmentDistance(float2 p, float2 a, float2 b, out float t)
            {
                float2 ab = b - a;

                // 線分上の最近接点を求める
                t = saturate(dot(p - a, ab) / max(dot(ab, ab), 0.0001));

                return length(p - (a + ab * t));
            }

            // ==================================================
            // ----- Noise -----
            // ランダムな明暗変化を生成する
            // ==================================================
            float Random(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
            }
            float CalculateNoise(float2 localPos)
            {
                // ----- Noise Coordinate -----
                // UI座標をノイズの格子座標に変換する
                float2 noiseUV = floor(localPos * _NoiseScale);

                // ----- Noise Animation -----
                // 時間を一定間隔で区切ってノイズを更新
                float timeStep = floor(_Time.y * _NoiseSpeed);

                // 各格子に異なる乱数を生成
                float noise = Random(noiseUV + timeStep * 17.13);

                // 0～1の乱数を-1～1へ変換
                return noise * 2.0 - 1.0;
            }

            // ==================================================
            // ----- UI Seed -----
            // UIの画面上の中心座標から固有のSeedを生成する
            // ==================================================
            float2 CalculateUISeed(float2 uv, float2 screenPos)
            {
                // ----- UV Derivatives -----
                float2 uvDx = ddx(uv);
                float2 uvDy = ddy(uv);

                // UVから画面座標への逆変換に使用
                float det = uvDx.x * uvDy.y - uvDx.y * uvDy.x;
                det = abs(det) < 1e-8 ? (det < 0.0 ? -1e-8 : 1e-8) : det;

                // UVの中心(0.5, 0.5)までの画面上の距離
                float2 delta = uv - 0.5;
                float2 offset = float2((delta.x * uvDy.y - delta.y * uvDx.y) / det, (uvDx.x * delta.y - uvDy.x * delta.x) / det);

                // 同じUI内では共通の中心座標になる
                float2 center = screenPos - offset;

                return floor(center * 0.02);
            }

            // ==================================================
            // ----- Horizontal Glitch -----
            // 一定間隔でランダムな水平帯ノイズを生成する
            // ==================================================
            float CalculateHorizontalGlitch(float2 uv, float2 rectSize, float2 uiSeed)
            {
                // ----- Glitch Timing -----
                float interval = max(_GlitchInterval, 0.001);
                float duration = max(_GlitchDuration, 0.0);

                // UIごとに異なる時間オフセットを生成
                float timeOffset = Random(uiSeed) * interval;
                float localTime = _Time.y + timeOffset;

                // 各UIで異なるタイミングで発生
                float cycle = floor(localTime / interval);
                float cycleTime = frac(localTime / interval) * interval;

                // 発生時間外なら表示しない
                if (cycleTime >= min(duration, interval))
                {
                    return 0.0;
                }

                // ----- Band Mask -----
                float mask = 0.0;
                float aa = max(fwidth(uv.y * rectSize.y), 0.001);

                // 最大8本の水平帯を生成
                [loop]
                for (int i = 0; i < 8; i++)
                {
                    if (i >= (int)_GlitchBandCount) break;

                    // 発生周期と帯番号からランダムな位置・太さを算出
                    float seed = cycle * 37.17 + (float)i * 91.31 + Random(uiSeed + 17.3) * 1000.0;

                    float positionY = Random(float2(seed, 13.7));
                    float widthFactor = lerp(0.5, 1.5, Random(float2(seed, 71.3)));
                    float strengthFactor = Random(float2(seed, 29.1));

                    // UI単位で帯の幅を指定
                    float halfWidthUV = (_GlitchBandWidth * widthFactor * 0.5) / max(rectSize.y, 1.0);

                    // 帯の中心からの距離
                    float distanceY = abs(uv.y - positionY) * rectSize.y;
                    float halfWidth = halfWidthUV * rectSize.y;

                    // 帯の境界を滑らかにする
                    float bandMask = 1.0 - smoothstep(halfWidth - aa, halfWidth + aa, distanceY);

                    // 複数の帯を合成
                    mask = max(mask, bandMask * strengthFactor);
                }

                return mask * _GlitchStrength;
            }

            // ==================================================
            // ----- Border Progress -----
            // 平行四辺形の輪郭上の進行位置と周長を求める
            // ==================================================
            float CalculateBorderProgress(float2 p, float2 rectSize, float skew, out float perimeter)
            {
                // ----- Shape Corners -----
                float halfHeight = max(rectSize.y * 0.5 - _EdgeInset, 0.0);
                float halfWidth = max(rectSize.x * 0.5 - abs(skew) * rectSize.y * 0.5 - _EdgeInset, 0.0);

                // 平行四辺形の4頂点
                float2 TL = float2(-halfWidth + skew * halfHeight, halfHeight);
                float2 TR = float2( halfWidth + skew * halfHeight, halfHeight);
                float2 BR = float2( halfWidth - skew * halfHeight, -halfHeight);
                float2 BL = float2(-halfWidth - skew * halfHeight, -halfHeight);

                // ----- Perimeter -----
                float topLength = length(TR - TL);
                float sideLength = length(BR - TR);

                perimeter = 2.0 * (topLength + sideLength);

                // ----- Closest Segment -----
                float t;
                float minDistance = CalculateSegmentDistance(p, TL, TR, t);

                // 左上を起点として時計回りに進む
                float progress = t * topLength;

                float distanceValue = CalculateSegmentDistance(p, TR, BR, t);
                if (distanceValue < minDistance)
                {
                    minDistance = distanceValue;
                    progress = topLength + t * sideLength;
                }

                distanceValue = CalculateSegmentDistance(p, BR, BL, t);
                if (distanceValue < minDistance)
                {
                    minDistance = distanceValue;
                    progress = topLength + sideLength + t * topLength;
                }

                distanceValue = CalculateSegmentDistance(p, BL, TL, t);
                if (distanceValue < minDistance)
                {
                    progress = topLength * 2.0 + sideLength + t * sideLength;
                }

                return progress;
            }

            // ==================================================
            // ----- Border Light -----
            // 指定時間で枠線を一周する光のマスクを生成する
            // ==================================================
            float CalculateBorderLight(float2 p, float2 rectSize, float skew)
            {
                // ----- Animation Time -----
                // 再生指示がない場合
                if (_BorderLightStartTime < 0.0)
                {
                    return 0.0;
                }

                float duration = max(_BorderLightDuration, 0.001);
                float elapsed = _Time.y - _BorderLightStartTime;

                // 再生時間外
                if (elapsed < 0.0 || elapsed >= duration)
                {
                    return 0.0;
                }

                // 再生進行率
                float animationT = saturate(elapsed / duration);

                // ----- Border Position -----
                float perimeter;
                float borderPosition = CalculateBorderProgress(p, rectSize, skew, perimeter);

                // 光の現在位置
                float lightPosition = animationT * perimeter;

                // ----- Light Mask -----
                // 輪郭を循環する距離計算
                float dist = abs(borderPosition - lightPosition);
                dist = min(dist, perimeter - dist);

                // 光の中心を最も明るくし、両端を滑らかに減衰
                float lightLength = max(_BorderLightLength, 0.001);
                float lightMask = 1.0 - smoothstep(0.0, lightLength, dist);

                return lightMask;
            }

            // ==================================================
            // ----- Highlight -----
            // 平行四辺形の斜辺と平行な光を左から右へ移動させる
            // ==================================================
            float CalculateHighlight(float2 p, float2 rectSize, float skew)
            {
                // ----- Animation Time -----
                // 再生指示がない場合
                if (_HighlightStartTime < 0.0)
                {
                    return 0.0;
                }

                float duration = max(_HighlightDuration, 0.001);
                float elapsed = _Time.y - _HighlightStartTime;

                // 再生時間外
                if (elapsed < 0.0 || elapsed >= duration)
                {
                    return 0.0;
                }

                float animationT = saturate(elapsed / duration);

                // ----- Highlight Position -----
                // 平行四辺形の斜辺に沿った座標へ変換
                float skewedX = p.x - p.y * skew;

                // 斜辺の傾きを考慮した距離補正
                float skewLength = sqrt(1.0 + skew * skew);

                // 平行四辺形の左右の境界位置
                float halfWidth = max(rectSize.x * 0.5 - abs(skew) * rectSize.y * 0.5 - _EdgeInset, 0.0);

                // 光の帯が完全に画面外から入り、画面外へ抜ける範囲
                float halfLightWidth = max(_HighlightWidth, 0.001) * 0.5 * skewLength;
                float startX = -halfWidth - halfLightWidth;
                float endX = halfWidth + halfLightWidth;

                // 光の現在位置
                float highlightX = lerp(startX, endX, animationT);

                // ----- Highlight Mask -----
                // 光の中心からの垂直距離
                float dist = abs(skewedX - highlightX) / skewLength;

                // 中心が最も明るく、両端へ向かって減衰
                float halfWidthLight = max(_HighlightWidth * 0.5, 0.001);
                float highlightMask = 1.0 - smoothstep(0.0, halfWidthLight, dist);

                return highlightMask;
            }

            // ==================================================
            // ----- Vertex Shader -----
            // ==================================================
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                OUT.localPos = IN.positionOS.xy;

                return OUT;
            }

            // ==================================================
            // ----- Fragment Shader -----
            // ==================================================
            float4 frag(Varyings IN) : SV_Target
            {
                // ----- Shape -----
                // RectTransformに対応するUIのサイズを取得
                float2 rectSize = GetRectSize(IN.uv, IN.localPos);

                // UV座標を中心原点のUI座標へ変換
                float2 p = (IN.uv - 0.5) * rectSize;

                // 傾斜角度を度数法から傾きへ変換
                float skew = tan(radians(_SkewAngle));

                // 極端な傾斜で平行四辺形が潰れないよう制限
                float maxSkew = max((rectSize.x - _EdgeInset * 2.0 - 2.0) / rectSize.y, 0.0);
                skew = clamp(skew, -maxSkew, maxSkew);

                // 平行四辺形の境界距離とアンチエイリアス幅
                float shapeDistance = GetShapeDistance(p, rectSize, skew);
                float aa = max(fwidth(shapeDistance), 0.001) * _EdgeSoftness;

                // ----- Shape Mask -----
                // 平行四辺形の内側を1、外側を0とする
                float shapeMask = 1.0 - smoothstep(-aa, aa, shapeDistance);

                // ----- Border Mask -----
                // 枠線の内側境界を計算
                float innerMask = 1.0 - smoothstep(-aa, aa, shapeDistance + _BorderWidth);

                // 外側と内側の差分を枠線として取得
                float borderMask = saturate(shapeMask - innerMask);

                // ----- Base Color -----
                float4 color = _FillColor;

                // ----- Noise -----
                // 面のRGBにノイズを適用
                float noise = CalculateNoise(IN.localPos);
                float noiseBrightness = 1.0 + noise * _NoiseStrength;

                color.rgb = saturate(color.rgb * noiseBrightness);

                // ----- UI Seed -----
                float2 uiSeed = CalculateUISeed(IN.uv, IN.positionCS.xy);

                // ----- Horizontal Glitch -----
                // 一定間隔で水平帯状の明るさの乱れを追加
                float glitch = CalculateHorizontalGlitch(IN.uv, rectSize, uiSeed);

                color.rgb = saturate(color.rgb + glitch);

                // ----- Highlight -----
                // 左から右へ移動するハイライト
                float highlight = CalculateHighlight(p, rectSize, skew);

                // 光の色を面に加算
                color.rgb += _HighlightColor.rgb * highlight * _HighlightColor.a;

                // ----- Border -----
                // 枠線の色を合成
                float borderBlend = saturate(borderMask / max(shapeMask, 0.0001));
                color = lerp(color, _BorderColor, borderBlend);

                // 形状の外側を透明にする
                color.a *= shapeMask;

                // ----- Accent Lines -----
                // 平行四辺形の幅を計算
                float halfWidth = max(rectSize.x * 0.5 - abs(skew) * rectSize.y * 0.5 - _EdgeInset, 0.0);

                // 傾斜を補正した座標
                float skewedX = p.x - p.y * skew;

                // 右側の斜辺から内側への距離
                float rightDistance = (halfWidth - skewedX) / sqrt(1.0 + skew * skew);

                // アクセント線を2本生成
                float line1 = GetLineMask(rightDistance, _AccentOffset, _AccentWidth, aa);
                float line2 = GetLineMask(rightDistance, _AccentOffset + _AccentSpacing, _AccentWidth, aa);

                // 2本の線を合成し、平行四辺形の内部に制限
                float accentMask = max(line1, line2) * shapeMask;

                // アクセント線の色と透明度を合成
                float accentAlpha = accentMask * _AccentColor.a;
                color.rgb = lerp(color.rgb, _AccentColor.rgb, accentAlpha);
                color.a = max(color.a, accentAlpha);

                // ----- Border Light -----
                // 枠線に沿って移動する光を計算
                float borderLight = CalculateBorderLight(p, rectSize, skew);

                // 枠線の範囲だけに光を描画
                float borderLightMask = borderLight * borderMask;

                // 光の色を加算
                color.rgb += _BorderLightColor.rgb * borderLightMask * _BorderLightColor.a;

                // ----- UI Tint -----
                // ImageのColorおよびCanvasGroupなどの色を反映
                color *= IN.color;

                // ----- RectMask2D -----
                #ifdef UNITY_UI_CLIP_RECT
                    float2 insideMin = step(_ClipRect.xy, IN.localPos);
                    float2 insideMax = step(IN.localPos, _ClipRect.zw);

                    color.a *= insideMin.x * insideMin.y * insideMax.x * insideMax.y;
                #endif

                // ----- Alpha Clip -----
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }

            ENDHLSL
        }
    }
}
