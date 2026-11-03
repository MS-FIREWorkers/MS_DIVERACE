/*
    HapticsObstaclePassingTest.cs
    Jキー・Lキーで障害物通過時の振動方向をテストする。
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsObstaclePassingTest : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("ハプティクス設定")]
    [Tooltip("振動要求を送信する HapticsManager")]
    [SerializeField] private HapticsManager _hapticsManager;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (Keyboard.current == null || _hapticsManager == null)
        {
            return;
        }

        // Jキー：右方向への障害物通過をテストする
        if (keyboard.jKey.wasPressedThisFrame)
        {
            _hapticsManager.PlayObstaclePassing(1f);
            Debug.Log("右方向の障害物通過振動を要求しました。", this);
        }

        // Lキー：左方向への障害物通過をテストする
        if (keyboard.lKey.wasPressedThisFrame)
        {
            _hapticsManager.PlayObstaclePassing(-1f);
            Debug.Log("左方向の障害物通過振動を要求しました。", this);
        }
    }
}
