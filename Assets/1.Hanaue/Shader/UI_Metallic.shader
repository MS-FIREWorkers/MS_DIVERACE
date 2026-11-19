/*
    UI_Metallic.shader
    20261117  hanaue sho
    Unity 6 / URP
*/


Shader "Custom/UI/Metallic"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        [Header(Base Color)]
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,3)) = 1.5

        [Header(Highlight)]
        _HighlightColor ("Highlight Color", Color) = (1,1,1,1)
        _HighlightIntensity ("Intensity", Range(0,5)) = 1.5
        _HighlightWidth ("Width", Range(0.01,0.5)) = 0.15
        _HighlightAngle ("Angle", Range(-180,180)) = 15
        _HighlightDuration ("Duration", Float) = 1.0
        _HighlightInterval ("Interval", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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

            float4 _HighlightColor;
            float _HighlightIntensity;
            float _HighlightWidth;
            float _HighlightAngle;
            float _HighlightDuration;
            float _HighlightInterval;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 元画像
                fixed4 tex = tex2D(_MainTex, i.uv);

                // ベースカラー
                float3 color = tex.rgb * _BaseColor.rgb * _Brightness;
                float alpha = tex.a * _BaseColor.a * i.color.a;

                // アニメーション周期
                float interval = max(_HighlightInterval, 0.001);
                float duration = max(_HighlightDuration, 0.001);
                float cycleTime = fmod(_Time.y, interval);

                // ハイライト
                float highlight = 0.0;

                if (cycleTime < duration)
                {
                    float progress = cycleTime / duration;

                    // 角度をラジアンに変換
                    float angle = radians(_HighlightAngle);

                    // 光の帯の法線方向
                    float2 direction = float2(cos(angle), sin(angle));

                    // UVを中心基準に変換
                    float2 uv = i.uv - 0.5;

                    // 指定角度に沿った座標
                    float pos = dot(uv, direction);

                    // 光の帯が完全に通過する範囲
                    float extent = (abs(direction.x) + abs(direction.y)) * 0.5 + _HighlightWidth;

                    // 光の移動
                    float center = lerp(-extent, extent, progress);

                    // ハイライト
                    float dist = abs(pos - center);
                    highlight = 1.0 - smoothstep(0.0, _HighlightWidth, dist);
                }

                // 光を加算
                color += _HighlightColor.rgb * highlight * _HighlightIntensity;

                return fixed4(color * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
