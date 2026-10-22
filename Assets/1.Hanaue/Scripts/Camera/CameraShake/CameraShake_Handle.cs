/*
    CameraShake_Handle
    20261021  hanaue sho
 */
using UnityEngine;

public class CameraShake_Handle
{
    internal CameraShake_Instance Instance { get; }

    internal CameraShake_Handle(CameraShake_Instance instance)
    {
        Instance = instance;
    }
}
