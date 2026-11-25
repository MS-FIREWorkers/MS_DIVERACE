/*
    ResultScene_PresentationController
    20261104  hanaue sho
    リザルトシーンでの演出を指示するスクリプト
    スコアの表示や演出の再生の指示など
 */
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResultScene_PresentationController : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Result _controller;

    // --------------------------------------------------
    // ----- Count Up Settings -----
    // --------------------------------------------------
    [Header("Count Up")]
    [Tooltip("クリアタイムのカウントアップにかける時間（秒）")]
    [SerializeField] private float _countUpDuration = 2.0f;
    [Tooltip("カウントアップの進み方（横：経過 0 から 1 / 縦：表示値 0 から 1）")]
    [SerializeField] private AnimationCurve _countUpCurve = new AnimationCurve(
        new Keyframe(0.0f, 0.0f, 2.0f, 2.0f),
        new Keyframe(1.0f, 1.0f, 0.0f, 0.0f));

    // --------------------------------------------------
    // ----- Pop Scale Settings -----
    // --------------------------------------------------
    [Header("Pop Scale")]
    [Tooltip("ポップアップ演出にかける時間（秒）。評価と確認ウィンドウで共通")]
    [SerializeField] private float _popDuration = 0.4f;
    [Tooltip("ポップアップのスケール変化（横：経過 0 から 1 / 縦：スケール）")]
    [SerializeField] private AnimationCurve _popCurve = new AnimationCurve(
        new Keyframe(0.0f, 0.0f),
        new Keyframe(0.6f, 1.2f),
        new Keyframe(1.0f, 1.0f));

    // --------------------------------------------------
    // ----- Thanks Settings -----
    // --------------------------------------------------
    private enum SlideDirection
    {
        None,       // スライドせずにフェードインだけ
        FromRight,
        FromLeft,
        FromTop,
        FromBottom,
    }
    [Header("Thanks")]
    [Tooltip("お礼の言葉がどの方向から入ってくるか（背景のレイアウトに合わせて変更する）")]
    [SerializeField] private SlideDirection _thanksSlideDirection = SlideDirection.FromRight;
    [Tooltip("お礼の言葉が入ってくる演出にかける時間（秒）")]
    [SerializeField] private float _thanksDuration = 0.6f;
    [Tooltip("お礼の言葉が入ってくる距離（ピクセル）。画面の外から来るように大きめにする")]
    [SerializeField] private float _thanksFlyDistance = 1400.0f;
    [Tooltip("表示後にふわふわ上下する幅（ピクセル）。0 で動かない")]
    [SerializeField] private float _thanksIdleAmplitude = 8.0f;
    [Tooltip("表示後にふわふわ上下する速さ（1 秒あたりの往復回数）")]
    [SerializeField] private float _thanksIdleSpeed = 0.6f;

    // --------------------------------------------------
    // ----- Confirm Window Settings -----
    // --------------------------------------------------
    [Header("Confirm Window")]
    [Tooltip("選択中のボタンのスケール")]
    [SerializeField] private float _selectedScale = 1.1f;
    [Tooltip("選択中のボタンの文字色")]
    [SerializeField] private Color _selectedTextColor = new Color(1.0f, 0.85f, 0.2f);
    [Tooltip("選択されていないボタンの文字色")]
    [SerializeField] private Color _unselectedTextColor = Color.white;

    // --------------------------------------------------
    // ----- Object Names -----
    // Canvas_Score の子オブジェクト名
    // --------------------------------------------------
    private const string CanvasScoreName = "Canvas_Score";
    private const string TextTimeName = "Text_Time";
    private const string TextRankName = "Text_Rank";
    private const string ImageThankName = "Image_Thank";
    private const string WindowConfirmName = "Window_Confirm";
    private const string FramePath = "Image_Frame";
    private const string ButtonYesPath = "Image_Frame/Button_Yes";
    private const string ButtonNoPath = "Image_Frame/Button_No";

    // --------------------------------------------------
    // ----- UI Objects -----
    // --------------------------------------------------
    private TextMeshProUGUI _textTime;
    private TextMeshProUGUI _textRank;
    private RectTransform _thanks;
    private CanvasGroup _thanksGroup;      // 無くても動く（フェードしないだけ）
    private GameObject _windowConfirm;
    private RectTransform _frameConfirm;
    private Button _buttonYes;
    private Button _buttonNo;
    private TextMeshProUGUI _textYes;
    private TextMeshProUGUI _textNo;

    // --------------------------------------------------
    // ----- Time Format -----
    // --------------------------------------------------
    private const float MaxDisplayTime = 99.0f * 60.0f + 59.99f;   // 99:59:99
    // 毎フレーム文字列を作らない（GC を出さない）ための文字バッファ "00:00:00"
    private readonly char[] _timeChars = { '0', '0', ':', '0', '0', ':', '0', '0' };

    // --------------------------------------------------
    // ----- Thanks State -----
    // --------------------------------------------------
    private Vector2 _thanksBasePosition;
    private bool _isThanksIdle = false;
    private float _thanksIdleStartTime = 0.0f;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        _controller = GetComponent<SceneController_Result>();
        if (_controller == null)
        {
            Debug.LogError("[Error] SceneController_Result Not Found!");
        }

        FindUIObjects();
        InitializeUI();
    }
    private void Update()
    {
        UpdateThanksIdle();
    }

    // --------------------------------------------------
    // ----- Initialize -----
    // --------------------------------------------------
    private void FindUIObjects()
    {
        GameObject canvasScore = GameObject.Find(CanvasScoreName);
        if (canvasScore == null)
        {
            Debug.LogError("[Error] Canvas_Score Not Found!");
            return;
        }
        Transform root = canvasScore.transform;

        _textTime = FindComponent<TextMeshProUGUI>(root, TextTimeName);
        _textRank = FindComponent<TextMeshProUGUI>(root, TextRankName);

        _thanks = FindComponent<RectTransform>(root, ImageThankName);
        if (_thanks != null)
        {
            _thanksBasePosition = _thanks.anchoredPosition;
            _thanksGroup = _thanks.GetComponent<CanvasGroup>();
            if (_thanksGroup == null)
            {
                Debug.LogWarning("[Warning] CanvasGroup Not Found on Image_Thank! フェードせずに表示します。");
            }
        }

        Transform window = root.Find(WindowConfirmName);
        if (window == null)
        {
            Debug.LogError("[Error] Window_Confirm Not Found!");
            return;
        }
        _windowConfirm = window.gameObject;
        _frameConfirm = FindComponent<RectTransform>(window, FramePath);
        _buttonYes = FindComponent<Button>(window, ButtonYesPath);
        _buttonNo = FindComponent<Button>(window, ButtonNoPath);

        // ボタンの文字と、クリック時の処理
        if (_buttonYes != null)
        {
            _textYes = _buttonYes.GetComponentInChildren<TextMeshProUGUI>(true);
            _buttonYes.onClick.AddListener(OnClick_Yes);
            AddHoverListener(_buttonYes, true);
        }
        if (_buttonNo != null)
        {
            _textNo = _buttonNo.GetComponentInChildren<TextMeshProUGUI>(true);
            _buttonNo.onClick.AddListener(OnClick_No);
            AddHoverListener(_buttonNo, false);
        }
    }
    // マウスカーソルを重ねたらそのボタンを選択状態にする
    private void AddHoverListener(Button button, bool isYes)
    {
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => OnHover(isYes));
        trigger.triggers.Add(entry);
    }
    private T FindComponent<T>(Transform parent, string path) where T : Component
    {
        Transform target = parent.Find(path);
        T component = (target != null) ? target.GetComponent<T>() : null;
        if (component == null)
        {
            Debug.LogError("[Error] " + typeof(T).Name + " Not Found: " + parent.name + "/" + path);
        }
        return component;
    }
    private void InitializeUI()
    {
        SetTimeText(0.0f);

        if (_textRank != null)
        {
            _textRank.gameObject.SetActive(false);
        }
        if (_thanks != null)
        {
            _thanks.gameObject.SetActive(false);
        }
        if (_windowConfirm != null)
        {
            _windowConfirm.SetActive(false);
        }
    }

    // --------------------------------------------------
    // ----- Time -----
    // --------------------------------------------------
    // カウントアップを進める。終わったら true を返す
    public bool UpdateCountUp(float elapsed, float targetTime)
    {
        float t = (_countUpDuration > 0.0f) ? Mathf.Clamp01(elapsed / _countUpDuration) : 1.0f;
        if (t >= 1.0f)
        {
            FinishCountUp(targetTime);
            return true;
        }

        SetTimeText(targetTime * _countUpCurve.Evaluate(t));
        return false;
    }
    public void FinishCountUp(float targetTime)
    {
        SetTimeText(targetTime);
    }
    private void SetTimeText(float seconds)
    {
        if (_textTime == null)
        {
            return;
        }

        // 秒を「分:秒:1/100秒」（例 01:23:45）にする
        float clamped = Mathf.Clamp(seconds, 0.0f, MaxDisplayTime);
        int totalCentiseconds = Mathf.FloorToInt(clamped * 100.0f);
        WriteTwoDigits(0, totalCentiseconds / 6000);           // 分
        WriteTwoDigits(3, (totalCentiseconds / 100) % 60);     // 秒
        WriteTwoDigits(6, totalCentiseconds % 100);            // 1/100 秒
        _textTime.SetCharArray(_timeChars, 0, _timeChars.Length);
    }
    private void WriteTwoDigits(int index, int value)
    {
        _timeChars[index] = (char)('0' + value / 10);
        _timeChars[index + 1] = (char)('0' + value % 10);
    }

    // --------------------------------------------------
    // ----- Rank -----
    // --------------------------------------------------
    public void ShowRank(string rank)
    {
        if (_textRank == null)
        {
            return;
        }
        _textRank.text = rank;
        _textRank.transform.localScale = Vector3.zero;
        _textRank.gameObject.SetActive(true);
    }
    // ポップアップを進める。終わったら true を返す
    public bool UpdateRankPop(float elapsed)
    {
        if (_textRank == null)
        {
            return true;
        }
        return UpdatePop(_textRank.transform, elapsed);
    }
    public void FinishRankPop()
    {
        if (_textRank == null)
        {
            return;
        }
        _textRank.transform.localScale = Vector3.one;
    }

    // --------------------------------------------------
    // ----- Thanks -----
    // 指定した方向から入ってきて減速して止まり、その後ふわふわ上下する
    // --------------------------------------------------
    public void ShowThanks()
    {
        if (_thanks == null)
        {
            return;
        }
        _isThanksIdle = false;
        _thanks.anchoredPosition = _thanksBasePosition + GetSlideOffset();
        SetThanksAlpha(0.0f);
        _thanks.gameObject.SetActive(true);
    }
    // 飛び込み演出を進める。終わったら true を返す
    public bool UpdateThanksEntry(float elapsed)
    {
        if (_thanks == null)
        {
            return true;
        }

        float t = (_thanksDuration > 0.0f) ? Mathf.Clamp01(elapsed / _thanksDuration) : 1.0f;
        if (t >= 1.0f)
        {
            FinishThanksEntry();
            return true;
        }

        _thanks.anchoredPosition = _thanksBasePosition + GetSlideOffset() * (1.0f - EaseOutCubic(t));
        SetThanksAlpha(Mathf.Clamp01(t * 3.0f));   // 最初の 1/3 でフェードイン
        return false;
    }
    public void FinishThanksEntry()
    {
        if (_thanks == null)
        {
            return;
        }
        _thanks.anchoredPosition = _thanksBasePosition;
        SetThanksAlpha(1.0f);

        // ここからふわふわ上下させる
        _isThanksIdle = true;
        _thanksIdleStartTime = Time.unscaledTime;
    }
    private void UpdateThanksIdle()
    {
        if (!_isThanksIdle || _thanks == null)
        {
            return;
        }
        float elapsed = Time.unscaledTime - _thanksIdleStartTime;
        float offsetY = Mathf.Sin(elapsed * _thanksIdleSpeed * Mathf.PI * 2.0f) * _thanksIdleAmplitude;
        _thanks.anchoredPosition = _thanksBasePosition + new Vector2(0.0f, offsetY);
    }
    private void SetThanksAlpha(float alpha)
    {
        if (_thanksGroup != null)
        {
            _thanksGroup.alpha = alpha;
        }
    }
    // 演出開始時の位置のずれ（最終位置からどれだけ離れた所から入ってくるか）
    private Vector2 GetSlideOffset()
    {
        switch (_thanksSlideDirection)
        {
            case SlideDirection.FromRight:
                return new Vector2(_thanksFlyDistance, 0.0f);
            case SlideDirection.FromLeft:
                return new Vector2(-_thanksFlyDistance, 0.0f);
            case SlideDirection.FromTop:
                return new Vector2(0.0f, _thanksFlyDistance);
            case SlideDirection.FromBottom:
                return new Vector2(0.0f, -_thanksFlyDistance);
            default:
                return Vector2.zero;
        }
    }
    // 最初は速く、最後はゆっくり止まる動き（0 から 1）
    private float EaseOutCubic(float t)
    {
        float inverse = 1.0f - t;
        return 1.0f - inverse * inverse * inverse;
    }

    // --------------------------------------------------
    // ----- Confirm Window -----
    // --------------------------------------------------
    public void OpenConfirm()
    {
        if (_windowConfirm == null)
        {
            return;
        }
        _windowConfirm.SetActive(true);
        if (_frameConfirm != null)
        {
            _frameConfirm.localScale = Vector3.zero;
        }
    }
    public void UpdateConfirmPop(float elapsed)
    {
        if (_frameConfirm == null)
        {
            return;
        }
        UpdatePop(_frameConfirm, elapsed);
    }
    public void CloseConfirm()
    {
        if (_windowConfirm == null)
        {
            return;
        }
        _windowConfirm.SetActive(false);
    }
    public void SetSelection(bool isYesSelected)
    {
        ApplyButtonLook(_buttonYes, _textYes, isYesSelected);
        ApplyButtonLook(_buttonNo, _textNo, !isYesSelected);
    }
    private void ApplyButtonLook(Button button, TextMeshProUGUI text, bool isSelected)
    {
        if (button != null)
        {
            button.transform.localScale = Vector3.one * (isSelected ? _selectedScale : 1.0f);
        }
        if (text != null)
        {
            text.color = isSelected ? _selectedTextColor : _unselectedTextColor;
        }
    }

    // --------------------------------------------------
    // ----- Pop Scale -----
    // --------------------------------------------------
    // 経過時間に合わせてスケールを変える。終わったら true を返す
    private bool UpdatePop(Transform target, float elapsed)
    {
        float t = (_popDuration > 0.0f) ? Mathf.Clamp01(elapsed / _popDuration) : 1.0f;
        if (t >= 1.0f)
        {
            target.localScale = Vector3.one;
            return true;
        }

        target.localScale = Vector3.one * _popCurve.Evaluate(t);
        return false;
    }

    // --------------------------------------------------
    // ----- OnClick Events -----
    // --------------------------------------------------
    private void OnHover(bool isYes)
    {
        if (_controller != null && _controller.StateMachine != null)
        {
            _controller.StateMachine.RequestSelect(isYes);
        }
    }
    private void OnClick_Yes()
    {
        if (_controller != null && _controller.StateMachine != null)
        {
            _controller.StateMachine.RequestConfirmYes();
        }
    }
    private void OnClick_No()
    {
        if (_controller != null && _controller.StateMachine != null)
        {
            _controller.StateMachine.RequestConfirmNo();
        }
    }
}
