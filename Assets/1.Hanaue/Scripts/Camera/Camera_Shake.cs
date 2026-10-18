/*
    Camera_Shake
    20261018  hanaue sho
 */
using UnityEngine;
using System.Collections.Generic;


public enum CameraShakePlayMode
{
    Replace,
    Additive
}
public struct CameraShakeOffset
{
    public Vector3 Position;
    public Vector3 Rotation;
    public float Fov;

    public static CameraShakeOffset operator +(
        CameraShakeOffset a, CameraShakeOffset b
        )
    {
        return new CameraShakeOffset
        {
            Position = a.Position + b.Position,
            Rotation = a.Rotation + b.Rotation,
            Fov = a.Fov + b.Fov
        };
    }

}

public class Camera_Shake : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Runtime -----
    // --------------------------------------------------
    private readonly List<CameraShake_Instance> _activeShakes = new List<CameraShake_Instance>();
    public CameraShakeOffset CurrentOffset { get; private set; }

    // --------------------------------------------------
    // ----- PlayShake -----
    // --------------------------------------------------
    public void PlayShake(CameraShake_Data data, CameraShakePlayMode playMode = CameraShakePlayMode.Replace )
    {
        if (data == null)
        {
            return;
        }

        // 同じグループのシェイクを置き換える
        if (playMode == CameraShakePlayMode.Replace )
        {
            StopGroup(data.Group);
        }

        // 新しいシェイクを生成
        CameraShake_Instance instance = new CameraShake_Instance(data);

        _activeShakes.Add(instance);
    }

    // --------------------------------------------------
    // ----- UpdateShake -----
    // --------------------------------------------------
    public void UpdateShake(float deltaTime)
    {
        CurrentOffset = default;

        // 逆順で更新（終了した Instance を削除するため）
        for (int i = _activeShakes.Count - 1; i >= 0; i--)
        {
            CameraShake_Instance instance = _activeShakes[i];

            instance.Update(deltaTime);

            if (instance.IsFinished)
            {
                _activeShakes.RemoveAt(i);
                continue;
            }

            // 各シェイクの結果を加算
            CurrentOffset += instance.CurrentOffset;
        }
    }

    // --------------------------------------------------
    // ----- StopGroup -----
    // --------------------------------------------------
    public void StopGroup(CameraShakeGroup group)
    {
        for (int i = _activeShakes.Count - 1; i >= 0; i--)
        {
            if (_activeShakes[i].Group == group)
            {
                _activeShakes.RemoveAt(i);
            }
        }
    }

    // --------------------------------------------------
    // ----- Stop All -----
    // --------------------------------------------------
    public void StopAllShakes()
    {
        _activeShakes.Clear();
        CurrentOffset = default;
    }
}
