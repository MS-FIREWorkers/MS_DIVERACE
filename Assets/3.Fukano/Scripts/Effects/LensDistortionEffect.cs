// ==================================================
// File: LensDistortionEffect.cs
// Created: 2026/10/22
// Author: Fukano
// Summary: レンズ歪みの設定と適用範囲の制御
// ==================================================

using UnityEngine;

// ==================================================
// LensDistortionEffect
// ==================================================

[CreateAssetMenu(
    fileName = "LensDistortionEffect",
    menuName = "MS41/Rendering/Post Effects/Lens Distortion")]
public class LensDistortionEffect : CustomPostEffect
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Lens Distortion Settings -----

    [Header("Lens Distortion Settings")]
    [Tooltip("歪みの強度。正の値で樽型、負の値で糸巻き型")]
    [SerializeField, Range(-1.0f, 1.0f)]
    private float _intensity = 0.0f;

    [Tooltip("歪みの中心座標。通常は(0.5, 0.5)")]
    [SerializeField]
    private Vector2 _center = new Vector2(0.5f, 0.5f);

    [Tooltip("UVの拡大縮小。1で標準")]
    [SerializeField, Range(0.1f, 2.0f)]
    private float _scale = 1.0f;

    // ----- Range Settings -----

    public enum RangeMode
    {
        Full = 0,
        Center = 1,
        Outer = 2,
        Ring = 3
    }

    [Header("Range Settings")]
    [Tooltip("レンズ歪みを適用する範囲")]
    [SerializeField]
    private RangeMode _rangeMode = RangeMode.Full;

    [Tooltip("歪みを適用する範囲の半径")]
    [SerializeField, Range(0.0f, 2.0f)]
    private float _rangeRadius = 0.5f;

    [Tooltip("Ringモードで使用する内側の半径")]
    [SerializeField, Range(0.0f, 2.0f)]
    private float _innerRadius = 0.2f;

    [Tooltip("歪みの境界を滑らかにする幅")]
    [SerializeField, Range(0.0f, 0.5f)]
    private float _edgeSoftness = 0.05f;

    // ----- Shader Property IDs -----
    private static readonly int IntensityId = Shader.PropertyToID("_DistortionIntensity");
    private static readonly int CenterId = Shader.PropertyToID("_DistortionCenter");
    private static readonly int ScaleId = Shader.PropertyToID("_DistortionScale");
    private static readonly int RangeModeId = Shader.PropertyToID("_RangeMode");
    private static readonly int RangeRadiusId = Shader.PropertyToID("_RangeRadius");
    private static readonly int InnerRadiusId = Shader.PropertyToID("_InnerRadius");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");

    // ==================================================
    // Custom Functions
    // ==================================================

    public override bool IsActive()
    {
        return base.IsActive() &&
               Mathf.Abs(_intensity) > 0.0001f;
    }

    public override void SetupMaterial()
    {
        Material material = EffectMaterial;

        if (material == null)
        {
            Debug.LogError(
                "[Error] LensDistortionEffect: Material is null.");
            return;
        }

        // レンズ歪み設定
        material.SetFloat(IntensityId, _intensity);
        material.SetVector(CenterId, _center);
        material.SetFloat(ScaleId, _scale);

        // 適用範囲設定
        material.SetFloat(RangeModeId, (float)_rangeMode);
        material.SetFloat(RangeRadiusId, _rangeRadius);
        material.SetFloat(InnerRadiusId, Mathf.Min(_innerRadius, _rangeRadius));
        material.SetFloat(EdgeSoftnessId, _edgeSoftness);
    }

    // ----- Set Parameters -----

    // ----- Set Parameters -----

    public void SetLensDistortionParameters(
        Vector2 center,
        float scale,
        RangeMode rangeMode,
        float rangeRadius,
        float innerRadius,
        float edgeSoftness)
    {
        // ----- Distortion Settings -----

        _center = new Vector2(
            Mathf.Clamp01(center.x),
            Mathf.Clamp01(center.y)
        );

        _scale = Mathf.Clamp(scale, 0.1f, 2.0f);

        // ----- Range Settings -----

        _rangeMode = rangeMode;
        _rangeRadius = Mathf.Clamp(rangeRadius, 0.0f, 2.0f);
        _innerRadius = Mathf.Clamp(innerRadius, 0.0f, _rangeRadius);
        _edgeSoftness = Mathf.Clamp(edgeSoftness, 0.0f, 0.5f);
    }
    // ==================================================
    // Runtime Control
    // ==================================================

    // ----- Properties -----

    public float Intensity => _intensity;

    // ----- Set Intensity -----

    public void SetIntensity(float intensity)
    {
        _intensity = Mathf.Clamp(intensity, -1.0f, 1.0f);
    }
}