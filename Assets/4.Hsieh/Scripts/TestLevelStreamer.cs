using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestLevelStreamer : MonoBehaviour
{
    [System.Serializable]
    public class LevelScene
    {
        public string sceneName;
    }

    [SerializeField] private Transform player;

    [SerializeField]
    private LevelScene[] levelScenes =
    {
        new LevelScene { sceneName = "Test_Scene_Level_05" },
        new LevelScene { sceneName = "Test_Scene_Level_04" },
        new LevelScene { sceneName = "Test_Scene_Level_03" },
        new LevelScene { sceneName = "Test_Scene_Level_02" },
        new LevelScene { sceneName = "Test_Scene_Level_01" }
    };

    [Range(0.0f, 1.0f)]
    [SerializeField] private float unloadPreviousProgress = 0.5f;

    // 環境エフェクト管理クラスへの参照。
    private EnvironmentEffectManager _environmentEffectManager;

    private int currentLevelIndex;
    private bool isStreaming;


    private void Awake()
    {
        // Main Sceneにある環境エフェクト管理クラスを取得する。
        _environmentEffectManager = FindFirstObjectByType<EnvironmentEffectManager>();

        if (_environmentEffectManager == null)
            Debug.LogWarning("[Warning] EnvironmentEffectManager not found.", this);
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player is not assigned.");
            enabled = false;
            return;
        }

        StartCoroutine(InitializeLevels());
    }

    private IEnumerator InitializeLevels()
    {
        isStreaming = true;

        yield return LoadAndPlaceLevel(
            0,
            Vector3.zero
        );

        if (levelScenes.Length > 1)
        {
            LevelSceneInfo currentInfo = GetLevelInfo(0);

            if (currentInfo != null)
            {
                yield return LoadAndPlaceLevel(
                    1,
                    currentInfo.BottomPoint.position
                );
            }
        }

        isStreaming = false;
    }

    private void Update()
    {
        if (isStreaming)
            return;

        LevelSceneInfo currentInfo =
            GetLevelInfo(currentLevelIndex);

        if (currentInfo == null)
            return;

        float progress =
            GetLevelProgress(currentInfo);

        if (progress >= unloadPreviousProgress)
        {
            int previousIndex =
                currentLevelIndex - 1;

            if (previousIndex >= 0 &&
                IsSceneLoaded(previousIndex))
            {
                StartCoroutine(
                    UnloadLevel(previousIndex)
                );
            }
        }

        if (player.position.y <=
            currentInfo.BottomPoint.position.y)
        {
            if (currentLevelIndex <
                levelScenes.Length - 1)
            {
                StartCoroutine(
                    MoveToNextLevel()
                );
            }
        }
    }

    private IEnumerator MoveToNextLevel()
    {
        isStreaming = true;

        currentLevelIndex++;

        // 現在のレベルに対応するパーティクルへ切り替える。
        if (_environmentEffectManager != null)
            _environmentEffectManager.SetLevel(currentLevelIndex);

        int nextIndex =
            currentLevelIndex + 1;

        if (nextIndex < levelScenes.Length)
        {
            LevelSceneInfo currentInfo =
                GetLevelInfo(currentLevelIndex);

            if (currentInfo != null)
            {
                yield return LoadAndPlaceLevel(
                    nextIndex,
                    currentInfo.BottomPoint.position
                );
            }
        }

        yield return UnloadLevelsOutsideWindow();

        Debug.Log(
            $"Current Level Index : {currentLevelIndex}"
        );

        isStreaming = false;
    }

    private IEnumerator LoadAndPlaceLevel(
        int index,
        Vector3 targetTopPosition
    )
    {
        if (index < 0 ||
            index >= levelScenes.Length)
            yield break;

        string sceneName =
            levelScenes[index].sceneName;

        if (!Application.CanStreamedLevelBeLoaded(
                sceneName))
        {
            Debug.LogError(
                $"Scene '{sceneName}' is not in the Build Profile."
            );

            yield break;
        }

        if (!IsSceneLoaded(index))
        {
            Debug.Log(
                $"Load Scene : {sceneName}"
            );

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive
                );

            if (operation == null)
                yield break;

            yield return operation;
        }

        LevelSceneInfo info =
            GetLevelInfo(index);

        if (info == null)
        {
            Debug.LogError(
                $"LevelSceneInfo not found : {sceneName}"
            );

            yield break;
        }

        Vector3 offset =
            targetTopPosition -
            info.TopPoint.position;

        info.transform.position += offset;

        Debug.Log(
            $"{sceneName} Top Y = {info.TopPoint.position.y}, " +
            $"Bottom Y = {info.BottomPoint.position.y}"
        );
    }

    private float GetLevelProgress(
        LevelSceneInfo info
    )
    {
        float topY =
            info.TopPoint.position.y;

        float bottomY =
            info.BottomPoint.position.y;

        return Mathf.InverseLerp(
            topY,
            bottomY,
            player.position.y
        );
    }

    private LevelSceneInfo GetLevelInfo(
        int index
    )
    {
        if (!IsSceneLoaded(index))
            return null;

        Scene scene =
            SceneManager.GetSceneByName(
                levelScenes[index].sceneName
            );

        foreach (GameObject root in
                 scene.GetRootGameObjects())
        {
            LevelSceneInfo info =
                root.GetComponentInChildren<LevelSceneInfo>(
                    true
                );

            if (info != null)
                return info;
        }

        return null;
    }

    private bool IsSceneLoaded(int index)
    {
        if (index < 0 ||
            index >= levelScenes.Length)
            return false;

        Scene scene =
            SceneManager.GetSceneByName(
                levelScenes[index].sceneName
            );

        return scene.IsValid() &&
               scene.isLoaded;
    }

    private IEnumerator UnloadLevel(int index)
    {
        if (!IsSceneLoaded(index))
            yield break;

        string sceneName =
            levelScenes[index].sceneName;

        Debug.Log(
            $"Unload Scene : {sceneName}"
        );

        AsyncOperation operation =
            SceneManager.UnloadSceneAsync(
                sceneName
            );

        if (operation != null)
            yield return operation;
    }

    private IEnumerator UnloadLevelsOutsideWindow()
    {
        for (int i = 0;
             i < levelScenes.Length;
             i++)
        {
            if (i >= currentLevelIndex - 1 &&
                i <= currentLevelIndex + 1)
                continue;

            if (IsSceneLoaded(i))
                yield return UnloadLevel(i);
        }
    }
}