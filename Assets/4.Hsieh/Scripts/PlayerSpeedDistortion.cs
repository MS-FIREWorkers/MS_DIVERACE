
/*
    PlayerSpeedDistortion.cs
    20261015  作成者：要記入
    プレイヤーの落下速度と画面上の位置に応じて、
    周囲の空気の歪みエフェクトを制御する。
*/

using UnityEngine;

public class PlayerSpeedDistortion : MonoBehaviour
{
    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    [Header("落下速度")]
    [Tooltip("歪みが発生し始める落下速度。")]
    [Min(0f)]
    [SerializeField] private float _minSpeed = 10f;

    [Tooltip("歪みが最大になる落下速度。")]
    [Min(0f)]
    [SerializeField] private float _maxSpeed = 50f;

    [Header("歪み設定")]
    [Tooltip("最大歪み強度。値を大きくすると背景の変形が強くなる。")]
    [Range(0f, 0.05f)]
    [SerializeField] private float _maxStrength = 0.012f;

    [Tooltip("歪み強度の変化速度。")]
    [Min(0.001f)]
    [SerializeField] private float _transitionSpeed = 0.04f;

    [Tooltip("歪ませない中心部分の半径。画面高さを基準とする。")]
    [Range(0f, 0.4f)]
    [SerializeField] private float _innerRadius = 0.1f;

    [Tooltip("歪みが消える外側の半径。画面高さを基準とする。")]
    [Range(0.05f, 0.7f)]
    [SerializeField] private float _outerRadius = 0.3f;

    [Tooltip("プレイヤー位置から歪みの中心までのY方向オフセット。")]
    [SerializeField] private float _centerOffsetY = 1f;

    [Header("デバッグ")]
    [Tooltip("有効にすると、落下速度に関係なく固定強度で歪みを表示する。")]
    [SerializeField] private bool _isTestMode = false;

    [Tooltip("デバッグ中に使用する歪み強度。")]
    [Range(0f, 0.05f)]
    [SerializeField] private float _testStrength = 0.02f;

    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private Camera _mainCamera;
    private float _currentStrength;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        // Playerタグからプレイヤーを取得する。
        GameObject player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogError("[Error] Player not found.", this);
            enabled = false;
            return;
        }

        _playerTransform = player.transform;
        _playerRigidbody = player.GetComponent<Rigidbody>();
        _mainCamera = Camera.main;

        if (_playerRigidbody == null || _mainCamera == null)
        {
            Debug.LogError("[Error] Rigidbody or MainCamera not found.", this);
            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        UpdateDistortion();
    }

    private void OnDisable()
    {
        // 停止時に歪みを無効化する。
        Shader.SetGlobalFloat("_SpeedDistortionStrength", 0f);
        _currentStrength = 0f;
    }

    private void OnValidate()
    {
        _maxSpeed = Mathf.Max(_minSpeed + 0.01f, _maxSpeed);
        _outerRadius = Mathf.Max(_innerRadius + 0.01f, _outerRadius);
    }

    // ==================================================
    // ----- Distortion Control -----
    // ==================================================

    // プレイヤーの落下速度と画面座標から歪みを更新する。
    private void UpdateDistortion()
    {
        if (_playerTransform == null || _playerRigidbody == null || _mainCamera == null)
            return;

        // プレイヤーの下降速度のみ取得する。
        float speed = Mathf.Max(0f, -_playerRigidbody.linearVelocity.y);
        float ratio = Mathf.InverseLerp(_minSpeed, _maxSpeed, speed);
        float targetStrength = _isTestMode
            ? _testStrength
            : Mathf.Lerp(0f, _maxStrength, ratio);

        // 歪み強度を滑らかに変化させる。
        _currentStrength = Mathf.MoveTowards(
            _currentStrength,
            targetStrength,
            _transitionSpeed * Time.deltaTime
        );

        // プレイヤーのワールド座標を画面上のUV座標へ変換する。
        Vector3 targetPosition = _playerTransform.position + Vector3.up * _centerOffsetY;
        Vector3 viewportPosition = _mainCamera.WorldToViewportPoint(targetPosition);

        // プレイヤーが画面外にいる場合は歪みを無効にする。
        if (viewportPosition.z <= 0f ||
            viewportPosition.x < 0f || viewportPosition.x > 1f ||
            viewportPosition.y < 0f || viewportPosition.y > 1f)
        {
            Shader.SetGlobalFloat("_SpeedDistortionStrength", 0f);
            return;
        }

        // Shaderへ歪みの中心、範囲、強度を渡す。
        Shader.SetGlobalVector("_SpeedDistortionCenter",
            new Vector4(viewportPosition.x, viewportPosition.y, 0f, 0f));

        Shader.SetGlobalFloat("_SpeedDistortionAspect", _mainCamera.aspect);
        Shader.SetGlobalFloat("_SpeedDistortionInnerRadius", _innerRadius);
        Shader.SetGlobalFloat("_SpeedDistortionOuterRadius", _outerRadius);
        Shader.SetGlobalFloat("_SpeedDistortionStrength", _currentStrength);
    }
}
