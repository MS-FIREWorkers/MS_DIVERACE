/*
    DualSenseHapticsController.cs
    DualSense の基本振動と振動パターンを管理する
*/

using System.Collections;
using UnityEngine;
using GamepadCore.Unity;

public class DualSenseHapticsController : MonoBehaviour
{
    // ==================================================
    // ----- Private Field -----
    // ==================================================
    private const int _deviceIndex = 0;

    private Coroutine _patternCoroutine;


    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    private void OnDisable()
    {
        Stop();
    }

    private void OnApplicationQuit()
    {
        Stop();
    }


    // ==================================================
    // ----- Motor Control -----
    // ==================================================

    /// <summary>
    /// 左右のモーターを指定した強度で動かす。
    /// 強度は0～1。
    /// </summary>
    public void SetMotors(float left, float right)
    {
        if (!CanControlDevice())
        {
            return;
        }

        left = Mathf.Clamp01(left);
        right = Mathf.Clamp01(right);

        GamepadCoreApi.SetVibration(_deviceIndex, left, right);
    }

    /// <summary>
    /// 左右のモーターを停止する。
    /// </summary>
    public void Stop()
    {
        StopPattern();

        if (CanControlDevice())
        {
            GamepadCoreApi.SetVibration(_deviceIndex, 0f, 0f);
        }
    }


    // ==================================================
    // ----- Haptics Pattern -----
    // ==================================================

    /// <summary>
    /// 指定時間だけ振動させる。
    /// </summary>
    public void PlayPulse(float left, float right, float duration)
    {
        StopPattern();

        if (!CanControlDevice())
        {
            return;
        }

        _patternCoroutine = StartCoroutine(PulseRoutine(left, right, duration));
    }

    /// <summary>
    /// 左右のモーターを交互に振動させる。
    /// </summary>
    public void PlayAlternating(float left, float right, float duration, float pause, int repeatCount)
    {
        StopPattern();

        if (!CanControlDevice())
        {
            return;
        }

        _patternCoroutine = StartCoroutine(AlternatingRoutine(left, right, duration, pause, repeatCount));
    }


    // ==================================================
    // ----- Coroutine -----
    // ==================================================

    private IEnumerator PulseRoutine(float left, float right, float duration)
    {
        SetMotors(left, right);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duration));

        SetMotors(0f, 0f);
        _patternCoroutine = null;
    }

    private IEnumerator AlternatingRoutine(float left, float right, float duration, float pause, int repeatCount)
    {
        duration = Mathf.Max(0.01f, duration);
        pause = Mathf.Max(0f, pause);
        repeatCount = Mathf.Max(1, repeatCount);

        for (int i = 0; i < repeatCount; i++)
        {
            SetMotors(left, 0f);

            yield return new WaitForSecondsRealtime(duration);

            SetMotors(0f, 0f);

            if (pause > 0f)
            {
                yield return new WaitForSecondsRealtime(pause);
            }

            SetMotors(0f, right);

            yield return new WaitForSecondsRealtime(duration);

            SetMotors(0f, 0f);

            if (i < repeatCount - 1 && pause > 0f)
            {
                yield return new WaitForSecondsRealtime(pause);
            }
        }

        _patternCoroutine = null;
    }


    // ==================================================
    // ----- Device Check -----
    // ==================================================

    private bool CanControlDevice()
    {
        return GamepadCoreApi.IsInitialized && GamepadCoreApi.ConnectedCount > _deviceIndex;
    }

    private void StopPattern()
    {
        if (_patternCoroutine != null)
        {
            StopCoroutine(_patternCoroutine);
            _patternCoroutine = null;
        }
    }
}
