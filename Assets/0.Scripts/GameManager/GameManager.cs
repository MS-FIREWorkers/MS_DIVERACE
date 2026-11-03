/*
    GameManager
    20261103  hanaue sho
    ゲームの開始から終わりまで管理するスクリプト
    シングルトン化する。
    SceneController_Base をシーン上に別に置いてそれを検索して取得する。
    SceneManager.OnLoaded は Awake() や OnEnable() の後、Start() の前に呼ばれるよ。
 */
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Singleton -----
    // --------------------------------------------------
    public static GameManager Instance {  get; private set; }

    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Base _sceneController;
    public SceneController_Base SceneController => _sceneController;

    // --------------------------------------------------
    // ----- SceneChange -----
    // --------------------------------------------------
    private bool _isChangingScene = false;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        // ----- Singleton -----
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // シーン読み込みイベント
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void Start()
    {
        // 初期シーンの SceneController 取得
        if (_sceneController == null)
        {
            SetSceneController(SceneManager.GetActiveScene());
        }
    }
    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;

        Instance = null;
    }

    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private void SetSceneController(Scene scene)
    {
        SceneController_Base controller = null;

        // 読み込まれたシーンから SceneController を検索
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            controller = root.GetComponentInChildren<SceneController_Base>(true);
            if (controller != null)
            {
                break;
            }
        }

        _sceneController = controller;
        if (_sceneController == null)
        {
            Debug.LogWarning("[Warning] SceneController Not Found: " + scene.name);
            return;
        }

        // Enter
        _sceneController?.Enter();
    }

    // --------------------------------------------------
    // ----- SceneEvents -----
    // --------------------------------------------------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetSceneController(scene);
    }

    // --------------------------------------------------
    // ----- SceneChange -----
    // --------------------------------------------------
    public void ChangeScene(string sceneName)
    {
        if (_isChangingScene)
        {
            return;
        }

        StartCoroutine(ChangeSceneCoroutine(sceneName));
    }
    private IEnumerator ChangeSceneCoroutine(string sceneName)
    {
        _isChangingScene = true;

        // FadeOut
        if (FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeOut();
        }

        // 現在のSceneControllerを終了
        _sceneController?.Exit();
        _sceneController = null;

        // シーン読み込み
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError("[Error] Scene Load Failed: " + sceneName);

            if (FadeController.Instance != null)
            {
                yield return FadeController.Instance.FadeIn();
            }

            _isChangingScene = false;
            yield break;
        }

        yield return operation;

        // FadeIn
        if (FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeIn();
        }

        _isChangingScene = false;
    }


}
