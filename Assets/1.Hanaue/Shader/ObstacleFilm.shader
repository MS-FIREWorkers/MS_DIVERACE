/*
    ObstacleFilm.shader
    20261114  hanaue sho
*/

Shader "Custom/ObstacleFilm"
{
    // インスペクターに表示するパラメータ
    Properties
    {
        _VoronoiTexA ("Voronoi Texture A", 2D) = "white" {}
        _VoronoiTexB ("Voronoi Texture B", 2D) = "white" {}

        [Header(Tiling)]
        _TileSize ("Tile Size (World Units)", Float) = 2.0

        [Header(Cross Fade)]
        _CrossFadeDuration ("Cross Fade Duration", Float) = 2.0
        _CrossFadeInterval ("Cross Fade Interval", Float) = 1.0

        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0.0, 1.0)) = 0.3

        [Header(Trim)]
        _TrimRadius ("Trim Radius", Float) = 1.0
        _TrimFeatherWidth ("Trim Feather Width", Float) = 0.25
        _TrimPointDensity ("Trim Point Density", Float) = 0.1
        _TrimMaxPointCount ("Trim Max Point Count", Range(1, 32)) = 16
        _TrimSeed ("Trim Seed", Float) = 123
        _TrimMoveSpeed("Trim Move Speed", Float) = 1.0
        _TrimAlpha ("Trim Alpha", Range(0.0, 1.0)) = 1.0

        [Header(Proximity Trim)]
        _ProximityHorizontalDistance ("Proximity Distance", Float) = 10.0
        _ProximityVerticalDistance ("Vertical Distance", Float) = 3.0
        _ProximityVerticalFadeDistance ("Vertical Fade Distance", Float) = 1.0

        _ProximityFadeDistance ("Proximity Fade Distance", Float) = 2.0
        _ProximityColorDistance ("Proximity Color Distance", Float) = 10.0
        _ProximityRadiusDistance ("Proximity Radius Distance", Float) = 5.0

        _ProximityRadiusFar ("Proximity Radius Far", Float) = 2.0
        _ProximityRadiusNear ("Proximity Radius Near", Float) = 0.8
        _ProximityFeatherWidth ("Proximity Feather Width", Float) = 0.3

        _ProximityColorFar ("Proximity Color Far", Color) = (1.0, 0.85, 0.1, 0.8)
        _ProximityColorNear ("Proximity Color Near", Color) = (1.0, 0.1, 0.05, 1.0)

        [Header(Landing Trim)]
        // 着地点トリミングの判定範囲
        _LandingHorizontalDistance ("Landing Horizontal Distance", Float) = 3.0
        _LandingVerticalDistance ("Landing Vertical Distance", Float) = 20.0
        // 判定範囲の境界で消える距離幅
        _LandingHorizontalFadeDistance ("Landing Horizontal Fade Distance", Float) = 1.0
        _LandingVerticalFadeDistance ("Landing Vertical Fade Distance", Float) = 3.0
        // 円形トリミング
        _LandingRadius ("Landing Radius", Float) = 1.5
        _LandingFeatherWidth ("Landing Feather Width", Float) = 0.3
        // 真下とずれた場合の色
        _LandingColorCenter ("Landing Color Center", Color) = (0.0, 0.0, 0.0, 1.0)
        _LandingColorOffset ("Landing Color Offset", Color) = (0.0, 0.0, 0.0, 1.0)
        // 色が変化する水平距離
        _LandingColorDistance ("Landing Color Distance", Float) = 3.0
        // 上向き面として扱う法線の閾値
        _LandingUpThreshold ("Landing Up Threshold", Range(0.0, 1.0)) = 0.7

        [Header(Color)]
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        // 描画パイプラインや描画順序など
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        // 一組の描画処理
        Pass
        {
            Name "ObstacleFilm"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha // 背景色との合成方法
            ZWrite Off // 深度バッファへの書き込みを無効化
            Cull Back  // カリング

            // HLSLで記述する GPU 側の処理
            HLSLPROGRAM

            #pragma vertex vert     // 頂点シェーダー
            #pragma fragment frag   // フラグメントシェーダー

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ==================================================
            // ----- Properties -----
            // ==================================================
            TEXTURE2D(_VoronoiTexA);
            SAMPLER(sampler_VoronoiTexA);

            TEXTURE2D(_VoronoiTexB);
            SAMPLER(sampler_VoronoiTexB);
            
            float4 _PlayerPositionWS;   // プレイヤーのワールド座標
            float _PlayerPositionValid; // 

            // Properties の値を HLSL で使用するための宣言
            CBUFFER_START(UnityPerMaterial)
                float _TileSize;
                float _CrossFadeDuration;
                float _CrossFadeInterval;
                
                // ----- Fresnel -----
                float _FresnelPower;
                float _FresnelStrength;
                
                // ----- Trim -----
                float _TrimRadius;
                float _TrimFeatherWidth;
                float _TrimPointDensity;
                float _TrimMaxPointCount;
                float _TrimSeed;
                float _TrimMoveSpeed;
                float _TrimAlpha;

                // ----- Proximity -----
                float _ProximityHorizontalDistance;
                float _ProximityVerticalDistance;
                float _ProximityVerticalFadeDistance;

                float _ProximityFadeDistance;
                float _ProximityColorDistance;
                float _ProximityRadiusDistance;

                float _ProximityRadiusFar;
                float _ProximityRadiusNear;
                float _ProximityFeatherWidth;

                half4 _ProximityColorFar;
                half4 _ProximityColorNear;

                float _LandingHorizontalDistance;
                
                // ----- Landing Trim -----
                float _LandingVerticalDistance;

                float _LandingHorizontalFadeDistance;
                float _LandingVerticalFadeDistance;

                float _LandingRadius;
                float _LandingFeatherWidth;

                half4 _LandingColorCenter;
                half4 _LandingColorOffset;

                float _LandingColorDistance;
                float _LandingUpThreshold;

                half4 _Color;
            CBUFFER_END
            
            // ==================================================
            // ----- Structures -----
            // ==================================================
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 voronoiUV : TEXCOORD0;
                float2 trimUV : TEXCOORD1;
                float2 faceSize : TEXCOORD2;
                nointerpolation float faceID : TEXCOORD3;

                float3 positionWS : TEXCOORD4;  // ワールド座標
                float3 normalWS : TEXCOORD5;    // ワールド法線
            };

            // ==================================================
            // ----- Random -----
            // ==================================================
            float Random(float seed)
            {
                return frac(sin(seed * 12.9898) * 43758.5453);
            }
            float2 GenerateTrimPoint(float pointIndex, float faceID)
            {
                float seed = _TrimSeed + faceID * 127.1 + pointIndex * 311.7;
            
                return float2(
                              Random(seed),
                              Random(seed + 73.3)
                             );
            }

            // ==================================================
            // ----- Calculate UV -----
            // ==================================================
            void CalculateBoxProjection(float3 positionOS, float3 normalOS, out float2 voronoiUV, out float2 trimUV, out float2 faceSize, out float faceID)
            {
                float3 scale = float3(
                                      length(mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0))), 
                                      length(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0))), 
                                      length(mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1)))
                                     );
                float3 scaledPosition = positionOS * scale;
                float3 normal = abs(normalOS);

                if (normal.x > normal.y && normal.x > normal.z)
                {
                    voronoiUV = scaledPosition.yz / max(_TileSize, 0.001);
                    trimUV = positionOS.yz + 0.5;
                    faceSize = scale.yz;
                    faceID = normalOS.x > 0.0 ? 0.0 : 1.0;
                }
                else if (normal.y > normal.z)
                {
                    voronoiUV = scaledPosition.xz / max(_TileSize, 0.001);
                    trimUV = positionOS.xz + 0.5;
                    faceSize = scale.xz;
                    faceID = normalOS.y > 0.0 ? 2.0 : 3.0;
                }
                else
                {
                    voronoiUV = scaledPosition.xy / max(_TileSize, 0.001);
                    trimUV = positionOS.xy + 0.5;
                    faceSize = scale.xy;
                    faceID = normalOS.z > 0.0 ? 4.0 : 5.0;
                }
            }

            // ==================================================
            // ----- Calculate Cross Fade -----
            // ==================================================
            float CalculateCrossFade()
            {
                float duration = max(_CrossFadeDuration, 0.001);
                float interval = max(_CrossFadeInterval, 0.0);

                // A -> B と B -> A の各フェードの後に停止時間を設ける
                float phaseLength = duration + interval;
                float phaseIndex = floor(_Time.y / phaseLength);
                float phaseTime = _Time.y - phaseIndex * phaseLength;

                // 停止時間中は1で固定
                float t = saturate(phaseTime / duration);

                // 滑らかな補間
                t = smoothstep(0.0, 1.0, t);

                // 偶数フェーズはA -> B、奇数フェーズはB -> A
                float direction = fmod(phaseIndex, 2.0);

                return lerp(t, 1.0 - t, direction);
            }

            // ==================================================
            // ----- Fresnel -----
            // ==================================================
            float CalculateFresnel(float3 positionWS, float3 normalWS)
            {
                float3 N = normalize(normalWS);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - positionWS);
            
                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
            
                return fresnel * _FresnelStrength;
            }
            
            // ==================================================
            // ----- Trim Movement -----
            // ==================================================
            float2 CalculateMovingTrimPoint(float pointIndex, float faceID, float2 faceSize)
            {
                // 初期位置
                float2 center = GenerateTrimPoint(pointIndex, faceID);

                // 代表点ごとに移動方向を生成
                float angleSeed = _TrimSeed + faceID * 173.3 + pointIndex * 271.9;
                float angle = Random(angleSeed) * 6.2831853;

                float2 direction = float2(cos(angle), sin(angle));

                // ワールド寸法を考慮して移動
                float2 safeFaceSize = max(faceSize, 0.001);
                float2 velocityUV = direction * _TrimMoveSpeed / safeFaceSize;

                // 時間経過による直線移動とループ
                return frac(center + velocityUV * _Time.y);
            }

            // ==================================================
            // ----- Trim Mask -----
            // ==================================================
            float CalculateTrimMask(float2 trimUV, float2 faceSize, float faceID)
            {
                // 面積から代表点数を算出
                float faceArea = faceSize.x * faceSize.y;
                int pointCount = (int)clamp(round(faceArea * max(_TrimPointDensity, 0.0)), 1.0, min(_TrimMaxPointCount, 32.0));

                float radius = max(_TrimRadius, 0.0);
                float feather = max(_TrimFeatherWidth, 0.0001);
                float inner = max(radius - feather, 0.0);

                float mask = 0.0;

                // 代表点ごとに円形マスクを生成
                [loop]
                for (int i = 0; i < 32; i++)
                {
                    if (i >= pointCount) break;

                    // 移動する代表点
                    float2 center = CalculateMovingTrimPoint((float)i, faceID, faceSize);
                    
                    // シームレスな距離計算
                    float2 delta = abs(trimUV - center);
                    delta = min(delta, 1.0 - delta);
                    
                    // ワールド寸法による正円補正
                    float dist = length(delta * faceSize);

                    // 線形フェザー
                    float circleMask = 1.0 - saturate((dist - inner) / max(radius - inner, 0.0001));

                    // 複数の円形マスクを合成
                    mask = max(mask, circleMask);
                }

                return mask;
            }

            // ==================================================
            // ----- Proximity Trim -----
            // ==================================================
            float3 GetObjectAxisScale()
            {
                return float3(
                              length(mul((float3x3)unity_ObjectToWorld, float3(1.0, 0.0, 0.0))), 
                              length(mul((float3x3)unity_ObjectToWorld, float3(0.0, 1.0, 0.0))), 
                              length(mul((float3x3)unity_ObjectToWorld, float3(0.0, 0.0, 1.0)))
                             );
            }
            // ==================================================
            // ----- Closest Point On Box Side -----
            // Boxの4つの側面から、水平方向で最も近い点を取得する
            // ==================================================
            float3 CalculateClosestPointOnBoxSide(float3 playerOS)
            {
                // プレイヤー位置をBoxの範囲内に制限
                float3 clampedOS = clamp(playerOS, -0.5, 0.5);

                // 4つの側面上に最近接点の候補を作成
                float3 pointPosX = float3( 0.5, clampedOS.y, clampedOS.z);
                float3 pointNegX = float3(-0.5, clampedOS.y, clampedOS.z);
                float3 pointPosZ = float3(clampedOS.x, clampedOS.y,  0.5);
                float3 pointNegZ = float3(clampedOS.x, clampedOS.y, -0.5);

                // 各候補をワールド座標へ変換
                float3 pointPosXWS = TransformObjectToWorld(pointPosX);
                float3 pointNegXWS = TransformObjectToWorld(pointNegX);
                float3 pointPosZWS = TransformObjectToWorld(pointPosZ);
                float3 pointNegZWS = TransformObjectToWorld(pointNegZ);

                // プレイヤーから各候補までの水平距離
                float2 playerXZ = _PlayerPositionWS.xz;

                float distPosX = length(playerXZ - pointPosXWS.xz);
                float distNegX = length(playerXZ - pointNegXWS.xz);
                float distPosZ = length(playerXZ - pointPosZWS.xz);
                float distNegZ = length(playerXZ - pointNegZWS.xz);

                // 最も水平距離の短い候補を選択
                float minDist = distPosX;
                float3 closestPointWS = pointPosXWS;

                if (distNegX < minDist)
                {
                    minDist = distNegX;
                    closestPointWS = pointNegXWS;
                }
                if (distPosZ < minDist)
                {
                    minDist = distPosZ;
                    closestPointWS = pointPosZWS;
                }
                if (distNegZ < minDist)
                {
                    closestPointWS = pointNegZWS;
                }
                return closestPointWS;
            }
            // ==================================================
            // ----- Proximity Distance Factor -----
            // 指定距離からどれだけ接近したかを0～1で返す
            // ==================================================
            float CalculateProximityFactor(float distanceToSurface, float startDistance, float transitionDistance)
            {
                // 変化開始距離から指定距離幅をかけて0→1へ変化
                float range = max(transitionDistance, 0.0001);

                return 1.0 - saturate((distanceToSurface - (startDistance - range)) / range);
            }
            // ==================================================
            // ----- Proximity Trim -----
            // 最近接点を中心に、距離に応じた接近警告を生成
            // ==================================================
            void CalculateProximityTrim(float3 positionWS, float3 normalWS, out float proximityMask, out half4 proximityColor)
            {
                proximityMask = 0.0;
                proximityColor = half4(0.0, 0.0, 0.0, 0.0);

                // プレイヤーが存在しない場合
                if (_PlayerPositionValid < 0.5)
                {
                    return;
                }

                // ----- Surface Check -----
                // 上面・下面を除外し、側面だけを対象にする
                float3 normal = normalize(normalWS);
                float verticalDot = abs(dot(normal, float3(0.0, 1.0, 0.0)));
                if (verticalDot >= 0.5)
                {
                    return;
                }

                // ----- Closest Point -----
                // プレイヤーをオブジェクト座標に変換
                float3 playerOS = TransformWorldToObject(_PlayerPositionWS.xyz);
                // 側面上の最近接点をワールド座標で取得
                float3 closestPointWS = CalculateClosestPointOnBoxSide(playerOS);

                // 水平方向と垂直方向の距離を分離
                float horizontalDistance = distance(_PlayerPositionWS.xz, closestPointWS.xz);
                float verticalDistance = abs(_PlayerPositionWS.y - closestPointWS.y);

                // ----- Distance Check -----
                // 水平方向の警告範囲
                if (horizontalDistance >= _ProximityHorizontalDistance)
                {
                    return;
                }
                // 垂直方向の警告範囲
                if (verticalDistance >= _ProximityVerticalDistance)
                {
                    return;
                }

                // ----- Distance Factors -----
                // 警告の強さ・色・半径は水平距離から算出
                float fadeT = CalculateProximityFactor(horizontalDistance, _ProximityHorizontalDistance, _ProximityFadeDistance);
                float colorT = CalculateProximityFactor(horizontalDistance, _ProximityHorizontalDistance, _ProximityColorDistance);
                float radiusT = CalculateProximityFactor(horizontalDistance, _ProximityHorizontalDistance, _ProximityRadiusDistance);

                // 垂直方向のフェードは警告強度にのみ反映
                float verticalFadeStart = max(_ProximityVerticalDistance - max(_ProximityVerticalFadeDistance, 0.0), 0.0);
                float verticalFadeT = 1.0 - saturate((verticalDistance - verticalFadeStart) / max(_ProximityVerticalDistance - verticalFadeStart, 0.0001));

                // ----- Circle Mask -----
                // 水平距離に応じて円半径を変更
                float radius = lerp(_ProximityRadiusFar, _ProximityRadiusNear, radiusT);

                // 側面上の最近接点を中心として円形マスクを生成
                float dist = distance(positionWS, closestPointWS);

                float inner = max(radius - max(_ProximityFeatherWidth, 0.0), 0.0);
                float circleMask = 1.0 - saturate((dist - inner) / max(radius - inner, 0.0001));

                // 円形マスクに水平・垂直方向のフェードを適用
                proximityMask = circleMask * fadeT * verticalFadeT;

                // 警告色は水平距離だけで変化させる
                proximityColor = lerp(_ProximityColorFar, _ProximityColorNear, colorT);
            }

            // ==================================================
            // ----- Landing Distance Fade -----
            // 判定範囲の境界付近で、表示強度を徐々に減らす
            // ==================================================
            float CalculateLandingDistanceFade(float distanceValue, float maxDistance, float fadeDistance)
            {
                float limit = max(maxDistance, 0.0);

                // 判定範囲外
                if (distanceValue >= limit)
                {
                    return 0.0;
                }

                // フェード開始距離
                float fadeStart = max(limit - max(fadeDistance, 0.0), 0.0);

                return 1.0 - saturate((distanceValue - fadeStart) / max(limit - fadeStart, 0.0001));
            }
            // ==================================================
            // ----- Landing Trim -----
            // プレイヤーの下方向に存在する上面へ警告を生成する
            // ==================================================
            void CalculateLandingTrim(float3 positionWS, float3 normalWS, out float landingMask, out half4 landingColor)
            {
                landingMask = 0.0;
                landingColor = half4(0.0, 0.0, 0.0, 0.0);

                // プレイヤー情報が存在しない場合
                if (_PlayerPositionValid < 0.5)
                {
                    return;
                }

                // ----- Surface Check -----
                // ワールド空間で上向きの面のみ対象とする
                float3 normal = normalize(normalWS);
                float upDot = dot(normal, float3(0.0, 1.0, 0.0));
                if (upDot < _LandingUpThreshold)
                {
                    return;
                }

                // ----- Closest Point -----
                // プレイヤーの位置をオブジェクト座標へ変換
                float3 playerOS = TransformWorldToObject(_PlayerPositionWS.xyz);

                // プレイヤーの真下に相当する座標を、上面の範囲内に制限
                float3 closestOS = float3(
                    clamp(playerOS.x, -0.5, 0.5),
                    0.5,
                    clamp(playerOS.z, -0.5, 0.5)
                );

                // 上面上の最近接点をワールド座標へ変換
                float3 closestPointWS = TransformObjectToWorld(closestOS);

                // ----- Distance Check -----
                // 水平方向は上面の範囲からどれだけ外れているか
                float horizontalDistance = distance(_PlayerPositionWS.xz, closestPointWS.xz);

                // 垂直方向はプレイヤーと上面の高さの差
                float verticalDistance = _PlayerPositionWS.y - closestPointWS.y;

                // プレイヤーより上にある面は対象外
                if (verticalDistance <= 0.0)
                {
                    return;
                }
                // 水平・垂直方向の判定範囲
                if (horizontalDistance >= _LandingHorizontalDistance || verticalDistance >= _LandingVerticalDistance)
                {
                    return;
                }

                // ----- Distance Fade -----
                // 判定範囲の端に近づくと表示を薄くする
                float horizontalFade = CalculateLandingDistanceFade(horizontalDistance, _LandingHorizontalDistance, _LandingHorizontalFadeDistance);
                float verticalFade = CalculateLandingDistanceFade(verticalDistance, _LandingVerticalDistance, _LandingVerticalFadeDistance);

                // ----- Circle Mask -----
                // 上面上の最近接点を中心とした円形マスク
                float dist = distance(positionWS, closestPointWS);

                float radius = max(_LandingRadius, 0.0);
                float inner = max(radius - max(_LandingFeatherWidth, 0.0), 0.0);

                float circleMask = 1.0 - saturate((dist - inner) / max(radius - inner, 0.0001));

                // 円形マスクと水平・垂直方向のフェードを合成
                landingMask = circleMask * horizontalFade * verticalFade;

                // ----- Landing Color -----
                // 上面の内部なら0、外側へ離れるにつれて1へ変化
                float colorT = saturate(horizontalDistance / max(_LandingColorDistance, 0.0001));

                // 真下用の色から、ずれた場合の色へ補間
                landingColor = lerp(_LandingColorCenter, _LandingColorOffset, colorT);
            }


            // ==================================================
            // ----- Vertex Shader -----
            // ==================================================
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz); // オブジェクト座標をクリップ空間へ変換
                CalculateBoxProjection(IN.positionOS.xyz, IN.normalOS, OUT.voronoiUV, OUT.trimUV, OUT.faceSize, OUT.faceID);    // 面ごとのUV計算

                // Fresnel
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                return OUT;
            }

            // ==================================================
            // ----- Fragment Shader -----
            // ==================================================
            half4 frag(Varyings IN) : SV_Target
            {
                half4 colorA = SAMPLE_TEXTURE2D(_VoronoiTexA, sampler_VoronoiTexA, IN.voronoiUV);
                half4 colorB = SAMPLE_TEXTURE2D(_VoronoiTexB, sampler_VoronoiTexB, IN.voronoiUV);

                // クロスフェード
                float crossFade = CalculateCrossFade();
                half4 color = lerp(colorA, colorB, crossFade);
                
                // 色調整
                color *= _Color;

                // ----- 円形トリミング -----
                float trimMask = CalculateTrimMask(IN.trimUV, IN.faceSize, IN.faceID);

                float trimAlpha = color.a * trimMask * _TrimAlpha;

                // Fresnel
                float fresnel = CalculateFresnel(IN.positionWS, IN.normalWS);

                // 円形トリミングとFresnelを合成
                color.a = saturate(trimAlpha + fresnel);

                // ----- 接近トリミング -----
                float proximityMask;
                half4 proximityColor;
                CalculateProximityTrim(IN.positionWS, IN.normalWS, proximityMask, proximityColor);

                float proximityAlpha = saturate(proximityMask * proximityColor.a);

                color.rgb = lerp(color.rgb, proximityColor.rgb, proximityAlpha);
                color.a = max(color.a, proximityAlpha);

                // ----- Landing Trim -----
                float landingMask;
                half4 landingColor;

                // 着地点トリミングの計算
                CalculateLandingTrim(IN.positionWS, IN.normalWS, landingMask, landingColor);

                // 着地点トリミングの透明度
                float landingAlpha = saturate(landingMask * landingColor.a);

                // 着地点の警告色へ上書きする
                color.rgb = lerp(color.rgb, landingColor.rgb, landingMask);
                // 既存の膜と着地点トリミングの透明度を合成
                color.a = max(color.a, landingAlpha);

                return color;
            }



            ENDHLSL
        }
    }
}