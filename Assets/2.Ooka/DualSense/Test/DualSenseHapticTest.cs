using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class DualSenseHapticTest : MonoBehaviour
{
    [Header("U“®Ý’è")]
    [SerializeField] private float vibrationStrength = 0.7f;

    [Tooltip("1‰ñ‚ ‚½‚è‚ÌU“®ŽžŠÔ")]
    [SerializeField] private float vibrationDuration = 0.12f;

    [Tooltip("ŽŸ‚ÌU“®‚Ü‚Å‚ÌŠÔŠu")]
    [SerializeField] private float stepInterval = 0.3f;

    private Gamepad gamepad;

    private float timer;
    private bool isVibrating;
    private bool isRightSide;

    private void Start()
    {
        gamepad = Gamepad.current;

        if (gamepad == null)
        {
            Debug.LogWarning("DualSense‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñB");
            return;
        }

        Debug.Log("DualSense‚ðŒŸo‚µ‚Ü‚µ‚½B");
    }

    private void Update()
    {
        if (gamepad == null)
            return;

        timer += Time.deltaTime;

        // U“®ŠJŽn
        if (!isVibrating && timer >= stepInterval)
        {
            timer = 0f;
            isVibrating = true;

            if (isRightSide)
            {
                // ‰E‘¤‚ðU“®
                gamepad.SetMotorSpeeds(0f, vibrationStrength);
            }
            else
            {
                // ¶‘¤‚ðU“®
                gamepad.SetMotorSpeeds(vibrationStrength, 0f);
            }

            isRightSide = !isRightSide;
        }

        // U“®I—¹
        if (isVibrating && timer >= vibrationDuration)
        {
            timer = 0f;
            isVibrating = false;

            gamepad.SetMotorSpeeds(0f, 0f);
        }
    }

    private void OnDisable()
    {
        if (gamepad != null)
        {
            gamepad.ResetHaptics();
        }
    }
}
