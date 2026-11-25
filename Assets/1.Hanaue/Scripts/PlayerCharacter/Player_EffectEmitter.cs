/*
    Player_EffectEmitter
    20261124  hanaue sho
 */
using UnityEngine;

public class Player_EffectEmitter : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Reference -----
    // --------------------------------------------------
    [Header("Reference")]
    [SerializeField] private Player_Animation _animation;

    // --------------------------------------------------
    // ----- WallSide Effect -----
    // --------------------------------------------------
    [Header("WallSide Effect")]
    [SerializeField] private Transform _wallSideEffectRoot;
    [SerializeField] private ParticleSystem _contactGlow;
    [SerializeField] private ParticleSystem _contactTrace;

    [SerializeField, Min(0.0f)] private float _wallOffset = 0.03f;

    private bool _isWallEffectPlaying;

    // --------------------------------------------------
    // ----- Contact Trace -----
    // --------------------------------------------------
    [Header("Contact Trace")]
    [Tooltip("エフェクト生成間隔")]
    [SerializeField, Min(0.01f)] private float _traceEmissionSpacing = 0.15f;
    [Tooltip("エフェクト生成数")]
    [SerializeField, Min(1)] private int _traceParticlesPerEmission = 6;
    [Tooltip("エフェクト生成半径")]
    [SerializeField, Min(0.0f)] private float _traceSpreadRadius = 0.12f;

    private Vector3 _previousTracePosition;
    private float _traceDistanceCarry;
    private bool _hasPreviousTracePosition;


    // --------------------------------------------------
    // ----- Unity Event -----
    // --------------------------------------------------
    private void Awake()
    {
        _animation = GetComponent<Player_Animation>();

        // 起動時はパーティクルを停止
        if (_contactGlow != null)
        {
            _contactGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (_contactTrace != null)
        {
            _contactTrace.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    private void LateUpdate()
    {
        UpdateWallSideEffect();
    }


    // --------------------------------------------------
    // ----- WallSide Effect -----
    // --------------------------------------------------
    private void UpdateWallSideEffect()
    {
        if (_animation == null || _wallSideEffectRoot == null)
        {
            return;
        }

        // 壁面接触がない場合はエフェクトを停止
        if (!_animation.HasHandContact)
        {
            StopWallSideEffect();
            return;
        }

        // ----- Position -----
        Vector3 contactPoint = _animation.CurrentHandContactPoint;
        Vector3 contactNormal = _animation.CurrentHandContactNormal;

        // 壁面から少し離して配置
        Vector3 effectPosition = contactPoint + contactNormal * _wallOffset;

        _wallSideEffectRoot.position = effectPosition;

        // ----- Play -----
        if (!_isWallEffectPlaying)
        {
            PlayWallSideEffect();
        }

        // 接触軌跡を生成
        UpdateContactTrace(effectPosition, contactNormal);
    }


    // --------------------------------------------------
    // ----- Play -----
    // --------------------------------------------------
    private void PlayWallSideEffect()
    {
        _isWallEffectPlaying = true;

        if (_contactGlow != null)
        {
            _contactGlow.Play();
        }

        if (_contactTrace != null)
        {
            _contactTrace.Play();
        }
    }


    // --------------------------------------------------
    // ----- Stop -----
    // --------------------------------------------------
    private void StopWallSideEffect()
    {
        if (!_isWallEffectPlaying)
        {
            return;
        }

        _isWallEffectPlaying = false;

        // 既存粒子は寿命まで残す
        if (_contactGlow != null)
        {
            _contactGlow.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (_contactTrace != null)
        {
            _contactTrace.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        _hasPreviousTracePosition = false;
        _traceDistanceCarry = 0.0f;
    }


    // --------------------------------------------------
    // ----- Update Contact Trace -----
    // 移動距離に応じて粒子を生成する
    // --------------------------------------------------
    private void UpdateContactTrace(Vector3 position, Vector3 normal)
    {
        if (_contactTrace == null)
        {
            return;
        }

        // 最初の位置を記録
        if (!_hasPreviousTracePosition)
        {
            _previousTracePosition = position;
            _traceDistanceCarry = 0.0f;
            _hasPreviousTracePosition = true;

            EmitContactTrace(position, normal);
            return;
        }

        Vector3 displacement = position - _previousTracePosition;
        float distance = displacement.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        // 前フレームから持ち越した距離を加味
        float nextDistance = _traceEmissionSpacing - _traceDistanceCarry;

        // フレーム間の経路を補間して粒子を配置
        while (nextDistance <= distance)
        {
            float t = nextDistance / distance;
            Vector3 emitPosition = Vector3.Lerp(_previousTracePosition, position, t);

            EmitContactTrace(emitPosition, normal);

            nextDistance += _traceEmissionSpacing;
        }

        _traceDistanceCarry = (_traceDistanceCarry + distance) % _traceEmissionSpacing;
        _previousTracePosition = position;
    }
    // --------------------------------------------------
    // ----- Emit Contact Trace -----
    // 一箇所から複数の粒子を散らばらせて生成
    // --------------------------------------------------
    private void EmitContactTrace(Vector3 position, Vector3 normal)
    {
        normal.Normalize();

        // 壁面上の2方向を求める
        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
        Vector3 bitangent = Vector3.Cross(tangent, normal).normalized;

        for (int i = 0; i < _traceParticlesPerEmission; i++)
        {
            Vector2 random = Random.insideUnitCircle * _traceSpreadRadius;

            Vector3 offset = tangent * random.x + bitangent * random.y;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = position + offset;

            _contactTrace.Emit(emitParams, 1);
        }
    }
}
