
/*
    WindMistAutoFit.cs
    20261022  作成者：要記入

    風エリアの範囲・風向に合わせて
    霧パーティクルの発生範囲・発生数・移動方向を自動調整する。
*/

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
public class WindMistAutoFit : MonoBehaviour
{
    // ==================================================
    // ----- Area Settings -----
    // ==================================================

    [Header("発生範囲")]

    [Tooltip("風エリアに対するパーティクル発生範囲の割合。")]
    [Range(0.1f, 1f)]
    [SerializeField] private float _areaCoverage = 0.9f;

    // ==================================================
    // ----- Emission Settings -----
    // ==================================================

    [Header("発生数")]

    [Tooltip("単位体積あたりのパーティクル発生密度。")]
    [Min(0f)]
    [SerializeField] private float _particleDensity = 0.00003f;

    [Tooltip("1秒あたりの最大発生数。")]
    [Min(1f)]
    [SerializeField] private float _maxEmissionRate = 30f;

    // ==================================================
    // ----- Wind Movement Settings -----
    // ==================================================

    [Header("風向・移動速度")]

    [Tooltip("親のWindAreaの風向に自動追従する。")]
    [SerializeField] private bool _isFollowWindDirection = true;

    [Tooltip("霧パーティクルの移動速度。")]
    [Min(0f)]
    [SerializeField] private float _mistMoveSpeed = 8f;

    // ==================================================
    // ----- Runtime Fields -----
    // ==================================================

    private ParticleSystem _particleSystem;
    private BoxCollider _areaCollider;
    private WindArea _windArea;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void OnEnable()
    {
        CacheReferences();
        SyncSettings();
    }

    private void Update()
    {
        if (_particleSystem == null ||
            _areaCollider == null ||
            _windArea == null)
        {
            CacheReferences();
        }

        SyncSettings();
    }

    private void OnTransformParentChanged()
    {
        _areaCollider = null;
        _windArea = null;

        CacheReferences();
        SyncSettings();
    }

    // ==================================================
    // ----- Reference Setup -----
    // ==================================================

    private void CacheReferences()
    {
        if (_particleSystem == null)
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        _windArea = GetComponentInParent<WindArea>();

        if (_windArea != null)
        {
            _areaCollider = _windArea.GetComponent<BoxCollider>();
        }
        else
        {
            _areaCollider = null;
        }
    }

    // ==================================================
    // ----- Settings Synchronization -----
    // ==================================================

    private void SyncSettings()
    {
        if (_particleSystem == null)
            return;

        SyncArea();
        SyncWindDirection();
    }

    // ==================================================
    // ----- Area Synchronization -----
    // ==================================================

    private void SyncArea()
    {
        if (_areaCollider == null)
            return;

        // 風エリアの中心にパーティクルを配置する。
        if (transform.localPosition != Vector3.zero)
        {
            transform.localPosition = Vector3.zero;
        }

        if (transform.localRotation != Quaternion.identity)
        {
            transform.localRotation = Quaternion.identity;
        }

        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
        }

        // BoxColliderの大きさから発生範囲を計算する。
        Vector3 areaSize =
            _areaCollider.size * _areaCoverage;

        var shape = _particleSystem.shape;

        if (!shape.enabled)
        {
            shape.enabled = true;
        }

        if (shape.shapeType != ParticleSystemShapeType.Box)
        {
            shape.shapeType = ParticleSystemShapeType.Box;
        }

        if (shape.scale != areaSize)
        {
            shape.scale = areaSize;
        }

        // BoxColliderの中心に合わせる。
        Vector3 areaCenter = _areaCollider.center;

        if (shape.position != areaCenter)
        {
            shape.position = areaCenter;
        }

        if (shape.rotation != Vector3.zero)
        {
            shape.rotation = Vector3.zero;
        }

        // 発生数をエリアの体積に応じて計算する。
        float volume =
            areaSize.x * areaSize.y * areaSize.z;

        float emissionRate = Mathf.Min(
            volume * _particleDensity,
            _maxEmissionRate
        );

        var emission = _particleSystem.emission;

        if (!emission.enabled)
        {
            emission.enabled = true;
        }

        ParticleSystem.MinMaxCurve currentRate =
            emission.rateOverTime;

        if (IsDifferent(currentRate, emissionRate))
        {
            emission.rateOverTime =
                new ParticleSystem.MinMaxCurve(emissionRate);
        }
    }

    // ==================================================
    // ----- Wind Direction Synchronization -----
    // ==================================================

    private void SyncWindDirection()
    {
        if (_windArea == null || !_isFollowWindDirection)
            return;

        // WindAreaからワールド座標の風向を取得する。
        Vector3 worldDirection =
            _windArea.GetWorldDirection();

        if (worldDirection.sqrMagnitude > 0.0001f)
        {
            worldDirection.Normalize();
        }
        else
        {
            worldDirection = Vector3.zero;
        }

        // ワールド座標からパーティクルのローカル座標へ変換する。
        Vector3 localDirection =
            transform.InverseTransformDirection(worldDirection);

        Vector3 targetVelocity =
            localDirection * _mistMoveSpeed;

        // 初速度を0にして、Velocity over Lifetimeだけで移動する。
        var main = _particleSystem.main;

        if (main.simulationSpace !=
            ParticleSystemSimulationSpace.Local)
        {
            main.simulationSpace =
                ParticleSystemSimulationSpace.Local;
        }

        ParticleSystem.MinMaxCurve currentStartSpeed =
            main.startSpeed;

        if (IsDifferent(currentStartSpeed, 0f))
        {
            main.startSpeed = 0f;
        }

        // 移動方向をローカル座標で設定する。
        var velocity = _particleSystem.velocityOverLifetime;

        if (!velocity.enabled)
        {
            velocity.enabled = true;
        }

        if (velocity.space !=
            ParticleSystemSimulationSpace.Local)
        {
            velocity.space =
                ParticleSystemSimulationSpace.Local;
        }

        // X方向の速度を更新する。
        if (IsDifferent(velocity.x, targetVelocity.x))
        {
            velocity.x =
                new ParticleSystem.MinMaxCurve(targetVelocity.x);
        }

        // Y方向の速度を更新する。
        if (IsDifferent(velocity.y, targetVelocity.y))
        {
            velocity.y =
                new ParticleSystem.MinMaxCurve(targetVelocity.y);
        }

        // Z方向の速度を更新する。
        if (IsDifferent(velocity.z, targetVelocity.z))
        {
            velocity.z =
                new ParticleSystem.MinMaxCurve(targetVelocity.z);
        }
    }

    // ==================================================
    // ----- Utility -----
    // ==================================================

    private bool IsDifferent(
        ParticleSystem.MinMaxCurve current,
        float target)
    {
        // Constant以外のモードなら設定を変更する。
        if (current.mode != ParticleSystemCurveMode.Constant)
        {
            return true;
        }

        // 値が変化している場合だけ更新する。
        return !Mathf.Approximately(
            current.constant,
            target
        );
    }
}
