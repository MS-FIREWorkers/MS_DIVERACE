using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsManagerTest : MonoBehaviour
{
    [SerializeField] private HapticsManager hapticsManager;

    private bool isPaused;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || hapticsManager == null)
            return;

        // 1キー：弱い振動
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            hapticsManager.RequestHaptics(HapticType.Falling, 0.2f);

            Debug.Log("弱い落下振動をリクエスト");
        }

        // 2キー：中程度の振動
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            hapticsManager.RequestHaptics(HapticType.Falling, 0.5f);

            Debug.Log("中程度の落下振動をリクエスト");
        }

        // 3キー：強い振動
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            hapticsManager.RequestHaptics(HapticType.Falling, 1.0f);

            Debug.Log("強い落下振動をリクエスト");
        }

        // Spaceキー：一時停止・再開
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isPaused = !isPaused;
            hapticsManager.SetPaused(isPaused);

            Debug.Log(isPaused ? "ハプティクス一時停止" : "ハプティクス再開");
        }

        // 0キー：落下振動のリクエストを停止
        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            hapticsManager.StopFallingHaptics();

            Debug.Log("落下振動を停止");
        }
    }
}
