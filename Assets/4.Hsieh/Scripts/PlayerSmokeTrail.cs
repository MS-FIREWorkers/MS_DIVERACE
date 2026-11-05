
/*
    PlayerSmokeTrail.cs
    20261021  作成者：要記入
    プレイヤーの落下速度に応じて煙のトレイルを制御する。
*/

using UnityEngine;

public class PlayerSmokeTrail : MonoBehaviour
{
    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    [Header("速度設定")]
    [Tooltip("トレイルが発生し始める落下速度。")]
    [SerializeField] private float _minSpeed = 10f;

    [Header("最大速度")]
    [Tooltip("トレイルが最大になる落下速度。")]
    [SerializeField] private float _maxSpeed = 25f;

    [Header("幅設定")]
    [Tooltip("最大速度時のトレイル幅。")]
    [SerializeField] private float _maxWidth = 1.2f;

    [Header("残存時間")]
    [Tooltip("最大速度時のトレイルの残存時間。")]
    [SerializeField] private float _maxTime = 0.8f;

    [Header("変化速度")]
    [Tooltip("トレイルの幅と長さが変化する速度。")]
    [SerializeField] private float _transitionSpeed = 5f;

    private Rigidbody _playerRigidbody;
    private TrailRenderer[] _trails;
    private float _currentRatio;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        _playerRigidbody = GetComponentInParent<Rigidbody>();
        _trails = GetComponentsInChildren<TrailRenderer>(true);

        if (_playerRigidbody == null)
        {
            Debug.LogError("[Error] Player Rigidbody not found.", this);
            enabled = false;
            return;
        }

        if (_trails.Length == 0)
        {
            Debug.LogError("[Error] TrailRenderer not found.", this);
            enabled = false;
            return;
        }

        foreach (TrailRenderer trail in _trails)
        {
            trail.emitting = false;
            trail.Clear();
        }
    }

    private void Update()
    {
        UpdateSmokeTrail();
    }

    private void OnDisable()
    {
        if (_trails == null) return;

        foreach (TrailRenderer trail in _trails)
            trail.emitting = false;
    }

    // ==================================================
    // ----- Smoke Trail Control -----
    // ==================================================

    private void UpdateSmokeTrail()
    {
        // プレイヤーの下方向の速度を取得する。
        float speed = Mathf.Max(0f, -_playerRigidbody.linearVelocity.y);
        float targetRatio = Mathf.InverseLerp(_minSpeed, _maxSpeed, speed);

        // 急激な変化を防ぎ、滑らかに切り替える。
        _currentRatio = Mathf.MoveTowards(
            _currentRatio,
            targetRatio,
            _transitionSpeed * Time.deltaTime
        );

        foreach (TrailRenderer trail in _trails)
        {
            // 最低速度以上の場合に新しいトレイルを生成する。
            trail.emitting = speed > _minSpeed;

            // 速度に応じて幅と残存時間を変更する。
            trail.widthMultiplier = Mathf.Lerp(0.15f, _maxWidth, _currentRatio);
            trail.time = Mathf.Lerp(0.2f, _maxTime, _currentRatio);
        }
    }
}
