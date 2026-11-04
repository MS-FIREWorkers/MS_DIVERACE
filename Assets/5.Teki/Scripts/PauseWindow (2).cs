/*
    PauseWindow.cs
    20261029  （TEKI）
    レベルシーンのポーズウィンドウの表示・非表示、3つのボタン処理、選択中ボタンのフレーム強調表示を行う
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
    【使い方】
    Canvas_PauseWindow にアタッチする（CanvasGroup は自動で追加される）。
    Esc キーで表示・非表示を切り替える（Toggle）。
    外部（オプションボタンの処理側）から Show() / Hide() / Toggle() を呼ぶこともできる。

    選択中ボタンのフレームは、各ボタンの Image に Outline を Awake で自動追加して表示する。
    そのため、ボタン側での手動の設定やスクリプトのアタッチは不要。
    ボタンの Transition は None 推奨（Color Tint だと選択色がフレームに重なって見えるため）。

    【想定するオブジェクト構成】
    Canvas_PauseWindow
     - Image_Blur       （RawImage。Show 時に画面を撮影し、ぼかした画像を表示する）
     - Image_Window
     - Text_Pause
     - Button_Resume
     - Button_Retry
     - Button_ToTitle
 */
[RequireComponent(typeof(CanvasGroup))]
public class PauseWindow : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Property -----
    // ==================================================
    [Header("Scene")]
    [Tooltip("タイトルシーンの名前（Build Settings に登録されていること）")]
    [SerializeField] private string _titleSceneName = "TitleScene";

    [Header("Animation")]
    [Tooltip("表示・非表示にかかる時間（秒）")]
    [SerializeField] private float _animationDuration = 0.3f;

    [Tooltip("下から上へスライドする距離（ピクセル）")]
    [SerializeField] private float _slideDistance = 200f;

    [Header("Blur")]
    [Tooltip("背景をぼかす強さ。画面を 1/2 ずつ縮小する回数（大きいほど強くぼける）")]
    [Range(1, 6)]
    [SerializeField] private int _downsampleSteps = 4;

    [Header("Selection Frame")]
    [Tooltip("選択中のボタンのフレームの色（仕様書は橙色）")]
    [SerializeField] private Color _frameColor = new Color(1f, 0.7f, 0f, 1f);

    [Tooltip("フレームの太さ（ピクセル）。ボタン同士の間隔はこの 2 倍以上空けておくこと")]
    [SerializeField] private float _frameThickness = 6f;

    [Tooltip("フレームの明滅の深さ（0〜1）。0 で明滅せず常に点灯（参考画像と同じ）")]
    [Range(0f, 1f)]
    [SerializeField] private float _framePulseDepth = 0f;

    [Tooltip("フレームの明滅の速さ")]
    [SerializeField] private float _framePulseSpeed = 5f;

    // ==================================================
    // ----- Private Property -----
    // ==================================================
    private static readonly string[] SlideTargetNames =
    {
        "Image_Window", "Text_Pause", "Button_Resume", "Button_Retry", "Button_ToTitle"
    };

    private CanvasGroup _canvasGroup;
    private Button _resumeButton;
    private Button _retryButton;
    private Button _toTitleButton;

    private RawImage _blurImage;
    private RenderTexture _blurTexture;

    private RectTransform[] _slideTargets;
    private Vector2[] _basePositions;

    private Button[] _frameButtons;     // フレームを表示する対象のボタン
    private Outline[] _frameOutlines;   // _frameButtons と同じ並び順
    private int _selectedFrameIndex = -1;   // 現在フレームが出ているボタン（-1 = なし）

    private float _progress;            // 0 = 非表示, 1 = 表示
    private bool _isVisible;
    private Coroutine _animationCoroutine;

    public bool IsVisible => _isVisible;

    // ==================================================
    // ----- Events -----
    // ==================================================
    public event Action OnShown;
    public event Action OnHidden;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        _resumeButton = FindChildComponent<Button>("Button_Resume");
        _retryButton = FindChildComponent<Button>("Button_Retry");
        _toTitleButton = FindChildComponent<Button>("Button_ToTitle");

        // ぼかしは無くても動作するので、見つからない場合は Warning にする
        Transform blurTransform = transform.Find("Image_Blur");
        if (blurTransform != null) _blurImage = blurTransform.GetComponent<RawImage>();
        if (_blurImage == null)
        {
            Debug.LogWarning("[Warning] Image_Blur (RawImage) Not Found! 背景ぼかしは無効になります");
        }

        CacheSlideTargets();
        SetupSelectionFrames();

        if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
        if (_retryButton != null) _retryButton.onClick.AddListener(OnRetryClicked);
        if (_toTitleButton != null) _toTitleButton.onClick.AddListener(OnToTitleClicked);

        // 最初は非表示状態にしておく（SetActive は使わない）
        ApplyProgress(0f);
        SetInteractable(false);
    }

    void Update()
    {
        // 時間停止中（timeScale = 0）でも Update は動くので、同じキーで閉じられる
        if (WasToggleKeyPressed()) Toggle();

        UpdateSelectionFrame();
    }

    void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (_retryButton != null) _retryButton.onClick.RemoveListener(OnRetryClicked);
        if (_toTitleButton != null) _toTitleButton.onClick.RemoveListener(OnToTitleClicked);

        // 表示中に破棄されても時間停止が残らないようにする
        if (_isVisible) Time.timeScale = 1f;

        ReleaseBlurTexture();
    }

    // ==================================================
    // ----- Show / Hide -----
    // 外部から呼ぶ入口。Inspector の ContextMenu からもテスト可能
    // ==================================================
    public void Toggle()
    {
        if (_isVisible) Hide();
        else Show();
    }

    [ContextMenu("Show")]
    public void Show()
    {
        if (_isVisible) return;
        _isVisible = true;

        Time.timeScale = 0f;
        SetInteractable(true);
        SelectButton(_resumeButton);
        StartAnimation(1f);
    }

    [ContextMenu("Hide")]
    public void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;

        SetInteractable(false);
        StartAnimation(0f);
    }

    // ==================================================
    // ----- Animation -----
    // 時間停止中でも動くよう unscaledDeltaTime を使う
    // ==================================================
    private void StartAnimation(float target)
    {
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(AnimateTo(target));
    }

    private IEnumerator AnimateTo(float target)
    {
        // 表示開始時、ウィンドウがまだ見えていない状態（_progress が 0）のときだけ撮影する
        if (target > 0f && _progress <= 0.001f)
        {
            yield return CaptureBackground();
        }

        float start = _progress;
        // 途中で切り替わっても自然につながるよう、残り距離に応じて時間を決める
        float time = _animationDuration * Mathf.Abs(target - start);
        float elapsed = 0f;

        while (elapsed < time)
        {
            // 1フレームが極端に長くてもアニメーションが飛ばないよう上限を設ける
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _progress = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / time));
            ApplyProgress(_progress);
            yield return null;
        }

        _progress = target;
        ApplyProgress(target);
        _animationCoroutine = null;

        if (target > 0f)
        {
            OnShown?.Invoke();
        }
        else
        {
            Time.timeScale = 1f;
            OnHidden?.Invoke();
        }
    }

    private void ApplyProgress(float progress)
    {
        // ゆっくり止まる動きにする（Ease Out）
        float eased = 1f - (1f - progress) * (1f - progress);
        _canvasGroup.alpha = progress;

        for (int i = 0; i < _slideTargets.Length; i++)
        {
            if (_slideTargets[i] == null) continue;
            _slideTargets[i].anchoredPosition = _basePositions[i] + Vector2.down * _slideDistance * (1f - eased);
        }
    }

    // ==================================================
    // ----- Button Actions -----
    // ==================================================
    private void OnResumeClicked()
    {
        Hide();
    }

    private void OnRetryClicked()
    {
        // TODO: Canvas_Fade のフェード処理ができたらそちらを経由する
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnToTitleClicked()
    {
        // TODO: Canvas_Fade のフェード処理ができたらそちらを経由する
        Time.timeScale = 1f;
        SceneManager.LoadScene(_titleSceneName);
    }

    // ==================================================
    // ----- Selection Frame -----
    // 選択中のボタンの外側に橙色のフレーム（Outline）を表示する。
    // 選択状態は EventSystem から毎フレーム調べるので、ボタン側にスクリプトは不要
    // ==================================================
    private void SetupSelectionFrames()
    {
        _frameButtons = new[] { _resumeButton, _retryButton, _toTitleButton };
        _frameOutlines = new Outline[_frameButtons.Length];

        for (int i = 0; i < _frameButtons.Length; i++)
        {
            if (_frameButtons[i] == null) continue;

            // 枠線は Button の Image に付ける。Image が無ければ表示できないので Warning にする
            if (_frameButtons[i].GetComponent<Graphic>() == null)
            {
                Debug.LogWarning($"[Warning] {_frameButtons[i].name} has no Graphic! 選択フレームは表示されません");
                continue;
            }

            Outline outline = _frameButtons[i].GetComponent<Outline>();
            if (outline == null) outline = _frameButtons[i].gameObject.AddComponent<Outline>();

            outline.effectDistance = new Vector2(_frameThickness, _frameThickness);
            outline.useGraphicAlpha = false;
            outline.effectColor = _frameColor;
            outline.enabled = false;
            _frameOutlines[i] = outline;
        }
    }

    private void UpdateSelectionFrame()
    {
        // 選択中のボタンが変わった時だけ、フレームの表示を切り替える
        int index = GetSelectedButtonIndex();
        if (index != _selectedFrameIndex)
        {
            _selectedFrameIndex = index;
            for (int i = 0; i < _frameOutlines.Length; i++)
            {
                if (_frameOutlines[i] == null) continue;

                _frameOutlines[i].enabled = (i == index);
                if (i == index) _frameOutlines[i].effectColor = _frameColor;
            }
        }

        // 明滅させる場合のみ、毎フレーム色を更新する
        if (_selectedFrameIndex < 0 || _framePulseDepth <= 0f) return;

        Outline selectedOutline = _frameOutlines[_selectedFrameIndex];
        if (selectedOutline == null) return;

        // 時間停止中（timeScale = 0）でも動くよう unscaledTime を使う
        float wave = (Mathf.Sin(Time.unscaledTime * _framePulseSpeed) + 1f) * 0.5f;   // 0〜1
        Color color = _frameColor;
        color.a = _frameColor.a * (1f - _framePulseDepth * wave);
        selectedOutline.effectColor = color;
    }

    private int GetSelectedButtonIndex()
    {
        if (EventSystem.current == null) return -1;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return -1;

        for (int i = 0; i < _frameButtons.Length; i++)
        {
            if (_frameButtons[i] != null && _frameButtons[i].gameObject == selected) return i;
        }
        return -1;
    }

    // ==================================================
    // ----- Blur -----
    // 画面を撮影し、1/2 ずつ縮小を繰り返してから拡大表示することでぼかす
    // （URP では GrabPass が使えないため、Shader を使わない方法にしている）
    // ==================================================
    private IEnumerator CaptureBackground()
    {
        if (_blurImage == null) yield break;

        // 描画が終わってから撮影する
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot == null)
        {
            Debug.LogWarning("[Warning] Screen Capture Failed! 背景ぼかしをスキップします");
            yield break;
        }

        ReleaseBlurTexture();

        Texture current = screenshot;
        RenderTexture previousTemp = null;

        for (int i = 1; i <= _downsampleSteps; i++)
        {
            int width = Mathf.Max(1, screenshot.width >> i);
            int height = Mathf.Max(1, screenshot.height >> i);
            bool isLast = (i == _downsampleSteps);

            // 最後の1枚だけ残し、途中の画像は一時的なものとして使い捨てる
            RenderTexture next = isLast
                ? new RenderTexture(width, height, 0)
                : RenderTexture.GetTemporary(width, height, 0);
            next.filterMode = FilterMode.Bilinear;

            Graphics.Blit(current, next);

            if (previousTemp != null) RenderTexture.ReleaseTemporary(previousTemp);
            if (isLast) _blurTexture = next;
            else previousTemp = next;
            current = next;
        }

        Destroy(screenshot);
        _blurImage.texture = _blurTexture;
    }

    private void ReleaseBlurTexture()
    {
        if (_blurTexture == null) return;

        if (_blurImage != null) _blurImage.texture = null;
        _blurTexture.Release();
        Destroy(_blurTexture);
        _blurTexture = null;
    }

    // ==================================================
    // ----- Input -----
    // 新 Input System のみ有効な場合と、旧 Input Manager の場合の両方に対応
    // ==================================================
    private bool WasToggleKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    // ==================================================
    // ----- Helper -----
    // ==================================================
    private void SetInteractable(bool value)
    {
        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value;
    }

    private void SelectButton(Button button)
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("[Error] EventSystem Not Found!");
            return;
        }
        if (button == null) return;

        // 一度クリアしてから選択し直すことで、毎回確実に選択状態にする
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void CacheSlideTargets()
    {
        _slideTargets = new RectTransform[SlideTargetNames.Length];
        _basePositions = new Vector2[SlideTargetNames.Length];

        for (int i = 0; i < SlideTargetNames.Length; i++)
        {
            _slideTargets[i] = FindChildComponent<RectTransform>(SlideTargetNames[i]);
            if (_slideTargets[i] != null) _basePositions[i] = _slideTargets[i].anchoredPosition;
        }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            Debug.LogError($"[Error] {childName} Not Found!");
            return null;
        }

        T component = child.GetComponent<T>();
        if (component == null)
        {
            Debug.LogError($"[Error] {childName} has no {typeof(T).Name}!");
        }
        return component;
    }
}
