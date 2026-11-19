/*
    LevelScene_PauseController
    20261104  hanaue sho
    レベルシーンでポーズウィンドウに指示を出すスクリプト
    ポーズボタンを押した際にこのスクリプトを経由してポーズウィンドウを有効化、無効化を行う
 */
using UnityEngine;

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
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _controller = GetComponent<SceneController_Level>();


        if (_pauseWindow != null)
        {
            _pauseWindow?.SetActive(false);
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
        }
    }
    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;

        // 時間再開
        Time.timeScale = 1.0f;

        // プレイヤー操作再開
        _controller.Player.StartPlay();

        // ウィンドウ非表示
        if (_pauseWindow != null)
        {
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
}
