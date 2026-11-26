
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


    // 正式ゲームの階層管理
    private LevelSceneStreamer levelStreamer;
    private float nextStreamerSearchTime;

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
            domeRenderer == null)
        {
            return;
        }

        // 毎フレーム現在の階層を確認
        RefreshVisibility();

        if (!domeRenderer.enabled)
            return;
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

        // 正式ゲームのLevelSceneStreamerを検索
        if (levelStreamer == null &&
            Time.unscaledTime >= nextStreamerSearchTime)
        {
            nextStreamerSearchTime =
                Time.unscaledTime + 0.5f;

            levelStreamer =
                FindFirstObjectByType<LevelSceneStreamer>();
        }

        Scene ownScene = gameObject.scene;

        bool isCurrentLevel = false;

        // 正式ゲームの場合
        if (levelStreamer != null &&
            levelStreamer.isActiveAndEnabled)
        {
            isCurrentLevel =
                ownScene.name ==
                levelStreamer.CurrentLevelSceneName;
        }
        else
        {
            // テストシーンの場合は従来方式
            isCurrentLevel =
                ownScene == SceneManager.GetActiveScene();
        }

        // 表示状態が変わらなければ何もしない
        if (domeRenderer.enabled == isCurrentLevel)
            return;

        domeRenderer.enabled = isCurrentLevel;

        if (debugMode)
        {
            Debug.Log(
                $"[LevelSkydome] {ownScene.name} : " +
                (isCurrentLevel ? "ON" : "OFF")
            );
        }
    }

}
