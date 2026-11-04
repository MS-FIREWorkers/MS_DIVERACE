
using UnityEngine;
using UnityEngine.InputSystem;

public class HUDLagController : MonoBehaviour
{
    // ==================================================
    // ----- References -----
    // ==================================================

    [Header("éQè∆")]
    [SerializeField] private RectTransform _hudRoot;
    [SerializeField] private RectTransform _visorRoot;

    [SerializeField] private Rigidbody _playerRigidbody;
    [SerializeField] private Transform _targetCamera;

    // ==================================================
    // ----- Movement Settings -----
    // ==================================================

    [Header("ç∂âEà⁄ìÆ")]
    [SerializeField] private float _referenceMoveSpeed = 10f;
    [SerializeField] private float _moveOffset = 8f;
    [SerializeField] private float _moveTilt = 0.8f;

    [Header("ÉJÉÅÉââÒì]")]
    [SerializeField] private float _referenceTurnSpeed = 120f;
    [SerializeField] private float _turnOffsetX = 18f;
    [SerializeField] private float _turnOffsetY = 10f;
    [SerializeField] private float _turnTilt = 1.5f;

    [Header("â¡ë¨óéâ∫")]
    [SerializeField] private float _boostOffsetY = 12f;
    [SerializeField] private float _minBoostFallSpeed = 1f;

    // ==================================================
    // ----- Smoothing Settings -----
    // ==================================================

    [Header("óhÇÍÇÃã≠Ç≥")]
    [SerializeField] private float _visorIntensity = 1f;
    [SerializeField] private float _hudIntensity = 0.45f;

    [Header("í«è]ÇÃíxâÑ")]
    [SerializeField] private float _visorSmoothTime = 0.22f;
    [SerializeField] private float _hudSmoothTime = 0.15f;

    // ==================================================
    // ----- Runtime Data -----
    // ==================================================

    private class LayerState
    {
        public RectTransform Rect;
        public Vector2 InitialPosition;
        public Quaternion InitialRotation;

        public Vector2 PositionVelocity;
        public float CurrentAngle;
        public float AngleVelocity;

        public LayerState(RectTransform rect)
        {
            Rect = rect;
            InitialPosition = rect.anchoredPosition;
            InitialRotation = rect.localRotation;
        }
    }

    private LayerState _hud;
    private LayerState _visor;

    private Vector3 _previousCameraEuler;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        if (_hudRoot == null || _playerRigidbody == null)
        {
            Debug.LogError(
                "HUDRoot or Player Rigidbody is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (_targetCamera == null && Camera.main != null)
            _targetCamera = Camera.main.transform;

        _hud = new LayerState(_hudRoot);

        if (_visorRoot != null)
            _visor = new LayerState(_visorRoot);

        if (_targetCamera != null)
            _previousCameraEuler = _targetCamera.eulerAngles;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        // ----- Player Movement -----

        Vector3 velocity = _playerRigidbody.linearVelocity;

        Vector3 right = _targetCamera != null
            ? _targetCamera.right
            : Vector3.right;

        right.y = 0f;

        if (right.sqrMagnitude > 0.0001f)
            right.Normalize();
        else
            right = Vector3.right;

        float lateralSpeed = Vector3.Dot(velocity, right);

        float moveFactor = Mathf.Clamp(
            lateralSpeed / Mathf.Max(0.01f, _referenceMoveSpeed),
            -1f,
            1f
        );

        // ----- Camera Rotation -----

        float yawFactor = 0f;
        float pitchFactor = 0f;

        if (_targetCamera != null)
        {
            Vector3 currentEuler = _targetCamera.eulerAngles;

            float yawDelta = Mathf.DeltaAngle(
                _previousCameraEuler.y,
                currentEuler.y
            );

            float pitchDelta = Mathf.DeltaAngle(
                _previousCameraEuler.x,
                currentEuler.x
            );

            float yawSpeed = yawDelta / dt;
            float pitchSpeed = pitchDelta / dt;

            float referenceSpeed =
                Mathf.Max(0.01f, _referenceTurnSpeed);

            yawFactor = Mathf.Clamp(
                yawSpeed / referenceSpeed, -1f, 1f
            );

            pitchFactor = Mathf.Clamp(
                pitchSpeed / referenceSpeed, -1f, 1f
            );

            _previousCameraEuler = currentEuler;
        }

        // ----- Boost Input -----

        Keyboard keyboard = Keyboard.current;

        bool boostPressed = keyboard != null &&
            (keyboard.sKey.isPressed ||
             keyboard.downArrowKey.isPressed);

        bool isBoosting = boostPressed &&
            velocity.y < -_minBoostFallSpeed;

        float boostFactor = isBoosting ? 1f : 0f;

        // ----- Target Offset -----

        float targetX =
            -moveFactor * _moveOffset
            - yawFactor * _turnOffsetX;

        float targetY =
            -pitchFactor * _turnOffsetY
            + boostFactor * _boostOffsetY;

        float targetAngle =
            -moveFactor * _moveTilt
            - yawFactor * _turnTilt;

        Vector2 targetOffset = new Vector2(
            targetX,
            targetY
        );

        // HUD text: smaller movement.
        UpdateLayer(
            _hud,
            targetOffset,
            targetAngle,
            _hudIntensity,
            _hudSmoothTime,
            dt
        );

        // Visor: stronger movement.
        if (_visor != null)
        {
            UpdateLayer(
                _visor,
                targetOffset,
                targetAngle,
                _visorIntensity,
                _visorSmoothTime,
                dt
            );
        }
    }

    // ==================================================
    // ----- HUD Lag -----
    // ==================================================

    private void UpdateLayer(
        LayerState layer,
        Vector2 offset,
        float angle,
        float intensity,
        float smoothTime,
        float dt)
    {
        float smooth = Mathf.Max(0.01f, smoothTime);

        Vector2 targetPosition =
            layer.InitialPosition + offset * intensity;

        layer.Rect.anchoredPosition = Vector2.SmoothDamp(
            layer.Rect.anchoredPosition,
            targetPosition,
            ref layer.PositionVelocity,
            smooth,
            Mathf.Infinity,
            dt
        );

        float targetAngle = angle * intensity;

        layer.CurrentAngle = Mathf.SmoothDampAngle(
            layer.CurrentAngle,
            targetAngle,
            ref layer.AngleVelocity,
            smooth,
            Mathf.Infinity,
            dt
        );

        layer.Rect.localRotation =
            layer.InitialRotation *
            Quaternion.Euler(0f, 0f, layer.CurrentAngle);
    }
}
