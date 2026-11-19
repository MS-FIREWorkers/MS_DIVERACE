// ==================================================
// File: PerformanceMonitor.cs
// Created: 2026/10/22
// Author: FUkano
// Summary: FPS・描画統計・メモリ使用量を収集するデバッグモニター
// ==================================================

using Unity.Profiling;
using UnityEngine;

// ==================================================
// PerformanceMonitor
// ==================================================
[DisallowMultipleComponent]
public class PerformanceMonitor : MonoBehaviour
{
    // ==================================================
    // Variables
    // ==================================================

    [Header("Sampling Settings")]
    [Tooltip("計測結果を更新する間隔（秒）")]
    [SerializeField, Min(0.1f)]
    private float _updateInterval = 0.25f;

    private ProfilerRecorder _mainThreadRecorder;
    private ProfilerRecorder _renderThreadRecorder;
    private ProfilerRecorder _gpuFrameRecorder;
    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _setPassRecorder;
    private ProfilerRecorder _trianglesRecorder;
    private ProfilerRecorder _memoryRecorder;

    private float _elapsedTime;
    private int _frameCount;
    private float _minimumFps = float.PositiveInfinity;
    private float _maximumFps;
    private float _totalFps;
    private int _sampleCount;

    public float CurrentFps { get; private set; }
    public float AverageFps => _sampleCount > 0 ? _totalFps / _sampleCount : 0f;
    public float MinimumFps => _sampleCount > 0 ? _minimumFps : 0f;
    public float MaximumFps => _maximumFps;
    public float FrameTimeMs => CurrentFps > 0f ? 1000f / CurrentFps : 0f;

    public double MainThreadMs => GetTimeMs(_mainThreadRecorder);
    public double RenderThreadMs => GetTimeMs(_renderThreadRecorder);
    public double GpuFrameMs => GetTimeMs(_gpuFrameRecorder);
    public long DrawCalls => GetCounter(_drawCallsRecorder);
    public long SetPassCalls => GetCounter(_setPassRecorder);
    public long Triangles => GetCounter(_trianglesRecorder);
    public long MemoryBytes => GetCounter(_memoryRecorder);

    public bool HasMainThreadTime => IsAvailable(_mainThreadRecorder);
    public bool HasRenderThreadTime => IsAvailable(_renderThreadRecorder);
    public bool HasGpuFrameTime => IsAvailable(_gpuFrameRecorder);
    public bool HasDrawCalls => IsAvailable(_drawCallsRecorder);
    public bool HasSetPassCalls => IsAvailable(_setPassRecorder);
    public bool HasTriangles => IsAvailable(_trianglesRecorder);
    public bool HasMemory => IsAvailable(_memoryRecorder);

    public event System.Action OnSampleUpdated;

    // ==================================================
    // Unity Functions
    // ==================================================

    private void OnEnable()
    {
        StartRecorders();
    }

    private void Update()
    {
        _elapsedTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_elapsedTime < _updateInterval)
        {
            return;
        }

        CurrentFps = _elapsedTime > 0f ? _frameCount / _elapsedTime : 0f;
        _minimumFps = Mathf.Min(_minimumFps, CurrentFps);
        _maximumFps = Mathf.Max(_maximumFps, CurrentFps);
        _totalFps += CurrentFps;
        _sampleCount++;

        _elapsedTime = 0f;
        _frameCount = 0;

        OnSampleUpdated?.Invoke();
    }

    private void OnDisable()
    {
        StopRecorders();
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    public void ResetFpsStatistics()
    {
        _minimumFps = float.PositiveInfinity;
        _maximumFps = 0f;
        _totalFps = 0f;
        _sampleCount = 0;
        _elapsedTime = 0f;
        _frameCount = 0;
        CurrentFps = 0f;
    }

    private void StartRecorders()
    {
        _mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Main Thread", 15);
        _renderThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Render Thread", 15);
        _gpuFrameRecorder = StartRecorder(ProfilerCategory.Render, "GPU Frame Time", 15);
        _drawCallsRecorder = StartRecorder(ProfilerCategory.Render, "Draw Calls Count", 1);
        _setPassRecorder = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count", 1);
        _trianglesRecorder = StartRecorder(ProfilerCategory.Render, "Triangles Count", 1);
        _memoryRecorder = StartRecorder(ProfilerCategory.Memory, "Total Used Memory", 1);
    }

    private static ProfilerRecorder StartRecorder(ProfilerCategory category, string name, int capacity)
    {
        return ProfilerRecorder.StartNew(category, name, capacity);
    }

    private void StopRecorders()
    {
        _mainThreadRecorder.Dispose();
        _renderThreadRecorder.Dispose();
        _gpuFrameRecorder.Dispose();
        _drawCallsRecorder.Dispose();
        _setPassRecorder.Dispose();
        _trianglesRecorder.Dispose();
        _memoryRecorder.Dispose();
    }

    private static bool IsAvailable(ProfilerRecorder recorder)
    {
        return recorder.Valid && recorder.Count > 0;
    }

    private static long GetCounter(ProfilerRecorder recorder)
    {
        return IsAvailable(recorder) ? recorder.LastValue : 0L;
    }

    private static double GetTimeMs(ProfilerRecorder recorder)
    {
        if (!IsAvailable(recorder))
        {
            return 0.0;
        }

        // Profilerの時間カウンターはナノ秒単位
        return recorder.LastValue / 1000000.0;
    }
}
