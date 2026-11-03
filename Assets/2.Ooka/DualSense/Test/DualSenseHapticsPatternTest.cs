using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GamepadCore.Unity;

public class DualSenseHapticsPatternTest : MonoBehaviour
{
    [Header("基本の振動強度（0～1）")]
    [Range(0f, 1f)]
    [SerializeField] private float leftStrength = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float rightStrength = 1f;

    [Header("弱い振動の割合")]
    [Range(0f, 1f)]
    [SerializeField] private float weakRatio = 0.25f;

    [Header("時間設定（秒）")]
    [Min(0.01f)]
    [SerializeField] private float vibrationDuration = 0.2f;

    [Min(0f)]
    [SerializeField] private float pauseDuration = 0.1f;

    [Min(1)]
    [SerializeField] private int repeatCount = 3;

    [Header("強弱変化の段階数")]
    [Range(2, 10)]
    [SerializeField] private int strengthSteps = 5;

    private const int DeviceIndex = 0;
    private Coroutine patternCoroutine;

    private void Update()
    {
        if (!GamepadCoreApi.IsInitialized ||
            GamepadCoreApi.ConnectedCount <= DeviceIndex)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            StartPattern(AlternatePattern());
            Debug.Log("パターン1：左→右交互");
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            StartPattern(StrengthPattern(true));
            Debug.Log("パターン2：弱→強");
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            StartPattern(StrengthPattern(false));
            Debug.Log("パターン3：強→弱");
        }

        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            StartPattern(SimultaneousPattern());
            Debug.Log("パターン4：左右同時");
        }

        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            StopPattern();
            Debug.Log("振動停止");
        }
    }

    private void StartPattern(IEnumerator pattern)
    {
        StopPattern();
        patternCoroutine = StartCoroutine(pattern);
    }

    private IEnumerator AlternatePattern()
    {
        for (int i = 0; i < repeatCount; i++)
        {
            yield return Vibrate(
                leftStrength, 0f, vibrationDuration);

            yield return Pause();

            yield return Vibrate(
                0f, rightStrength, vibrationDuration);

            if (i < repeatCount - 1)
                yield return Pause();
        }

        StopPattern();
    }

    private IEnumerator StrengthPattern(bool ascending)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            for (int step = 0; step < strengthSteps; step++)
            {
                float t = step / (float)(strengthSteps - 1);

                if (!ascending)
                    t = 1f - t;

                float ratio = Mathf.Lerp(weakRatio, 1f, t);

                yield return Vibrate(
                    leftStrength * ratio,
                    rightStrength * ratio,
                    vibrationDuration);

                if (pauseDuration > 0f)
                    yield return Pause();
            }

            if (i < repeatCount - 1)
                yield return Pause();
        }

        StopPattern();
    }

    private IEnumerator SimultaneousPattern()
    {
        for (int i = 0; i < repeatCount; i++)
        {
            yield return Vibrate(
                leftStrength, rightStrength,
                vibrationDuration);

            if (i < repeatCount - 1)
                yield return Pause();
        }

        StopPattern();
    }

    private IEnumerator Vibrate(
        float left, float right, float duration)
    {
        GamepadCoreApi.SetVibration(
            DeviceIndex, left, right);

        yield return new WaitForSecondsRealtime(duration);

        GamepadCoreApi.SetVibration(
            DeviceIndex, 0f, 0f);
    }

    private IEnumerator Pause()
    {
        GamepadCoreApi.SetVibration(
            DeviceIndex, 0f, 0f);

        yield return new WaitForSecondsRealtime(pauseDuration);
    }

    private void StopPattern()
    {
        if (patternCoroutine != null)
        {
            StopCoroutine(patternCoroutine);
            patternCoroutine = null;
        }

        if (GamepadCoreApi.IsInitialized &&
            GamepadCoreApi.ConnectedCount > DeviceIndex)
        {
            GamepadCoreApi.SetVibration(
                DeviceIndex, 0f, 0f);
        }
    }

    private void OnDisable()
    {
        StopPattern();
    }

    private void OnApplicationQuit()
    {
        StopPattern();
    }
}
