/*
    UI_ConfirmWindow
    20261118  hanaue sho
    確認ウィンドウにアタッチするスクリプト
 */
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UI_ConfirmWindow : MonoBehaviour
{
    // --------------------------------------------------
    // ----- References -----
    // --------------------------------------------------
    [Header("References（手動参照）")]
    [SerializeField] private CanvasGroup _mainButtonsCanvasGroup;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private UI_Button_Controller _buttonYes;
    [SerializeField] private UI_Button_Controller _buttonNo;

    private Action _onConfirm; // 確認後に発火するイベント
    private UI_Button_Controller _previousButton;

    // --------------------------------------------------
    // ----- UnityEvents -----
    // --------------------------------------------------
    private void Awake()
    {
        // 初期状態では非表示
        gameObject.SetActive(false);

        // ----- Button Events -----
        if (_buttonYes != null)
        {
            _buttonYes.AddClickListener(OnClick_Yes);
        }
        if (_buttonNo != null)
        {
            _buttonNo.AddClickListener(OnClick_No);
        }
    }
    private void OnDestroy()
    {
        // ----- Button Events -----
        if (_buttonYes != null)
        {
            _buttonYes.RemoveClickListener(OnClick_Yes);
        }
        if (_buttonNo != null)
        {
            _buttonNo.RemoveClickListener(OnClick_No);
        }
    }


    // --------------------------------------------------
    // ----- Open -----
    // 確認ウィンドウを開く
    // --------------------------------------------------
    public void Open(string message, UI_Button_Controller previousButton, Action onConfirm)
    {
        _onConfirm = onConfirm;
        _previousButton = previousButton;

        // ----- Message -----
        if (_messageText != null)
        {
            _messageText.text = message;
        }

        // ----- Main Buttons -----
        // 背後のボタン操作を無効化
        if (_mainButtonsCanvasGroup != null)
        {
            _mainButtonsCanvasGroup.interactable = false;
            _mainButtonsCanvasGroup.blocksRaycasts = false;
        }

        // ----- Selection -----
        // 元のボタンの選択を解除
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // ウィンドウを表示
        gameObject.SetActive(true);

        // 「はい」を初期選択
        if (_buttonYes != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_buttonYes.gameObject);
        }
    }

    // --------------------------------------------------
    // ----- Close -----
    // 確認ウィンドウを閉じ、元の選択を復元する
    // --------------------------------------------------
    private void Close()
    {
        // ----- Selection -----
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // ----- Main Buttons -----
        gameObject.SetActive(false);

        if (_mainButtonsCanvasGroup != null)
        {
            _mainButtonsCanvasGroup.interactable = true;
            _mainButtonsCanvasGroup.blocksRaycasts = true;
        }

        // ----- Restore Selection -----
        if (_previousButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_previousButton.gameObject);
        }

        _previousButton = null;
    }

    // --------------------------------------------------
    // ----- OnClick Events -----
    // --------------------------------------------------
    public void OnClick_Yes()
    {
        Debug.Log("[Debug] OnClick_Yes");
        // 登録された処理を退避
        Action confirmAction = _onConfirm;
        _onConfirm = null;

        Close();

        // 確認された処理を実行
        confirmAction?.Invoke();
    }
    public void OnClick_No()
    {
        _onConfirm = null;

        Close();
    }
}
