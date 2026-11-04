/*
    LevelScene_StateMachine
    20261104  hanaue sho
    レベルシーンの状態を管理するスクリプト
    以下は管理する状態フロー
    １．オープニングムービー再生（スキップ可）
    ２．ゲームプレイ
    ３．ゴール待ち
    ４．ゴール後演出
    ５．ゴール後ムービー再生（スキップ可）

    ゲームプレイ中は一時停止の処理も加える。
 */
using UnityEngine;

public class LevelScene_StateMachine : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Level _controller;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Level>();

    }
    private void Update()
    {

    }
}
