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
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _stateMachine = GetComponent<TitleScene_StateMachine>();
        _presentationController = GetComponent<TitleScene_PresentationController>();
        _inputHandler = GetComponent<TitleScene_InputHandler>();
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
        // チュートリアルシーン遷移
        GameManager.Instance.ChangeScene(_nameToTutorialScene);
    }
    public void OnClick_ToLevel()
    {
        // レベルシーン遷移
        GameManager.Instance.ChangeScene(_nameToLevelScene);
    }
    public void OnClick_Exit()
    {
        // アプリケーション終了
        Application.Quit();
    }

}
