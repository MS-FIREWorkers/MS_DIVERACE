
/*
    PlayerGlowEffect.cs
    20261014  作成者：要記入
    プレイヤーの移動速度に応じて複数の発光パーティクルを制御する。
*/

using UnityEngine;

public class PlayerGlowEffect : MonoBehaviour
{
    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    [Header("移動速度")]
    [Tooltip("パーティクルの発生量が増え始める速度。")]
    [Min(0f)]
    [SerializeField] private float _minSpeed = 5f;

    [Tooltip("パーティクルの発生量が最大になる速度。")]
    [Min(0f)]
    [SerializeField] private float _maxSpeed = 40f;

    [Header("パーティクル発生量")]
    [Tooltip("低速時の全発生地点を合計した1秒あたりの発生数。")]
    [Min(0f)]
    [SerializeField] private float _minEmission = 5f;

    [Tooltip("高速時の全発生地点を合計した1秒あたりの発生数。")]
    [Min(0f)]
    [SerializeField] private float _maxEmission = 80f;

    private Rigidbody _playerRigidbody;
    private ParticleSystem[] _particleSystems;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        // 親階層からPlayerのRigidbodyを取得する。
        _playerRigidbody = GetComponentInParent<Rigidbody>();

        // 子階層にある全てのParticleSystemを取得する。
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        if (_playerRigidbody == null)
        {
            Debug.LogError("[Error] Player Rigidbody not found.", this);
            enabled = false;
            return;
        }

        if (_particleSystems.Length == 0)
        {
            Debug.LogError("[Error] ParticleSystem not found.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled) return;

        foreach (ParticleSystem particle in _particleSystems)
        {
            // 発生済みの粒子はワールド空間に残す。
            var main = particle.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particle.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            particle.Play();
        }
    }

    private void Update()
    {
        UpdateEmission();
    }

    private void OnValidate()
    {
        _maxSpeed = Mathf.Max(_maxSpeed, _minSpeed + 0.01f);
        _maxEmission = Mathf.Max(_maxEmission, _minEmission);
    }

    // ==================================================
    // ----- Particle Control -----
    // ==================================================

    // 移動速度に応じて全パーティクルの発生量を変更する。
    private void UpdateEmission()
    {
        if (_playerRigidbody == null || _particleSystems == null ||
            _particleSystems.Length == 0) return;

        float speed = _playerRigidbody.linearVelocity.magnitude;
        float ratio = Mathf.InverseLerp(_minSpeed, _maxSpeed, speed);

        // 全発生地点の合計発生量を計算する。
        float totalRate = Mathf.Lerp(_minEmission, _maxEmission, ratio);

        // 発生地点の数で分配する。
        float ratePerParticle = totalRate / _particleSystems.Length;

        foreach (ParticleSystem particle in _particleSystems)
        {
            if (particle == null) continue;

            var emission = particle.emission;
            emission.rateOverTime = ratePerParticle;
        }
    }
}
