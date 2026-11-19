/*
    ResultScene_StateMachine
    20261104  hanaue sho
    リザルトシーンの状態を管理するスクリプト
    以下は管理する状態フロー
    １．タイムとスコアの表示演出（スキップ可）
    ２．シーン遷移待ち
 */
using UnityEngine;

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
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Result>();
        _inputHandler = _controller?.InputHandler;
    }
    private void Update()
    {
        // 入力取得の例（消しても大丈夫です）
        if (_inputHandler.ConsumeSubmitPressed())
        {
            // 決定ボタンが押された処理
            GameManager.Instance.ChangeScene(_controller.NameToTitleScene);
        }
        if (_inputHandler.ConsumeCancelPressed())
        {
            // キャンセルボタンが押された処理

        }
        if (_inputHandler.Navigate.y > 0.0f)
        {
            // 上入力
        }
        if (_inputHandler.Navigate.y < 0.0f)
        {
            // 下入力
        }
        if (_inputHandler.Navigate.x > 0.0f)
        {
            // 右入力
        }
        if (_inputHandler.Navigate.x < 0.0f)
        {
            // 左入力
        }

    }
}
