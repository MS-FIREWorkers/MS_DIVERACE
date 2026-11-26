/*
    Player_CharacterController
    20261004  hanaue sho
    PlayerCharacter のコンポーネントをまとめるもの
    外部からの窓口
 */
using UnityEngine;
using System;


public enum ControllerMode
{
    Manual,   // 操作
    Cutscene, // カットシーン用
}

public class Player_CharacterController : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    [Header("Components")]
    private Player_InputHandler _input;
    private Player_Movement _movement;
    private Player_StateMachine _stateMachine;
    private Player_CollisionDetector _collisionDetector;
    private Player_Animation _animation;
    private Player_EffectEmitter _effectEmitter;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public Player_InputHandler InputHandler => _input;
    public Player_Movement Movement => _movement;
    public Player_StateMachine StateMachine => _stateMachine;
    public Player_CollisionDetector CollisionDetector => _collisionDetector;
    public Player_Animation Animation => _animation;
    public Player_EffectEmitter EffectEmitter => _effectEmitter;

    // --------------------------------------------------
    // ----- ControllerMode -----
    // --------------------------------------------------
    [Header("ControllerMode")]
    [Tooltip("現在のコントロール主導権")]
    [SerializeField] private ControllerMode _currentControllerMode = ControllerMode.Manual;
    public ControllerMode CurrentControllerMode => _currentControllerMode;

    // --------------------------------------------------
    // ----- Events -----
    // --------------------------------------------------
    public event Action OnBoostRingEntered; 
    public event Action<PlayerImpactType> OnObstacleCollision;
    public event Action OnWallSlideStarted;
    public event Action OnWallSlideEnded;



    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    void Awake()
    {
        _input = GetComponent<Player_InputHandler>();
        _movement = GetComponent<Player_Movement>();
        _stateMachine = GetComponent<Player_StateMachine>();
        _collisionDetector = GetComponent<Player_CollisionDetector>();
        _animation = GetComponent<Player_Animation>();
    }
    void Update()
    {
        
    }


    // --------------------------------------------------
    // ----- CameraTargetData -----
    // --------------------------------------------------
    public CameraTargetData GetCameraTargetData()
    {
        CameraTargetData data = new CameraTargetData();

        data.Position = transform.position;
        data.Forward = transform.forward;
        data.Right = transform.right;

        data.Velocity = Movement.CurrentVelocity;
        data.Posture = InputHandler.PoseInput;

        data.IsWallSide = StateMachine.CurrentState == StateMachine.StateWallSide;
        data.WallNormal = data.IsWallSide ? StateMachine.StateWallSide.WallNormal : Vector3.zero;

        return data;
    }

    // --------------------------------------------------
    // ----- RequestBoost -----
    // --------------------------------------------------
    public void ApplyBoost(BoostRequest request)
    {
        _movement.ApplyBoost(request);
    }

    // --------------------------------------------------
    // ----- Event Notification -----
    // --------------------------------------------------
    public void NotifyBoostRingEntered()
    {
        OnBoostRingEntered?.Invoke();
    }
    public void NotifyObstacleCollision(PlayerImpactType impactType)
    {
        OnObstacleCollision?.Invoke(impactType);
    }
    public void NotifyWallSlideStarted()
    {
        OnWallSlideStarted?.Invoke();
    }
    public void NotifyWallSlideEnded()
    {
        OnWallSlideEnded?.Invoke();
    }


    // --------------------------------------------------
    // ----- Public Event -----
    // --------------------------------------------------
    public void StartPlay()
    {
        _currentControllerMode = ControllerMode.Manual;
    }
    public void PausePlay()
    {
        _currentControllerMode = ControllerMode.Cutscene;
    }

}
