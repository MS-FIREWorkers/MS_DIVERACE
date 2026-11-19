
// ==================================================
// File: SimpleFreeCamera.cs
// Created: 2026/10/
// Author: 
// Summary: 指定または自動取得したカメラを自由操作する
// ==================================================

using UnityEngine;
using UnityEngine.InputSystem;

// ==================================================
// SimpleFreeCamera
// ==================================================

public class SimpleFreeCamera : MonoBehaviour
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Camera Settings -----

    [Header("Camera Settings")]
    [Tooltip("操作するカメラ。未指定ならMainCameraを自動取得")]
    [SerializeField]
    private Camera _targetCamera;

    // ----- Movement Settings -----

    [Header("Movement Settings")]
    [Tooltip("通常時の移動速度")]
    [SerializeField, Min(0f)]
    private float _moveSpeed = 5.0f;

    [Tooltip("Shiftキー押下中の移動速度")]
    [SerializeField, Min(0f)]
    private float _fastSpeed = 15.0f;

    // ----- Rotation Settings -----

    [Header("Rotation Settings")]
    [Tooltip("マウスによる視点回転の感度")]
    [SerializeField, Min(0f)]
    private float _mouseSensitivity = 0.15f;

    // ----- Runtime Variables -----

    private Transform _cameraTransform;

    private float _yaw;
    private float _pitch;

    // ==================================================
    // Unity Functions
    // ==================================================

    private void Awake()
    {
        InitializeCamera();
    }

    private void Start()
    {
        InitializeRotation();
    }

    private void Update()
    {
        if (_cameraTransform == null)
            return;

        if (Keyboard.current == null)
            return;

        MoveCamera();
        RotateCamera();
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Initialization -----

    private void InitializeCamera()
    {
        // Inspector未指定ならMainCameraを取得
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }

        // 取得失敗
        if (_targetCamera == null)
        {
            Debug.LogError(
                "[Error] SimpleFreeCamera: " +
                "操作対象のCameraが見つかりません。",
                this
            );

            enabled = false;
            return;
        }

        _cameraTransform = _targetCamera.transform;
    }

    private void InitializeRotation()
    {
        if (_cameraTransform == null)
            return;

        Vector3 angles = _cameraTransform.eulerAngles;

        _yaw = angles.y;
        _pitch = angles.x;

        if (_pitch > 180f)
            _pitch -= 360f;
    }

    // ----- Camera Movement -----

    private void MoveCamera()
    {
        Keyboard kb = Keyboard.current;

        Vector3 move = Vector3.zero;

        if (kb.wKey.isPressed)
            move += Vector3.forward;

        if (kb.sKey.isPressed)
            move += Vector3.back;

        if (kb.aKey.isPressed)
            move += Vector3.left;

        if (kb.dKey.isPressed)
            move += Vector3.right;

        if (kb.qKey.isPressed)
            move += Vector3.up;

        if (kb.eKey.isPressed)
            move += Vector3.down;

        float speed = kb.leftShiftKey.isPressed
            ? _fastSpeed
            : _moveSpeed;

        _cameraTransform.Translate(
            move.normalized * speed * Time.deltaTime,
            Space.Self
        );
    }

    // ----- Camera Rotation -----

    private void RotateCamera()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.rightButton.isPressed)
            return;

        Vector2 delta = mouse.delta.ReadValue();

        _yaw += delta.x * _mouseSensitivity;
        _pitch -= delta.y * _mouseSensitivity;

        _pitch = Mathf.Clamp(_pitch, -89f, 89f);

        _cameraTransform.rotation = Quaternion.Euler(
            _pitch,
            _yaw,
            0f
        );
    }
}
