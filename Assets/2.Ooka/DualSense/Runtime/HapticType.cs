/*
    HapticType.cs
    DIVE RACE の振動タイプを定義する
*/

/// <summary>
/// 振動の種類を定義する。
/// </summary>
public enum HapticType
{
    // ==================================================
    // ----- Haptic Type -----
    // ==================================================
    Falling,            // 落下
    HorizontalMovement, // 水平移動
    ObstacleApproach,   // 障害物への接近
    ObstacleCollision,  // 障害物との衝突
    ObstaclePassing     // 動的障害物の通過
}
