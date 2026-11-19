// ==================================================
// File: GaussianBlurEffect.cs
// Created: 2026/10/21
// Author: Fukano
// Summary: ガウシアンブラーの設定と複数Pass登録
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

// ==================================================
// GaussianBlurEffect
// ==================================================

[CreateAssetMenu(
    fileName = "GaussianBlurEffect",
    menuName = "MS41/Rendering/Post Effects/Gaussian Blur")]
public class GaussianBlurEffect : CustomPostEffect
{
    // ==================================================
    // Variables
    // ==================================================

    [Header("Gaussian Blur Settings")]
    [Tooltip("ぼかしの半径。大きいほど広範囲をぼかす")]
    [SerializeField, Range(0.0f, 10.0f)]
    private float _blurRadius = 2.0f;

    [Tooltip("ぼかしの強度。半径に乗算される")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _blurStrength = 1.0f;

    [Tooltip("横方向と縦方向のぼかしを繰り返す回数")]
    [SerializeField, Range(1, 8)]
    private int _iterations = 1;

    private const int HorizontalPass = 0;
    private const int VerticalPass = 1;
    private const int RequiredPassCount = 2;

    private static readonly int BlurRadiusId =
        Shader.PropertyToID("_BlurRadius");

    // Gaussian BlurはDepth Textureを使用しない
    public override bool RequiresDepth => false;

    // ==================================================
    // Custom Functions
    // ==================================================

    // エフェクトが実行可能か確認
    public override bool IsActive()
    {
        return base.IsActive() &&
               _blurRadius > 0.0f &&
               _blurStrength > 0.0f;
    }

    // Shaderパラメータを設定
    public override void SetupMaterial()
    {
        Material material = EffectMaterial;

        if (material == null)
        {
            Debug.LogError(
                "[Error] GaussianBlurEffect: Material is null.");
            return;
        }

        float radius = Mathf.Clamp(
            _blurRadius * _blurStrength,
            0.0f,
            10.0f
        );

        material.SetFloat(BlurRadiusId, radius);
    }

    // ==================================================
    // Render Graph Functions
    // ==================================================

    // 横方向と縦方向のブラーを順番に登録
    public override TextureHandle RecordEffect(
        RenderGraph renderGraph,
        TextureHandle source,
        RenderTextureDescriptor descriptor,
        int effectIndex)
    {
        if (!source.IsValid())
            return source;

        Material material = EffectMaterial;

        if (material == null)
            return source;

        if (material.passCount < RequiredPassCount)
        {
            Debug.LogError(
                "[Error] GaussianBlurEffect: " +
                "Shader requires at least 2 passes.");
            return source;
        }

        SetupMaterial();

        TextureHandle currentSource = source;

        int iterationCount = Mathf.Clamp(
            _iterations,
            1,
            8
        );

        for (int i = 0; i < iterationCount; i++)
        {
            // 横方向
            currentSource = AddBlurPass(
                renderGraph,
                currentSource,
                descriptor,
                material,
                HorizontalPass,
                $"GaussianBlur_H_{effectIndex}_{i}"
            );

            // 縦方向
            currentSource = AddBlurPass(
                renderGraph,
                currentSource,
                descriptor,
                material,
                VerticalPass,
                $"GaussianBlur_V_{effectIndex}_{i}"
            );
        }

        return currentSource;
    }

    // ==================================================
    // Internal Functions
    // ==================================================

    // 指定方向のブラーPassを登録
    private TextureHandle AddBlurPass(
        RenderGraph renderGraph,
        TextureHandle source,
        RenderTextureDescriptor descriptor,
        Material material,
        int materialPass,
        string passName)
    {
        if (!source.IsValid())
            return source;

        return CustomPostProcessBlitter.AddBlitPass(
            renderGraph,
            source,
            descriptor,
            material,
            materialPass,
            passName
        );
    }

    // ==================================================
    // Runtime Control
    // ==================================================

    // ----- Properties -----

    public float BlurStrength => _blurStrength;

    // ----- Set Parameters -----

    public void SetBlurParameters(
        float blurRadius,
        int iterations)
    {
        _blurRadius = Mathf.Clamp(
            blurRadius,
            0.0f,
            10.0f
        );

        _iterations = Mathf.Clamp(
            iterations,
            1,
            8
        );
    }

    // ----- Set Strength -----

    public void SetBlurStrength(float strength)
    {
        _blurStrength = Mathf.Clamp01(strength);
    }
}
