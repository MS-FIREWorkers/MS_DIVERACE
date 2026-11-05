Shader "Custom/ToonShader"
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
                // メインライト取得
                //==================================================
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);

                //==================================================
                // Lambert
                //==================================================
                float NdotL = saturate(dot(normalWS, lightDir));

                //==================================================
                // Toon化
                //==================================================
                float shadowStep = step(_ShadowThreshold, NdotL);
                float lightStep = step(_LightThreshold, NdotL);
                
                //==================================================
                // 3段階の色
                //==================================================
                half3 shadowColor = baseColor.rgb * _ShadowColor.rgb;
                half3 midColor = baseColor.rgb * _MidColor.rgb;
                half3 lightColor = baseColor.rgb * _LightColor.rgb;
                
                //==================================================
                // 明暗を3段階に分ける
                //==================================================
                half3 finalColor;
                
                if (NdotL < _ShadowThreshold)
                {
                    finalColor = shadowColor;
                }
                else if (NdotL < _LightThreshold)
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
