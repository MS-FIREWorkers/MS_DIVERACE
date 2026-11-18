/*
    TitleCharacter_Movement
    20261117  hanaue sho
    タイトルキャラクタ立ち絵の動き制御
 */
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TitleCharacter_Movement : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Floating -----
    // --------------------------------------------------
    [Header("Floating")]
    [SerializeField] private float _floatAmplitude = 10.0f;
    [SerializeField] private float _floatFrequency = 0.2f;

    private float _floatTime;
    private float _floatOffset;

    // --------------------------------------------------
    // ----- Spring -----
    // --------------------------------------------------
    [Header("Spring")]
    [SerializeField] private float _inputForce = 300.0f;
    [SerializeField] private float _springStrength = 5.0f;
    [SerializeField] private float _damping = 3.0f;
    [SerializeField] private float _maxOffset = 100.0f;

    private float _moveInput;
    private float _springOffset;
    private float _springVelocity;

    // --------------------------------------------------
    // ----- Rotation Spring -----
    // --------------------------------------------------
    [Header("Rotation Spring")]
    [SerializeField] private float _rotationInputForce = 100.0f;
    [SerializeField] private float _rotationSpringStrength = 15.0f;
    [SerializeField] private float _rotationDamping = 5.0f;
    [SerializeField] private float _maxRotation = 10.0f;

    private float _rotationOffset;
    private float _rotationVelocity;

    private Quaternion _baseRotation;

    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    private RectTransform _rectTransform;
    private Vector2 _basePosition;
    private TitleScene_InputHandler _inputHandler;

    // --------------------------------------------------
    // ----- UnityEvents -----
    // --------------------------------------------------
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _basePosition = _rectTransform.anchoredPosition;
        _baseRotation = _rectTransform.localRotation;
        _inputHandler = FindFirstObjectByType<TitleScene_InputHandler>();
    }
    private void Update()
    {
        float dt = Time.deltaTime;
        // 入力取得
        if (_inputHandler != null)
        {
            SetMoveInput(_inputHandler.Navigate.x);
        }

        UpdateFloating(dt);
        UpdateSpring(dt);
        UpdateRotationSpring(dt);

        ApplyTransform();
    }

    // --------------------------------------------------
    // ----- Floating -----
    // --------------------------------------------------
    private void UpdateFloating(float dt)
    {
        _floatTime += dt;

        // Sin波による上下移動
        _floatOffset = Mathf.Sin(_floatTime * _floatFrequency * Mathf.PI * 2.0f) * _floatAmplitude;
    }

    // --------------------------------------------------
    // ----- Spring -----
    // --------------------------------------------------
    private void UpdateSpring(float dt)
    {
        // 入力による力
        float inputAcceleration = _moveInput * _inputForce;

        // バネによる復元力
        float springAcceleration = -_springOffset * _springStrength;

        // 減衰
        float dampingAcceleration = -_springVelocity * _damping;

        // 合成加速度
        float acceleration = inputAcceleration + springAcceleration + dampingAcceleration;

        // 速度更新
        _springVelocity += acceleration * dt;

        // 位置更新
        _springOffset += _springVelocity * dt;

        // 最大移動範囲の制限
        if (_springOffset > _maxOffset)
        {
            _springOffset = _maxOffset;
            _springVelocity = Mathf.Min(_springVelocity, 0.0f);
        }
        else if (_springOffset < -_maxOffset)
        {
            _springOffset = -_maxOffset;
            _springVelocity = Mathf.Max(_springVelocity, 0.0f);
        }
    }

    // --------------------------------------------------
    // ----- Rotation Spring -----
    // --------------------------------------------------
    private void UpdateRotationSpring(float dt)
    {
        // 入力による回転加速度
        float inputAcceleration = -_moveInput * _rotationInputForce;

        // バネによる復元力
        float springAcceleration = -_rotationOffset * _rotationSpringStrength;

        // 減衰
        float dampingAcceleration = -_rotationVelocity * _rotationDamping;

        // 合成加速度
        float acceleration = inputAcceleration + springAcceleration + dampingAcceleration;

        // 角速度更新
        _rotationVelocity += acceleration * dt;

        // 角度更新
        _rotationOffset += _rotationVelocity * dt;

        // 最大回転角度の制限
        if (_rotationOffset > _maxRotation)
        {
            _rotationOffset = _maxRotation;
            _rotationVelocity = Mathf.Min(_rotationVelocity, 0.0f);
        }
        else if (_rotationOffset < -_maxRotation)
        {
            _rotationOffset = -_maxRotation;
            _rotationVelocity = Mathf.Max(_rotationVelocity, 0.0f);
        }
    }

    // --------------------------------------------------
    // ----- Apply Transform -----
    // --------------------------------------------------
    private void ApplyTransform()
    {
        // 位置
        Vector2 offset = new Vector2(_springOffset, _floatOffset);

        _rectTransform.anchoredPosition = _basePosition + offset;

        // 回転
        _rectTransform.localRotation = _baseRotation * Quaternion.Euler(0.0f, 0.0f, _rotationOffset);
    }

    // --------------------------------------------------
    // ----- Input -----
    // --------------------------------------------------
    public void SetMoveInput(float input)
    {
        _moveInput = Mathf.Clamp(input, -1.0f, 1.0f);
    }
}