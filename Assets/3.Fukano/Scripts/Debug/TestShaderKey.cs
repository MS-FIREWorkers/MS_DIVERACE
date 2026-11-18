using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TestShaderKey : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PostEffectManager.PlayFog(
            30,
            600,
            30,
            (Color.red),
            0.0f,
            100.0f,
            0.01f,
            1.0f,
            FogEffect.FogMode.Linear
        );
    }

    // Update is called once per frame
    void Update()
    {

    }
}

