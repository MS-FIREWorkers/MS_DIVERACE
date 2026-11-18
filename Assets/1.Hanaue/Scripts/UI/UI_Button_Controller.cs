/*
    UI_Button_Controller
    20261118  hanaue sho
    ボタンにアタッチするスクリプト
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Button))]
public class UI_Button_Controller : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    // --------------------------------------------------
    // ----- SelectButton -----
    // --------------------------------------------------
    [Header("References")]
    [SerializeField] private TitleUI_SelectionController _selectionController;
    [SerializeField] private UI_ParallelogramEffect _effect;

    [Header("Confirm")]
    [SerializeField, TextArea] private string _confirmMessage;

    private Button _button;
    private RectTransform _rectTransform;

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
        _selectionController = FindFirstObjectByType<TitleUI_SelectionController>();

        if (_effect == null)
        {
            _effect = GetComponent<UI_ParallelogramEffect>();
        }
    }
    private void OnDisable()
    {
        if (_selectionController != null)
        {
            _selectionController.ClearSelectedButton(this);
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
            //_effect.PlayHighlight();
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
}
