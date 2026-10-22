using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsObstacleApproachTest : MonoBehaviour
{
    [SerializeField] private HapticsManager hapticsManager;

    private void Update()
    {
        if (Keyboard.current == null || hapticsManager == null)
            return;

        // Jキー：接近振動を開始
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            hapticsManager.StartObstacleApproachHaptics();
            Debug.Log("障害物接近ハプティクス開始");
        }

        // Lキー：接近振動を停止
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            hapticsManager.StopObstacleApproachHaptics();
            Debug.Log("障害物接近ハプティクス停止");
        }
    }
}
