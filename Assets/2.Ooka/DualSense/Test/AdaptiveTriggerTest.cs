using GamepadCore.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DualSenseのアダプティブトリガーを単体検証するテスト。
/// GamepadCoreBehaviourによる初期化を利用するため、
/// このスクリプトでは初期化・終了処理を行わない。
/// </summary>
public class AdaptiveTriggerTest : MonoBehaviour
{
    [Header("トリガー抵抗の設定")]

    [Tooltip("抵抗が始まる位置。正確な値の範囲はGamepadCoreの仕様を確認して設定します。")]
    [SerializeField, Range(0, 9)]
    private int startZone = 0;

    [Tooltip("トリガーの抵抗の強さ。実機で感触を確認しながら調整します。")]
    [SerializeField, Range(0, 255)]
    private int strength = 100;

    [Header("入力確認")]

    [Tooltip("L2・R2の入力値をログに表示する間隔（秒）。")]
    [SerializeField, Min(0.1f)]
    private float logInterval = 0.5f;

    [Tooltip("GamepadCoreで制御するコントローラーのインデックス。")]
    [SerializeField, Min(0)]
    private int deviceIndex = 0;

    private bool resistanceEnabled;
    private float logTimer;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Jキー：左右のトリガーに抵抗を設定
        if (keyboard.jKey.wasPressedThisFrame)
        {
            SetResistance();
        }

        // Kキー：左右のトリガーの抵抗を解除
        if (keyboard.kKey.wasPressedThisFrame)
        {
            StopResistance();
        }

        // L2・R2のアナログ入力を定期的に確認
        logTimer += Time.unscaledDeltaTime;

        if (logTimer >= logInterval)
        {
            logTimer = 0f;
            LogTriggerInput();
        }
    }

    /// <summary>
    /// L2・R2両方の抵抗を設定する。
    /// </summary>
    private void SetResistance()
    {
        GamepadCoreApi.SetTriggerResistance(deviceIndex, (byte)startZone,
            (byte)strength, GamepadHand.Both);

        resistanceEnabled = true;

        Debug.Log($"[AdaptiveTriggerTest] 抵抗を設定しました。" +
            $" startZone={startZone}, strength={strength}");
    }

    /// <summary>
    /// L2・R2両方の抵抗を解除する。
    /// </summary>
    private void StopResistance()
    {
        GamepadCoreApi.StopTrigger(deviceIndex, GamepadHand.Both);

        resistanceEnabled = false;

        Debug.Log("[AdaptiveTriggerTest] 抵抗を解除しました。");
    }

    /// <summary>
    /// DualSenseのL2・R2の押し込み量をログに表示する。
    /// </summary>
    private void LogTriggerInput()
    {
        var gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        float l2 = gamepad.leftTrigger.ReadValue();
        float r2 = gamepad.rightTrigger.ReadValue();

        Debug.Log($"[AdaptiveTriggerTest] " + $"L2={l2:F2}, R2={r2:F2}, " +
            $"抵抗={(resistanceEnabled ? "有効" : "解除")}");
    }

    private void OnDisable()
    {
        // テストオブジェクトが無効化された場合も抵抗を解除する。
        // GamepadCoreBehaviourの初期化・終了処理は呼び出さない。
        GamepadCoreApi.StopTrigger(deviceIndex, GamepadHand.Both);

        resistanceEnabled = false;
    }
}
