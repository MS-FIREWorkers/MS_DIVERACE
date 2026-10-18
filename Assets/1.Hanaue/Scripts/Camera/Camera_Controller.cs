/*
    Camera_Controller
    20261017  hanaue sho
    カメラを制御するクラス
    外部からの窓口
 */
using UnityEngine;

public struct CameraTargetData
{
    public Vector3 Position;
    public Vector3 Velocity;

    public Vector3 Forward;
    public Vector3 Right;

    // 姿勢
    public float Posture;
}

public class Camera_Controller : MonoBehaviour
{
    [SerializeField] private Player_CharacterController _characterController;

    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    [SerializeField] private Camera _camera;
    [SerializeField] private Camera_Shake _shake;
    [SerializeField] private Camera_Movement _movement;
    public Camera Camera => _camera;
    public Camera_Shake Shake => _shake;
    public Camera_Movement Movement => _movement;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    void Start()
    {
        _movement = GetComponent<Camera_Movement>();
    }
    void LateUpdate()
    {
        float dt = Time.deltaTime;

        CameraTargetData targetData = _characterController.GetCameraTargetData();

        _movement.UpdateCamera(targetData, dt);

    }
}
