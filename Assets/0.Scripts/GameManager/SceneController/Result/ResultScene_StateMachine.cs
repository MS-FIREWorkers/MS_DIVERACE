/*
    ResultScene_StateMachine
    20261104  hanaue sho
    リザルトシーンの状態を管理するスクリプト
    以下は管理する状態フロー
    １．タイムとスコアの表示演出（スキップ可）
    ２．シーン遷移待ち
 */
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ResultScene_StateMachine : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Result _controller;

    // --------------------------------------------------
    // ----- InputHandler -----
    // --------------------------------------------------
    private ResultScene_InputHandler _inputHandler;

    // --------------------------------------------------
    // ----- PresentationController -----
    // --------------------------------------------------
    private ResultScene_PresentationController _presentation;

    // --------------------------------------------------
    // ----- State -----
    // --------------------------------------------------
    private enum ResultState
    {
        None,
        WaitFadeIn,     // フェードイン待ち
        CountUpTime,    // クリアタイムのカウントアップ（スキップ可）
        ShowRank,       // 評価のポップアップ表示（スキップ可）
        ShowThanks,     // お礼の言葉を右から飛び込ませる（スキップ可）
        WaitInput,      // ボタン入力待ち
        Confirm,        // 確認ウィンドウ表示中
        Transition,     // シーン遷移中
    }
    private ResultState _state = ResultState.None;
    private float _stateTimer = 0.0f;

    // --------------------------------------------------
    // ----- Wait Settings -----
    // --------------------------------------------------
    private enum FadeWaitMode
    {
        None,                   // 待たない
        FixedTime,              // 指定した秒数だけ待つ
        FadeDurationAndMargin,  // フェードの時間 + マージンだけ待つ（フェード時間が変わっても自動で合わせる）
    }
    [Header("Fade Wait")]
    [Tooltip("カウントアップ開始までの待ち方")]
    [SerializeField] private FadeWaitMode _fadeWaitMode = FadeWaitMode.FadeDurationAndMargin;
    [Tooltip("FixedTime の時の待ち時間（秒）")]
    [SerializeField] private float _fadeWaitTime = 0.5f;
    [Tooltip("FadeDurationAndMargin の時、フェードの時間に足す余白（秒）")]
    [SerializeField] private float _fadeWaitMargin = 0.2f;
    private float _fadeWaitDuration = 0.0f;   // 実際に待つ時間（Start で決める）

    // --------------------------------------------------
    // ----- Confirm Window Settings -----
    // --------------------------------------------------
    [Header("Confirm Window")]
    [Tooltip("ボタン入力後に確認ウィンドウを出すか。オフにするとそのままタイトルへ遷移する")]
    [SerializeField] private bool _useConfirmWindow = true;

    // --------------------------------------------------
    // ----- Rank Settings -----
    // --------------------------------------------------
    // クリアタイム（秒）が以下の値以下ならその評価、B より遅ければ C 評価
    // S ≦ A ≦ B の順になるように設定する
    [Header("Rank (Clear Time Seconds)")]
    [Tooltip("クリアタイムがこの秒数以下なら S 評価")]
    [SerializeField] private float _rankSTime = 120.0f;
    [Tooltip("クリアタイムがこの秒数以下なら A 評価（S の秒数より大きくする）")]
    [SerializeField] private float _rankATime = 180.0f;
    [Tooltip("クリアタイムがこの秒数以下なら B 評価（A の秒数より大きくする）。これより遅いと C 評価")]
    [SerializeField] private float _rankBTime = 240.0f;

    // --------------------------------------------------
    // ----- Debug -----
    // プレイ時間の受け渡し方法が決まるまでの仮の値
    // --------------------------------------------------
    [Header("Debug")]
    [Tooltip("表示するプレイ時間（秒）。評価の確認はこの値を変えて行う")]
    [SerializeField] private float _debugPlayTime = 83.45f;

    // --------------------------------------------------
    // ----- Result Values -----
    // --------------------------------------------------
    private float _playTime = 0.0f;
    private string _rank = "";

    // --------------------------------------------------
    // ----- Confirm Selection -----
    // --------------------------------------------------
    private const float NavigateThreshold = 0.5f;   // 左右入力とみなす傾きの大きさ
    private float _prevNavigateX = 0.0f;
    private bool _isYesSelected = false;

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

        _inputHandler = GetComponent<ResultScene_InputHandler>();
        if (_inputHandler == null)
        {
            Debug.LogError("[Error] ResultScene_InputHandler Not Found!");
        }

        _presentation = GetComponent<ResultScene_PresentationController>();
        if (_presentation == null)
        {
            Debug.LogError("[Error] ResultScene_PresentationController Not Found!");
        }
    }
    private void Start()
    {
        _playTime = GetPlayTime();
        _rank = CalculateRank(_playTime);
        _fadeWaitDuration = GetFadeWaitDuration();

        ChangeState(ResultState.WaitFadeIn);
    }
    private void OnValidate()
    {
        // 評価の秒数の並びが逆だと正しく判定できないため警告する
        if (_rankSTime > _rankATime || _rankATime > _rankBTime)
        {
            Debug.LogWarning("[Warning] Rank Time Order Is Wrong! S ≦ A ≦ B の順に設定してください。");
        }
    }
    private void Update()
    {
        if (_inputHandler == null || _presentation == null)
        {
            return;
        }

        _stateTimer += Time.unscaledDeltaTime;

        switch (_state)
        {
            case ResultState.WaitFadeIn:
                UpdateWaitFadeIn();
                break;
            case ResultState.CountUpTime:
                UpdateCountUpTime();
                break;
            case ResultState.ShowRank:
                UpdateShowRank();
                break;
            case ResultState.ShowThanks:
                UpdateShowThanks();
                break;
            case ResultState.WaitInput:
                UpdateWaitInput();
                break;
            case ResultState.Confirm:
                UpdateConfirm();
                break;
            case ResultState.Transition:
                ClearInput();
                break;
        }
    }

    // --------------------------------------------------
    // ----- Result Values -----
    // --------------------------------------------------
    private float GetPlayTime()
    {
        // TODO: プレイ時間の受け渡し方法が決まったら、ここで取得するように変更する
        float playTime = _debugPlayTime;
        if (_controller.RaceResultData != null)
        {
            playTime = _controller.RaceResultData.RaceTime;
        }

        return playTime;
    }
    private string CalculateRank(float playTime)
    {
        if (playTime <= _rankSTime)
        {
            return "S";
        }
        if (playTime <= _rankATime)
        {
            return "A";
        }
        if (playTime <= _rankBTime)
        {
            return "B";
        }
        return "C";
    }

    // --------------------------------------------------
    // ----- State Control -----
    // --------------------------------------------------
    private void ChangeState(ResultState next)
    {
        _state = next;
        _stateTimer = 0.0f;

        // 各状態に入った時の処理
        switch (next)
        {
            case ResultState.ShowRank:
                _presentation.ShowRank(_rank);
                break;
            case ResultState.ShowThanks:
                _presentation.ShowThanks();
                break;
            case ResultState.Confirm:
                // 誤操作防止のため「いいえ」を初期選択にする
                _isYesSelected = false;
                _prevNavigateX = _inputHandler.Navigate.x;
                _presentation.OpenConfirm();
                _presentation.SetSelection(_isYesSelected);
                break;
            case ResultState.Transition:
                StartTransition();
                break;
        }
    }

    // --------------------------------------------------
    // ----- State Update -----
    // --------------------------------------------------
    private void UpdateWaitFadeIn()
    {
        // フェード中の入力は捨てる
        ClearInput();

        if (_stateTimer >= _fadeWaitDuration)
        {
            ChangeState(ResultState.CountUpTime);
        }
    }
    private float GetFadeWaitDuration()
    {
        switch (_fadeWaitMode)
        {
            case FadeWaitMode.FixedTime:
                return _fadeWaitTime;
            case FadeWaitMode.FadeDurationAndMargin:
                if (FadeController.Instance == null)
                {
                    Debug.LogWarning("[Warning] FadeController Not Found! マージンの時間だけ待ちます。");
                    return _fadeWaitMargin;
                }
                return FadeController.Instance.FadeDuration + _fadeWaitMargin;
            default:
                return 0.0f;
        }
    }
    private void UpdateCountUpTime()
    {
        bool isSkipped = _inputHandler.ConsumeSubmitPressed();
        _inputHandler.ConsumeCancelPressed();

        if (isSkipped)
        {
            // スキップ：即プレイタイムまで増加させる
            _presentation.FinishCountUp(_playTime);
            ChangeState(ResultState.ShowRank);
            return;
        }

        if (_presentation.UpdateCountUp(_stateTimer, _playTime))
        {
            ChangeState(ResultState.ShowRank);
        }
    }
    private void UpdateShowRank()
    {
        bool isSkipped = _inputHandler.ConsumeSubmitPressed();
        _inputHandler.ConsumeCancelPressed();

        if (isSkipped)
        {
            // スキップ：即アニメーション終了
            _presentation.FinishRankPop();
            ChangeState(ResultState.ShowThanks);
            return;
        }

        if (_presentation.UpdateRankPop(_stateTimer))
        {
            ChangeState(ResultState.ShowThanks);
        }
    }
    private void UpdateShowThanks()
    {
        bool isSkipped = _inputHandler.ConsumeSubmitPressed();
        _inputHandler.ConsumeCancelPressed();

        if (isSkipped)
        {
            // スキップ：即アニメーション終了
            _presentation.FinishThanksEntry();
            ChangeState(ResultState.WaitInput);
            return;
        }

        if (_presentation.UpdateThanksEntry(_stateTimer))
        {
            ChangeState(ResultState.WaitInput);
        }
    }
    private void UpdateWaitInput()
    {
        _inputHandler.ConsumeCancelPressed();

        // マウスは「タイトルへ」ボタンを押した時だけ開く（OnClick 側で処理）。画面の空いている所のクリックでは開かない
        if (_inputHandler.ConsumeSubmitPressed() && !IsMouseClickedThisFrame())
        {
            // ベータ版用に一時的に無効化
            //OpenConfirmOrTransition();
        }
    }
    private void OpenConfirmOrTransition()
    {
        // 確認ウィンドウを使わない設定なら、そのままタイトルへ遷移する
        ChangeState(_useConfirmWindow ? ResultState.Confirm : ResultState.Transition);
    }
    private void UpdateConfirm()
    {
        _presentation.UpdateConfirmPop(_stateTimer);

        // 左右入力（倒した瞬間だけ反応させる）
        float navigateX = _inputHandler.Navigate.x;
        if (navigateX <= -NavigateThreshold && _prevNavigateX > -NavigateThreshold)
        {
            SelectConfirm(true);
        }
        if (navigateX >= NavigateThreshold && _prevNavigateX < NavigateThreshold)
        {
            SelectConfirm(false);
        }
        _prevNavigateX = navigateX;

        if (_inputHandler.ConsumeCancelPressed())
        {
            CloseConfirm();
            return;
        }
        // マウスクリックは Yes / No ボタン側（OnClick）で処理するので、ここでは無視する
        if (_inputHandler.ConsumeSubmitPressed() && !IsMouseClickedThisFrame())
        {
            DecideConfirm(_isYesSelected);
        }
    }

    // --------------------------------------------------
    // ----- Confirm Window -----
    // --------------------------------------------------
    private void SelectConfirm(bool isYes)
    {
        _isYesSelected = isYes;
        _presentation.SetSelection(_isYesSelected);
    }
    private void DecideConfirm(bool isYes)
    {
        if (isYes)
        {
            ChangeState(ResultState.Transition);
        }
        else
        {
            CloseConfirm();
        }
    }
    private void CloseConfirm()
    {
        _presentation.CloseConfirm();
        ChangeState(ResultState.WaitInput);
    }

    // --------------------------------------------------
    // ----- Transition -----
    // --------------------------------------------------
    private void StartTransition()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Error] GameManager Not Found!");
            return;
        }
        GameManager.Instance.ChangeScene(_controller.NameToTitleScene);
    }

    // --------------------------------------------------
    // ----- OnClick Requests -----
    // マウスでボタンを押した時に呼ばれる
    // --------------------------------------------------
    public void RequestConfirm()
    {
        ClearSelectedButton();
        if (_state == ResultState.WaitInput)
        {
            OpenConfirmOrTransition();
        }
    }
    // マウスカーソルを Yes / No に重ねた時に呼ばれる
    public void RequestSelect(bool isYes)
    {
        if (_state == ResultState.Confirm)
        {
            SelectConfirm(isYes);
        }
    }
    public void RequestConfirmYes()
    {
        ClearSelectedButton();
        if (_state == ResultState.Confirm)
        {
            DecideConfirm(true);
        }
    }
    public void RequestConfirmNo()
    {
        ClearSelectedButton();
        if (_state == ResultState.Confirm)
        {
            DecideConfirm(false);
        }
    }

    // --------------------------------------------------
    // ----- Utility -----
    // --------------------------------------------------
    private void ClearInput()
    {
        _inputHandler.ConsumeSubmitPressed();
        _inputHandler.ConsumeCancelPressed();
    }
    private bool IsMouseClickedThisFrame()
    {
        // マウス左クリックも Submit に割り当てられているため、クリックかどうかを判定する
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
    private void ClearSelectedButton()
    {
        // クリックしたボタンが選択状態のまま残ると、決定ボタンで二重に反応するため解除する
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
