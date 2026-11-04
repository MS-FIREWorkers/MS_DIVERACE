/*
    ResultScene_PresentationController
    20261104  hanaue sho
    リザルトシーンでの演出を指示するスクリプト
    スコアの表示や演出の再生の指示など
 */
using UnityEngine;

public class ResultScene_PresentationController : MonoBehaviour
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
}
