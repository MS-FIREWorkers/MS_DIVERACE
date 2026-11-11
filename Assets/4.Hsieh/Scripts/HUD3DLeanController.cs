
using UnityEngine;

[DisallowMultipleComponent]
public class HUD3DLeanController : MonoBehaviour
{
    // ==========================================
    // ----- References -----
    // ==========================================

    [Header("References")]
    [SerializeField] private RectTransform visorRoot;
    [SerializeField] private RectTransform hudRoot;
    [SerializeField] private Player_Movement playerMovement;
    [SerializeField] private Player_InputHandler playerInput;

    // ==========================================
    // ----- Acceleration Settings -----
    // ==========================================

    [Header("Acceleration Settings")]

    [SerializeField, Min(0.1f)]
    private float sideAccelerationForMaxTilt = 15f;

    [SerializeField, Min(0.1f)]
    private float forwardAccelerationForMaxTilt = 20f;

    [SerializeField, Min(0f)]
    private float accelerationDeadZone = 0.5f;

    [SerializeField, Min(0.1f)]
    private float accelerationFilterSpeed = 12f;

    // ==========================================
    // ----- 3D Tilt Settings -----
    // ==========================================

    [Header("3D Tilt Settings")]

    [SerializeField, Range(0f, 30f)]
    private float horizontalTiltAngle = 8f;

    [SerializeField, Range(0f, 30f)]
    private float verticalTiltAngle = 5f;

    [SerializeField, Range(0f, 1f)]
    private float hudTiltStrength = 0.25f;

    // ==========================================
    // ----- Tilt Smooth Settings -----
    // ==========================================

    [Header("Tilt Smooth Settings")]

    [SerializeField, Min(0.01f)]
    private float tiltSmoothTime = 0.16f;

    [SerializeField, Min(0.01f)]
    private float returnSmoothTime = 0.25f;

    // ==========================================
    // ----- Vertical Fall Scale Settings -----
    // ==========================================

    [Header("Vertical Fall Scale Settings")]

    [Tooltip("ècépê®Ç∆îªífÇµénÇﬂÇÈì¸óÕíl")]
    [SerializeField, Range(0f, 1f)]
    private float verticalPostureThreshold = 0.75f;

    [Tooltip("HUDÇÃèkè¨ÇäJénÇ∑ÇÈóéâ∫ë¨ìx")]
    [SerializeField, Min(0f)]
    private float shrinkStartFallSpeed = 20f;

    [Tooltip("HUDÇ™ç≈ëÂèkè¨Ç…Ç»ÇÈóéâ∫ë¨ìx")]
    [SerializeField, Min(0f)]
    private float shrinkMaxFallSpeed = 45f;

    [Tooltip("ç≈ëÂóéâ∫ë¨ìxéûÇÃHUDî{ó¶")]
    [SerializeField, Range(0.8f, 1f)]
    private float verticalFallScale = 0.97f;

    [Tooltip("èkè¨éûÇÃï‚ä‘éûä‘")]
    [SerializeField, Min(0.01f)]
    private float shrinkSmoothTime = 0.20f;

    [Tooltip("í èÌÉTÉCÉYÇ÷ñﬂÇÈéûä‘")]
    [SerializeField, Min(0.01f)]
    private float scaleReturnSmoothTime = 0.25f;

    // ==========================================
    // ----- Debug -----
    // ==========================================

    [Header("Acceleration Debug")]

    [SerializeField] private bool debugOverride = false;
    [SerializeField] private Vector2 debugAcceleration;

    [SerializeField] private Vector2 debugActualAcceleration;
    [SerializeField] private Vector2 debugCurrentTilt;

    [Header("Vertical Fall Debug")]

    [SerializeField] private bool debugFallOverride = false;

    [SerializeField, Range(0f, 1f)]
    private float debugPoseInput = 1f;

    [SerializeField, Min(0f)]
    private float debugFallSpeed = 40f;

    [SerializeField] private float currentPosture;
    [SerializeField] private float currentFallSpeed;
    [SerializeField] private float currentHUDScale = 1f;

    // ==========================================
    // ----- Runtime -----
    // ==========================================

    private Quaternion initialVisorRotation;
    private Quaternion initialHudRotation;
    private Vector3 initialVisorScale;

    private Vector3 previousHorizontalVelocity;
    private bool hasPreviousVelocity;

    private Vector2 filteredAcceleration;
    private Vector2 currentTilt;
    private Vector2 tiltVelocity;

    private float currentScale = 1f;
    private float scaleVelocity;

    private float nextSearchTime;
    private bool initialized;
    private bool controlHudRotation;

    // ==========================================
    // ----- Unity Events -----
    // ==========================================

    private void Awake()
    {
        if (visorRoot == null)
        {
            Debug.LogError(
                "HUD3DLeanController: VisorRoot is missing.",
                this
            );

            enabled = false;
            return;
        }

        initialVisorRotation = visorRoot.localRotation;
        initialVisorScale = visorRoot.localScale;

        // HUDèÓïÒÇ∆äOògÇÃìÒèdâÒì]Çñhé~
        if (hudRoot != null)
        {
            if (hudRoot == visorRoot ||
                hudRoot.IsChildOf(visorRoot))
            {
                Debug.LogWarning(
                    "HUDRoot must be separate from VisorRoot.",
                    this
                );
            }
            else
            {
                initialHudRotation = hudRoot.localRotation;
                controlHudRotation = true;
            }
        }

        FindPlayerReferences();

        initialized = true;
    }

    private void OnEnable()
    {
        hasPreviousVelocity = false;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        // ----- Player Search -----

        if ((playerMovement == null || playerInput == null) &&
            Time.unscaledTime >= nextSearchTime)
        {
            FindPlayerReferences();
            nextSearchTime = Time.unscaledTime + 1f;
        }

        // ----- 3D Inertia -----

        UpdateInertiaTilt(dt);

        // ----- Vertical Fall Scale -----

        UpdateVerticalFallScale(dt);
    }

    // ==========================================
    // ----- Inertia Tilt -----
    // ==========================================

    private void UpdateInertiaTilt(float dt)
    {
        Vector2 acceleration =
            CalculateNormalizedAcceleration(dt);

        debugActualAcceleration = acceleration;

        if (debugOverride)
        {
            acceleration = Vector2.ClampMagnitude(
                debugAcceleration, 1f
            );
        }

        // â¡ë¨ìxÇïΩääâª
        float filterFactor =
            1f - Mathf.Exp(-accelerationFilterSpeed * dt);

        filteredAcceleration = Vector2.Lerp(
            filteredAcceleration,
            acceleration,
            filterFactor
        );

        // â¡ë¨ìxÇ∆ãtï˚å¸Ç…åXÇØÇÈ
        Vector2 targetTilt = -filteredAcceleration;

        float smoothTime =
            targetTilt.sqrMagnitude > currentTilt.sqrMagnitude
            ? tiltSmoothTime
            : returnSmoothTime;

        currentTilt = Vector2.SmoothDamp(
            currentTilt,
            targetTilt,
            ref tiltVelocity,
            smoothTime,
            Mathf.Infinity,
            dt
        );

        currentTilt = Vector2.ClampMagnitude(
            currentTilt, 1f
        );

        debugCurrentTilt = currentTilt;

        // ----- 3D Rotation -----

        float rotationX =
            -currentTilt.y * verticalTiltAngle;

        float rotationY =
            currentTilt.x * horizontalTiltAngle;

        visorRoot.localRotation =
            initialVisorRotation *
            Quaternion.Euler(rotationX, rotationY, 0f);

        // èÓïÒHUDÇÕé„Ç≠åXÇØÇÈ
        if (controlHudRotation && hudRoot != null)
        {
            hudRoot.localRotation =
                initialHudRotation *
                Quaternion.Euler(
                    rotationX * hudTiltStrength,
                    rotationY * hudTiltStrength,
                    0f
                );
        }
    }

    // ==========================================
    // ----- Calculate Acceleration -----
    // ==========================================

    private Vector2 CalculateNormalizedAcceleration(float dt)
    {
        if (playerMovement == null)
        {
            hasPreviousVelocity = false;
            return Vector2.zero;
        }

        Vector3 velocity = playerMovement.CurrentVelocity;

        // èdóÕÇ…ÇÊÇÈYï˚å¸ÇÃë¨ìxÇèúäO
        velocity.y = 0f;

        if (!hasPreviousVelocity)
        {
            previousHorizontalVelocity = velocity;
            hasPreviousVelocity = true;
            return Vector2.zero;
        }

        Vector3 acceleration =
            (velocity - previousHorizontalVelocity) / dt;

        previousHorizontalVelocity = velocity;

        Transform player = playerMovement.transform;

        Vector3 right = player.right;
        Vector3 forward = player.forward;

        right.y = 0f;
        forward.y = 0f;

        if (right.sqrMagnitude < 0.0001f ||
            forward.sqrMagnitude < 0.0001f)
        {
            return Vector2.zero;
        }

        right.Normalize();
        forward.Normalize();

        float side = Vector3.Dot(acceleration, right);
        float front = Vector3.Dot(acceleration, forward);

        if (Mathf.Abs(side) < accelerationDeadZone)
            side = 0f;

        if (Mathf.Abs(front) < accelerationDeadZone)
            front = 0f;

        Vector2 normalized = new Vector2(
            side / Mathf.Max(0.1f, sideAccelerationForMaxTilt),
            front / Mathf.Max(0.1f, forwardAccelerationForMaxTilt)
        );

        return Vector2.ClampMagnitude(normalized, 1f);
    }

    // ==========================================
    // ----- Vertical Fall Scale -----
    // ==========================================

    private void UpdateVerticalFallScale(float dt)
    {
        float posture = 0f;
        float fallSpeed = 0f;

        // ----- Debug or Real Player -----

        if (debugFallOverride)
        {
            posture = debugPoseInput;
            fallSpeed = debugFallSpeed;
        }
        else
        {
            if (playerInput != null)
            {
                posture = Mathf.Clamp01(
                    playerInput.PoseInput
                );
            }

            if (playerMovement != null)
            {
                // â∫ï˚å¸ÇÃë¨ìxÇê≥ÇÃílÇ…ïœä∑
                fallSpeed = Mathf.Max(
                    0f,
                    -playerMovement.CurrentVelocity.y
                );
            }
        }

        currentPosture = posture;
        currentFallSpeed = fallSpeed;

        // ----- Posture Factor -----

        // êÖïΩépê® = 0
        // êÇíºépê® = 1
        float postureFactor = Mathf.InverseLerp(
            verticalPostureThreshold,
            1f,
            posture
        );

        // ----- Fall Speed Factor -----

        float maxSpeed = Mathf.Max(
            shrinkStartFallSpeed + 0.01f,
            shrinkMaxFallSpeed
        );

        float speedFactor = Mathf.InverseLerp(
            shrinkStartFallSpeed,
            maxSpeed,
            fallSpeed
        );

        // ----- Target Scale -----

        // êÇíºépê®Ç©Ç¬çÇë¨óéâ∫éûÇÃÇ›èkè¨
        float shrinkFactor = postureFactor * speedFactor;

        float targetScale = Mathf.Lerp(
            1f,
            verticalFallScale,
            shrinkFactor
        );

        // ----- Smooth Scale -----

        float smoothTime =
            targetScale < currentScale
            ? shrinkSmoothTime
            : scaleReturnSmoothTime;

        currentScale = Mathf.SmoothDamp(
            currentScale,
            targetScale,
            ref scaleVelocity,
            smoothTime,
            Mathf.Infinity,
            dt
        );

        // ----- Apply Scale -----

        visorRoot.localScale =
            initialVisorScale * currentScale;

        currentHUDScale = currentScale;
    }

    // ==========================================
    // ----- Find Player -----
    // ==========================================

    private void FindPlayerReferences()
    {
        Player_Movement previousPlayer = playerMovement;

        if (playerMovement == null)
        {
            playerMovement =
                FindFirstObjectByType<Player_Movement>();
        }

        if (playerMovement != previousPlayer)
        {
            playerInput = null;
            hasPreviousVelocity = false;
        }

        if (playerInput == null && playerMovement != null)
        {
            playerInput =
                playerMovement.GetComponent<Player_InputHandler>();
        }
    }

    // ==========================================
    // ----- Reset -----
    // ==========================================

    private void OnDisable()
    {
        if (!initialized)
            return;

        if (visorRoot != null)
        {
            visorRoot.localRotation =
                initialVisorRotation;

            visorRoot.localScale =
                initialVisorScale;
        }

        if (controlHudRotation && hudRoot != null)
        {
            hudRoot.localRotation =
                initialHudRotation;
        }

        filteredAcceleration = Vector2.zero;
        currentTilt = Vector2.zero;
        tiltVelocity = Vector2.zero;

        currentScale = 1f;
        scaleVelocity = 0f;

        currentHUDScale = 1f;
        hasPreviousVelocity = false;
    }
}
