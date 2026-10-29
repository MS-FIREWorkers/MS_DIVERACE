using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSceneStreamer : MonoBehaviour
{
    // 各レベルシーンの情報
    [System.Serializable]
    public class LevelScene
    {
        public string sceneName;
    }

    // プレイヤー
    [SerializeField] private Transform player;

    // 上から下へ順番に使用するレベルシーン
    [SerializeField]
    private LevelScene[] levelScenes =
    {
        new LevelScene { sceneName = "Scene_Level_05" },
        new LevelScene { sceneName = "Scene_Level_04" },
        new LevelScene { sceneName = "Scene_Level_03" },
        new LevelScene { sceneName = "Scene_Level_02" },
        new LevelScene { sceneName = "Scene_Level_01" }
    };

    // 現在の階層を何％通過したら前のシーンを破棄するか
    [Range(0.0f, 1.0f)]
    [SerializeField] private float unloadPreviousProgress = 0.5f;

    // Debug.Logを表示するか
    [SerializeField] private bool enableDebugLog = true;

    // 現在プレイヤーがいる階層番号
    private int currentLevelIndex;

    // 前の階層を破棄済みか
    private bool previousUnloaded = true;

    // シーンのロード・アンロード処理中か
    private bool isStreaming;

    // ゲーム開始時の初期化
    private IEnumerator Start()
    {
        // Playerが設定されていない場合は処理を停止
        if (player == null)
        {
            Debug.LogError("Player is not assigned.");
            enabled = false;
            yield break;
        }

        // LevelSceneが設定されていない場合は処理を停止
        if (levelScenes == null || levelScenes.Length == 0)
        {
            Debug.LogError("Level scenes are not assigned.");
            enabled = false;
            yield break;
        }

        isStreaming = true;

        // 最初のLevelのTopPointをワールド原点に配置
        yield return LoadAndPlaceLevel(0, Vector3.zero);

        LevelSceneInfo firstLevel = GetLevelInfo(0);

        // TopPointまたはBottomPointが設定されているか確認
        if (firstLevel == null || !firstLevel.IsValid())
        {
            Debug.LogError("First LevelSceneInfo is invalid.");
            enabled = false;
            yield break;
        }

        // 次のLevelを事前に読み込む
        if (levelScenes.Length > 1)
            yield return LoadAndPlaceLevel(
                1,
                firstLevel.BottomPoint.position
            );

        isStreaming = false;
    }

    private void Update()
    {
        // シーン処理中、またはPlayerが存在しない場合は処理しない
        if (isStreaming || player == null)
            return;

        // 現在の階層情報を取得
        LevelSceneInfo currentLevel =
            GetLevelInfo(currentLevelIndex);

        if (currentLevel == null || !currentLevel.IsValid())
            return;

        // PlayerがBottomPointを通過したら次の階層へ移動
        if (currentLevelIndex < levelScenes.Length - 1 &&
            player.position.y <= currentLevel.BottomPoint.position.y)
        {
            StartCoroutine(MoveToNextLevel());
            return;
        }

        // 現在の階層を指定割合以上進んだら前の階層を破棄
        if (!previousUnloaded &&
            currentLevelIndex > 0 &&
            GetLevelProgress(currentLevel) >= unloadPreviousProgress)
        {
            StartCoroutine(UnloadPreviousLevel());
        }
    }

    // Playerが次の階層に入った時の処理
    private IEnumerator MoveToNextLevel()
    {
        // 重複実行防止
        if (isStreaming)
            yield break;

        isStreaming = true;

        // 現在のLevel番号を次へ進める
        currentLevelIndex++;

        // 前のLevelはまだ必要
        previousUnloaded = false;

        LevelSceneInfo currentLevel =
            GetLevelInfo(currentLevelIndex);

        if (currentLevel == null || !currentLevel.IsValid())
        {
            Debug.LogError(
                $"Level info is invalid : {currentLevelIndex}"
            );

            isStreaming = false;
            yield break;
        }

        // 現在の次のLevel番号
        int nextIndex = currentLevelIndex + 1;

        // 次のLevelを事前に読み込む
        if (nextIndex < levelScenes.Length)
        {
            yield return LoadAndPlaceLevel(
                nextIndex,
                currentLevel.BottomPoint.position
            );
        }

        // さらに上に残っている不要なLevelを破棄
        for (int i = 0; i < currentLevelIndex - 1; i++)
        {
            if (IsSceneLoaded(i))
                yield return UnloadLevel(i);
        }

        Log(
            $"Current Level : {levelScenes[currentLevelIndex].sceneName}"
        );

        isStreaming = false;
    }

    // 現在の階層を一定割合進んだ後、前のLevelを破棄
    private IEnumerator UnloadPreviousLevel()
    {
        // 重複実行防止
        if (isStreaming || previousUnloaded)
            yield break;

        int previousIndex = currentLevelIndex - 1;

        if (previousIndex < 0)
        {
            previousUnloaded = true;
            yield break;
        }

        isStreaming = true;

        yield return UnloadLevel(previousIndex);

        previousUnloaded = true;
        isStreaming = false;
    }

    // LevelSceneをロードして指定された位置に接続
    private IEnumerator LoadAndPlaceLevel(
        int index,
        Vector3 targetTopPosition
    )
    {
        // 配列範囲外の場合は終了
        if (index < 0 || index >= levelScenes.Length)
            yield break;

        string sceneName = levelScenes[index].sceneName;

        // Scene名が設定されていない場合は終了
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        // Build ProfileにSceneが登録されているか確認
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Scene '{sceneName}' is not in the Build Profile."
            );

            yield break;
        }

        // Sceneが未ロードの場合だけAdditiveでロード
        if (!IsSceneLoaded(index))
        {
            Log($"Load Scene : {sceneName}");

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive
                );

            if (operation == null)
                yield break;

            // Sceneのロード完了まで待機
            yield return operation;
        }

        // 読み込んだSceneのTopPoint・BottomPointを取得
        LevelSceneInfo info = GetLevelInfo(index);

        if (info == null || !info.IsValid())
        {
            Debug.LogError(
                $"LevelSceneInfo is invalid : {sceneName}"
            );

            yield break;
        }

        // 新しいLevelのTopPointを前のLevelのBottomPointに合わせる
        Vector3 offset =
            targetTopPosition - info.TopPoint.position;

        // LevelRoot全体を移動
        info.transform.position += offset;

        Log(
            $"{sceneName} | Top Y = {info.TopPoint.position.y} | " +
            $"Bottom Y = {info.BottomPoint.position.y}"
        );
    }

    // 指定したLevelSceneをアンロード
    private IEnumerator UnloadLevel(int index)
    {
        if (!IsSceneLoaded(index))
            yield break;

        string sceneName =
            levelScenes[index].sceneName;

        Log($"Unload Scene : {sceneName}");

        AsyncOperation operation =
            SceneManager.UnloadSceneAsync(sceneName);

        if (operation != null)
            yield return operation;
    }

    // Playerが現在のLevelを何％進んだかを計算
    private float GetLevelProgress(
        LevelSceneInfo level
    )
    {
        float topY =
            level.TopPoint.position.y;

        float bottomY =
            level.BottomPoint.position.y;

        // TopPointとBottomPointが同じ高さの場合は計算しない
        if (Mathf.Approximately(topY, bottomY))
            return 0.0f;

        // TopPoint = 0.0
        // BottomPoint = 1.0
        return Mathf.InverseLerp(
            topY,
            bottomY,
            player.position.y
        );
    }

    // 指定したSceneからLevelSceneInfoを取得
    private LevelSceneInfo GetLevelInfo(
        int index
    )
    {
        // Sceneがロードされていない場合は取得不可
        if (!IsSceneLoaded(index))
            return null;

        Scene scene =
            SceneManager.GetSceneByName(
                levelScenes[index].sceneName
            );

        // Scene内のRootからLevelSceneInfoを探す
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            LevelSceneInfo info =
                root.GetComponentInChildren<LevelSceneInfo>(true);

            if (info != null)
                return info;
        }

        return null;
    }

    // 指定したLevelSceneがロード済みか確認
    private bool IsSceneLoaded(
        int index
    )
    {
        if (index < 0 || index >= levelScenes.Length)
            return false;

        Scene scene =
            SceneManager.GetSceneByName(
                levelScenes[index].sceneName
            );

        return scene.IsValid() && scene.isLoaded;
    }

    // デバッグログ
    private void Log(
        string message
    )
    {
        if (enableDebugLog)
            Debug.Log(message);
    }
}