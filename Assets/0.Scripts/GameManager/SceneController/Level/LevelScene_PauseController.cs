/*
    LevelScene_PauseController
    20261104  hanaue sho
    レベルシーンでポーズウィンドウに指示を出すスクリプト
    ポーズボタンを押した際にこのスクリプトを経由してポーズウィンドウを有効化、無効化を行う
 */
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelScene_PauseController : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Level _controller;

    // --------------------------------------------------
    // ----- Pause -----
    // --------------------------------------------------
    private bool _isPaused = false;

    public bool IsPaused => _isPaused;

    // --------------------------------------------------
    // ----- PauseWindow -----
    // --------------------------------------------------
    [Header("PauseWindow（手動参照）")]
    [SerializeField] private GameObject _pauseWindow;

    // --------------------------------------------------
    // ----- UI_ButtonSelectionController -----
    // --------------------------------------------------
    [Header("UI_ButtonSelectionController（手動参照）")]
    [SerializeField] private UI_ButtonSelectionController _buttonSelectionController;

    // --------------------------------------------------
    // ----- OnClick Reference -----
    // --------------------------------------------------
    [Header("ボタン参照（手動参照）")]
    [SerializeField] private UI_ConfirmWindow _confirmWindow;
    [SerializeField] private UI_Button_Controller _buttonResume;
    [SerializeField] private UI_Button_Controller _buttonRestart;
    [SerializeField] private UI_Button_Controller _buttonToTitle;

    // --------------------------------------------------
    // ----- ChangeSceneName -----
    // --------------------------------------------------
    [Header("ChangeScene Name")]
    [Tooltip("タイトル遷移ボタンでの遷移先シーン名")]
    [SerializeField] private string _nameToTitleScene;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _controller = GetComponent<SceneController_Level>();
        _confirmWindow = FindFirstObjectByType<UI_ConfirmWindow>(FindObjectsInactive.Include);

        if (_pauseWindow != null)
        {
            _pauseWindow?.SetActive(false);
        }

        // ----- Button -----
        if (_buttonResume != null)
        {
            _buttonResume.AddClickListener(OnClick_Resume);
        }
        if (_buttonRestart != null)
        {
            _buttonRestart.AddClickListener(OnClick_Restart);
        }
        if (_buttonToTitle != null)
        {
            _buttonToTitle.AddClickListener(OnClick_ToTitle);
        }
    }
    private void OnDestroy()
    {
        // ----- Button -----
        if (_buttonResume != null)
        {
            _buttonResume.RemoveClickListener(OnClick_Resume);
        }
        if (_buttonRestart != null)
        {
            _buttonRestart.RemoveClickListener(OnClick_Restart);
        }
        if (_buttonToTitle != null)
        {
            _buttonToTitle.RemoveClickListener(OnClick_ToTitle);
        }
    }

    // --------------------------------------------------
    // ----- Public Events -----
    // --------------------------------------------------
    public void Pause()
    {
        if (_isPaused)
        {
            return;
        }

        _isPaused = true;

        // プレイヤー操作停止
        _controller.Player.PausePlay();

        // 時間停止
        Time.timeScale = 0.0f;

        // ウィンドウ表示
        if (_pauseWindow != null)
        {
            _pauseWindow.SetActive(true);
            _buttonSelectionController?.SelectFirstButton();
        }
    }
    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;

        // 確認ウィンドウを閉じる
        if (_confirmWindow != null && _confirmWindow.gameObject.activeSelf)
        {
            _confirmWindow.Close(false);
        }

        // 時間再開
        Time.timeScale = 1.0f;

        // プレイヤー操作再開
        _controller.Player.StartPlay();

        // ウィンドウ非表示
        if (_pauseWindow != null)
        {
            _buttonSelectionController?.ClearSelection();
            _pauseWindow?.SetActive(false);
        }
    }
    public void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }


    // --------------------------------------------------
    // ----- OnClick Events -----
    // --------------------------------------------------
    public void OnClick_Resume()
    {
        // 再開
        Resume();
    }
    public void OnClick_Restart()
    {
        _confirmWindow.Open(_buttonRestart.ConfirmMessage, _buttonRestart, Confirm_Restart);
    }
    public void OnClick_ToTitle()
    {
        _confirmWindow.Open(_buttonToTitle.ConfirmMessage, _buttonToTitle, Confirm_ToTitle);
    }

    // --------------------------------------------------
    // ----- Confirm Events -----
    // --------------------------------------------------
    private void Confirm_Restart()
    {
        // 現在のシーンを再ロード
        // 実際はステートを初期化したい。間に合うかどうか。
        string currentSceneName = SceneManager.GetActiveScene().name;
        GameManager.Instance.ChangeScene(currentSceneName);


        // 時間再開
        Time.timeScale = 1.0f;
    }
    private void Confirm_ToTitle()
    {
        GameManager.Instance.ChangeScene(_nameToTitleScene);


        // 時間再開
        Time.timeScale = 1.0f;
    }

}
