
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class HUDSpeedTest : MonoBehaviour
{
    // ==========================================
    // ----- UI -----
    // ==========================================

    [Header("速度表示")]

    [InspectorName("速度表示テキスト")]
    [SerializeField]
    private TMP_Text speedText;

    [InspectorName("バーの配置先（空欄なら自動生成）")]
    [Tooltip("未設定ならSpeedBarRootを自動生成します")]
    [SerializeField]
    private RectTransform speedBarRoot;

    // ==========================================
    // ----- Auto Generate -----
    // ==========================================

    [Header("バーの自動生成")]

    [InspectorName("一番上のバーの幅")]
    [SerializeField, Min(1f)]
    private float firstBarWidth = 36f;

    [InspectorName("下のバーへの幅の増加量")]
    [SerializeField, Min(0f)]
    private float barWidthStep = 14f;

    [InspectorName("バーの高さ")]
    [SerializeField, Min(1f)]
    private float barHeight = 9f;

    [InspectorName("バーの間隔")]
    [SerializeField, Min(0f)]
    private float barSpacing = 6f;

    // 実行時に自動作成・取得
    private const int BarCount = 5;
    private readonly Image[] speedBars = new Image[BarCount];

    // ==========================================
    // ----- Speed Thresholds -----
    // ==========================================

    [Header("点灯速度（km/h）")]

    [InspectorName("1本目")]
    [SerializeField] private float speed1 = 25f;

    [InspectorName("2本目")]
    [SerializeField] private float speed2 = 60f;

    [InspectorName("3本目")]
    [SerializeField] private float speed3 = 130f;

    [InspectorName("4本目")]
    [SerializeField] private float speed4 = 220f;

    [InspectorName("5本目")]
    [SerializeField] private float speed5 = 320f;

    // ==========================================
    // ----- Colors -----
    // ==========================================

    [Header("通常時の色")]

    [InspectorName("消灯色")]
    [SerializeField]
    private Color inactiveColor =
        new Color(0.18f, 0.27f, 0.32f, 0.5f);

    [InspectorName("点灯色")]
    [SerializeField]
    private Color activeColor =
        new Color(0.38f, 0.87f, 1f, 1f);

    [InspectorName("落下中の文字色")]
    [SerializeField]
    private Color fallingColor = Color.white;

    [InspectorName("上昇中の文字色")]
    [SerializeField]
    private Color risingColor =
        new Color(1f, 0.7f, 0.4f);

    // ==========================================
    // ----- Special Mode -----
    // ==========================================

    [Header("特殊レインボーモード")]

    [InspectorName("特殊モードをテスト")]
    [SerializeField]
    private bool debugSpecialMode = false;

    [InspectorName("ブースト中に自動切替")]
    [SerializeField]
    private bool specialWhileBoosting = false;

    [InspectorName("高速到達で自動切替")]
    [SerializeField]
    private bool specialAtHighSpeed = false;

    [InspectorName("特殊モード開始速度")]
    [SerializeField]
    private float specialSpeedKmh = 300f;

    [InspectorName("虹色の流れる速度")]
    [SerializeField, Min(0f)]
    private float rainbowSpeed = 0.2f;

    [InspectorName("色の変化速度")]
    [SerializeField, Min(0.1f)]
    private float colorChangeSpeed = 15f;

    // ==========================================
    // ----- Runtime -----
    // ==========================================

    private Player_Movement playerMovement;
    private bool externalSpecialMode;
    private float nextSearchTime;

    private void Awake()
    {
        // バーを一度だけ生成
        GenerateSpeedBars();

        // プレイヤーを自動取得
        FindPlayer();
    }

    private void Update()
    {
        if (playerMovement == null &&
            Time.unscaledTime >= nextSearchTime)
        {
            FindPlayer();
            nextSearchTime = Time.unscaledTime + 1f;
        }

        float velocityY = playerMovement != null
            ? playerMovement.CurrentVelocity.y
            : 0f;

        // 落下はプラス、上昇はマイナス
        float speedKmh = -velocityY * 3.6f;

        // ----- Speed Text -----

        if (speedText != null)
        {
            speedText.text = playerMovement == null
                ? "PLAYER NOT FOUND"
                : $"{speedKmh:F1} km/h";

            speedText.color = velocityY > 0f
                ? risingColor
                : fallingColor;
        }

        // ----- Special Mode -----

        bool boostSpecial =
            specialWhileBoosting &&
            playerMovement != null &&
            playerMovement.IsBoosting;

        bool highSpeedSpecial =
            specialAtHighSpeed &&
            speedKmh >= specialSpeedKmh;

        bool specialMode =
            debugSpecialMode ||
            externalSpecialMode ||
            boostSpecial ||
            highSpeedSpecial;

        UpdateSpeedBars(speedKmh, specialMode);
    }

    // ==========================================
    // ----- Auto Generate Speed Bars -----
    // ==========================================

    private void GenerateSpeedBars()
    {
        // 既存のSpeedBarRootを検索
        if (speedBarRoot == null)
        {
            Transform existingRoot =
                transform.Find("SpeedBarRoot");

            if (existingRoot != null)
            {
                speedBarRoot =
                    existingRoot.GetComponent<RectTransform>();
            }
        }

        // 存在しない場合は生成
        if (speedBarRoot == null)
        {
            GameObject rootObject = new GameObject(
                "SpeedBarRoot",
                typeof(RectTransform)
            );

            rootObject.transform.SetParent(
                transform,
                false
            );

            speedBarRoot =
                rootObject.GetComponent<RectTransform>();

            // 親SpeedDisplay内の左下に配置
            speedBarRoot.anchorMin = Vector2.zero;
            speedBarRoot.anchorMax = Vector2.zero;
            speedBarRoot.pivot = Vector2.zero;
            speedBarRoot.anchoredPosition =
                new Vector2(12f, 12f);
        }

        // ----- Vertical Layout Group -----

        VerticalLayoutGroup layout =
            speedBarRoot.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout =
                speedBarRoot.gameObject
                    .AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = barSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperLeft;

        layout.childControlWidth = false;
        layout.childControlHeight = false;

        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // ----- Root Size -----

        float maxWidth =
            firstBarWidth + barWidthStep * (BarCount - 1);

        float totalHeight =
            barHeight * BarCount +
            barSpacing * (BarCount - 1);

        speedBarRoot.sizeDelta =
            new Vector2(maxWidth, totalHeight);

        // ----- Create 5 Bars -----

        for (int i = 0; i < BarCount; i++)
        {
            string barName = $"Bar_{i + 1:00}";

            // 同名のバーがある場合は再利用
            Transform existing =
                speedBarRoot.Find(barName);

            GameObject barObject;

            if (existing != null)
            {
                barObject = existing.gameObject;
            }
            else
            {
                barObject = new GameObject(
                    barName,
                    typeof(RectTransform),
                    typeof(Image)
                );

                barObject.transform.SetParent(
                    speedBarRoot,
                    false
                );
            }

            RectTransform rect =
                barObject.GetComponent<RectTransform>();

            Image image =
                barObject.GetComponent<Image>();

            if (image == null)
            {
                image = barObject.AddComponent<Image>();
            }

            // 上から下へ長くなるサイズ
            float width =
                firstBarWidth + barWidthStep * i;

            rect.sizeDelta =
                new Vector2(width, barHeight);

            // UIカラーを使用
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.color = inactiveColor;

            speedBars[i] = image;
        }
    }

    // ==========================================
    // ----- Get Player -----
    // ==========================================

    private void FindPlayer()
    {
        playerMovement =
            FindFirstObjectByType<Player_Movement>();
    }

    // ==========================================
    // ----- Special Mode API -----
    // ==========================================

    public void SetSpecialMode(bool enabled)
    {
        externalSpecialMode = enabled;
    }

    // ==========================================
    // ----- Get Threshold -----
    // ==========================================

    private float GetBarThreshold(int index)
    {
        switch (index)
        {
            case 0: return speed1;
            case 1: return speed2;
            case 2: return speed3;
            case 3: return speed4;
            case 4: return speed5;
            default: return float.MaxValue;
        }
    }

    // ==========================================
    // ----- Update Bars -----
    // ==========================================

    private void UpdateSpeedBars(
        float speedKmh,
        bool specialMode)
    {
        float fallSpeed = Mathf.Max(0f, speedKmh);

        float t = 1f - Mathf.Exp(
            -colorChangeSpeed * Time.deltaTime
        );

        for (int i = 0; i < speedBars.Length; i++)
        {
            Image bar = speedBars[i];

            if (bar == null)
                continue;

            Color targetColor;

            if (specialMode)
            {
                // 全バーを虹色にする
                float hue = Mathf.Repeat(
                    (float)i / BarCount +
                    Time.unscaledTime * rainbowSpeed,
                    1f
                );

                targetColor =
                    Color.HSVToRGB(hue, 0.85f, 1f);
            }
            else
            {
                // 速度に応じて点灯
                bool active =
                    fallSpeed >= GetBarThreshold(i);

                targetColor = active
                    ? activeColor
                    : inactiveColor;
            }

            // 滑らかな色変化
            bar.color = Color.Lerp(
                bar.color,
                targetColor,
                t
            );
        }
    }
}
