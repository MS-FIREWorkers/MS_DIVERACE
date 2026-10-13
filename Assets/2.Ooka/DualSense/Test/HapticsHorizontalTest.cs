/*
    HapticsHorizontalTest.cs
    Aキー・Dキーによる左右移動のハプティクスをテストする
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsHorizontalTest : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("ハプティクス設定")]
    [Tooltip("テスト対象の HapticsManager")]
    [SerializeField] private HapticsManager _hapticsManager;

    [Header("振動設定")]
    [Tooltip("左右移動時の振動強度")]
    [SerializeField, Range(0.01f, 1f)]
    private float _intensity = 0.8f;

    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    // 左右移動の振動が有効だったかどうか
    private bool _wasHorizontalActive;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Update()
    {
        if (Keyboard.current == null || _hapticsManager == null)
        {
            return;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            _hapticsManager.SetHorizontalMovement(_intensity, -1f);
            _wasHorizontalActive = true;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            _hapticsManager.SetHorizontalMovement(_intensity, 1f);
            _wasHorizontalActive = true;
        }
        else if (_wasHorizontalActive)
        {
            // キーを離したタイミングで一度だけ振動を停止する
            _hapticsManager.StopHorizontalMovement();
            _wasHorizontalActive = false;
        }
    }
}
