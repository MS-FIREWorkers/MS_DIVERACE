/*
    ObstacleFilm_VoronoiGenerator
    20261113  hanaue sho
 */
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ObstacleFilm_VoronoiGenerator : MonoBehaviour
{
    // ==================================================
    // ----- Voronoi Parameters -----
    // ==================================================
    [Header("Texture")]
    [SerializeField, Min(1)] private int _textureSize = 256;

    [Header("Voronoi")]
    [SerializeField, Min(1)] private int _pointCount = 30;
    [SerializeField, Range(0f, 1f)] private float _minimumPointDistance = 0.05f;
    [SerializeField] private int _seed = 12345;

    [Header("Color")]
    [SerializeField] private List<Color> _colors = new()
    {
        new Color(0.45f, 0.75f, 1.0f),
        new Color(0.65f, 0.65f, 1.0f),
        new Color(0.65f, 1.0f, 1.0f)
    };
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.8f;

    // ==================================================
    // ----- Components -----
    // ==================================================
    private static Texture2D _sharedTextureA;
    private static Texture2D _sharedTextureB;
    private MaterialPropertyBlock _propertyBlock;
    private Renderer _renderer;

    // ==================================================
    // ----- Voronoi Point -----
    // ==================================================
    private struct VoronoiPoint
    {
        public Vector2 Position;
        public Color Color;
    }


    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();

        Generate();
    }
    private void OnDestroy()
    {
        // 共有テクスチャは破棄しない
    }

    // ==================================================
    // ----- Generate -----
    // ==================================================
    [ContextMenu("Generate Voronoi")]
    public void Generate()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<Renderer>();
        }
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        

        if (_sharedTextureA == null || _sharedTextureB == null)
        {
            if (_colors == null || _colors.Count == 0)
            {
                Debug.LogWarning("[Warining]Voronoi Colors is empty.");
                return;
            }

            ReleaseCache();

            _sharedTextureA = GenerateTexture(_seed);
            _sharedTextureB = GenerateTexture(unchecked(_seed + 1));
        }


        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetTexture("_VoronoiTexA", _sharedTextureA);
        _propertyBlock.SetTexture("_VoronoiTexB", _sharedTextureB);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
    private Texture2D GenerateTexture(int seed)
    {
        int textureSize = Mathf.Max(1, _textureSize);
        int pointCount = Mathf.Max(1, _pointCount);

        System.Random random = new System.Random(seed);

        List<VoronoiPoint> points = GeneratePoints(random, pointCount);

        Color[] pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / textureSize, (y + 0.5f) / textureSize);

                float nearestDistance = float.MaxValue;
                Color nearestColor = Color.clear;

                foreach (VoronoiPoint point in points)
                {
                    float distance = GetWrappedDistanceSquared(uv, point.Position);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestColor = point.Color;
                    }
                }

                pixels[y * textureSize + x] = nearestColor;
            }
        }

        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    // ==================================================
    // ----- Generate Points -----
    // ==================================================
    private List<VoronoiPoint> GeneratePoints(System.Random random, int pointCount)
    {
        List<VoronoiPoint> points = new();

        const int maxAttemptsPerPoint = 100;

        for (int i = 0; i < pointCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttemptsPerPoint; attempt++)
            {
                Vector2 position = new Vector2((float)random.NextDouble(), (float)random.NextDouble());

                bool valid = true;

                foreach (VoronoiPoint point in points)
                {
                    float distance = GetWrappedDistanceSquared(position, point.Position);

                    if (distance < _minimumPointDistance * _minimumPointDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    continue;
                }

                Color color = _colors[random.Next(_colors.Count)];

                color.a = Mathf.Lerp( _minAlpha, _maxAlpha, (float)random.NextDouble());

                points.Add(new VoronoiPoint{ Position = position, Color = color });

                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning("[Warning] Voronoi: Could only place {" + points + ".Count} / { " + pointCount + "} points." );
                break;
            }
        }

        return points;
    }

    // ==================================================
    // ----- Wrapped Distance -----
    // ==================================================
    private float GetWrappedDistanceSquared(Vector2 a, Vector2 b)
    {
        float dx = Mathf.Abs(a.x - b.x);
        float dy = Mathf.Abs(a.y - b.y);

        // 上下左右がつながっている空間として扱う
        dx = Mathf.Min(dx, 1f - dx);
        dy = Mathf.Min(dy, 1f - dy);

        return dx * dx + dy * dy;
    }

    // ==================================================
    // ----- Release Cache -----
    // ==================================================
    public static void ReleaseCache()
    {
        if (_sharedTextureA != null)
        {
            if (Application.isPlaying) Destroy(_sharedTextureA);
            else DestroyImmediate(_sharedTextureA);

            _sharedTextureA = null;
        }

        if (_sharedTextureB != null)
        {
            if (Application.isPlaying) Destroy(_sharedTextureB);
            else DestroyImmediate(_sharedTextureB);

            _sharedTextureB = null;
        }
    }
}