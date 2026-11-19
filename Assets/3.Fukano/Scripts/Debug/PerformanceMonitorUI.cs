// ==================================================
// File: PerformanceMonitorUI.cs
// Created: 2026/10/22
// Author: Fukano
// Summary: IMGUIを使用したパフォーマンスデバッグ表示
// ==================================================

using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// ==================================================
// PerformanceMonitorUI
// ==================================================
[DisallowMultipleComponent]
[RequireComponent(typeof(PerformanceMonitor))]
public class PerformanceMonitorUI : MonoBehaviour
{
    // ==================================================
    // Variables
    // ==================================================

    [Header("Display Settings")]
    [Tooltip("起動時にデバッグ情報を表示するか")]
    [SerializeField]
    private bool _isVisibleOnStart = true;

    [Tooltip("FPSを正常と判断するしきい値")]
    [SerializeField, Min(1f)]
    private float _goodFpsThreshold = 60f;

    [Tooltip("FPSを警告と判断するしきい値")]
    [SerializeField, Min(1f)]
    private float _warningFpsThreshold = 30f;

    [Header("Window Settings")]
    [Tooltip("表示位置")]
    [SerializeField]
    private Vector2 _windowPosition = new Vector2(10f, 10f);

    [Tooltip("ウィンドウの横幅")]
    [SerializeField, Min(200f)]
    private float _windowWidth = 350f;

    [Tooltip("文字サイズ")]
    [SerializeField, Range(10, 30)]
    private int _fontSize = 16;

    [Header("Background Settings")]
    [Tooltip("背景色と透明度")]
    [SerializeField]
    private Color _backgroundColor = new Color(0f, 0f, 0f, 0.55f);

    private Texture2D _backgroundTexture;

    private PerformanceMonitor _monitor;

    private bool _isVisible;

    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _fpsStyle;
    private GUIStyle _titleStyle;

    private string _displayText = "Collecting samples...";
    private string _fpsText = "FPS: --";

    private Color _fpsColor = Color.white;

    private readonly StringBuilder _builder = new StringBuilder(512);

    private const float WindowHeight = 365f;
    private const float Padding = 12f;
    private const float HeaderHeight = 30f;

    // ==================================================
    // Unity Functions
    // ==================================================

    private void Awake()
    {
        _monitor = GetComponent<PerformanceMonitor>();

        if (_monitor == null)
        {
            Debug.LogError(
                "[Error] PerformanceMonitor が見つかりません。",
                this
            );

            enabled = false;
            return;
        }

        _isVisible = _isVisibleOnStart;
    }

    private void OnEnable()
    {
        if (_monitor != null)
        {
            _monitor.OnSampleUpdated += RefreshUI;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.f3Key.wasPressedThisFrame)
        {
            ToggleVisibility();
        }
    }

    private void OnDisable()
    {
        if (_monitor != null)
        {
            _monitor.OnSampleUpdated -= RefreshUI;
        }
    }

    private void OnGUI()
    {
        if (!_isVisible)
        {
            return;
        }

        InitializeStyles();

        Rect windowRect = new Rect(
            _windowPosition.x,
            _windowPosition.y,
            _windowWidth,
            WindowHeight
        );

        // ==========================================
        // 半透明背景
        // ==========================================

        Color previousColor = GUI.color;

        GUI.color = _backgroundColor;

        GUI.DrawTexture(
            windowRect,
            Texture2D.whiteTexture,
            ScaleMode.StretchToFill,
            true
        );

        GUI.color = previousColor;

        // ==========================================
        // 文字描画
        // ==========================================

        float x = windowRect.x + Padding;
        float y = windowRect.y + Padding;
        float width = windowRect.width - Padding * 2f;

        GUI.Label(
            new Rect(x, y, width, HeaderHeight),
            "PERFORMANCE MONITOR",
            _titleStyle
        );

        y += HeaderHeight + 5f;

        _fpsStyle.normal.textColor = _fpsColor;

        GUI.Label(
            new Rect(x, y, width, 30f),
            _fpsText,
            _fpsStyle
        );

        y += 35f;

        GUI.Label(
            new Rect(
                x,
                y,
                width,
                WindowHeight - (y - windowRect.y) - Padding
            ),
            _displayText,
            _labelStyle
        );
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;

        if (_isVisible)
        {
            RefreshUI();
        }
    }

    private void InitializeStyles()
    {
        if (_labelStyle != null)
        {
            return;
        }

        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.fontSize = _fontSize;
        _labelStyle.normal.textColor = Color.white;
        _labelStyle.alignment = TextAnchor.UpperLeft;
        _labelStyle.wordWrap = false;
        _labelStyle.richText = false;

        _fpsStyle = new GUIStyle(_labelStyle);
        _fpsStyle.fontSize = _fontSize + 5;
        _fpsStyle.fontStyle = FontStyle.Bold;

        _titleStyle = new GUIStyle(_labelStyle);
        _titleStyle.fontSize = _fontSize;
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.normal.textColor = Color.cyan;
    }

    private void RefreshUI()
    {
        if (_monitor == null)
        {
            return;
        }

        float fps = _monitor.CurrentFps;

        _fpsColor =
            fps >= _goodFpsThreshold ? Color.green :
            fps >= _warningFpsThreshold ? Color.yellow :
            Color.red;

        _fpsText = $"FPS: {fps:F1}";

        _builder.Clear();

        _builder.Append("Frame Time: ")
            .Append(_monitor.FrameTimeMs.ToString("F2"))
            .AppendLine(" ms");

        _builder.Append("Min / Avg / Max: ")
            .Append(_monitor.MinimumFps.ToString("F0"))
            .Append(" / ")
            .Append(_monitor.AverageFps.ToString("F0"))
            .Append(" / ")
            .Append(_monitor.MaximumFps.ToString("F0"))
            .AppendLine();

        _builder.AppendLine("----------------------------");

        AppendTime(
            "Main Thread",
            _monitor.HasMainThreadTime,
            _monitor.MainThreadMs
        );

        AppendTime(
            "Render Thread",
            _monitor.HasRenderThreadTime,
            _monitor.RenderThreadMs
        );

        AppendTime(
            "GPU Frame",
            _monitor.HasGpuFrameTime,
            _monitor.GpuFrameMs
        );

        _builder.AppendLine("----------------------------");

        AppendCount(
            "Draw Calls",
            _monitor.HasDrawCalls,
            _monitor.DrawCalls
        );

        AppendCount(
            "SetPass Calls",
            _monitor.HasSetPassCalls,
            _monitor.SetPassCalls
        );

        AppendCount(
            "Triangles",
            _monitor.HasTriangles,
            _monitor.Triangles
        );

        _builder.Append("Memory: ");

        if (_monitor.HasMemory)
        {
            double memoryMiB =
                _monitor.MemoryBytes / (1024.0 * 1024.0);

            _builder.Append(memoryMiB.ToString("F1"))
                .AppendLine(" MiB");
        }
        else
        {
            _builder.AppendLine("N/A");
        }

        _builder.AppendLine("----------------------------");
        _builder.Append("F3 : Show / Hide");

        _displayText = _builder.ToString();
    }

    private void AppendTime(
        string label,
        bool isAvailable,
        double milliseconds)
    {
        _builder.Append(label).Append(": ");

        if (isAvailable)
        {
            _builder.Append(milliseconds.ToString("F2"))
                .AppendLine(" ms");
        }
        else
        {
            _builder.AppendLine("N/A");
        }
    }

    private void AppendCount(
        string label,
        bool isAvailable,
        long value)
    {
        _builder.Append(label).Append(": ");

        if (isAvailable)
        {
            _builder.AppendLine(value.ToString("N0"));
        }
        else
        {
            _builder.AppendLine("N/A");
        }
    }
}
