/*
    Player_EffectController
    20261126  hanaue sho
    効果音やコントローラー振動などの演出をまとめるクラス
 */
using TMPro.Examples;
using UnityEngine;

public class Player_EffectController : MonoBehaviour
{
    // --------------------------------------------------
    // ----- References -----
    // --------------------------------------------------
    private Player_CharacterController _characterController;
    private Camera_Controller _cameraController;
    private HapticsManager _hapticManager;

    // --------------------------------------------------
    // ----- Parameters -----
    // --------------------------------------------------
    private bool _isFreeFalling = true;
    private bool _isWallSliding;
    private float _seWindTimer = 0.0f;
    private float _seWindInterval = 3.0f;

    // --------------------------------------------------
    // ----- CameraShake -----
    // --------------------------------------------------
    [Header("CameraShake_FreeFall")]
    [SerializeField] private CameraShake_Data _fallShakeData;
    private CameraShake_Handle _fallShakeHandle;
    [SerializeField] private float _minFallSpeed = 0.0f;
    [SerializeField] private float _maxFallSpeed = 100.0f;
    [SerializeField] private float _minFallIntensity = 0.0f;
    [SerializeField] private float _maxFallIntensity = 1.0f;

    [Header("CameraShake_WallSide")]
    [SerializeField] private CameraShake_Data _wallSideShakeData;
    private CameraShake_Handle _wallSideShakeHandle;
    [SerializeField] private CameraShake_Data _wallSideExitShakeData;


    [Header("CameraShake_CollideObstacle")]
    [SerializeField] private CameraShake_Data _topCollisionShakeData;
    [SerializeField] private CameraShake_Data _sideCollisionShakeData;
    [SerializeField] private CameraShake_Data _topEdgeCollisionShakeData;

    [Header("CameraShake_BoostRing")]
    [SerializeField] private CameraShake_Data _boostShakeData;


    // --------------------------------------------------
    // ----- UnityEvents -----
    // --------------------------------------------------
    private void Awake()
    {
        _characterController = GetComponent<Player_CharacterController>();
        _cameraController = FindFirstObjectByType<Camera_Controller>();
        _hapticManager = FindFirstObjectByType<HapticsManager>();
    }
    private void Start()
    {
        StartFreeFall();
    }
    private void OnEnable()
    {
        _characterController.OnBoostRingEntered += HandleBoostRingEntered;
        _characterController.OnObstacleCollision += HandleObstacleCollision;
        _characterController.OnWallSlideStarted += HandleWallSlideStarted;
        _characterController.OnWallSlideEnded += HandleWallSlideEnded;
    }
    private void OnDisable()
    {
        _characterController.OnBoostRingEntered -= HandleBoostRingEntered;
        _characterController.OnObstacleCollision -= HandleObstacleCollision;
        _characterController.OnWallSlideStarted -= HandleWallSlideStarted;
        _characterController.OnWallSlideEnded -= HandleWallSlideEnded;

        _isWallSliding = false;
    }
    private void Update()
    {
        if (_isFreeFalling)
        {
            UpdateFallEffect();
        }

        if (_isWallSliding)
        {
            UpdateWallSlideEffect();
        }
    }

    // --------------------------------------------------
    // ----- Continuous Effects -----
    // --------------------------------------------------
    private void UpdateFallEffect()
    {
        float fallSpeed = Mathf.Max(0.0f, -_characterController.Movement.CurrentVelocity.y);
        float ratio = Mathf.InverseLerp(_minFallSpeed, _maxFallSpeed, fallSpeed);
        float intensity = Mathf.Lerp(_minFallIntensity, _maxFallIntensity, ratio);

        // 常時カメラシェイク
        if (_fallShakeHandle != null)
        {
            _fallShakeHandle.SetIntensity(intensity);
        }

        // 風切りSE
        _seWindTimer += Time.deltaTime;
        if (_seWindTimer > _seWindInterval)
        {
            _seWindTimer -= _seWindInterval;
            if (ratio > 0.8f)
            {
                AudioManager.Instance.PlaySE(AudioId.FallingWind_01);
            }
            else
            {
                AudioManager.Instance.PlaySE(AudioId.FallingWind_02);
            }
        }

        // 微弱なコントローラー振動
        _hapticManager?.SetFalling(ratio);

    }
    private void UpdateWallSlideEffect()
    {
        // 壁滑り中のカメラシェイク
        // 壁滑り中のループSE
        // 壁滑り中のコントローラー振動
    }

    // --------------------------------------------------
    // ----- Event Handlers -----
    // --------------------------------------------------
    private void HandleBoostRingEntered()
    {
        Debug.Log("[Effect] BoostRingEntered");
        _cameraController.PlayShake(_boostShakeData);
        AudioManager.Instance.PlaySE(AudioId.BoostRingEntry);
    }
    private void HandleObstacleCollision(PlayerImpactType collisionType)
    {
        switch (collisionType)
        {
            case PlayerImpactType.Top:
                _cameraController.PlayShake(_topCollisionShakeData);
                AudioManager.Instance.PlaySE(AudioId.CollideObstacle);
                break;

            case PlayerImpactType.Side:
                _cameraController.PlayShake(_sideCollisionShakeData);
                AudioManager.Instance.PlaySE(AudioId.CollideObstacle);
                break;

            case PlayerImpactType.TopEdge:
                _cameraController.PlayShake(_topEdgeCollisionShakeData);
                AudioManager.Instance.PlaySE(AudioId.CollideObstacle);
                break;

            case PlayerImpactType.TopEdgeGraze:
                AudioManager.Instance.PlaySE(AudioId.Applause);
                break;
        }
        _hapticManager?.PlayObstacleCollision();
    }
    private void HandleWallSlideStarted()
    {
        _isWallSliding = true;

        // ----- CameraShake -----
        if (_wallSideShakeHandle != null)
        {
            return;
        }
        _wallSideShakeHandle = _cameraController.PlayShake(_wallSideShakeData, CameraShakePlayMode.Additive);
        //_wallSideShakeHandle?.SetIntensity(_minFallIntensity);

        AudioManager.Instance.PlaySE(AudioId.Cheers);
        Debug.Log("[Effect] WallSlideStarted");
    }
    private void HandleWallSlideEnded()
    {
        _isWallSliding = false;

        // ----- CameraShake -----
        _cameraController.StopShake(_wallSideShakeHandle);
        _wallSideShakeHandle = null;

        _cameraController.PlayShake(_wallSideExitShakeData); // 加速演出

        Debug.Log("[Effect] WallSlideEnded");
    }

    // --------------------------------------------------
    // ----- Public Event -----
    // --------------------------------------------------
    public void  StartFreeFall()
    {
        _isFreeFalling = true;

        if (_fallShakeHandle != null)
        {
            return;
        }
        _fallShakeHandle = _cameraController.PlayShake(_fallShakeData);
        _fallShakeHandle?.SetIntensity(_minFallIntensity);

        //AudioManager.Instance.PlaySE(AudioId.FallingWind);
    }
    public void  StopFreeFall()
    {
        _isFreeFalling = false;
        _cameraController.StopShake(_fallShakeHandle);
        _fallShakeHandle = null;

        // SEを止めたい
        //AudioManager.Instance.PlaySE(AudioId.FallingWind);
    }

}