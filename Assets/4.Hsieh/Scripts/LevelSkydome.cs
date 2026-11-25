
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(MeshRenderer))]
public class LevelSkydome : MonoBehaviour
{
    [Header("カメラ設定")]
    [InspectorName("メインカメラ")]
    [SerializeField]
    private Transform targetCamera;

    [Header("追従設定")]
    [InspectorName("カメラのX/Z座標に追従")]
    [SerializeField]
    private bool followCameraXZ = true;

    [InspectorName("カメラのY座標に追従")]
    [SerializeField]
    private bool followCameraY = true;

    [InspectorName("ワールド回転を固定")]
    [SerializeField]
    private bool lockWorldRotation = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugMode = false;

    private MeshRenderer domeRenderer;

    // ==========================================
    // ----- Unity Events -----
    // ==========================================

    private void Awake()
    {
        domeRenderer = GetComponent<MeshRenderer>();

        // ゲーム開始時は非表示
        if (Application.isPlaying)
        {
            domeRenderer.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        SceneManager.activeSceneChanged +=
            OnActiveSceneChanged;

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            SceneManager.activeSceneChanged -=
                OnActiveSceneChanged;
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying ||
            domeRenderer == null ||
            !domeRenderer.enabled)
        {
            return;
        }

        if (targetCamera == null)
        {
            Camera cam = Camera.main;

            if (cam != null)
                targetCamera = cam.transform;
        }

        if (targetCamera == null)
            return;

        // ----- Position -----

        Vector3 position = transform.position;

        if (followCameraXZ)
        {
            position.x = targetCamera.position.x;
            position.z = targetCamera.position.z;
        }

        if (followCameraY)
        {
            position.y = targetCamera.position.y;
        }

        transform.position = position;

        // ----- Rotation -----

        if (lockWorldRotation)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    // ==========================================
    // ----- Scene Change -----
    // ==========================================

    private void OnActiveSceneChanged(
        Scene oldScene,
        Scene newScene)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (domeRenderer == null)
            return;

        // このSkydomeが所属するSceneを取得
        Scene ownScene = gameObject.scene;

        // 現在のActive Sceneを取得
        Scene activeScene = SceneManager.GetActiveScene();

        // 自分のSceneがActiveの場合のみ表示
        bool isCurrentLevel =
            ownScene == activeScene;

        domeRenderer.enabled = isCurrentLevel;

        if (debugMode)
        {
            Debug.Log(
                $"[LevelSkydome] " +
                $"{ownScene.name} : " +
                $"{(isCurrentLevel ? "ON" : "OFF")}"
            );
        }
    }
}
