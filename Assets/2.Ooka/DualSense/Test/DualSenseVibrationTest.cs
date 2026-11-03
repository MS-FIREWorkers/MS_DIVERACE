using UnityEngine;
using UnityEngine.InputSystem;
using GamepadCore.Unity;

public class DualSenseVibrationTest : MonoBehaviour
{
    [Header("振動強度（0～1）")]
    [Range(0f, 1f)]
    [SerializeField] private float leftStrength = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float rightStrength = 1f;

    private int deviceIndex = 0;

    private void Update()
    {
        if (!GamepadCoreApi.IsInitialized ||
            GamepadCoreApi.ConnectedCount <= deviceIndex)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            GamepadCoreApi.SetVibration(
                deviceIndex, leftStrength, 0f);
            Debug.Log("左モーターのみ");
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            GamepadCoreApi.SetVibration(
                deviceIndex, 0f, rightStrength);
            Debug.Log("右モーターのみ");
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            GamepadCoreApi.SetVibration(
                deviceIndex, leftStrength, rightStrength);
            Debug.Log("左右モーター");
        }

        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            GamepadCoreApi.SetVibration(deviceIndex, 0f, 0f);
            Debug.Log("振動停止");
        }
    }

    private void OnDisable()
    {
        if (GamepadCoreApi.IsInitialized &&
            GamepadCoreApi.ConnectedCount > deviceIndex)
        {
            GamepadCoreApi.SetVibration(deviceIndex, 0f, 0f);
        }
    }
}
