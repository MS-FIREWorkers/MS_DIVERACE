// ==================================================
// File: PostEffectManager.GaussianBlur.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: GaussianBlurの公開APIと実行時操作
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

    public static void PlayGaussianBlur(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float blurRadius,
        float blurStrength,
        int iterations = 1)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartGaussianBlurEffectInternal(
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            blurRadius,
            blurStrength,
            iterations
        );
    }

    public static void StopGaussianBlur()
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopGaussianBlurEffectInternal();
    }


    // ==================================================
    // Gaussian Blur By ID Public API
    // ==================================================

    // ----- Play Gaussian Blur By ID -----

    public static void PlayGaussianBlur(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float blurRadius,
        float blurStrength,
        int iterations = 1)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartGaussianBlurByIdInternal(
            effectId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            blurRadius,
            blurStrength,
            iterations
        );
    }

    // ----- Stop Gaussian Blur By ID -----

    public static void StopGaussianBlur(string effectId)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopGaussianBlurByIdInternal(effectId);
    }


    // ==================================================
    // Gaussian Blur Effect Control
    // ==================================================

    // ----- Start Default Gaussian Blur -----

    private void StartGaussianBlurEffectInternal(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float blurRadius,
        float blurStrength,
        int iterations)
    {
        StartGaussianBlurByIdInternal(
            DefaultGaussianBlurId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            blurRadius,
            blurStrength,
            iterations
        );
    }

    // ----- Stop Default Gaussian Blur -----

    private void StopGaussianBlurEffectInternal()
    {
        _runtimeController?.Stop<GaussianBlurEffect>(DefaultGaussianBlurId);
    }



    // ==================================================
    // Multiple Gaussian Blur Control
    // ==================================================

    // ----- Start Gaussian Blur By ID -----

    private void StartGaussianBlurByIdInternal(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float blurRadius,
        float blurStrength,
        int iterations)
    {
        // ----- Get Source Effect -----

        GaussianBlurEffect sourceEffect =
            GetEffectInternal<GaussianBlurEffect>();

        if (sourceEffect == null || _runtimeController == null)
            return;

        // ----- Play Runtime Effect -----

        _runtimeController.Play(
            effectId,
            sourceEffect,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            Mathf.Clamp01(blurStrength),

            // ----- Setup Parameters -----

            effect =>
            {
                effect.SetBlurParameters(
                    blurRadius,
                    iterations
                );
            },

            // ----- Apply Strength -----

            (effect, value) =>
            {
                effect.SetBlurStrength(value);
            }
        );
    }

    // ----- Stop Gaussian Blur By ID -----

    private void StopGaussianBlurByIdInternal(string effectId)
    {
        _runtimeController?.Stop<GaussianBlurEffect>(effectId);
    }


}
