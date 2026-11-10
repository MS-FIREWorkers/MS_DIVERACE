/*
    TitleScene_StateMachine
    20261104  hanaue sho
    タイトルシーンの状態を管理するスクリプト
    以下は管理する状態フロー
    １．チームロゴの表示（スキップ可）
    ２．タイトル画面表示 
    ３．シーン遷移待ち
 */
using UnityEngine;

public class TitleScene_StateMachine : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Title _controller;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Title>();

    }
    private void Update()
    {

    }
}
