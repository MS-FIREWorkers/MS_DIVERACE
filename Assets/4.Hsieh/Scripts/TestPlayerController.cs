using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TestPlayerController : MonoBehaviour
{
    [Header("Horizontal Move")]
    [SerializeField] private float moveSpeed = 10.0f;
    [SerializeField] private float moveAcceleration = 30.0f;

    [Header("Fall Speed")]
    [SerializeField] private float normalMaxFallSpeed = 12.0f;
    [SerializeField] private float fastMaxFallSpeed = 25.0f;
    [SerializeField] private float slowMaxFallSpeed = 5.0f;

    [Header("Gravity")]
    [SerializeField] private float fastGravityMultiplier = 2.5f;
    [SerializeField] private float slowGravityMultiplier = 0.3f;

    [Header("Slow Fall")]
    [SerializeField] private float slowFallDeceleration = 20.0f;

    private Rigidbody rb;

    private float moveInput;
    private bool fastFall;
    private bool slowFall;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        moveInput = 0.0f;

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
        {
            moveInput = -1.0f;
        }

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
        {
            moveInput = 1.0f;
        }

        fastFall =
            keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed;

        slowFall =
            keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed;
    }

    private void FixedUpdate()
    {
        MoveHorizontal();
        ApplyFallControl();
    }

    private void MoveHorizontal()
    {
        Vector3 velocity = rb.linearVelocity;

        float targetX = moveInput * moveSpeed;

        velocity.x = Mathf.MoveTowards(
            velocity.x,
            targetX,
            moveAcceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = velocity;
    }

    private void ApplyFallControl()
    {
        if (fastFall)
        {
            ApplyGravityMultiplier(fastGravityMultiplier);
            LimitFallSpeed(fastMaxFallSpeed);
            return;
        }

        if (slowFall)
        {
            ApplyGravityMultiplier(slowGravityMultiplier);
            SlowDownFall();
            return;
        }

        LimitFallSpeed(normalMaxFallSpeed);
    }

    private void ApplyGravityMultiplier(float multiplier)
    {
        Vector3 extraGravity =
            Physics.gravity * (multiplier - 1.0f);

        rb.AddForce(
            extraGravity,
            ForceMode.Acceleration
        );
    }

    private void SlowDownFall()
    {
        Vector3 velocity = rb.linearVelocity;

        if (velocity.y < -slowMaxFallSpeed)
        {
            velocity.y = Mathf.MoveTowards(
                velocity.y,
                -slowMaxFallSpeed,
                slowFallDeceleration * Time.fixedDeltaTime
            );

            rb.linearVelocity = velocity;
        }
    }

    private void LimitFallSpeed(float maxFallSpeed)
    {
        Vector3 velocity = rb.linearVelocity;

        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
            rb.linearVelocity = velocity;
        }
    }
}