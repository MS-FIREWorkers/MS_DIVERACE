/*
    LevelScene_InputHandler
    20261104  hanaue sho
    レベルシーンでの入力を受けとるスクリプト
    ポーズボタン押下など
 */
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelScene_InputHandler : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Level _controller;

    // --------------------------------------------------
    // ----- InputAction -----
    // --------------------------------------------------
    [Header("InputAction")]
    [SerializeField] private InputActionReference _pauseAction;

    // --------------------------------------------------
    // ----- InputValues Property -----
    // --------------------------------------------------
    public bool PauseInput { get; private set; }

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void OnEnable()
    {
        if (_pauseAction == null) return;

        _pauseAction.action.performed += OnPause;
        _pauseAction.action.Enable();
    }
    private void OnDisable()
    {
        if (_pauseAction == null) return;

        _pauseAction.action.performed -= OnPause;
        _pauseAction.action.Disable();
    }
    private void Awake()
    {
        _controller = GetComponent<SceneController_Level>();

        if (_pauseAction == null)
        {
            Debug.LogWarning("[Warning] PauseAction Not Atatched!");
        }
    }

    // --------------------------------------------------
    // ----- Input Events -----
    // --------------------------------------------------
    private void OnPause(InputAction.CallbackContext context)
    {
        PauseInput = true;
    }

    // --------------------------------------------------
    // ----- Public Methods -----
    // --------------------------------------------------
    public void ConsumePauseInput()
    {
        PauseInput = false;
    }
}
