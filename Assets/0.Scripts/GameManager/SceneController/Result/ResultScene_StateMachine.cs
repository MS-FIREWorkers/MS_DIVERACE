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
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Result>();

    }
    private void Update()
    {
        
    }
}
