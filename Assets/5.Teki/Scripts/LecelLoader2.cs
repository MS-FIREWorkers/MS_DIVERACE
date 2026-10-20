using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 文件名和类名必须一致：LecelLoader2.cs / LecelLoader2。
[DisallowMultipleComponent]
public class LecelLoader2 : MonoBehaviour
{
    [Header("转场模式：旧遮罩选 UI，新像素化选 Fullscreen")]
    [SerializeField] private bool useFullscreen = false;
    [Tooltip("拖入当前摄像机使用的专用 Transition_Renderer")]
    [SerializeField] private UniversalRendererData fullscreenRenderer;
    [Tooltip("拖入该 Renderer 的 Full Screen Pass 使用的材质")]
    [SerializeField] private Material fullscreenMaterial;

    [Header("转场 Image（全屏模式下仅用于自动关闭旧 Image）")]
    [SerializeField] private Image transitionImage;

    [Header("Shader 浮点参数")]
    [SerializeField] private string radiusProperty = "_Float";
    [Tooltip("正常画面的值：像素化通常为 1；圆形遮罩按原设置")]
    [SerializeField] private float openValue = 2f;
    [Tooltip("离场效果最强时的值：像素化建议 0.02，不能为 0；圆形遮罩按原设置")]
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
    private FullScreenPassRendererFeature fullscreenFeature;
    private int radiusId;
    private bool isTransitioning;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(radiusProperty))
        {
            Debug.LogError("LecelLoader2：请填写 Shader 参数的 Reference。", this);
            enabled = false;
            return;
        }

        radiusId = Shader.PropertyToID(radiusProperty);
        if (useFullscreen)
        {
            // 全屏 Shader 由 Renderer Feature 绘制，不能再由 Image 绘制。
            if (transitionImage != null)
                transitionImage.enabled = false;

            if (fullscreenRenderer == null || fullscreenMaterial == null)
            {
                Debug.LogError("LecelLoader2：全屏模式需要绑定 Fullscreen Renderer 和 Fullscreen Material。", this);
                enabled = false;
                return;
            }

            foreach (var feature in fullscreenRenderer.rendererFeatures)
            {
                if (feature is FullScreenPassRendererFeature pass &&
                    pass.passMaterial == fullscreenMaterial)
                {
                    if (fullscreenFeature != null)
                    {
                        Debug.LogError("LecelLoader2：多个 Full Screen Pass 使用了此材质，请只保留一个。", this);
                        enabled = false;
                        return;
                    }
                    fullscreenFeature = pass;
                }
            }

            if (fullscreenFeature == null || !fullscreenFeature.isActive)
            {
                Debug.LogError("LecelLoader2：找不到使用此材质且已开启的 Full Screen Pass，请核对 Pass Material。", this);
                enabled = false;
                return;
            }

            originalMaterial = fullscreenFeature.passMaterial;
        }
        else
        {
            if (transitionImage == null)
            {
                Debug.LogError("LecelLoader2：UI 模式需要绑定 Transition Image。", this);
                enabled = false;
                return;
            }
            originalMaterial = transitionImage.material;
        }

        if (originalMaterial == null || !originalMaterial.HasProperty(radiusId))
        {
            Debug.LogError($"LecelLoader2：材质缺少参数 {radiusProperty}，请核对 Shader Graph 的 Reference。", this);
            enabled = false;
            return;
        }

        // 动画控制材质副本，同时将副本交给实际绘制它的组件。
        transitionMaterial = new Material(originalMaterial);
        if (useFullscreen)
            fullscreenFeature.passMaterial = transitionMaterial;
        else
        {
            transitionImage.material = transitionMaterial;
            transitionImage.raycastTarget = false;
        }

        // B 在首次绘制前先设为全黑；A 可以直接显示场景。
        isTransitioning = playEnterOnStart;
        transitionMaterial.SetFloat(radiusId, playEnterOnStart ? closedValue : openValue);
        if (!useFullscreen && transitionImage != null)
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
        if (!useFullscreen && transitionImage != null)
            transitionImage.enabled = true;
        yield return AnimateRadius(openValue, closedValue);

        // 离场效果保持一帧，然后加载 B。像素化本身不会变为全黑。
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

        if (useFullscreen)
        {
            // 仅撤销自己绑定的副本，避免覆盖其他控制器的新绑定。
            if (fullscreenFeature != null && fullscreenFeature.passMaterial == transitionMaterial)
                fullscreenFeature.passMaterial = originalMaterial;
        }
        else if (transitionImage != null && transitionImage.material == transitionMaterial)
            transitionImage.material = originalMaterial;

        Destroy(transitionMaterial);
    }
}
