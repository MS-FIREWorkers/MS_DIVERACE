// ==================================================
// File: RadialBlurEffect.cs
// Created: 2026/10/28
// Author: Fukano
// Summary: ラジアルブラーの設定とマテリアル制御
// ==================================================

using UnityEngine;

// ==================================================
// RadialBlurEffect
// ==================================================

[CreateAssetMenu(
    fileName = "RadialBlurEffect",
    menuName = "MS41/Rendering/Post Effects/Radial Blur")]
public class RadialBlurEffect : CustomPostEffect
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Blur Settings -----

    [Header("Radial Blur Settings")]

    [Tooltip("ブラーの強度")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _intensity = 0.3f;

    [Tooltip("ブラーの中心座標")]
    [SerializeField]
    private Vector2 _center = new Vector2(0.5f, 0.5f);

    [Tooltip("中央のブラーを適用しない半径")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _innerRadius = 0.1f;

    [Tooltip("ブラーの適用終了半径")]
    [SerializeField, Range(0.0f, 2.0f)]
    private float _outerRadius = 1.0f;

    [Tooltip("サンプリング回数。増やすほど滑らかになるが負荷も増える")]
    [SerializeField, Range(4, 32)]
    private int _sampleCount = 12;

    [Tooltip("ブラー境界の滑らかさ")]
    [SerializeField, Range(0.001f, 0.5f)]
    private float _falloff = 0.15f;

    // ----- Shader Property IDs -----

    private static readonly int IntensityId = Shader.PropertyToID("_BlurIntensity");
    private static readonly int CenterId = Shader.PropertyToID("_BlurCenter");
    private static readonly int InnerRadiusId = Shader.PropertyToID("_InnerRadius");
    private static readonly int OuterRadiusId = Shader.PropertyToID("_OuterRadius");
    private static readonly int SampleCountId = Shader.PropertyToID("_SampleCount");
    private static readonly int FalloffId = Shader.PropertyToID("_Falloff");

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Effect State -----

    public override bool IsActive()
    {
        return base.IsActive() && _intensity > 0.0f;
    }

    // ----- Material Setup -----


    public override void SetupMaterial()
    {
        Material material = EffectMaterial;

        if (material == null)
        {
            Debug.LogError("[Error] RadialBlurEffect: Runtime Material is null.");
            return;
        }

        material.SetFloat(IntensityId, _intensity);
        material.SetVector(CenterId, new Vector4(_center.x, _center.y, 0, 0));
        material.SetFloat(InnerRadiusId, _innerRadius);
        material.SetFloat(OuterRadiusId, Mathf.Max(_outerRadius, _innerRadius + 0.001f));
        material.SetInt(SampleCountId, _sampleCount);
        material.SetFloat(FalloffId, _falloff);
    }

    // ----- Shader Pass -----

    public override int GetMaterialPass()
    {
        return 0;
    }

    // ==================================================
    // Runtime Control
    // ==================================================

    // ----- Properties -----

    public float Intensity => _intensity;

    // ----- Set Parameters -----

    // ----- Set Parameters -----

    public void SetRadialBlurParameters(
        Vector2 center,
        float innerRadius,
        float outerRadius,
        int sampleCount,
        float falloff)
    {
        _center = new Vector2(
            Mathf.Clamp01(center.x),
            Mathf.Clamp01(center.y)
        );

        _innerRadius = Mathf.Clamp01(innerRadius);
        _outerRadius = Mathf.Clamp(outerRadius, _innerRadius + 0.001f, 2.0f);
        _sampleCount = Mathf.Clamp(sampleCount, 4, 32);
        _falloff = Mathf.Clamp(falloff, 0.001f, 0.5f);
    }

    // ----- Set Intensity -----

    public void SetIntensity(float intensity)
    {
        _intensity = Mathf.Clamp01(intensity);
    }
}