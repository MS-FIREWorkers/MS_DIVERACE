// ==================================================
// File: CustomPostEffect.cs
// Created: 2026/10/21
// Author: MS41
// Summary: ポストエフェクト共通基底クラス
// ==================================================

using UnityEngine;

// ==================================================
// CustomPostEffect
// ==================================================

public abstract class CustomPostEffect : ScriptableObject
{
    // ==================================================
    // Variables
    // ==================================================

    [Header("Effect Settings")]
    [Tooltip("エフェクトを有効にするか")]
    [SerializeField]
    private bool _isEnabled = true;

    [Tooltip("エフェクトの元となるMaterial")]
    [SerializeField]
    private Material _material;

    // Effectアセット専用の実行用Material
    [System.NonSerialized]
    private Material _runtimeMaterial;

    [System.NonSerialized]
    private Material _cachedSourceMaterial;

    public bool IsEnabled => _isEnabled;

    public Material EffectMaterial => GetRuntimeMaterial();

    // ==================================================
    // Unity Functions
    // ==================================================

    protected virtual void OnDisable()
    {
        ReleaseMaterial();
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // エフェクトが実行可能か確認
    public virtual bool IsActive()
    {
        return _isEnabled && _material != null;
    }

    // 実行用Materialを取得
    public Material GetRuntimeMaterial()
    {
        if (_material == null)
        {
            ReleaseMaterial();
            return null;
        }

        // 元Materialが変更された場合は再生成
        if (_runtimeMaterial != null &&
            _cachedSourceMaterial != _material)
        {
            ReleaseMaterial();
        }

        if (_runtimeMaterial == null)
        {
            _runtimeMaterial = new Material(_material)
            {
                name = $"{name}_RuntimeMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };

            _cachedSourceMaterial = _material;
        }

        return _runtimeMaterial;
    }

    // Shaderパラメータ設定
    public virtual void SetupMaterial()
    {
    }

    // 使用するShader Pass番号
    public virtual int GetMaterialPass()
    {
        return 0;
    }

    // 実行用Materialを解放
    private void ReleaseMaterial()
    {
        if (_runtimeMaterial == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(_runtimeMaterial);
        }

        _runtimeMaterial = null;
        _cachedSourceMaterial = null;
    }
}