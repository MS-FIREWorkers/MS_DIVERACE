// ==================================================
// File: OutlineTarget.cs
// Created: 2026/11/25
// Author: 深野大輝
// Summary: OutlineTargetコンポーネントの設定を保持する。
// ==================================================
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Rendering/Outline Target")]
public sealed class OutlineTarget : MonoBehaviour
{
    private static readonly HashSet<OutlineTarget> ActiveTargets = new();

    [SerializeField] private bool _enableOutline = true;

    [Tooltip("子として配置するPrefab向け。親とその子階層のRendererを対象にします。")]
    [SerializeField] private bool _useParentAsTarget;

    [Tooltip("輪郭線の幅。単位は描画解像度のピクセルです。")]
    [SerializeField, Range(1, 8)] private int _widthPixels = 2;

    public bool EnableOutline
    {
        get => _enableOutline;
        set => _enableOutline = value;
    }

    internal static IEnumerable<OutlineTarget> RegisteredTargets => ActiveTargets;
    internal static bool HasActiveTargets => ActiveTargets.Count != 0;
    internal int WidthPixels => Mathf.Clamp(_widthPixels, 1, 8);
    internal Transform TargetRoot => _useParentAsTarget ? transform.parent : transform;

    internal static bool HasWideTargets()
    {
        foreach (OutlineTarget target in ActiveTargets)
        {
            if (target != null && target.EnableOutline && target.TargetRoot != null && target.WidthPixels > 4)
                return true;
        }

        return false;
    }

    private void OnEnable()
    {
        ActiveTargets.Add(this);
    }

    private void OnDisable()
    {
        ActiveTargets.Remove(this);
    }
}
