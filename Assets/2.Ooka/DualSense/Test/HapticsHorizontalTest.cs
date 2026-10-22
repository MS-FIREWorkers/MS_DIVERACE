using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsHorizontalTest : MonoBehaviour
{
    [SerializeField] private HapticsManager hapticsManager;

    [SerializeField, Range(0.01f, 1f)]
    private float intensity = 0.8f;

    // 横移動の振動を要求中かどうか
    private bool wasHorizontalActive;

    private void Update()
    {
        if (Keyboard.current == null || hapticsManager == null)
            return;

        if (Keyboard.current.aKey.isPressed)
        {
            hapticsManager.RequestHaptics(HapticType.HorizontalMovement,
                intensity, direction: -1f);

            wasHorizontalActive = true;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            hapticsManager.RequestHaptics(HapticType.HorizontalMovement,
                intensity, direction: 1f);

            wasHorizontalActive = true;
        }
        else if (wasHorizontalActive)
        {
            // キーを離したタイミングで1回だけ停止する
            hapticsManager.StopHorizontalHaptics();
            wasHorizontalActive = false;
        }
    }
}
