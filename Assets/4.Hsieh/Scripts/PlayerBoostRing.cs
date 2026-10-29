
/*
    PlayerBoostRing.cs
    20261021  作成者：要記入
    プレイヤーの落下加速中にリングエフェクトを生成する。
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBoostRing : MonoBehaviour
{
    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    [Header("リング生成設定")]
    [Tooltip("加速中にリングを生成する間隔（秒）。")]
    [Min(0.05f)]
    [SerializeField] private float _emitInterval = 0.2f;

    [Tooltip("エフェクトが発生する最低落下速度。")]
    [Min(0f)]
    [SerializeField] private float _minFallSpeed = 1f;

    private ParticleSystem _ringParticle;
    private Rigidbody _playerRigidbody;
    private float _emitTimer;
    private bool _wasBoosting;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        _ringParticle = GetComponent<ParticleSystem>();
        _playerRigidbody = GetComponentInParent<Rigidbody>();

        if (_ringParticle == null || _playerRigidbody == null)
        {
            Debug.LogError("[Error] ParticleSystem or Player Rigidbody not found.", this);
            enabled = false;
            return;
        }

        // スクリプトから任意のタイミングで粒子を生成する。
        if (!_ringParticle.isPlaying)
            _ringParticle.Play();
    }

    private void Update()
    {
        UpdateBoostRing();
    }

    // ==================================================
    // ----- Boost Ring Control -----
    // ==================================================

    private void UpdateBoostRing()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Sキーまたは下方向キーで加速状態を判定する。
        bool isBoostPressed =
            keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed;

        float fallSpeed = Mathf.Max(
            0f, -_playerRigidbody.linearVelocity.y
        );

        bool shouldEmit = isBoostPressed && fallSpeed >= _minFallSpeed;

        if (!shouldEmit)
        {
            _wasBoosting = false;
            _emitTimer = 0f;
            return;
        }

        // 加速開始時にリングを1つ生成する。
        if (!_wasBoosting)
        {
            _ringParticle.Emit(1);
            _emitTimer = 0f;
            _wasBoosting = true;
            return;
        }

        // 加速中は一定間隔でリングを生成する。
        _emitTimer += Time.deltaTime;

        if (_emitTimer >= _emitInterval)
        {
            _ringParticle.Emit(1);
            _emitTimer = 0f;
        }
    }
}
