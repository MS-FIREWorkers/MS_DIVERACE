/*
    HapticsObstacleApproachTest.cs
    障害物への接近振動の開始・停止をテストする
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsObstacleApproachTest : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("ハプティクス設定")]
    [Tooltip("テスト対象の HapticsManager")]
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

        // Jキー：障害物への接近振動を開始
        if (keyboard.jKey.wasPressedThisFrame)
        {
            _hapticsManager.StartObstacleApproach();
            Debug.Log("障害物への接近ハプティクスを開始しました。");
        }

        // Lキー：障害物への接近振動を停止
        if (keyboard.lKey.wasPressedThisFrame)
        {
            _hapticsManager.StopObstacleApproach();
            Debug.Log("障害物への接近ハプティクスを停止しました。");
        }
    }
}
