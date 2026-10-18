/*
    Camera_Controller
    20261017  hanaue sho
    カメラを制御するクラス
    外部からの窓口
 */
using UnityEngine;
using UnityEngine.InputSystem;

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
    // --------------------------------------------------
    // ----- Player_CharacterController -----
    // --------------------------------------------------
    [Header("PlayerCharacter")]
    [SerializeField] private Player_CharacterController _characterController;

    // --------------------------------------------------
    // ----- Camera -----
    // --------------------------------------------------
    [Header("Camera")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _shakeRoot;
    public Camera Camera => _camera;

    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    [Header("Components")]
    [SerializeField] private Camera_Shake _shake;
    [SerializeField] private Camera_Movement _movement;
    public Camera_Shake Shake => _shake;
    public Camera_Movement Movement => _movement;

    // --------------------------------------------------
    // ----- Debug -----
    // --------------------------------------------------
    [Header("Debug")]
    [SerializeField] private CameraShake_Data _testShakeData;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    void Start()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }
        _movement = GetComponent<Camera_Movement>();
        _shake = GetComponent<Camera_Shake>();

        if (_characterController == null)
        {
            Debug.LogError("[Error] PlayerCharacter Not Found !!");
        }
        if (_shakeRoot == null)
        {
            Debug.LogError("[Error] ShakeRoot Not Found !!");
        }
    }
    void LateUpdate()
    {
        float dt = Time.deltaTime;

        // ----- Playuer Data -----
        CameraTargetData targetData = _characterController.GetCameraTargetData();

        // ----- Movement -----
        _movement.UpdateCamera(targetData, dt);

        // ----- Shake -----
        _shake.UpdateShake(dt);

        // ----- Apply -----
        ApplyCameraShake(_shake.CurrentOffset);
        ApplyCameraFov(_movement.CurrentBaseFov, _shake.CurrentOffset.Fov);
    }
    private void Update()
    {
        // Tキーでシェイクを再生（Debug）
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            PlayShake(
                _testShakeData,
                CameraShakePlayMode.Replace
            );
        }
    }

    // --------------------------------------------------
    // ----- ApplyCameraShake -----
    // --------------------------------------------------
    private void ApplyCameraShake(CameraShakeOffset offset)
    {
        // ローカル位置
        _shakeRoot.localPosition = offset.Position;

        // ローカル回転（Euler）
        _shakeRoot.localRotation = Quaternion.Euler(offset.Rotation);
    }

    // --------------------------------------------------
    // ----- ApplyCameraFov -----
    // --------------------------------------------------
    private void ApplyCameraFov(float baseFov, float shakeFov)
    {
        float finalFov = baseFov + shakeFov;
        _camera.fieldOfView = Mathf.Clamp(finalFov, 1.0f, 179.0f);
    }

    // --------------------------------------------------
    // ----- PlayShake -----
    // --------------------------------------------------
    public void PlayShake(CameraShake_Data shakeData, CameraShakePlayMode mode = CameraShakePlayMode.Replace)
    {
        if (_shake == null)
        {
            return;
        }

        _shake.PlayShake(shakeData, mode);
    }
}
