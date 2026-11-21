/*
    UI_ButtonSelectionController
    20261118  hanaue sho
    ボタンを管理するスクリプト
 */
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ButtonSelectionController : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SelectButton -----
    // --------------------------------------------------
    [Header("References")]
    [SerializeField] private UI_Button_Controller _firstSelectedButton;
    [SerializeField] private RectTransform _selectionIcon;

    [Header("Icon")]
    [SerializeField] private float _iconOffset = -25.0f;

    private UI_Button_Controller _selectedButton;

    public UI_Button_Controller SelectedButton => _selectedButton;

    // --------------------------------------------------
    // ----- UnityEvents -----
    // --------------------------------------------------
    private void Start()
    {
        // 初期状態ではアイコンを非表示
        if (_selectionIcon != null)
        {
            _selectionIcon.gameObject.SetActive(false);
        }

        // ----- Initial Selection -----
        if (_firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_firstSelectedButton.gameObject);
        }
    }
    private void LateUpdate()
    {
        if (_selectedButton == null)
        {
            return;
        }

        // ボタンの位置が変わってもアイコンを追従させる
        UpdateIconPosition();
    }


    // --------------------------------------------------
    // ----- Set Selected Button -----
    // 選択されたボタンを登録する
    // --------------------------------------------------
    public void SetSelectedButton(UI_Button_Controller button)
    {
        if (button == null)
        {
            return;
        }
        if (_selectedButton == button)
        {
            return;
        }

        _selectedButton = button;

        // ----- Icon -----
        if (_selectionIcon != null)
        {
            _selectionIcon.gameObject.SetActive(true);
            UpdateIconPosition();
        }
    }

    // --------------------------------------------------
    // ----- Clear Selected Button -----
    // 指定ボタンの選択状態を解除する
    // --------------------------------------------------
    public void ClearSelectedButton(UI_Button_Controller button)
    {
        // 現在選択中のボタン以外からの解除通知は無視
        if (_selectedButton != button)
        {
            return;
        }

        _selectedButton = null;

        if (_selectionIcon != null)
        {
            _selectionIcon.gameObject.SetActive(false);
        }
    }
 
    // --------------------------------------------------
    // ----- Update Icon Position -----
    // 選択中のボタンの左側へアイコンを配置する
    // --------------------------------------------------
    private void UpdateIconPosition()
    {
        if (_selectionIcon == null || _selectedButton == null)
        {
            return;
        }

        RectTransform buttonRect = _selectedButton.RectTransform;
        Rect rect = buttonRect.rect;

        // ----- Target Position -----
        // ボタンの左中央を基準とする
        Vector3 localPosition = new Vector3(rect.xMin - _iconOffset, rect.center.y, 0.0f);

        // ボタンのローカル座標からワールド座標へ変換
        Vector3 worldPosition = buttonRect.TransformPoint(localPosition);

        // アイコンを移動
        _selectionIcon.position = worldPosition;
    }
}
