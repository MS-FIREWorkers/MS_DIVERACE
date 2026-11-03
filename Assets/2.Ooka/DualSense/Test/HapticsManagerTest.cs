/*
    HapticsManagerTest.cs
    HapticsManager の落下振動と一時停止をテストする
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsManagerTest : MonoBehaviour
{
    // ==================================================
    // ----- Private Field -----
    // ==================================================

    [Header("ハプティクス設定")]
    [Tooltip("テスト対象の HapticsManager")]
    [SerializeField] private HapticsManager _hapticsManager;

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

        // 1キー：弱い落下振動
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            _hapticsManager.SetFalling(0.2f);

            Debug.Log("弱い落下振動を要求しました。");
        }

        // 2キー：中程度の落下振動
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            _hapticsManager.SetFalling(0.5f);

            Debug.Log("中程度の落下振動を要求しました。");
        }

        // 3キー：強い落下振動
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            _hapticsManager.SetFalling(1.0f);

            Debug.Log("強い落下振動を要求しました。");
        }

        // Spaceキー：一時停止・再開
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            _isPaused = !_isPaused;
            _hapticsManager.SetPaused(_isPaused);

            Debug.Log(_isPaused ? "ハプティクスを一時停止しました" : "ハプティクスを再開しました");
        }

        // 0キー：落下振動を停止
        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            _hapticsManager.StopFallingHaptics();

            Debug.Log("落下振動を停止しました。");
        }
    }
}
