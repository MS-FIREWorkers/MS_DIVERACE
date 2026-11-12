
 // ==================================================
 // File: Fog.shader
 // Created: 2026/10/22
 // Author: MS41
 // Summary: Linear / Exponential / ExponentialSquared距離フォグ
 // ==================================================

 Shader "MS41/PostProcess/Fog"
 {
     Properties
     {
         _FogMode("Fog Mode", Float) = 0
         _FogColor("Fog Color", Color) = (0.65, 0.7, 0.75, 1)
         _FogStart("Fog Start", Float) = 10
         _FogEnd("Fog End", Float) = 100
         _FogDensity("Fog Density", Range(0, 0.2)) = 0.03
         _FogIntensity("Fog Intensity", Range(0, 1)) = 1
     }

     SubShader
     {
         Tags
         {
             "RenderPipeline" = "UniversalPipeline"
             "RenderType" = "Opaque"
         }

         ZTest Always
         ZWrite Off
         Cull Off

         Pass
         {
             Name "DistanceFog"

             HLSLPROGRAM

             #pragma vertex Vert
             #pragma fragment Frag

             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
             #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
             #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

             CBUFFER_START(UnityPerMaterial)
                 float _FogMode;
                 float4 _FogColor;
                 float _FogStart;
                 float _FogEnd;
                 float _FogDensity;
                 float _FogIntensity;
             CBUFFER_END

             // ==================================================
             // Fog Functions
             // ==================================================

             float CalculateFogFactor(float eyeDepth)
             {
                 float distanceFromStart = max(
                     eyeDepth - _FogStart,
                     0.0
                 );

                 float fogFactor = 0.0;

                 // Linear
                 if (_FogMode < 0.5)
                 {
                     float fogRange = max(
                         _FogEnd - _FogStart,
                         0.001
                     );

                     fogFactor = saturate(
                         distanceFromStart / fogRange
                     );
                 }
                 // Exponential
                 else if (_FogMode < 1.5)
                 {
                     fogFactor = 1.0 - exp(
                         -distanceFromStart * _FogDensity
                     );
                 }
                 // Exponential Squared
                 else
                 {
                     float d =
                         distanceFromStart * _FogDensity;

                     fogFactor = 1.0 - exp(-(d * d));
                 }

                 return saturate(
                     fogFactor * _FogIntensity
                 );
             }

             // ==================================================
             // Fragment
             // ==================================================

             half4 Frag(Varyings input) : SV_Target
             {
                 float2 uv = input.texcoord;

                 // 元の画面色
                 half4 sceneColor = SAMPLE_TEXTURE2D_X(
                     _BlitTexture,
                     sampler_LinearClamp,
                     uv
                 );

                 // 深度テクスチャ
                 float rawDepth = SampleSceneDepth(uv);

                 // 深度のない背景はFog対象外
                 #if UNITY_REVERSED_Z
                     if (rawDepth <= 0.00001)
                         return sceneColor;
                 #else
                     if (rawDepth >= 0.99999)
                         return sceneColor;
                 #endif

                 // カメラ前方方向の深度
                 float eyeDepth = LinearEyeDepth(
                     rawDepth,
                     _ZBufferParams
                 );

                 // Fog係数
                 float fogFactor =
                     CalculateFogFactor(eyeDepth);

                 // Fog合成
                 sceneColor.rgb = lerp(
                     sceneColor.rgb,
                     (half3)_FogColor.rgb,
                     fogFactor
                 );

                 return sceneColor;
             }

             ENDHLSL
         }
     }
 }
