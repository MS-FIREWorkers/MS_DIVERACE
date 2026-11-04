/*
    Player_InputHandler
    20261004  hanaue sho
 */
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_InputHandler : MonoBehaviour
{
    // --------------------------------------------------
    // ----- CharacterController -----
    // --------------------------------------------------
    private Player_CharacterController _characterController;

    // --------------------------------------------------
    // ----- InputValues Property -----
    // --------------------------------------------------
    public Vector2 MoveInput {  get; private set; }
    public float PoseInput { get; private set; }
    public float RotateInput { get; private set; }

    [SerializeField] private float _debugPoseInput;

    // --------------------------------------------------
    // ----- PoseInput Property -----
    // --------------------------------------------------
    // Gamepad
    private float _leftTriggerInput;
    private float _rightTriggerInput;
    // Keyboard
    private float _keyboardPoseInput;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _characterController = GetComponent<Player_CharacterController>();
    }

    // --------------------------------------------------
    // ----- Input Events -----
    // --------------------------------------------------
    // 移動入力
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }
    // 姿勢変更（左トリガー）
    public void OnLeftPose(InputAction.CallbackContext context)
    {
        _leftTriggerInput = context.ReadValue<float>();
        UpdatePoseInput();
    }
    // 姿勢変更（右トリガー）
    public void OnRightPose(InputAction.CallbackContext context)
    {
        _rightTriggerInput = context.ReadValue<float>();
        UpdatePoseInput();
    }
    // 姿勢変更（キーボード）
    public void OnKeyboardPose(InputAction.CallbackContext context)
    {
        _keyboardPoseInput = context.ReadValue<float>();
        UpdatePoseInput();
    }
    public void OnPoseRotate(InputAction.CallbackContext context)
    {
        RotateInput = context.ReadValue<float>();
    }

    // --------------------------------------------------
    // ----- Internal -----
    // --------------------------------------------------
    private void UpdatePoseInput()
    {
        // L2 と R2 の両方を押した分だけ有効 
        float gamepadInput = Mathf.Min(_leftTriggerInput, _rightTriggerInput);

        // Gamepad と Keyboard の大きい方を採用する
        PoseInput = Mathf.Max(gamepadInput, _keyboardPoseInput);

        _debugPoseInput = PoseInput;
    }
}
