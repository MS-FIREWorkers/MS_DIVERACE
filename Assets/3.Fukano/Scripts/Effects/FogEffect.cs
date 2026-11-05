// ==================================================
// File: FogEffect.cs
// Created: 2026/10/22
// Author: Fukano
// Summary: 距離フォグの設定と描画処理
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

// ==================================================
// FogEffect
// ==================================================

[CreateAssetMenu(
    fileName = "FogEffect",
    menuName = "MS41/Rendering/Post Effects/Fog")]
public class FogEffect : CustomPostEffect
{
    // ==================================================
    // Enums
    // ==================================================

    public enum FogMode
    {
        Linear = 0,
        Exponential = 1,
        ExponentialSquared = 2
    }

    // ==================================================
    // Variables
    // ==================================================

    [Header("Fog Settings")]
    [Tooltip("距離フォグの計算方式")]
    [SerializeField]
    private FogMode _fogMode = FogMode.Linear;

    [Tooltip("霧の色")]
    [SerializeField]
    private Color _fogColor =
        new Color(0.65f, 0.7f, 0.75f, 1.0f);

    [Header("Distance Settings")]
    [Tooltip("霧が発生し始める距離")]
    [SerializeField, Min(0.0f)]
    private float _startDistance = 10.0f;

    [Tooltip("Linear Fogで霧が最大になる距離")]
    [SerializeField, Min(0.0f)]
    private float _endDistance = 100.0f;

    [Header("Fog Strength Settings")]
    [Tooltip("指数型フォグの距離減衰係数。Linearでは使用しない")]
    [SerializeField, Range(0.0f, 0.2f)]
    private float _density = 0.03f;

    [Tooltip("最終的な霧の適用強度")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _intensity = 1.0f;

    private static readonly int FogModeId = Shader.PropertyToID("_FogMode");
    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int FogStartId = Shader.PropertyToID("_FogStart");
    private static readonly int FogEndId = Shader.PropertyToID("_FogEnd");
    private static readonly int FogDensityId = Shader.PropertyToID("_FogDensity");
    private static readonly int FogIntensityId = Shader.PropertyToID("_FogIntensity");
    public override bool RequiresDepth => true;

    // ==================================================
    // Custom Functions
    // ==================================================

    public override bool IsActive()
    {
        return base.IsActive() &&
               _intensity > 0.0f &&
               (_fogMode == FogMode.Linear ||
                _density > 0.0f);
    }

    public override void SetupMaterial()
    {
        Material material = EffectMaterial;

        if (material == null)
        {
            Debug.LogError(
                "[Error] FogEffect: Material is null.");
            return;
        }

        material.SetFloat(
            FogModeId,
            (float)_fogMode
        );

        material.SetColor(
            FogColorId,
            _fogColor
        );

        material.SetFloat(
            FogStartId,
            Mathf.Max(0.0f, _startDistance)
        );

        material.SetFloat(
            FogEndId,
            Mathf.Max(
                _startDistance + 0.001f,
                _endDistance
            )
        );

        material.SetFloat(
            FogDensityId,
            Mathf.Max(0.0f, _density)
        );

        material.SetFloat(
            FogIntensityId,
            Mathf.Clamp01(_intensity)
        );
    }

    // ==================================================
    // Runtime Control
    // ==================================================

    // ----- Properties -----

    public float Intensity => _intensity;

    // ----- Parameters -----

    public void SetFogParameters(
        FogMode fogMode,
        Color fogColor,
        float startDistance,
        float endDistance,
        float density)
    {
        _fogMode = fogMode;
        _fogColor = fogColor;

        _startDistance = Mathf.Max(0.0f, startDistance);

        _endDistance = Mathf.Max(
            _startDistance + 0.001f,
            endDistance
        );

        _density = Mathf.Clamp(density, 0.0f, 0.2f);
    }

    // ----- Intensity -----

    public void SetIntensity(float intensity)
    {
        _intensity = Mathf.Clamp01(intensity);
    }
    // ==================================================
    // Render Graph Functions
    // ==================================================

    public override TextureHandle RecordEffect(
        RenderGraph renderGraph,
        TextureHandle source,
        TextureHandle depth,
        RenderTextureDescriptor descriptor,
        int effectIndex)
    {
        if (!source.IsValid() || !depth.IsValid())
            return source;

        Material material = EffectMaterial;

        if (material == null || material.passCount < 1)
            return source;

        SetupMaterial();

        return CustomPostProcessBlitter.AddBlitPass(
            renderGraph,
            source,
            depth,
            descriptor,
            material,
            0,
            $"Fog_{effectIndex}_{name}"
        );
    }
}
