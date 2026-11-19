// ==================================================
// File: PostEffectManager.LensDistortion.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: LensDistortionの公開APIと実行時操作
// ==================================================

using UnityEngine;

// ==================================================
// PostEffectManager
// ==================================================

public partial class PostEffectManager
{
    // ==================================================
    // Static Public API
    // ==================================================

    public static void PlayLensDistortion(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float scale = 1.0f,
        LensDistortionEffect.RangeMode rangeMode = LensDistortionEffect.RangeMode.Full,
        float rangeRadius = 0.5f,
        float innerRadius = 0.2f,
        float edgeSoftness = 0.05f)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartLensDistortionEffectInternal(
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            intensity,
            center,
            scale,
            rangeMode,
            rangeRadius,
            innerRadius,
            edgeSoftness
        );
    }

    public static void StopLensDistortion()
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopLensDistortionEffectInternal();
    }


    // ==================================================
    // Lens Distortion By ID Public API
    // ==================================================

    // ----- Play Lens Distortion By ID -----

    public static void PlayLensDistortion(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float scale = 1.0f,
        LensDistortionEffect.RangeMode rangeMode =
            LensDistortionEffect.RangeMode.Full,
        float rangeRadius = 0.5f,
        float innerRadius = 0.2f,
        float edgeSoftness = 0.05f)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartLensDistortionByIdInternal(
            effectId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            intensity,
            center,
            scale,
            rangeMode,
            rangeRadius,
            innerRadius,
            edgeSoftness
        );
    }

    // ----- Stop Lens Distortion By ID -----

    public static void StopLensDistortion(string effectId)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopLensDistortionByIdInternal(effectId);
    }


    // ==================================================
    // Lens Distortion Effect Control
    // ==================================================

    // ----- Start Default Lens Distortion -----

    private void StartLensDistortionEffectInternal(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float scale,
        LensDistortionEffect.RangeMode rangeMode,
        float rangeRadius,
        float innerRadius,
        float edgeSoftness)
    {
        StartLensDistortionByIdInternal(
            DefaultLensDistortionId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            intensity,
            center,
            scale,
            rangeMode,
            rangeRadius,
            innerRadius,
            edgeSoftness
        );
    }

    // ----- Stop Default Lens Distortion -----

    private void StopLensDistortionEffectInternal()
    {
        StopLensDistortionByIdInternal(DefaultLensDistortionId);
    }



    // ==================================================
    // Multiple Lens Distortion Control
    // ==================================================

    // ----- Start Lens Distortion By ID -----

    private void StartLensDistortionByIdInternal(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float scale,
        LensDistortionEffect.RangeMode rangeMode,
        float rangeRadius,
        float innerRadius,
        float edgeSoftness)
    {
        // ----- Get Source Effect -----

        LensDistortionEffect sourceEffect =
            GetEffectInternal<LensDistortionEffect>();

        if (sourceEffect == null || _runtimeController == null)
            return;

        // ----- Play Runtime Effect -----

        _runtimeController.Play(
            effectId,
            sourceEffect,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            Mathf.Clamp(intensity, -1.0f, 1.0f),

            // ----- Setup Parameters -----

            effect =>
            {
                effect.SetLensDistortionParameters(
                    center,
                    scale,
                    rangeMode,
                    rangeRadius,
                    innerRadius,
                    edgeSoftness
                );
            },

            // ----- Apply Intensity -----

            (effect, value) =>
            {
                effect.SetIntensity(value);
            }
        );
    }

    // ----- Stop Lens Distortion By ID -----

    private void StopLensDistortionByIdInternal(string effectId)
    {
        _runtimeController?.Stop<LensDistortionEffect>(effectId);
    }

}
