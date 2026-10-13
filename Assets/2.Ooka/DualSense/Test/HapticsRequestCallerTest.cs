/*
    HapticsRequestCallerTest.cs
    外部スクリプトから HapticsManager の振動要求・停止処理をテストする。
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsRequestCallerTest : MonoBehaviour
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

        if (keyboard == null || _hapticsManager == null)
        {
            return;
        }

        // Jキー：落下振動を要求する
        if (keyboard.jKey.wasPressedThisFrame)
        {
            _hapticsManager.SetFalling(0.3f);

            Debug.Log("外部スクリプト：落下振動を要求しました。");
        }

        // Kキー：障害物との衝突振動を要求する
        if (keyboard.kKey.wasPressedThisFrame)
        {
            _hapticsManager.PlayObstacleCollision();

            Debug.Log("外部スクリプト：衝突振動を要求しました。");
        }

        // Lキー：落下振動を停止する
        if (keyboard.lKey.wasPressedThisFrame)
        {
            _hapticsManager.StopFalling();

            Debug.Log("外部スクリプト：落下振動を停止しました。");
        }
    }
}
