/*
    HapticsFallSpeedSimulationTest.cs
    落下速度をシミュレーションし、速度に応じた落下振動をテストする
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsFallSpeedSimulationTest : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("ハプティクス設定")]
    [Tooltip("テスト対象の HapticsManager")]
    [SerializeField] private HapticsManager _hapticsManager;

    [Header("落下速度設定")]
    [Tooltip("落下速度が1秒あたりに増加する量")]
    [SerializeField] private float _acceleration = 10f;

    [Tooltip("シミュレーション上の最大落下速度")]
    [SerializeField] private float _maxFallSpeed = 50f;

    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    private float _simulatedFallSpeed;
    private bool _isPaused;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || _hapticsManager == null)
        {
            return;
        }

        // Spaceキー：一時停止・再開
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            _isPaused = !_isPaused;
            _hapticsManager.SetPaused(_isPaused);

            Debug.Log(_isPaused ? "ハプティクスを一時停止しました" : "ハプティクスを再開しました");
        }

        // Rキー：落下速度をリセット
        if (keyboard.rKey.wasPressedThisFrame)
        {
            _simulatedFallSpeed = 0f;
            _hapticsManager.StopFallingHaptics();

            Debug.Log("落下速度をリセットしました");
        }

        if (_isPaused)
        {
            return;
        }

        // 落下速度を時間経過に応じて増加させる
        _simulatedFallSpeed = Mathf.Min(_simulatedFallSpeed + _acceleration * Time.deltaTime,
            _maxFallSpeed);

        // 最大落下速度に対する現在の速度の割合を計算する
        float fallSpeedRatio = Mathf.Clamp01(_simulatedFallSpeed / _maxFallSpeed);

        // 落下速度の割合に応じて振動を要求する
        if (_simulatedFallSpeed > 0f)
        {
            _hapticsManager.SetFalling(fallSpeedRatio);
        }
    }
}
