/*
    LevelScene_StateMachine
    20261104  hanaue sho
    レベルシーンの状態を管理するスクリプト
    以下は管理する状態フロー
    １．オープニングムービー再生（スキップ可）
    ２．ゲームプレイ
    ３．ゴール待ち
    ４．ゴール後演出
    ５．ゴール後ムービー再生（スキップ可）

    ゲームプレイ中は一時停止の処理も加える。
 */
using UnityEngine;

public class LevelScene_StateMachine : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Level _controller;
    private LevelScene_PauseController _pauseController;

    // --------------------------------------------------
    // ----- State -----
    // --------------------------------------------------
    private LevelScene_State_FadeIn _stateFadeIn;
    private LevelScene_State_Opening _stateOpening;
    private LevelScene_State_Countdown _stateCountdown;
    private LevelScene_State_Racing _stateRacing;
    private LevelScene_State_Goal _stateGoal;

    private LevelScene_State_Base _currentState;
    public LevelScene_State_Base CurrentState => _currentState;

    public enum StateType
    {
        FadeIn,
        Opening,
        Countdown,
        Racing,
        Goal,
    }
    [Header("State")]
    [SerializeField] private StateType _initialState = StateType.Countdown;
    [SerializeField] private StateType _currentStateName = StateType.FadeIn;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _controller = GetComponent<SceneController_Level>();
        _pauseController = _controller.PauseController;

        // ----- State -----
        _stateFadeIn = new LevelScene_State_FadeIn(_controller);
        _stateOpening = new LevelScene_State_Opening(_controller);
        _stateCountdown = new LevelScene_State_Countdown(_controller);
        _stateRacing = new LevelScene_State_Racing(_controller);
        _stateGoal = new LevelScene_State_Goal(_controller);


        _currentState = GetState(_initialState);
        ChangeStateName();
    }
    private void Start()
    {
        _currentState?.Enter();
    }
    private void Update()
    {
        // ポーズウィンドウ
        if (_pauseController.IsPaused)
        {
            return;
        }

        _currentState?.Update();
    }

    // --------------------------------------------------
    // ----- ChangeState -----
    // --------------------------------------------------
    public void ChangeState(StateType newState)
    {
        LevelScene_State_Base state = GetState(newState);

        if (state == null || state == _currentState)
        {
            return;
        }

        _currentState?.Exit();

        _currentState = state;

        _currentState?.Enter();

        ChangeStateName();
    }

    // --------------------------------------------------
    // ----- GetState -----
    // --------------------------------------------------
    private LevelScene_State_Base GetState(StateType stateType)
    {
        switch (stateType)
        {
            case StateType.FadeIn:      return _stateFadeIn;
            case StateType.Opening:     return _stateOpening;
            case StateType.Countdown:   return _stateCountdown;
            case StateType.Racing:      return _stateRacing;
            case StateType.Goal:        return _stateGoal;
            default: return null;
        }
    }
    private void ChangeStateName()
    {
        if (_currentState == _stateFadeIn)
        {
            _currentStateName = StateType.FadeIn;
        }
        else if (_currentState == _stateOpening)
        {
            _currentStateName = StateType.Opening;
        }
        else if (_currentState == _stateCountdown)
        {
            _currentStateName = StateType.Countdown;
        }
        else if (_currentState == _stateRacing)
        {
            _currentStateName = StateType.Racing;
        }
        else if (_currentState == _stateGoal)
        {
            _currentStateName = StateType.Goal;
        }
    }
}
