
using UnityEngine;
using UnityEngine.InputSystem;

public class HUDLagInputTest : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private RectTransform _targetUI;

    [Header("à⁄ìÆê›íË")]
    [SerializeField] private float _horizontalOffset = 20f;
    [SerializeField] private float _verticalOffset = 12f;
    [SerializeField] private float _tiltAngle = 2f;

    [Header("äµê´ê›íË")]
    [SerializeField] private float _smoothTime = 0.18f;

    private Vector2 _initialPosition;
    private Quaternion _initialRotation;

    private Vector2 _positionVelocity;
    private float _currentAngle;
    private float _angleVelocity;

    private void Start()
    {
        if (_targetUI == null)
        {
            Debug.LogError("Target UI is missing.", this);
            enabled = false;
            return;
        }

        _initialPosition = _targetUI.anchoredPosition;
        _initialRotation = _targetUI.localRotation;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || _targetUI == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
            horizontal -= 1f;

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
            horizontal += 1f;

        if (keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed)
            vertical += 1f;

        if (keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed)
            vertical -= 1f;

        // HUD âùà⁄ìÆï˚å¸ìIîΩï˚å¸ïŒà⁄
        Vector2 targetPosition = _initialPosition +
            new Vector2(
                -horizontal * _horizontalOffset,
                vertical * _verticalOffset
            );

        float dt = Time.unscaledDeltaTime;
        float smooth = Mathf.Max(0.01f, _smoothTime);

        // ïΩääà⁄ìÆ
        _targetUI.anchoredPosition = Vector2.SmoothDamp(
            _targetUI.anchoredPosition,
            targetPosition,
            ref _positionVelocity,
            smooth,
            Mathf.Infinity,
            dt
        );

        // ïΩääåXéŒ
        float targetAngle = -horizontal * _tiltAngle;

        _currentAngle = Mathf.SmoothDampAngle(
            _currentAngle,
            targetAngle,
            ref _angleVelocity,
            smooth,
            Mathf.Infinity,
            dt
        );

        _targetUI.localRotation = _initialRotation *
            Quaternion.Euler(0f, 0f, _currentAngle);
    }
}
