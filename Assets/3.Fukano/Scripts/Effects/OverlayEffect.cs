// ==================================================
// File: OverlayEffect.cs
// Created: 2026/10/21
// Author: Fukano
// Summary: 画面全体に色を合成するポストエフェクト
// ==================================================

using UnityEngine;

// ==================================================
// OverlayEffect
// ==================================================

[CreateAssetMenu(
    fileName = "OverlayEffect",
    menuName = "MS41/Rendering/Post Effects/Overlay")]
public class OverlayEffect : CustomPostEffect
{
    // ==================================================
    // Enum
    // ==================================================

    public enum BlendMode
    {
        Normal = 0,
        Additive = 1,
        Multiply = 2
    }

    // ==================================================
    // Variables
    // ==================================================

    [Header("Overlay Settings")]
    [Tooltip("画面に上塗りする色")]
    [SerializeField]
    private Color _overlayColor = Color.white;

    [Tooltip("上塗りの強度")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _intensity = 0.5f;

    [Tooltip("色の合成方法")]
    [SerializeField]
    private BlendMode _blendMode = BlendMode.Normal;

    // ==================================================
    // Custom Functions
    // ==================================================

    // エフェクトが有効か確認
    public override bool IsActive()
    {
        if (!base.IsActive())
            return false;

        return _intensity > 0.0f;
    }

    // Shaderへパラメータを設定
    public override void SetupMaterial()
    {
        Material material = EffectMaterial;

        if (material == null)
            return;

        material.SetColor(
            "_OverlayColor",
            _overlayColor
        );

        material.SetFloat(
            "_Intensity",
            _intensity
        );

        material.SetFloat(
            "_BlendMode",
            (float)_blendMode
        );
    }

    // ==================================================
    // Runtime Control
    // ==================================================

    // ----- Properties -----

    public float Intensity => _intensity;

    // ----- Set Parameters -----

    public void SetOverlayParameters(
        Color overlayColor,
        BlendMode blendMode)
    {
        _overlayColor = overlayColor;
        _blendMode = blendMode;
    }

    // ----- Set Intensity -----

    public void SetIntensity(float intensity)
    {
        _intensity = Mathf.Clamp01(intensity);
    }
}