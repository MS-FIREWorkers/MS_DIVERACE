/*
    SceneController_Result
    20261103  hanaue sho
    リザルトシーンを管理するスクリプト
    Update の処理は基本的に StateMachine にまとめる。
 */
using UnityEngine;

public class SceneController_Result : SceneController_Base
{
    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    [Header("Components")]
    private ResultScene_StateMachine _stateMachine;
    private ResultScene_PresentationController _presentationController;
    private ResultScene_InputHandler _inputHandler;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public ResultScene_StateMachine StateMachine => _stateMachine;
    public ResultScene_PresentationController PresentationController => _presentationController;
    public ResultScene_InputHandler InputHandler => _inputHandler;

    // --------------------------------------------------
    // ----- ChangeSceneName -----
    // --------------------------------------------------
    [Header("ChangeScene Name")]
    [Tooltip("タイトル遷移ボタンでの遷移先シーン名")]
    [SerializeField] private string _nameToTitleScene;
    public string NameToTitleScene => _nameToTitleScene;

    // --------------------------------------------------
    // ----- ToTitleButton -----
    // --------------------------------------------------
    [Header("ボタン参照（手動参照）")]
    [SerializeField] private UI_ConfirmWindow _confirmWindow;
    [SerializeField] private UI_Button_Controller _buttonToTitleScene;

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
        _stateMachine = GetComponent<ResultScene_StateMachine>();
        _presentationController = GetComponent<ResultScene_PresentationController>();
        _inputHandler = GetComponent<ResultScene_InputHandler>();

        if (_buttonToTitleScene != null )
        {
            _buttonToTitleScene.AddClickListener(OnClick_ToTitle_Hanaue);
        }

        if (RaceResultData == null)
        {
            Debug.LogError("[Error] RaceResultData Is Not Found!!");
        }
    }


    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        Debug.Log("Result Enter");
        AudioManager.Instance.PlayBGM(AudioId.Result);
    }
    public override void Exit()
    {
        Debug.Log("Result Exit");
        AudioManager.Instance.StopBGM();
    }


    // --------------------------------------------------
    // ----- OnClick Events -----
    // --------------------------------------------------
    public void OnClick_ToTitle()
    {
        // 確認ウィンドウを開く（遷移は確認ウィンドウで「Yes」を選んだ時に行う）
        if (_stateMachine == null)
        {
            Debug.LogError("[Error] ResultScene_StateMachine Not Found!");
            return;
        }
        _stateMachine.RequestConfirm();
    }
    public void OnClick_ToTitle_Hanaue()
    {
        if (_confirmWindow != null && _buttonToTitleScene != null)
        {
            _confirmWindow.Open(_buttonToTitleScene.ConfirmMessage, _buttonToTitleScene, Confirm_ToTitle);
        }
        else
        {
            Confirm_ToTitle();
        }
    }

    // --------------------------------------------------
    // ----- Confirm Events -----
    // --------------------------------------------------
    private void Confirm_ToTitle()
    {
        // チュートリアルシーン遷移
        GameManager.Instance.ChangeScene(_nameToTitleScene);
    }
}
