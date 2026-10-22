using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 障害物衝突ハプティクスの動作確認用。
/// Kキーを押すと、HapticsManagerへ衝突振動を要求する。
/// 本番の障害物判定とは接続しないテスト専用スクリプト。
/// </summary>
public class HapticsObstacleCollisionTest : MonoBehaviour
{
    [Tooltip("振動要求を送るHapticsManager")]
    [SerializeField] private HapticsManager hapticsManager;

    private void Update()
    {
        // キーボードまたは参照先がない場合は処理しない。
        if (Keyboard.current == null || hapticsManager == null)
        {
            return;
        }

        // Kキーを押した瞬間に、衝突振動を1回要求する。
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            hapticsManager.TriggerObstacleCollisionHaptics();
            Debug.Log("障害物衝突ハプティクスを要求しました", this);
        }
    }
}
