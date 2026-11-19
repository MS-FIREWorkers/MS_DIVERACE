
using UnityEngine;
using UnityEngine.UI;

// 画像素材を使わず、斜めカット・枠線・
// グラデーションを描くUI Graphic。
[AddComponentMenu("UI/HUD Tech Speed Bar Graphic")]
public class HUDTechSpeedBarGraphic : MaskableGraphic
{
    // ==========================================
    // ----- Colors -----
    // ==========================================

    private Color inactiveFill =
        new Color(0.045f, 0.105f, 0.145f, 0.94f);

    private Color activeFill =
        new Color(0.08f, 0.79f, 0.92f, 1f);

    private Color inactiveBorder =
        new Color(0.18f, 0.37f, 0.44f, 0.8f);

    // ==========================================
    // ----- Runtime -----
    // ==========================================

    private float brightness;
    private bool rainbowMode;
    private float rainbowPhase;

    // ==========================================
    // ----- Setup -----
    // ==========================================

    public void Setup(
        Color inactive,
        Color active,
        Color border)
    {
        inactiveFill = inactive;
        activeFill = active;
        inactiveBorder = border;

        raycastTarget = false;

        color = Color.white;

        SetVerticesDirty();
    }

    // ==========================================
    // ----- Update Display -----
    // ==========================================

    public void SetDisplay(
        float value,
        bool rainbow,
        float phase)
    {
        float nextBrightness =
            Mathf.Clamp01(value);

        if (
            Mathf.Abs(brightness - nextBrightness) < 0.001f &&
            rainbowMode == rainbow &&
            (
                !rainbow ||
                Mathf.Abs(rainbowPhase - phase) < 0.001f
            )
        )
        {
            return;
        }

        brightness = nextBrightness;
        rainbowMode = rainbow;
        rainbowPhase = phase;

        SetVerticesDirty();
    }

    // ==========================================
    // ----- Draw Mesh -----
    // ==========================================

    protected override void OnPopulateMesh(
        VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();

        float left = rect.xMin;
        float right = rect.xMax;
        float bottom = rect.yMin;
        float top = rect.yMax;

        float width = right - left;
        float height = top - bottom;

        if (width <= 4f || height <= 4f)
            return;

        float slant =
            Mathf.Min(7f, height * 0.45f);

        const float inset = 1.3f;

        // ======================================
        // ----- Outer Border -----
        // ======================================

        Color border = Color.Lerp(
            inactiveBorder,
            new Color(0.58f, 0.96f, 1f, 1f),
            brightness
        );

        if (rainbowMode)
        {
            border = Color.HSVToRGB(
                Mathf.Repeat(rainbowPhase, 1f),
                0.55f,
                1f
            );

            border.a = 1f;
        }

        AddTrapezoid(
            vh,
            left,
            right,
            bottom,
            top,
            slant,
            border,
            border
        );

        // ======================================
        // ----- Inner Fill -----
        // ======================================

        float innerLeftBottom =
            left + inset;

        float innerLeftTop =
            left + inset + slant;

        float innerRightTop =
            right - inset;

        float innerRightBottom =
            right - inset - slant;

        float innerBottom =
            bottom + inset;

        float innerTop =
            top - inset;

        const int slices = 12;

        // 12分割してグラデーションを作成
        for (int i = 0; i < slices; i++)
        {
            float t0 =
                (float)i / slices;

            float t1 =
                (float)(i + 1) / slices;

            Color c0 =
                GetFillColor(t0);

            Color c1 =
                GetFillColor(t1);

            AddQuad(
                vh,

                new Vector2(
                    Mathf.Lerp(
                        innerLeftBottom,
                        innerRightBottom,
                        t0
                    ),
                    innerBottom
                ),

                new Vector2(
                    Mathf.Lerp(
                        innerLeftTop,
                        innerRightTop,
                        t0
                    ),
                    innerTop
                ),

                new Vector2(
                    Mathf.Lerp(
                        innerLeftTop,
                        innerRightTop,
                        t1
                    ),
                    innerTop
                ),

                new Vector2(
                    Mathf.Lerp(
                        innerLeftBottom,
                        innerRightBottom,
                        t1
                    ),
                    innerBottom
                ),

                c0,
                c1
            );
        }

        // ======================================
        // ----- Highlight -----
        // ======================================

        Color glint = Color.Lerp(
            new Color(0.26f, 0.43f, 0.48f, 0.16f),
            new Color(0.92f, 1f, 1f, 0.70f),
            brightness
        );

        if (rainbowMode)
        {
            glint =
                new Color(1f, 1f, 1f, 0.7f);
        }

        float lineY =
            top - 2.5f;

        AddQuad(
            vh,

            new Vector2(
                left + slant + 4f,
                lineY - 0.7f
            ),

            new Vector2(
                left + slant + 4f,
                lineY
            ),

            new Vector2(
                right - 4f,
                lineY
            ),

            new Vector2(
                right - 4f,
                lineY - 0.7f
            ),

            glint,
            glint
        );
    }

    // ==========================================
    // ----- Fill Color -----
    // ==========================================

    private Color GetFillColor(float t)
    {
        // ----- Special Rainbow Mode -----

        if (rainbowMode)
        {
            // 一つのバー内でも
            // 赤→黄→緑→青→紫へ変化する
            Color c = Color.HSVToRGB(
                Mathf.Repeat(
                    rainbowPhase + t * 0.85f,
                    1f
                ),
                0.76f,
                1f
            );

            c.a = 1f;

            return c;
        }

        // ----- Normal Mode -----

        Color lit =
            activeFill *
            Mathf.Lerp(0.84f, 1.15f, t);

        lit.a = activeFill.a;

        return Color.Lerp(
            inactiveFill,
            lit,
            brightness
        );
    }

    // ==========================================
    // ----- Trapezoid -----
    // ==========================================

    private static void AddTrapezoid(
        VertexHelper vh,
        float left,
        float right,
        float bottom,
        float top,
        float slant,
        Color leftColor,
        Color rightColor)
    {
        AddQuad(
            vh,

            new Vector2(left, bottom),

            new Vector2(
                left + slant,
                top
            ),

            new Vector2(right, top),

            new Vector2(
                right - slant,
                bottom
            ),

            leftColor,
            rightColor
        );
    }

    // ==========================================
    // ----- Add Quad -----
    // ==========================================

    private static void AddQuad(
        VertexHelper vh,
        Vector2 bottomLeft,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Color leftColor,
        Color rightColor)
    {
        int start =
            vh.currentVertCount;

        UIVertex vertex =
            UIVertex.simpleVert;

        vertex.uv0 =
            Vector2.zero;

        // ----- Bottom Left -----

        vertex.position =
            bottomLeft;

        vertex.color =
            leftColor;

        vh.AddVert(vertex);

        // ----- Top Left -----

        vertex.position =
            topLeft;

        vertex.color =
            leftColor;

        vh.AddVert(vertex);

        // ----- Top Right -----

        vertex.position =
            topRight;

        vertex.color =
            rightColor;

        vh.AddVert(vertex);

        // ----- Bottom Right -----

        vertex.position =
            bottomRight;

        vertex.color =
            rightColor;

        vh.AddVert(vertex);

        // ----- Triangles -----

        vh.AddTriangle(
            start,
            start + 1,
            start + 2
        );

        vh.AddTriangle(
            start,
            start + 2,
            start + 3
        );
    }
}
