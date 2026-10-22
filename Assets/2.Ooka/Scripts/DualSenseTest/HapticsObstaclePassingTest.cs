using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 障害物通過ハプティクスの動作確認用。
/// Jキーで右方向、Lキーで左方向の通過振動を要求する。
/// </summary>
public class HapticsObstaclePassingTest : MonoBehaviour
{
    [Tooltip("振動要求を送るHapticsManager")]
    [SerializeField] private HapticsManager hapticsManager;

    private void Update()
    {
        if (Keyboard.current == null || hapticsManager == null)
        {
            return;
        }

        // Jキー：右方向への通過をテストする。
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            hapticsManager.StartObstaclePassingHaptics(1f);
            Debug.Log("右方向の障害物通過振動を要求しました", this);
        }

        // Lキー：左方向への通過をテストする。
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            hapticsManager.StartObstaclePassingHaptics(-1f);
            Debug.Log("左方向の障害物通過振動を要求しました", this);
        }
    }
}
