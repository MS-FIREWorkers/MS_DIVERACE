
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(MeshRenderer))]
public class SkyDomeFollow : MonoBehaviour
{
    [Header("カメラ参照")]
    [InspectorName("メインカメラ")]
    [SerializeField] private Transform targetCamera;

    [Header("階層別マテリアル")]
    [Tooltip("Level05からLevel01の順番")]
    [SerializeField]
    private Material[] levelMaterials =
        new Material[5];

    [Header("位置設定")]

    [InspectorName("カメラのX/Z座標に追従")]
    [SerializeField] private bool followCameraXZ = true;

    [InspectorName("カメラのY座標に追従")]
    [Tooltip("OFFなら階層のTop/Bottomの中心に固定")]
    [SerializeField] private bool followCameraY = false;

    [InspectorName("ワールド回転を固定")]
    [SerializeField] private bool lockWorldRotation = true;

    [Header("サイズ設定")]

    [InspectorName("階層の高さからサイズを計算")]
    [SerializeField] private bool autoSize = true;

    [InspectorName("階層高さの倍率")]
    [SerializeField, Min(1.1f)]
    private float diameterMultiplier = 1.6f;

    [InspectorName("手動サイズ")]
    [SerializeField, Min(1f)]
    private float manualDiameter = 600f;

    [InspectorName("最小サイズ")]
    [SerializeField, Min(1f)]
    private float minimumDiameter = 300f;

    [InspectorName("下降進行率でサイズを変更")]
    [SerializeField] private bool scaleWithProgress = false;

    [InspectorName("上端・下端のサイズ倍率")]
    [SerializeField]
    private Vector2 progressScale = new Vector2(1f, 1.2f);

    [Header("Debug")]
    [SerializeField] private bool debugDome = false;

    private float nextDebugTime;

    private MeshRenderer domeRenderer;
    private LevelSceneInfo currentLevelInfo;

    private float currentDiameter;
    private int currentFloor = -1;

    private void Awake()
    {
        domeRenderer = GetComponent<MeshRenderer>();
        domeRenderer.enabled = false;

        FindCamera();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        ApplyLevelScene(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            FindCamera();

        if (targetCamera == null || currentLevelInfo == null)
            return;

        if (!currentLevelInfo.IsValid())
            return;

        // ----- Level Height -----

        Vector3 top =
            currentLevelInfo.TopPoint.position;

        Vector3 bottom =
            currentLevelInfo.BottomPoint.position;

        Vector3 center = (top + bottom) * 0.5f;

        // ----- Position -----

        Vector3 position = center;

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

        // ----- Size -----

        float height = Mathf.Abs(top.y - bottom.y);

        float diameter = autoSize
            ? Mathf.Max(
                minimumDiameter,
                height * diameterMultiplier
            )
            : manualDiameter;

        if (scaleWithProgress)
        {
            float progress = Mathf.InverseLerp(
                top.y,
                bottom.y,
                targetCamera.position.y
            );

            float multiplier = Mathf.Lerp(
                progressScale.x,
                progressScale.y,
                progress
            );

            diameter *= Mathf.Max(0.1f, multiplier);
        }

        currentDiameter = diameter;

        // Unity標準Sphereの直径は1
        transform.localScale =
            Vector3.one * currentDiameter;


        // ----- Skydome Debug -----

        if (debugDome && Time.unscaledTime >= nextDebugTime)
        {
            nextDebugTime = Time.unscaledTime + 0.5f;

            MeshFilter filter = GetComponent<MeshFilter>();
            Camera cam = targetCamera.GetComponent<Camera>();

            if (filter != null &&
                filter.sharedMesh != null &&
                cam != null)
            {
                // 標準Sphere與等比例縮放を想定
                float radius =
                    filter.sharedMesh.bounds.extents.x *
                    Mathf.Abs(transform.lossyScale.x);

                Vector3 domeCenter = transform.TransformPoint(
                    filter.sharedMesh.bounds.center
                );

                float distance = Vector3.Distance(
                    cam.transform.position,
                    domeCenter
                );

                bool inside = distance < radius;

                float nearestSurfaceDistance =
                    Mathf.Max(0f, radius - distance);

                Debug.Log(
                    $"[SkyDome Debug] " +
                    $"CameraY={cam.transform.position.y:F1} | " +
                    $"DomeY={domeCenter.y:F1} | " +
                    $"Radius={radius:F1} | " +
                    $"Distance={distance:F1} | " +
                    $"Inside={inside} | " +
                    $"NearestSurface={nearestSurfaceDistance:F1} | " +
                    $"FarClip={cam.farClipPlane:F1}"
                );
            }
        }

    }

    private void OnSceneChanged(
        Scene previousScene,
        Scene newScene)
    {
        ApplyLevelScene(newScene);
    }

    private void ApplyLevelScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        string sceneName = scene.name;
        int lastUnderscore = sceneName.LastIndexOf('_');

        if (lastUnderscore < 0)
            return;

        string floorText =
            sceneName.Substring(lastUnderscore + 1);

        if (!int.TryParse(floorText, out int floor))
            return;

        if (floor < 1 || floor > 5)
            return;

        int index = 5 - floor;

        if (levelMaterials == null ||
            index >= levelMaterials.Length ||
            levelMaterials[index] == null)
        {
            domeRenderer.enabled = false;
            Debug.LogWarning(
                $"[Skydome] Material missing: {floor}F"
            );
            return;
        }

        // ----- Search LevelSceneInfo -----

        LevelSceneInfo info = null;

        foreach (GameObject root
                 in scene.GetRootGameObjects())
        {
            info = root.GetComponentInChildren<LevelSceneInfo>(
                true
            );

            if (info != null && info.IsValid())
                break;
        }

        if (info == null || !info.IsValid())
        {
            domeRenderer.enabled = false;
            Debug.LogWarning(
                $"[Skydome] LevelSceneInfo missing: {floor}F"
            );
            return;
        }

        currentLevelInfo = info;

        // ----- Change Material -----

        domeRenderer.sharedMaterial =
            levelMaterials[index];

        domeRenderer.enabled = true;

        currentFloor = floor;

        Debug.Log(
            $"[Skydome] Level {currentFloor:00} activated"
        );
    }

    private void FindCamera()
    {
        Camera camera = Camera.main;

        if (camera != null)
            targetCamera = camera.transform;
    }
}
