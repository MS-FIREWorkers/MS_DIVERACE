
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class HUDHeightMiniMap : MonoBehaviour
{
    // ==========================================
    // ----- References -----
    // ==========================================

    [Header("参照設定")]

    [InspectorName("プレイヤー（空欄なら自動取得）")]
    [SerializeField]
    private Transform player;

    [InspectorName("ルートCanvasに配置")]
    [Tooltip("他のHUDの移動や縮小の影響を受けないようにする")]
    [SerializeField]
    private bool attachToRootCanvas = true;

    // ==========================================
    // ----- Floor Settings -----
    // ==========================================

    [Header("階層設定")]

    [InspectorName("一番上の階層番号")]
    [SerializeField, Min(1)]
    private int highestFloor = 5;

    [InspectorName("一番下の階層番号")]
    [SerializeField, Min(1)]
    private int lowestFloor = 1;

    // ==========================================
    // ----- Layout Settings -----
    // ==========================================

    [Header("表示位置")]

    [InspectorName("画面右上からの余白")]
    [SerializeField]
    private Vector2 screenMargin =
        new Vector2(35f, 70f);

    [InspectorName("各階層の表示高さ")]
    [SerializeField, Min(12f)]
    private float floorHeight = 46f;

    [InspectorName("階層バーの幅")]
    [SerializeField, Min(4f)]
    private float floorBarWidth = 18f;

    [InspectorName("階層バーの間隔")]
    [SerializeField, Range(0f, 10f)]
    private float floorGap = 4f;

    // ==========================================
    // ----- Colors -----
    // ==========================================

    [Header("色設定")]

    [InspectorName("通常階層の色")]
    [SerializeField]
    private Color inactiveFloorColor =
        new Color(0.10f, 0.20f, 0.26f, 0.65f);

    [InspectorName("現在階層の色")]
    [SerializeField]
    private Color activeFloorColor =
        new Color(0.12f, 0.46f, 0.57f, 0.85f);

    [InspectorName("プレイヤーマーカーの色")]
    [SerializeField]
    private Color playerMarkerColor =
        new Color(0.48f, 0.94f, 1f, 1f);

    [InspectorName("階層番号の色")]
    [SerializeField]
    private Color labelColor =
        new Color(0.40f, 0.60f, 0.68f, 1f);

    // ==========================================
    // ----- Debug -----
    // ==========================================

    [Header("デバッグ表示")]

    [InspectorName("テストモード")]
    [Tooltip("実際のプレイヤー位置を使わず表示を確認する")]
    [SerializeField]
    private bool debugMode = false;

    [InspectorName("テスト階層")]
    [SerializeField]
    private int debugFloor = 5;

    [InspectorName("階層内の進行率")]
    [SerializeField, Range(0f, 1f)]
    private float debugProgress = 0.5f;

    // ==========================================
    // ----- Runtime -----
    // ==========================================

    private readonly Dictionary<int, LevelSceneInfo>
        levelInfos = new Dictionary<int, LevelSceneInfo>();

    private Image[] floorImages;
    private TextMeshProUGUI[] floorLabels;

    private RectTransform playerMarker;

    private int floorCount;

    private float nextSceneScanTime;
    private float nextPlayerSearchTime;

    private const float TopPadding = 10f;
    private const float TrackX = 46f;

    // ==========================================
    // ----- Unity Events -----
    // ==========================================

    private void Awake()
    {
        highestFloor = Mathf.Max(1, highestFloor);

        lowestFloor = Mathf.Clamp(
            lowestFloor,
            1,
            highestFloor
        );

        floorCount =
            highestFloor - lowestFloor + 1;

        SetupRoot();

        GenerateMiniMap();

        FindPlayer();

        RefreshLevelInfos();
    }

    private void LateUpdate()
    {
        // ----- Find Player -----

        if (player == null &&
            Time.unscaledTime >= nextPlayerSearchTime)
        {
            FindPlayer();

            nextPlayerSearchTime =
                Time.unscaledTime + 1f;
        }

        // ----- Refresh Loaded Floors -----

        if (Time.unscaledTime >= nextSceneScanTime)
        {
            RefreshLevelInfos();

            nextSceneScanTime =
                Time.unscaledTime + 0.25f;
        }

        // ----- Calculate Height -----

        int currentFloor;
        float progress;

        bool valid;

        if (debugMode)
        {
            currentFloor = Mathf.Clamp(
                debugFloor,
                lowestFloor,
                highestFloor
            );

            progress = debugProgress;

            valid = true;
        }
        else
        {
            valid = TryGetPlayerHeight(
                out currentFloor,
                out progress
            );
        }

        if (!valid)
        {
            if (playerMarker != null)
                playerMarker.gameObject.SetActive(false);

            UpdateFloorColors(-1);

            return;
        }

        UpdatePlayerMarker(
            currentFloor,
            progress
        );
    }

    // ==========================================
    // ----- Root Position -----
    // ==========================================

    private void SetupRoot()
    {
        RectTransform rect =
            GetComponent<RectTransform>();

        if (attachToRootCanvas)
        {
            Canvas canvas =
                GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                Transform canvasRoot =
                    canvas.rootCanvas.transform;

                if (rect.parent != canvasRoot)
                {
                    rect.SetParent(
                        canvasRoot,
                        false
                    );
                }
            }
        }

        // 画面右上に固定
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;

        rect.sizeDelta = new Vector2(
            112f,
            floorCount * floorHeight +
            TopPadding * 2f
        );

        rect.anchoredPosition = new Vector2(
            -Mathf.Abs(screenMargin.x),
            -Mathf.Abs(screenMargin.y)
        );

        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    // ==========================================
    // ----- Generate Mini Map -----
    // ==========================================

    private void GenerateMiniMap()
    {
        floorImages = new Image[floorCount];

        floorLabels =
            new TextMeshProUGUI[floorCount];

        for (int i = 0; i < floorCount; i++)
        {
            int floorNumber =
                highestFloor - i;

            float y =
                TopPadding + i * floorHeight;

            // ----- Floor Bar -----

            Image bar = CreateImage(
                $"Floor_{floorNumber:00}",
                new Vector2(TrackX, y),
                new Vector2(
                    floorBarWidth,
                    Mathf.Max(1f, floorHeight - floorGap)
                ),
                inactiveFloorColor
            );

            floorImages[i] = bar;

            // ----- Floor Number -----

            TextMeshProUGUI label =
                CreateText(
                    $"FloorLabel_{floorNumber:00}",
                    new Vector2(0f, y),
                    new Vector2(37f, floorHeight - floorGap)
                );

            label.text = $"{floorNumber:00}F";

            label.fontSize = 14f;

            label.alignment =
                TextAlignmentOptions.MidlineRight;

            label.color = labelColor;

            floorLabels[i] = label;
        }

        // ======================================
        // ----- Player Marker -----
        // ======================================

        Image marker = CreateImage(
            "PlayerHeightMarker",
            new Vector2(TrackX - 10f, TopPadding),
            new Vector2(
                floorBarWidth + 31f,
                4f
            ),
            playerMarkerColor
        );

        playerMarker =
            marker.rectTransform;

        // Y座標がマーカーの中心になるようにする
        playerMarker.pivot =
            new Vector2(0f, 0.5f);

        // 最前面に表示
        playerMarker.SetAsLastSibling();

        playerMarker.gameObject.SetActive(false);
    }

    // ==========================================
    // ----- Detect Loaded Levels -----
    // ==========================================

    private void RefreshLevelInfos()
    {
        levelInfos.Clear();

        // 現在ロードされているSceneを調べる
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene =
                SceneManager.GetSceneAt(i);

            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            // Scene_Level_05
            // Test_Scene_Level_05
            // どちらにも対応
            if (!scene.name.Contains("Level_"))
                continue;

            int underscore =
                scene.name.LastIndexOf('_');

            if (underscore < 0)
                continue;

            string numberText =
                scene.name.Substring(underscore + 1);

            if (!int.TryParse(
                    numberText,
                    out int floorNumber))
            {
                continue;
            }

            if (floorNumber < lowestFloor ||
                floorNumber > highestFloor)
            {
                continue;
            }

            // Scene内のLevelSceneInfoを探す
            foreach (GameObject root
                     in scene.GetRootGameObjects())
            {
                LevelSceneInfo info =
                    root.GetComponentInChildren<LevelSceneInfo>(
                        true
                    );

                if (info == null || !info.IsValid())
                    continue;

                levelInfos[floorNumber] = info;

                break;
            }
        }
    }

    // ==========================================
    // ----- Player Height -----
    // ==========================================

    private bool TryGetPlayerHeight(
        out int currentFloor,
        out float progress)
    {
        currentFloor = -1;
        progress = 0f;

        if (player == null ||
            levelInfos.Count == 0)
        {
            return false;
        }

        float playerY = player.position.y;

        // プレイヤーが最も近い階層を探す
        float nearestDistance =
            float.PositiveInfinity;

        LevelSceneInfo nearestInfo = null;

        foreach (KeyValuePair<int, LevelSceneInfo> entry
                 in levelInfos)
        {
            LevelSceneInfo info =
                entry.Value;

            if (info == null || !info.IsValid())
                continue;

            float topY =
                info.TopPoint.position.y;

            float bottomY =
                info.BottomPoint.position.y;

            float minY =
                Mathf.Min(topY, bottomY);

            float maxY =
                Mathf.Max(topY, bottomY);

            // 階層の高さ範囲までの距離
            float distance = 0f;

            if (playerY > maxY)
            {
                distance = playerY - maxY;
            }
            else if (playerY < minY)
            {
                distance = minY - playerY;
            }

            if (distance < nearestDistance)
            {
                nearestDistance = distance;

                currentFloor = entry.Key;

                nearestInfo = info;
            }
        }

        if (nearestInfo == null)
            return false;

        float currentTop =
            nearestInfo.TopPoint.position.y;

        float currentBottom =
            nearestInfo.BottomPoint.position.y;

        // TopPoint = 0%
        // BottomPoint = 100%
        progress = Mathf.InverseLerp(
            currentTop,
            currentBottom,
            playerY
        );

        return true;
    }

    // ==========================================
    // ----- Update Marker -----
    // ==========================================

    private void UpdatePlayerMarker(
        int currentFloor,
        float progress)
    {
        int index =
            highestFloor - currentFloor;

        progress =
            Mathf.Clamp01(progress);

        // 現在階層の上端からの距離
        float y =
            TopPadding +
            (index + progress) * floorHeight;

        // Unity UIの下方向はマイナスY
        playerMarker.anchoredPosition =
            new Vector2(
                TrackX - 10f,
                -y
            );

        playerMarker.gameObject.SetActive(true);

        UpdateFloorColors(index);
    }

    // ==========================================
    // ----- Floor Highlight -----
    // ==========================================

    private void UpdateFloorColors(
        int activeIndex)
    {
        for (int i = 0; i < floorCount; i++)
        {
            bool active =
                i == activeIndex;

            if (floorImages[i] != null)
            {
                floorImages[i].color =
                    active
                        ? activeFloorColor
                        : inactiveFloorColor;
            }

            if (floorLabels[i] != null)
            {
                floorLabels[i].color =
                    active
                        ? playerMarkerColor
                        : labelColor;
            }
        }
    }

    // ==========================================
    // ----- Player Search -----
    // ==========================================

    private void FindPlayer()
    {
        // 実際のPlayer_Movementを優先
        Player_Movement movement =
            FindFirstObjectByType<Player_Movement>();

        if (movement != null)
        {
            player = movement.transform;
            return;
        }

        // テストシーンのPlayerにも対応
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    // ==========================================
    // ----- UI Image Helper -----
    // ==========================================

    private Image CreateImage(
        string objectName,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(
            transform,
            false
        );

        Image image =
            obj.GetComponent<Image>();

        image.color = color;
        image.raycastTarget = false;

        SetupRect(
            image.rectTransform,
            position,
            size
        );

        return image;
    }

    // ==========================================
    // ----- UI Text Helper -----
    // ==========================================

    private TextMeshProUGUI CreateText(
        string objectName,
        Vector2 position,
        Vector2 size)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        obj.transform.SetParent(
            transform,
            false
        );

        TextMeshProUGUI text =
            obj.GetComponent<TextMeshProUGUI>();

        text.text = "";
        text.raycastTarget = false;

        text.enableAutoSizing = false;

        SetupRect(
            text.rectTransform,
            position,
            size
        );

        return text;
    }

    // ==========================================
    // ----- RectTransform Helper -----
    // ==========================================

    private void SetupRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 1f);

        // position.yは上からの距離
        rect.anchoredPosition =
            new Vector2(
                position.x,
                -position.y
            );

        rect.sizeDelta = size;

        rect.localScale = Vector3.one;
    }
}
