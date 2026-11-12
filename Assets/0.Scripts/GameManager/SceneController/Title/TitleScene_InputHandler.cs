/*
    TitleScene_InputHandler
    20261104  hanaue sho
    タイトルシーンでの入力を受けとるスクリプト
    スタートボタン押下など
 */
using UnityEngine;
using UnityEngine.InputSystem;

public class TitleScene_InputHandler : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Result _controller;

    // --------------------------------------------------
    // ----- Input Values -----
    // --------------------------------------------------
    private bool _cancelPressed;
    private bool _submitPressed;
    public Vector2 Navigate { get; private set; }

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _controller = GetComponent<SceneController_Result>();

    }

    // --------------------------------------------------
    // ----- Input Events -----
    // --------------------------------------------------
    public void OnSubmitButton(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _submitPressed = true;
        }
    }
    public void OnCancelButton(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _cancelPressed = true;
        }
    }
    public void OnNavigate(InputAction.CallbackContext context)
    {
        Navigate = context.ReadValue<Vector2>();
    }

    // --------------------------------------------------
    // ----- InputGetter Events -----
    // --------------------------------------------------
    public bool ConsumeSubmitPressed()
    {
        if (!_submitPressed)
        {
            return false;
        }

        _submitPressed = false;
        return true;
    }
    public bool ConsumeCancelPressed()
    {
        if (!_cancelPressed)
        {
            return false;
        }

        _cancelPressed = false;
        return true;
    }
}
