/*
    UI_Button_Controller
    20261118  hanaue sho
    ボタンにアタッチするスクリプト
    本当は確認ウィンドウのハンドラーを独立させるべきかも
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
public class UI_Button_Controller : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    // --------------------------------------------------
    // ----- SelectButton -----
    // --------------------------------------------------
    [Header("References（手動参照）")]
    [SerializeField] private UI_ButtonSelectionController _selectionController;
    [SerializeField] private UI_ParallelogramEffect _effect;
    [Header("Confirm")]
    [SerializeField, TextArea] private string _confirmMessage;

    private Button _button;
    private RectTransform _rectTransform;

    // --------------------------------------------------
    // ----- Click Event -----
    // --------------------------------------------------
    [Header("Click Event")]
    [SerializeField] private float _clickInterval = 0.3f;
    [SerializeField] private UnityEvent _onClickAfterEffect;
    private bool _isClickProcessing = false;

    // --------------------------------------------------
    // ----- PublicProperty -----
    // --------------------------------------------------
    public string ConfirmMessage => _confirmMessage;
    public RectTransform RectTransform => _rectTransform;


    // --------------------------------------------------
    // ----- UnityEvents -----
    // --------------------------------------------------
    private void Awake()
    {
        _button = GetComponent<Button>();
        _rectTransform = GetComponent<RectTransform>();

        if (_effect == null)
        {
            _effect = GetComponent<UI_ParallelogramEffect>();
        }

        // ----- Button Events -----
        _button.onClick.AddListener(OnClick);
    }
    private void OnDisable()
    {
        if (_selectionController != null)
        {
            _selectionController.ClearSelectedButton(this);
        }
    }
    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClick);
        }
    }

    // --------------------------------------------------
    // ----- Pointer Enter -----
    // マウスがボタンに入ったとき
    // --------------------------------------------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_button.IsActive() || !_button.IsInteractable())
        {
            return;
        }

        // ----- Mouse Check -----
        // マウスが動いているときだけ選択を変更する
        if (Mouse.current == null)
        {
            return;
        }
        if (Mouse.current.delta.ReadValue().sqrMagnitude < 0.01f)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.alreadySelecting)
        {
            return;
        }

        // 選択中のボタンなら変更しない
        if (eventSystem.currentSelectedGameObject == gameObject)
        {
            return;
        }


        eventSystem.SetSelectedGameObject(gameObject);

        // SE
        AudioManager.Instance.PlaySE(AudioId.SelectButton);
    }
    // --------------------------------------------------
    // ----- On Select -----
    // EventSystemから選択されたとき
    // --------------------------------------------------
    public void OnSelect(BaseEventData eventData)
    {
        if (_selectionController != null)
        {
            _selectionController.SetSelectedButton(this);
        }

        // ----- Effects -----
        if (_effect != null)
        {
            _effect.PlayBorderLight();
        }

    }
    // --------------------------------------------------
    // ----- On Deselect -----
    // EventSystemから選択解除されたとき
    // --------------------------------------------------
    public void OnDeselect(BaseEventData eventData)
    {
        if (_selectionController != null)
        {
            _selectionController.ClearSelectedButton(this);
        }
    }

    // --------------------------------------------------
    // ----- OnClick -----
    // --------------------------------------------------
    private void OnClick()
    {
        if (_isClickProcessing)
        {
            return;
        }

        StartCoroutine(ClickSequence());
    }

    // --------------------------------------------------
    // ----- Click Sequence -----
    // 演出終了後にクリックイベントを発火する
    // --------------------------------------------------
    private IEnumerator ClickSequence()
    {
        _isClickProcessing = true;

        // ----- Effect -----
        if (_effect != null)
        {
            _effect.PlayHighlight();
        }

        // ----- SE -----
        AudioManager.Instance.PlaySE(AudioId.PressButton);

        // ----- Interval -----
        yield return new WaitForSecondsRealtime(Mathf.Max(_clickInterval, 0.0f));

        // ----- Click Event -----
        _isClickProcessing = false;
        _onClickAfterEffect?.Invoke();
    }

    // --------------------------------------------------
    // ----- ClickEvent Registration -----
    // --------------------------------------------------
    public void AddClickListener(UnityAction action)
    {
        _onClickAfterEffect.AddListener(action);
    }
    public void RemoveClickListener(UnityAction action)
    {
        _onClickAfterEffect.RemoveListener(action);
    }
}
