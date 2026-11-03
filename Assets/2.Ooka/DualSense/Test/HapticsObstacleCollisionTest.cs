/*
    HapticsObstacleCollisionTest.cs
    Kキー入力によって障害物衝突時のハプティクスをテストする。
    実際の障害物との衝突判定には接続しない検証用スクリプト。
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsObstacleCollisionTest : MonoBehaviour
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

        // キーボードまたは参照先が存在しない場合は処理しない
        if (Keyboard.current == null || _hapticsManager == null)
        {
            return;
        }

        // Kキー：障害物衝突時の振動を1回要求する
        if (keyboard.kKey.wasPressedThisFrame)
        {
            _hapticsManager.PlayObstacleCollision();
            Debug.Log("障害物衝突ハプティクスを要求しました。", this);
        }
    }
}
