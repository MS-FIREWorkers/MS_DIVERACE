Shader "MS_DIVERACE/SpeedDistortion"
{
    Properties
    {
        _EdgeWidthPixels ("Effect Width (Pixels)", Range(2, 64)) = 24
        _BlurLengthPixels ("Blur Length (Pixels)", Range(0, 64)) = 12
        _BlurMix ("Blur Strength", Range(0, 1)) = 1
        _WarpPixels ("Distortion Pixels", Range(0, 40)) = 15
        _DebugMask ("Debug Mask", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "MaskedSpeedDistortion"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_PlayerScreenMask);

            float _EdgeWidthPixels;
            float _BlurLengthPixels;
            float _BlurMix;
            float _WarpPixels;
            float _DebugMask;

            float _SpeedDistortionStrength;
            float4 _SpeedDistortionCenter;

            float GetMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(
                    _PlayerScreenMask,
                    sampler_LinearClamp,
                    saturate(uv),
                    0
                ).r;
            }

            float3 GetSceneColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    saturate(uv),
                    _BlitMipLevel
                ).rgb;
            }

            float GetNearbyMask(float2 uv, float2 radius)
            {
                float2 diagonal = radius * 0.70710678;

                float result = 0.0;

                result = max(result, GetMask(uv + float2(radius.x, 0.0)));
                result = max(result, GetMask(uv - float2(radius.x, 0.0)));
                result = max(result, GetMask(uv + float2(0.0, radius.y)));
                result = max(result, GetMask(uv - float2(0.0, radius.y)));

                result = max(result, GetMask(uv + diagonal));
                result = max(result, GetMask(uv - diagonal));
                result = max(result, GetMask(uv + float2(diagonal.x, -diagonal.y)));
                result = max(result, GetMask(uv + float2(-diagonal.x, diagonal.y)));

                return result;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float3 original = GetSceneColor(uv);

                float currentMask = GetMask(uv);

                // Mask確認用
                if (_DebugMask > 0.5)
                {
                    return half4(currentMask, currentMask, currentMask, 1.0);
                }

                // プレイヤー本体は歪ませない
                if (currentMask > 0.1 || _SpeedDistortionStrength <= 0.00001)
                {
                    return half4(original, 1.0);
                }

                float2 pixelSize = 1.0 / _ScaledScreenParams.xy;
                float2 outerRadius = pixelSize * _EdgeWidthPixels;
                float2 innerRadius = outerRadius * 0.45;

                float nearMask = GetNearbyMask(uv, innerRadius);
                float farMask = GetNearbyMask(uv, outerRadius);

                float effectMask = saturate(nearMask + farMask * 0.4);

                if (effectMask < 0.001)
                {
                    return half4(original, 1.0);
                }

                float aspect = _ScaledScreenParams.x / _ScaledScreenParams.y;

                float2 direction = uv - _SpeedDistortionCenter.xy;
                direction.x *= aspect;
                direction = normalize(direction + float2(0.00001, 0.00001));

                float2 blurStep = float2(direction.x / aspect, direction.y);
                blurStep *= _BlurLengthPixels / _ScaledScreenParams.y;

                // プレイヤー内部を引っ張らないようにする
                float2 uv1 = saturate(uv + blurStep * 0.35);
                float2 uv2 = saturate(uv + blurStep * 0.70);
                float2 uv3 = saturate(uv + blurStep * 1.00);

                float3 color1 = (GetMask(uv1) > 0.1) ? original : GetSceneColor(uv1);
                float3 color2 = (GetMask(uv2) > 0.1) ? original : GetSceneColor(uv2);
                float3 color3 = (GetMask(uv3) > 0.1) ? original : GetSceneColor(uv3);

                float3 blurred =
                    original * 0.25 +
                    color1   * 0.30 +
                    color2   * 0.25 +
                    color3   * 0.20;

                float speedFactor = saturate(_SpeedDistortionStrength * 60.0);
                float mixAmount = saturate(effectMask * _BlurMix * speedFactor);

                // 波打つ歪み
                float wave =
                    sin(uv.y * 90.0 - _Time.y * 16.0) *
                    cos(uv.x * 55.0 + _Time.y * 9.0);

                float2 warpDirection = float2(direction.x / aspect, direction.y);

                float2 warpedUV = saturate(
                    uv + warpDirection * wave *
                    (_WarpPixels / _ScaledScreenParams.y) * speedFactor
                );

                float3 warpedColor = (GetMask(warpedUV) > 0.1)
                    ? original
                    : GetSceneColor(warpedUV);

                float3 combinedColor = lerp(blurred, warpedColor, 0.85);

                return half4(lerp(original, combinedColor, mixAmount), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}