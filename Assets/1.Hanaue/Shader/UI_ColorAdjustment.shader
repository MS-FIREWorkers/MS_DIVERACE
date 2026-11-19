/*
    UI_ColorAdjustment.shader
    20261117  hanaue sho
    Unity 6 / URP
*/

Shader "Custom/UI/ColorAdjustment"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        [Header(Color Adjustment)]
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,3)) = 1.0    // １：元の明るさ
        _Contrast ("Contrast", Range(0,3)) = 1.0        // １：元のコントラスト
        _Saturation ("Saturation", Range(0,3)) = 1.0    // １：元の彩度、０：モノクロ
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;

            float4 _BaseColor;
            float _Brightness;
            float _Contrast;
            float _Saturation;

            // ==========================================
            // Vertex Shader
            // ==========================================
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // ==========================================
            // Fragment Shader
            // ==========================================
            fixed4 frag(v2f i) : SV_Target
            {
                // 元画像
                fixed4 tex = tex2D(_MainTex, i.uv);

                // ベースカラー
                float3 color = tex.rgb * _BaseColor.rgb;

                // ----- Brightness -----
                color *= _Brightness;

                // ----- Contrast -----
                // 0.5を基準に明暗差を調整
                color = (color - 0.5) * _Contrast + 0.5;

                // ----- Saturation -----
                // 輝度を計算
                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));

                // グレースケールと元の色を補間
                color = lerp(float3(luminance, luminance, luminance), color, _Saturation);

                // ----- Final Color -----
                float alpha = tex.a * _BaseColor.a * i.color.a;

                return fixed4(color * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
