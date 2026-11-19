/*
    SceneController_Title
    20261103  hanaue sho
    タイトルシーンを管理するスクリプト
    Update の処理は基本的に StateMachine にまとめる。
 */
using UnityEngine;

public class SceneController_Title : SceneController_Base
{
    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    [Header("Components")]
    private TitleScene_StateMachine _stateMachine;
    private TitleScene_PresentationController _presentationController;
    private TitleScene_InputHandler _inputHandler;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public TitleScene_StateMachine StateMachine => _stateMachine;
    public TitleScene_PresentationController PresentationController => _presentationController;
    public TitleScene_InputHandler InputHandler => _inputHandler;

    // --------------------------------------------------
    // ----- ChangeSceneName -----
    // --------------------------------------------------
    [Header("ChangeScene Name")]
    [Tooltip("チュートリアル遷移ボタンでの遷移先シーン名")]
    [SerializeField] private string _nameToTutorialScene;
    [Tooltip("レベル遷移ボタンでの遷移先シーン名")]
    [SerializeField] private string _nameToLevelScene;

    // --------------------------------------------------
    // ----- OnClick Reference -----
    // --------------------------------------------------
    [Header("ボタン参照（手動参照）")]
    [SerializeField] private UI_ConfirmWindow _confirmWindow;
    [SerializeField] private UI_Button_Controller _buttonLevel;
    [SerializeField] private UI_Button_Controller _buttonTutorial;
    [SerializeField] private UI_Button_Controller _buttonExit;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _stateMachine = GetComponent<TitleScene_StateMachine>();
        _presentationController = GetComponent<TitleScene_PresentationController>();
        _inputHandler = GetComponent<TitleScene_InputHandler>();

        // ----- Button Events -----
        if (_buttonTutorial != null)
        {
            _buttonTutorial.AddClickListener(OnClick_Totutorial);
        }
        if (_buttonLevel != null)
        {
            _buttonLevel.AddClickListener(OnClick_ToLevel);
        }
        if (_buttonExit != null)
        {
            _buttonExit.AddClickListener(OnClick_Exit);
        }
    }
    private void OnDestroy()
    {
        // ----- Button Events -----
        if (_buttonTutorial != null)
        {
            _buttonTutorial.RemoveClickListener(OnClick_Totutorial);
        }
        if (_buttonLevel != null)
        {
            _buttonLevel.RemoveClickListener(OnClick_ToLevel);
        }
        if (_buttonExit != null)
        {
            _buttonExit.RemoveClickListener(OnClick_Exit);
        }
    }

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        Debug.Log("Title Enter");
    }
    public override void Exit()
    {
        Debug.Log("Title Exit");
    }


    // --------------------------------------------------
    // ----- OnClick Events -----
    // --------------------------------------------------
    public void OnClick_Totutorial()
    {
        if (_confirmWindow != null && _buttonTutorial != null)
        {
            _confirmWindow.Open(_buttonTutorial.ConfirmMessage, _buttonTutorial, Confirm_ToTutorial);
        }
        else
        {
            // 強制遷移
            Confirm_ToTutorial();
        }
    }
    public void OnClick_ToLevel()
    {
        if (_confirmWindow != null && _buttonLevel != null)
        {
            _confirmWindow.Open(_buttonLevel.ConfirmMessage, _buttonLevel, Confirm_ToLevel);
        }
        else
        {
            Confirm_ToLevel();
        }
    }
    public void OnClick_Exit()
    {
        if (_confirmWindow != null && _buttonExit != null)
        {
            _confirmWindow.Open(_buttonExit.ConfirmMessage, _buttonExit, Confirm_Exit);
        }
        else
        {
            Confirm_Exit();
        }
    }

    // --------------------------------------------------
    // ----- Confirm Events -----
    // --------------------------------------------------
    private void Confirm_ToTutorial()
    {
        // チュートリアルシーン遷移
        GameManager.Instance.ChangeScene(_nameToTutorialScene);
    }
    private void Confirm_ToLevel()
    {
        // レベルシーン遷移
        GameManager.Instance.ChangeScene(_nameToLevelScene);
    }
    private void Confirm_Exit()
    {
        // アプリケーション終了
        Application.Quit();
    }

}
