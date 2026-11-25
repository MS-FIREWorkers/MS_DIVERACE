/*
    SceneController_Level
    20261103  hanaue sho
    レベルシーンを管理するスクリプト
    Update の処理は基本的に StateMachine にまとめる。
 */
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class SceneController_Level : SceneController_Base
{
    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    [Header("Components")]
    private LevelScene_StateMachine _stateMachine;
    private LevelScene_PauseController _pauseController;
    private LevelScene_GuidView _guidView;
    private LevelScene_InputHandler _inputHandler;
    private LevelScene_Timer _timer;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public LevelScene_StateMachine StateMachine => _stateMachine;
    public LevelScene_PauseController PauseController => _pauseController;
    public LevelScene_GuidView GuidView => _guidView;
    public LevelScene_InputHandler InputHandler => _inputHandler;
    public LevelScene_Timer Timer => _timer;

    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    [SerializeField] private Player_CharacterController _player;
    public Player_CharacterController Player => _player;

    // --------------------------------------------------
    // ----- ChangeScene -----
    // --------------------------------------------------
    [Header("ChangeSceneName")]
    [SerializeField] private string _resultSceneName;

    // --------------------------------------------------
    // ----- RaceResultData -----
    // --------------------------------------------------
    [Header("RaceResultData（手動参照）")]
    [SerializeField] private RaceResultData _raceResultData;
    public RaceResultData RaceResultData => _raceResultData;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _stateMachine = GetComponent<LevelScene_StateMachine>();
        _pauseController = GetComponent<LevelScene_PauseController>();
        _guidView = GetComponent<LevelScene_GuidView>();
        _inputHandler = GetComponent<LevelScene_InputHandler>();
        _timer = GetComponent<LevelScene_Timer>();

        _player = FindFirstObjectByType<Player_CharacterController>();
        if ( _player == null )
        {
            Debug.LogError("[Error] Player Not Found!!");
        }

        if (RaceResultData == null)
        {
            Debug.LogError("[Error] RaceResultData Is Not Found!!");
        }
        RaceResultData.ResetResult();
    }
    private void Update()
    {
        // ----- ポーズ入力処理 -----
        if (_inputHandler.PauseInput)
        {
            _inputHandler.ConsumePauseInput();

            _pauseController.TogglePause();
        }

    }

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        Debug.Log("Level Enter");
    }
    public override void Exit()
    {
        Debug.Log("Level Exit");
    }


    // --------------------------------------------------
    // ----- OnPlayerGoal -----
    // --------------------------------------------------
    public void OnPlayerGoal()
    {
        // レース中以外なら無視
        if (_stateMachine.CurrentStateName != LevelScene_StateMachine.StateType.Racing)
        {
            return;
        }

        _stateMachine.ChangeState(LevelScene_StateMachine.StateType.Goal);
    }
    public void ChangeSceneResult()
    {
        GameManager.Instance.ChangeScene(_resultSceneName);
    }

}
