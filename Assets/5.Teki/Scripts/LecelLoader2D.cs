using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 文件名和类名必须一致：LecelLoader2.cs / LecelLoader2。
[DisallowMultipleComponent]
public class LecelLoader2D : MonoBehaviour
{
    [Header("转场 Image（绑定当前场景里的 Image）")]
    [SerializeField] private Image transitionImage;

    [Header("Shader 浮点参数")]
    [SerializeField] private string radiusProperty = "_Float";
    [Tooltip("透明圆扩大到能看见整个画面时的值")]
    [SerializeField] private float openValue = 2f;
    [Tooltip("整个画面完全变黑时的值")]
    [SerializeField] private float closedValue = 0f;

    [Header("播放设置")]
    [Tooltip("A 可以关闭；B 开启，加载后自动从黑屏展开")]
    [SerializeField] private bool playEnterOnStart = false;
    [Tooltip("A 开启；B 如果只需入场动画，就关闭")]
    [SerializeField] private bool clickToLoadNextScene = true;
    [Min(0.01f)]
    [SerializeField] private float transitionDuration = 1f;

    [Header("离场后要加载的场景")]
    [Tooltip("填写 Build Profiles 的 Scene List 中的场景名称，例如 B")]
    [SerializeField] private string nextSceneName = "B";

    private Material originalMaterial;
    private Material transitionMaterial;
    private int radiusId;
    private bool isTransitioning;

    private void Awake()
    {
        if (transitionImage == null || string.IsNullOrWhiteSpace(radiusProperty))
        {
            Debug.LogError("LecelLoader2：请绑定 Transition Image 并填写 Shader 参数名。", this);
            enabled = false;
            return;
        }

        originalMaterial = transitionImage.material;
        radiusId = Shader.PropertyToID(radiusProperty);
        if (originalMaterial == null || !originalMaterial.HasProperty(radiusId))
        {
            Debug.LogError($"LecelLoader2：Image 材质缺少参数 {radiusProperty}，请核对 Shader Graph 的 Reference。", this);
            enabled = false;
            return;
        }

        // 每个场景独立使用材质副本，避免修改共用的材质资源。
        transitionMaterial = new Material(originalMaterial);
        transitionImage.material = transitionMaterial;
        transitionImage.raycastTarget = false;

        // B 在首次绘制前先设为全黑；A 可以直接显示场景。
        isTransitioning = playEnterOnStart;
        transitionMaterial.SetFloat(radiusId, playEnterOnStart ? closedValue : openValue);
        transitionImage.enabled = true;
    }

    private IEnumerator Start()
    {
        if (transitionMaterial == null || !playEnterOnStart)
            yield break;

        // 新场景自动执行 Start，不需要鼠标触发。
        yield return null;
        yield return AnimateRadius(closedValue, openValue);
        
        isTransitioning = false;
    }

    private void Update()
    {
        if (isTransitioning || !clickToLoadNextScene)
            return;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            LoadNextScene();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
            LoadNextScene();
#endif
    }

    // 也可以把这个方法绑定到 UI Button 的 OnClick。
    public void LoadNextScene()
    {
        if (!isActiveAndEnabled || isTransitioning || transitionMaterial == null)
            return;

        // 先检查目标，避免场景名称写错后画面一直停在黑屏。
        if (string.IsNullOrWhiteSpace(nextSceneName) ||
            !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"LecelLoader2：无法加载场景 '{nextSceneName}'，请检查名称并把场景加入启用的 Scene List。", this);
            return;
        }

        isTransitioning = true;
        StartCoroutine(ExitAndLoad());
    }

    private IEnumerator ExitAndLoad()
    {
        transitionMaterial.SetFloat(radiusId, openValue);
        transitionImage.enabled = true;
        yield return AnimateRadius(openValue, closedValue);

        // 全黑状态保持一帧，然后加载 B。
        yield return null;
        yield return SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
        // A 的对象随场景卸载销毁，B 用自己的脚本自动播放入场。
    }

    private IEnumerator AnimateRadius(float from, float to)
    {
        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;
        transitionMaterial.SetFloat(radiusId, from);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transitionMaterial.SetFloat(radiusId, Mathf.Lerp(from, to, t));
            yield return null;
        }

        transitionMaterial.SetFloat(radiusId, to);
    }

    private void OnDestroy()
    {
        if (transitionMaterial == null)
            return;

        if (transitionImage != null)
            transitionImage.material = originalMaterial;

        Destroy(transitionMaterial);
    }
}
