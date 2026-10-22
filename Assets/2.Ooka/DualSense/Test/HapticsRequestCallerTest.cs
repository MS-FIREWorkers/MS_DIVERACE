using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsRequestCallerTest : MonoBehaviour
{
    [SerializeField] private HapticsManager hapticsManager;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || hapticsManager == null)
            return;

        // Jキー：Playerから落下振動を要求する想定
        if (keyboard.jKey.wasPressedThisFrame)
        {
            hapticsManager.RequestHaptics(HapticType.Falling, 0.3f);

            Debug.Log("外部スクリプト：落下振動を要求");
        }

        // Kキー：障害物との衝突振動を要求する想定
        if (keyboard.kKey.wasPressedThisFrame)
        {
            hapticsManager.RequestHaptics(HapticType.ObstacleCollision, 1.0f);

            Debug.Log("外部スクリプト：衝突振動を要求");
        }

        // Lキー：落下振動を停止
        if (keyboard.lKey.wasPressedThisFrame)
        {
            hapticsManager.StopFallingHaptics();

            Debug.Log("外部スクリプト：落下振動を停止");
        }
    }
}
