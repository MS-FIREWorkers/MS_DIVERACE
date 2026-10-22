using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsFallSpeedSimulationTest : MonoBehaviour
{
    [SerializeField] private HapticsManager hapticsManager;

    [Header("仮の落下速度")]
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float maxFallSpeed = 50f;

    private float simulatedFallSpeed;
    private bool isPaused;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || hapticsManager == null)
            return;

        // Space：一時停止・再開
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isPaused = !isPaused;
            hapticsManager.SetPaused(isPaused);

            Debug.Log(isPaused ? "一時停止" : "再開");
        }

        // R：落下速度をリセット
        if (keyboard.rKey.wasPressedThisFrame)
        {
            simulatedFallSpeed = 0f;
            hapticsManager.StopFallingHaptics();

            Debug.Log("落下速度をリセット");
        }

        if (isPaused)
            return;

        // 仮の落下速度を徐々に上げる
        simulatedFallSpeed =
            Mathf.Min(simulatedFallSpeed + acceleration * Time.deltaTime, maxFallSpeed);

        float fallSpeedRatio = Mathf.Clamp01(simulatedFallSpeed / maxFallSpeed);

        // 毎フレーム、現在の速度に応じた強さを要求
        if (simulatedFallSpeed > 0f)
        {
            hapticsManager.RequestHaptics(HapticType.Falling, fallSpeedRatio);
        }
    }
}
