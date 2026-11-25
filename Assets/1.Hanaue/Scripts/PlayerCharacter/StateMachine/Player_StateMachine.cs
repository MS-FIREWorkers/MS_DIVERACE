/*
    Player_StateMachine
    20261004  hanaue sho
 */
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum PlayerStateName
{
    None,
    FreeFall,
    Damage,
    WallSide,
}

public class Player_StateMachine : MonoBehaviour
{
    // --------------------------------------------------
    // ----- CharacterController -----
    // --------------------------------------------------
    private Player_CharacterController _characterController;

    // --------------------------------------------------
    // ----- States -----
    // --------------------------------------------------
    private PlayerState_FreeFall _stateFreeFall;
    private PlayerState_Damage _stateDamage;
    private PlayerState_WallSide _stateWallSide;
    public PlayerState_FreeFall StateFreeFall => _stateFreeFall;
    public PlayerState_Damage StateDamage => _stateDamage;
    public PlayerState_WallSide StateWallSide => _stateWallSide;

    // --------------------------------------------------
    // ----- CurrentState -----
    // --------------------------------------------------
    private PlayerState_Base _currentState;
    public PlayerState_Base CurrentState => _currentState;

    // --------------------------------------------------
    // ----- CurrentStateName -----
    // --------------------------------------------------
    [Header("CurrentStateName")]
    [Tooltip("CurrentStateName")]
#pragma warning disable CS0414
    [SerializeField] PlayerStateName _currentStateName;
#pragma warning restore CS0414
    // --------------------------------------------------
    // ----- ControlData -----
    // --------------------------------------------------
    [Header("Control Data")]
    [Tooltip("FreeFallState ControlData")]
    [SerializeField] private PlayerMovement_ControlData _freeFallControlData;
    [Tooltip("DamageState ControlData")]
    [SerializeField] private PlayerMovement_ControlData _damageControlData;
    [Tooltip("WallSideState ControlData")]
    [SerializeField] private PlayerMovement_ControlData _wallSideControlData;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        // 初期化
        _characterController = GetComponent<Player_CharacterController>();
        _stateFreeFall = new PlayerState_FreeFall(_characterController, _freeFallControlData);
        _stateDamage = new PlayerState_Damage(_characterController, _damageControlData);
        _stateWallSide = new PlayerState_WallSide(_characterController, _wallSideControlData);

        ChangeState(_stateFreeFall);
    }
    private void Update()
    {
        // ControllerMode が Manual かチェック
        if ( _characterController.CurrentControllerMode != ControllerMode.Manual)
        {
            return;
        }

        _currentState?.Update();
    }

    // --------------------------------------------------
    // ----- Change State -----
    // --------------------------------------------------
    public void ChangeState(PlayerState_Base newState)
    {
        _currentState?.Exit();

        _currentState = newState;

        _currentState?.Enter();

        ChangeStateName();
    }


    // --------------------------------------------------
    // ----- Change StateName -----
    // --------------------------------------------------
    private void ChangeStateName()
    {
        if (_currentState == _stateFreeFall)
        {
            _currentStateName = PlayerStateName.FreeFall;
        }
        else if (_currentState == _stateDamage)
        {
            _currentStateName = PlayerStateName.Damage;
        }
        else if (_currentState == _stateWallSide)
        {
            _currentStateName = PlayerStateName.WallSide;
        }
    }


}
