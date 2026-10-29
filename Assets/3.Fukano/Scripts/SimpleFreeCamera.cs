
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFreeCamera : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float fastSpeed = 15.0f;

    [Header("視点設定")]
    [SerializeField] private float mouseSensitivity = 0.15f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;

        yaw = angles.y;
        pitch = angles.x;

        if (pitch > 180f)
            pitch -= 360f;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        MoveCamera();
        RotateCamera();
    }

    // カメラ移動
    private void MoveCamera()
    {
        Keyboard kb = Keyboard.current;

        Vector3 move = Vector3.zero;

        if (kb.wKey.isPressed) move += Vector3.forward;
        if (kb.sKey.isPressed) move += Vector3.back;
        if (kb.aKey.isPressed) move += Vector3.left;
        if (kb.dKey.isPressed) move += Vector3.right;

        if (kb.qKey.isPressed) move += Vector3.up;
        if (kb.eKey.isPressed) move += Vector3.down;

        float speed = kb.leftShiftKey.isPressed
            ? fastSpeed
            : moveSpeed;

        transform.Translate(
            move.normalized * speed * Time.deltaTime,
            Space.Self
        );
    }

    // カメラ回転
    private void RotateCamera()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.rightButton.isPressed)
            return;

        Vector2 delta = mouse.delta.ReadValue();

        yaw += delta.x * mouseSensitivity;
        pitch -= delta.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(
            pitch,
            yaw,
            0f
        );
    }
}
