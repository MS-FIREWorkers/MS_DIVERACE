Shader "Hidden/MS41/ToonSilhouetteComposite"
{
    Properties
    {
        _SilhouetteColor("Outline Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragSilhouette
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_OutlineWideMask);
            float _OutlineHasWideMask;

            CBUFFER_START(UnityPerMaterial)
                float4 _SilhouetteColor;
            CBUFFER_END

            half4 ReadMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
            }

            half MaxCoverage(half4 coverage)
            {
                return max(max(coverage.r, coverage.g), max(coverage.b, coverage.a));
            }

            half4 FragSilhouette(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 pixelX = ddx(uv);
                float2 pixelY = ddy(uv);
                half center = MaxCoverage(ReadMask(uv));
                bool hasWideMask = _OutlineHasWideMask > 0.5;
                if (hasWideMask)
                {
                    half4 wideCenter = SAMPLE_TEXTURE2D_X_LOD(_OutlineWideMask, sampler_PointClamp, uv, 0);
                    center = max(center, MaxCoverage(wideCenter));
                }

                if (center >= 1.0)
                    return 0.0;

                half dilated = center;
                int maxRadius = hasWideMask ? 8 : 4;
                const float4 radiusSquared = float4(1, 4, 9, 16);
                const float4 wideRadiusSquared = float4(25, 36, 49, 64);

                [loop]
                for (int y = -maxRadius; y <= maxRadius; ++y)
                {
                    [loop]
                    for (int x = -maxRadius; x <= maxRadius; ++x)
                    {
                        int distanceSquared = x * x + y * y;
                        if (distanceSquared == 0 || distanceSquared > maxRadius * maxRadius)
                            continue;

                        float2 sampleUV = uv + x * pixelX + y * pixelY;
                        if (distanceSquared <= 16)
                        {
                            half4 coverage = ReadMask(sampleUV);
                            dilated = max(dilated, MaxCoverage(coverage * step(distanceSquared, radiusSquared)));
                        }

                        if (hasWideMask)
                        {
                            half4 wideCoverage = SAMPLE_TEXTURE2D_X_LOD(_OutlineWideMask, sampler_PointClamp, sampleUV, 0);
                            dilated = max(dilated, MaxCoverage(wideCoverage * step(distanceSquared, wideRadiusSquared)));
                        }
                    }
                }

                return half4(_SilhouetteColor.rgb, _SilhouetteColor.a * (dilated - center));
            }
            ENDHLSL
        }
    }
}
