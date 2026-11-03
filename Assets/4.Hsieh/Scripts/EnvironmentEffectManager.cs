
/*
    EnvironmentEffectManager.cs
    20261014  作成者：要記入
    プレイヤー周辺の環境パーティクルを管理する。
    現在のレベルに応じてエフェクトを切り替える。
*/

using UnityEngine;

public class EnvironmentEffectManager : MonoBehaviour
{
    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    [Header("レベル別環境パーティクル")]
    [Tooltip("Level05からLevel01までの順番でParticleSystemのPrefabを設定する。")]
    [SerializeField] private ParticleSystem[] _levelParticlePrefabs = new ParticleSystem[5];

    [Header("発生位置")]
    [Tooltip("プレイヤーを基準としたパーティクル発生位置のオフセット。")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, -5f, 0f);

    private Transform _player;
    private ParticleSystem _currentParticles;
    private int _currentLevelIndex = -1;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        // Playerタグからプレイヤーを自動取得する。
        GameObject playerObject = GameObject.FindWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("[Error] Player not found.", this);
            enabled = false;
            return;
        }

        _player = playerObject.transform;

        if (_levelParticlePrefabs == null || _levelParticlePrefabs.Length != 5)
            Debug.LogWarning("[Warning] Please configure 5 level particle slots.", this);
    }

    private void Start()
    {
        // ゲーム開始時はLevel05のエフェクトを使用する。
        if (enabled)
            SetLevel(0);
    }

    private void LateUpdate()
    {
        if (_player == null || _currentParticles == null)
            return;

        // 発生器のみプレイヤーを追従させる。
        _currentParticles.transform.position = _player.position + _spawnOffset;
    }

    // ==================================================
    // ----- Level Effect Control -----
    // ==================================================

    // 現在のレベルに対応する環境パーティクルへ切り替える。
    public void SetLevel(int levelIndex)
    {
        if (!enabled || levelIndex == _currentLevelIndex)
            return;

        if (_levelParticlePrefabs == null ||
            levelIndex < 0 || levelIndex >= _levelParticlePrefabs.Length)
        {
            Debug.LogError($"[Error] Invalid particle level index: {levelIndex}", this);
            return;
        }

        _currentLevelIndex = levelIndex;

        // 前のレベルのパーティクルを停止・破棄する。
        if (_currentParticles != null)
        {
            _currentParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(_currentParticles.gameObject);
            _currentParticles = null;
        }

        ParticleSystem prefab = _levelParticlePrefabs[levelIndex];

        if (prefab == null)
        {
            Debug.LogWarning($"[Warning] Particle prefab is not assigned: {levelIndex}", this);
            return;
        }

        // 必要なレベルのPrefabだけを生成する。
        Vector3 spawnPosition = _player.position + _spawnOffset;
        _currentParticles = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
        _currentParticles.name = $"Particle_Level{5 - levelIndex:00}";

        // 発生済みの粒子はワールド空間に残す。
        ParticleSystem[] systems = _currentParticles.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem system in systems)
        {
            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            system.Clear();
        }

        _currentParticles.Play(true);
    }
}
