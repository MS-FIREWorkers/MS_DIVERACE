/*
    WindowEffect_Projection
    20261117  hanaue sho
    透視投影を計算するクラス
 */
using UnityEngine;

public class WindEffect_Projection
{
    // ==================================================
    // ----- Fields -----
    // ==================================================
    private Vector2 _vanishingPoint;
    private float _focalLength;

    // ==================================================
    // ----- Constructor -----
    // ==================================================
    public WindEffect_Projection(Vector2 vanishingPoint, float focalLength)
    {
        _vanishingPoint = vanishingPoint;
        _focalLength = focalLength;
    }

    // ==================================================
    // ----- Projection -----
    // ==================================================
    // 仮想3D座標をUIのローカル座標に変換
    public Vector2 Project(Vector3 position)
    {
        float scale = GetScale(position.z);

        Vector2 screenPosition = new Vector2(position.x, position.y) * scale;

        return _vanishingPoint + screenPosition;
    }
    // 奥行きに応じた拡大率
    public float GetScale(float depth)
    {
        return _focalLength / Mathf.Max(depth, 0.001f);
    }

    // ==================================================
    // ----- Settings -----
    // ==================================================
    public void SetVanishingPoint(Vector2 point)
    {
        _vanishingPoint = point;
    }
    public void SetFocalLength(float focalLength)
    {
        _focalLength = focalLength;
    }
}
