
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class HUDSpeedTest : MonoBehaviour
{
    // ==========================================
    // ----- HUD Position -----
    // ==========================================

    [Header("HUD配置")]

    [InspectorName("Canvas直下に自動配置")]
    [Tooltip("画面サイズに追従するため、HUDをCanvas直下へ移動する")]
    [SerializeField]
    private bool attachToCanvas = true;

    [InspectorName("画面左下からの余白")]
    [SerializeField]
    private Vector2 screenMargin =
        new Vector2(30f, 30f);

    [InspectorName("HUD全体のサイズ")]
    [SerializeField]
    private Vector2 hudSize =
        new Vector2(340f, 110f);

    // ==========================================
    // ----- Speed Text -----
    // ==========================================

    [Header("速度テキスト")]

    [InspectorName("速度表示テキスト（空欄なら自動生成）")]
    [SerializeField]
    private TMP_Text speedText;

    [InspectorName("速度テキストの位置")]
    [SerializeField]
    private Vector2 speedTextPosition =
        new Vector2(0f, 24f);

    [InspectorName("速度テキストのサイズ")]
    [SerializeField]
    private Vector2 speedTextSize =
        new Vector2(205f, 68f);

    [InspectorName("速度の文字サイズ")]
    [SerializeField, Min(1f)]
    private float speedFontSize = 40f;

    // ==========================================
    // ----- Speed Bars -----
    // ==========================================

    [Header("台形速度バー")]

    [InspectorName("速度バーの位置")]
    [Tooltip("HUD左下からの相対位置")]
    [SerializeField]
    private Vector2 barPosition =
        new Vector2(210f, 10f);

    [InspectorName("一番上のバーの幅")]
    [SerializeField, Min(10f)]
    private float firstBarWidth = 56f;

    [InspectorName("バーの幅の増加量")]
    [SerializeField, Min(0f)]
    private float barWidthStep = 14f;

    [InspectorName("バーの高さ")]
    [SerializeField, Min(4f)]
    private float barHeight = 12f;

    [InspectorName("バーの間隔")]
    [SerializeField, Min(0f)]
    private float barSpacing = 7f;

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

    [Header("色設定")]

    [InspectorName("消灯色")]
    [SerializeField]
    private Color inactiveColor =
        new Color(0.10f, 0.25f, 0.32f, 0.45f);

    [InspectorName("点灯色")]
    [SerializeField]
    private Color activeColor =
        new Color(0.12f, 0.85f, 1f, 1f);

    [InspectorName("通常文字色")]
    [SerializeField]
    private Color normalTextColor =
        new Color(0.95f, 0.99f, 1f, 1f);

    [InspectorName("上昇時の文字色")]
    [SerializeField]
    private Color reboundTextColor =
        new Color(1f, 0.7f, 0.4f, 1f);

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
    [SerializeField, Min(0f)]
    private float specialSpeedKmh = 300f;

    [InspectorName("虹色の流れる速度")]
    [SerializeField, Min(0f)]
    private float rainbowSpeed = 0.18f;

    [Header("アニメーション")]

    [InspectorName("バーの点灯速度")]
    [SerializeField, Min(0.1f)]
    private float colorChangeSpeed = 12f;

    // ==========================================
    // ----- Runtime Data -----
    // ==========================================

    private const int BarCount = 5;
    private const int TextureWidth = 128;
    private const int TextureHeight = 24;
    private const int RainbowFrames = 24;

    private readonly Image[] bars =
        new Image[BarCount];

    private readonly float[] brightness =
        new float[BarCount];

    private readonly List<Texture2D> runtimeTextures =
        new List<Texture2D>();

    private RectTransform barRoot;

    private Sprite normalSprite;
    private Sprite[] rainbowSprites;

    private Player_Movement playerMovement;

    private bool externalSpecialMode;
    private float nextSearchTime;

    // ==========================================
    // ----- Unity Events -----
    // ==========================================

    private void Awake()
    {
        // 左下固定
        SetupHUDPosition();

        // 旧バージョンの装飾を非表示
        DisableOldHUDObjects();

        // 台形Spriteを生成
        GenerateSprites();

        // 速度テキストを生成・配置
        SetupSpeedText();

        // 台形バーを5本生成
        GenerateBars();

        FindPlayer();
    }

    private void Update()
    {
        // ----- Find Player -----

        if (playerMovement == null &&
            Time.unscaledTime >= nextSearchTime)
        {
            FindPlayer();
            nextSearchTime = Time.unscaledTime + 1f;
        }

        // ----- Vertical Velocity -----

        float velocityY = playerMovement != null
            ? playerMovement.CurrentVelocity.y
            : 0f;

        // 落下はプラス、上昇はマイナス
        float speedKmh = -velocityY * 3.6f;

        if (Mathf.Abs(speedKmh) < 0.05f)
        {
            speedKmh = 0f;
        }

        // ----- Special Mode -----

        bool boosting =
            specialWhileBoosting &&
            playerMovement != null &&
            playerMovement.IsBoosting;

        bool highSpeed =
            specialAtHighSpeed &&
            speedKmh >= specialSpeedKmh;

        bool specialMode =
            debugSpecialMode ||
            externalSpecialMode ||
            boosting ||
            highSpeed;

        // ----- Update HUD -----

        UpdateSpeedText(speedKmh, velocityY);
        UpdateBars(speedKmh, specialMode);
    }

    // ==========================================
    // ----- Position: Bottom Left -----
    // ==========================================

    private void SetupHUDPosition()
    {
        Canvas canvas =
            GetComponentInParent<Canvas>();

        // Canvas直下に配置すると、
        // 親UIのサイズや回転の影響を避けられる
        if (attachToCanvas && canvas != null)
        {
            Transform canvasRoot =
                canvas.rootCanvas.transform;

            if (transform.parent != canvasRoot)
            {
                transform.SetParent(canvasRoot, false);
            }
        }

        RectTransform rect =
            GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;

        rect.sizeDelta = new Vector2(
            Mathf.Max(1f, hudSize.x),
            Mathf.Max(1f, hudSize.y)
        );

        rect.anchoredPosition = new Vector2(
            Mathf.Abs(screenMargin.x),
            Mathf.Abs(screenMargin.y)
        );

        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    // ==========================================
    // ----- Remove Old Appearance -----
    // ==========================================

    private void DisableOldHUDObjects()
    {
        // 以前の背景やラベルを非表示
        string[] oldNames =
        {
            "Tech_Backplate",
            "Tech_AccentLine",
            "Tech_BottomLine",
            "Tech_Header",
            "Tech_Unit",
            "Tech_Status",
            "SpeedBarRoot"
        };

        foreach (string objectName in oldNames)
        {
            Transform child =
                transform.Find(objectName);

            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    // ==========================================
    // ----- Setup Speed Text -----
    // ==========================================

    private void SetupSpeedText()
    {
        // UI用TextMeshPro以外なら自動生成する
        if (!(speedText is TextMeshProUGUI))
        {
            speedText = null;
        }

        // 既存のSpeedTextを探す
        if (speedText == null)
        {
            Transform existing =
                transform.Find("SpeedText");

            if (existing != null)
            {
                speedText =
                    existing.GetComponent<TextMeshProUGUI>();
            }
        }

        // なければ生成
        if (speedText == null)
        {
            GameObject textObject =
                new GameObject(
                    "SpeedText",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI)
                );

            textObject.transform.SetParent(
                transform,
                false
            );

            speedText =
                textObject.GetComponent<TextMeshProUGUI>();
        }

        // 親を統一する
        if (speedText.transform.parent != transform)
        {
            speedText.transform.SetParent(
                transform,
                false
            );
        }

        speedText.gameObject.SetActive(true);

        RectTransform rect =
            speedText.rectTransform;

        // HUD左下からの相対位置
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;

        rect.anchoredPosition =
            speedTextPosition;

        rect.sizeDelta =
            speedTextSize;

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;

        // ----- Text Settings -----

        if (speedText.font == null)
        {
            speedText.font =
                TMP_Settings.defaultFontAsset;
        }

        speedText.fontSize =
            speedFontSize;

        speedText.fontStyle =
            FontStyles.Bold;

        speedText.alignment =
            TextAlignmentOptions.Left;

        speedText.enableAutoSizing =
            false;

        speedText.enableWordWrapping =
            false;

        speedText.richText =
            true;

        speedText.overflowMode =
            TextOverflowModes.Overflow;

        speedText.raycastTarget =
            false;

        speedText.color =
            normalTextColor;
    }

    // ==========================================
    // ----- Generate Five Bars -----
    // ==========================================

    private void GenerateBars()
    {
        // 新しい専用Rootを使う
        // 古いImageやGraphicとの競合を防ぐ
        Transform existingRoot =
            transform.Find("SpeedBarRoot_Minimal");

        if (existingRoot != null)
        {
            barRoot =
                existingRoot.GetComponent<RectTransform>();
        }

        if (barRoot == null)
        {
            GameObject rootObject =
                new GameObject(
                    "SpeedBarRoot_Minimal",
                    typeof(RectTransform)
                );

            rootObject.transform.SetParent(
                transform,
                false
            );

            barRoot =
                rootObject.GetComponent<RectTransform>();
        }

        float maxWidth =
            firstBarWidth +
            barWidthStep * (BarCount - 1);

        float totalHeight =
            barHeight * BarCount +
            barSpacing * (BarCount - 1);

        // ----- Root Position -----

        barRoot.anchorMin = Vector2.zero;
        barRoot.anchorMax = Vector2.zero;
        barRoot.pivot = Vector2.zero;

        barRoot.anchoredPosition =
            barPosition;

        barRoot.sizeDelta = new Vector2(
            maxWidth,
            totalHeight
        );

        barRoot.localScale = Vector3.one;
        barRoot.localRotation = Quaternion.identity;

        // ----- Generate Bars -----

        for (int i = 0; i < BarCount; i++)
        {
            string objectName =
                $"TechSpeedBar_{i + 1:00}";

            Transform existing =
                barRoot.Find(objectName);

            GameObject barObject;

            if (existing != null)
            {
                barObject = existing.gameObject;
            }
            else
            {
                barObject =
                    new GameObject(
                        objectName,
                        typeof(RectTransform),
                        typeof(Image)
                    );

                barObject.transform.SetParent(
                    barRoot,
                    false
                );
            }

            Image image =
                barObject.GetComponent<Image>();

            if (image == null)
            {
                image =
                    barObject.AddComponent<Image>();
            }

            RectTransform rect =
                barObject.GetComponent<RectTransform>();

            float width =
                firstBarWidth + barWidthStep * i;

            // 上から下へ並べる
            rect.anchorMin =
                new Vector2(0f, 1f);

            rect.anchorMax =
                new Vector2(0f, 1f);

            rect.pivot =
                new Vector2(0f, 1f);

            rect.anchoredPosition =
                new Vector2(
                    0f,
                    -i * (barHeight + barSpacing)
                );

            rect.sizeDelta =
                new Vector2(width, barHeight);

            rect.localScale =
                Vector3.one;

            // ----- Image -----

            image.sprite = normalSprite;

            image.type =
                Image.Type.Simple;

            image.preserveAspect =
                false;

            image.raycastTarget =
                false;

            image.color =
                inactiveColor;

            barObject.SetActive(true);

            bars[i] = image;
        }
    }

    // ==========================================
    // ----- Update Speed Text -----
    // ==========================================

    private void UpdateSpeedText(
        float speedKmh,
        float velocityY)
    {
        if (speedText == null)
            return;

        if (playerMovement == null)
        {
            speedText.text =
                "---.-<size=16> km/h</size>";
        }
        else
        {
            // 数字と単位のサイズを変える
            speedText.text =
                $"{speedKmh:000.0}" +
                "<size=16><color=#75DDEE> km/h</color></size>";
        }

        // 上昇時は文字をオレンジ色にする
        speedText.color =
            velocityY > 0.05f
                ? reboundTextColor
                : normalTextColor;
    }

    // ==========================================
    // ----- Update Speed Bars -----
    // ==========================================

    private void UpdateBars(
        float speedKmh,
        bool specialMode)
    {
        float fallSpeed =
            Mathf.Max(0f, speedKmh);

        float t =
            1f - Mathf.Exp(
                -colorChangeSpeed * Time.deltaTime
            );

        for (int i = 0; i < BarCount; i++)
        {
            Image bar = bars[i];

            if (bar == null)
                continue;

            bool active =
                fallSpeed >= GetThreshold(i);

            float target =
                specialMode || active ? 1f : 0f;

            brightness[i] =
                Mathf.Lerp(
                    brightness[i],
                    target,
                    t
                );

            // ==================================
            // ----- Rainbow Mode -----
            // ==================================

            if (specialMode)
            {
                float phase =
                    Mathf.Repeat(
                        Time.unscaledTime * rainbowSpeed +
                        i * 0.12f,
                        1f
                    );

                int frame =
                    Mathf.FloorToInt(
                        phase * RainbowFrames
                    );

                frame = Mathf.Clamp(
                    frame,
                    0,
                    RainbowFrames - 1
                );

                bar.sprite =
                    rainbowSprites[frame];

                bar.color =
                    Color.Lerp(
                        bar.color,
                        Color.white,
                        t
                    );
            }
            else
            {
                // 通常モードへ戻す
                if (bar.sprite != normalSprite)
                {
                    bar.sprite =
                        normalSprite;
                }

                Color targetColor =
                    Color.Lerp(
                        inactiveColor,
                        activeColor,
                        brightness[i]
                    );

                bar.color =
                    Color.Lerp(
                        bar.color,
                        targetColor,
                        t
                    );
            }
        }
    }

    // ==========================================
    // ----- Generate Trapezoid Sprites -----
    // ==========================================

    private void GenerateSprites()
    {
        // 通常時用の台形
        normalSprite =
            CreateTrapezoidSprite(false, 0f);

        // 虹色のアニメーション用画像を
        // 起動時にまとめて生成
        rainbowSprites =
            new Sprite[RainbowFrames];

        for (int i = 0; i < RainbowFrames; i++)
        {
            float phase =
                (float)i / RainbowFrames;

            rainbowSprites[i] =
                CreateTrapezoidSprite(true, phase);
        }
    }

    private Sprite CreateTrapezoidSprite(
        bool rainbow,
        float phase)
    {
        Texture2D texture =
            new Texture2D(
                TextureWidth,
                TextureHeight,
                TextureFormat.RGBA32,
                false
            );

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.filterMode =
            FilterMode.Bilinear;

        Color32[] pixels =
            new Color32[TextureWidth * TextureHeight];

        for (int y = 0; y < TextureHeight; y++)
        {
            float py = y + 0.5f;

            float vertical =
                py / TextureHeight;

            // 台形の斜め部分
            float slant = 10f;

            float leftEdge =
                slant * vertical;

            float rightEdge =
                TextureWidth -
                slant * (1f - vertical);

            for (int x = 0; x < TextureWidth; x++)
            {
                float px = x + 0.5f;

                // 各辺までの距離
                float distance = Mathf.Min(
                    px - leftEdge,
                    rightEdge - px
                );

                distance = Mathf.Min(
                    distance,
                    py
                );

                distance = Mathf.Min(
                    distance,
                    TextureHeight - py
                );

                // 輪郭を少し滑らかにする
                float alpha =
                    Mathf.Clamp01(distance + 0.5f);

                Color color;

                // ----- Rainbow Gradient -----

                if (rainbow)
                {
                    float hue =
                        Mathf.Repeat(
                            phase +
                            (float)x / TextureWidth * 0.82f,
                            1f
                        );

                    color = Color.HSVToRGB(
                        hue,
                        0.78f,
                        1f
                    );
                }
                else
                {
                    // 通常時は白ベース
                    // Image.colorで色を調整する
                    float intensity =
                        Mathf.Lerp(
                            0.62f,
                            0.95f,
                            vertical
                        );

                    color = new Color(
                        intensity,
                        intensity,
                        intensity,
                        1f
                    );
                }

                // ----- Tech Border -----

                if (distance < 1.8f)
                {
                    // 斜めの縁に明るいライン
                    color = Color.Lerp(
                        color,
                        Color.white,
                        rainbow ? 0.35f : 0.7f
                    );
                }

                // ----- Highlight -----

                if (distance >= 1.8f &&
                    vertical > 0.72f)
                {
                    color = Color.Lerp(
                        color,
                        Color.white,
                        0.10f
                    );
                }

                color.a = alpha;

                pixels[
                    y * TextureWidth + x
                ] = (Color32)color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        runtimeTextures.Add(texture);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                TextureWidth,
                TextureHeight
            ),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return sprite;
    }

    // ==========================================
    // ----- Speed Threshold -----
    // ==========================================

    private float GetThreshold(int index)
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
    // ----- Player -----
    // ==========================================

    private void FindPlayer()
    {
        playerMovement =
            FindFirstObjectByType<Player_Movement>();
    }

    // ==========================================
    // ----- External Special Mode -----
    // ==========================================

    public void SetSpecialMode(bool enabled)
    {
        externalSpecialMode = enabled;
    }

    // ==========================================
    // ----- Cleanup -----
    // ==========================================

    private void OnDestroy()
    {
        if (normalSprite != null)
        {
            Destroy(normalSprite);
        }

        if (rainbowSprites != null)
        {
            foreach (Sprite sprite in rainbowSprites)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }
        }

        foreach (Texture2D texture in runtimeTextures)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        runtimeTextures.Clear();
    }
}
