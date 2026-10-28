Shader "Custom/ToonShader_2"
{
    //Insoectorから設定できるパラメータを定義する
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        _ShadowColor("Shadow Color", Color) = (0.3, 0.3, 0.4, 1)
        _MidColor("Mid Color", Color) = (0.7, 0.7, 0.7, 1)
        _LightColor("Light Color", Color) = (1, 1, 1, 1)

        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.3
        _LightThreshold("Light Threshold", Range(0, 1)) = 0.6
    }

    //描画条件・描画方法(1つのShaderに複数のSubShaderを含めることも可能)
    SubShader
    {
        //レンダリングパイプラインの設定(シンプルにTag)
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        //GPUに1回の描画処理として何をさせるか(複数Passを用いることで、複雑な効果を実現できる)
        Pass
        {
            //GPUで実行するHLSLコード
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            //Meshからvertex shaderに渡されるデータを定義
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            //vertex shaderからfragment shaderに渡されるデータを定義
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // GPU側への定数バッファ
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _MidColor;
                half4 _LightColor;
            
                float4 _BaseMap_ST;

                float _ShadowThreshold;
                float _LightThreshold;
            CBUFFER_END

            // Vertex Shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                // オブジェクト空間の法線をワールド空間へ
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // ワールド座標
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                return OUT;
            }

            // Fragment Shader(Pixel Shader)
            half4 frag(Varyings IN) : SV_Target
            {
                //==================================================
                // ベースカラー
                //==================================================
                half4 baseColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        IN.uv
                    ) * _BaseColor;

                //==================================================
                // 法線
                //==================================================
                float3 normalWS = normalize(IN.normalWS);

                //==================================================
                //  全ライトの中で最大の光量を取得
                //==================================================
                float lighting = 0.0;
            
                //==================================================
                // メインライト
                //==================================================
                Light mainLight = GetMainLight();

                float3 mainLightDir = normalize(mainLight.direction);       // メインライトの方向を正規化
                float mainNdotL = saturate(dot(normalWS, mainLightDir));    // 法線とライトの方向の内積を計算して、0～1に収める
                float3 mainLightColor = mainLight.color.rgb;                // メインライトの色を取得
                float lightLuminance = dot(mainLight.color.rgb, float3(0.2126, 0.7152, 0.0722));    // メインライトの輝度を計算
                float mainLighting = mainNdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation * lightLuminance;

                //最大値で比較して、メインライトの光量を優先する
                if(1)
                {
                    lighting = max(lighting, mainLighting);
                }
                
               //==================================================
               // 追加ライト
               //==================================================
               InputData inputData = (InputData)0;
               
               inputData.positionWS = IN.positionWS;
               inputData.normalWS = normalWS;
               inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
               inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
               float3 strongestLightColor = mainLight.color.rgb;    // 最も強い光の色をmainLightで初期化
               float strongestLighting = mainLighting;              // 最も強い光の光量をmainLightで初期化
               
               uint pixelLightCount = GetAdditionalLightsCount();
               
               LIGHT_LOOP_BEGIN(pixelLightCount)
               
                   Light light = GetAdditionalLight(lightIndex, IN.positionWS); // 追加ライトの情報を取得               
                   float3 lightDir = normalize(light.direction);    // 追加ライトの方向を正規化               
                   float NdotL = saturate(dot(normalWS, lightDir)); // 法線とライトの方向の内積を計算して、0～1に収める               
                   float3 additionalLightColor = light.color.rgb;   // 追加ライトの色を取得
                   float additionalLuminance = dot(additionalLightColor, float3(0.2126, 0.7152, 0.0722));   // 追加ライトの輝度を計算

                   // 追加ライトの光量を計算
                   float additionalLighting = NdotL * light.distanceAttenuation * light.shadowAttenuation * additionalLuminance;
               
                   // 追加ライトの光量が最も強い光の光量より大きい場合、最も強い光の色と光量を更新
                   if(additionalLighting > strongestLighting)
                   {
                       strongestLighting = additionalLighting;
                       strongestLightColor = additionalLightColor;
                   }

               LIGHT_LOOP_END

                   lighting = saturate(strongestLighting);


               //==================================================
               // 光量を0～1に収める
               //==================================================
               lighting = saturate(lighting);

               //==================================================
               // 3段階の色
               //==================================================
               half3 shadowColor = baseColor.rgb * _ShadowColor.rgb * strongestLightColor;
               half3 midColor    = baseColor.rgb * _MidColor.rgb    * strongestLightColor;
               half3 lightColor  = baseColor.rgb * _LightColor.rgb  * strongestLightColor;

               //==================================================
               // 最終的な光量を3段階に分ける
               //==================================================
               half3 finalColor;

               if (lighting < _ShadowThreshold)
               {
                   finalColor = shadowColor;
               }
               else if (lighting < _LightThreshold)
               {
                   finalColor = midColor;
               }
               else
               {
                   finalColor = lightColor;
               }

               return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
}


/*

               Main Light
                    │
                    ▼
              mainLighting
                    │
                    ▼
                lighting
                    │
                    │ max()
                    │
       ┌────────────┴────────────┐
       │                         │
 Point Light 1              Point Light 2
       │                         │
       ▼                         ▼
additionalLighting         additionalLighting
       │                         │
       └────────── max() ────────┘
                    │
                    ▼
              lighting = 0～1
                    │
          ┌─────────┼─────────┐
          ▼         ▼         ▼
       Shadow      Mid       Light]



*/