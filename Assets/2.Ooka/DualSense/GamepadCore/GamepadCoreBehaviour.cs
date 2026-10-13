/*
    GamepadCoreBehaviour.cs
    GamepadCore の初期化・更新・終了処理を管理する
*/

using GamepadCore.Unity;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GamepadCoreBehaviour : MonoBehaviour
{
    // ==================================================
    // ----- Private Field -----
    // ==================================================
    private bool _isInitialized;


    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        _isInitialized = GamepadCoreApi.Initialize();

        if (!_isInitialized)
        {
            Debug.LogError("GamepadCore initialization failed: " + GamepadCoreApi.LastError, this);
        }
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        GamepadCoreApi.Update(Time.unscaledDeltaTime);
    }

    private void OnApplicationQuit()
    {
        ShutdownGamepadCore();
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            ShutdownGamepadCore();
        }
    }


    // ==================================================
    // ----- GamepadCore Management -----
    // ==================================================

    private void ShutdownGamepadCore()
    {
        if (!_isInitialized)
        {
            return;
        }

        GamepadCoreApi.Shutdown();
        _isInitialized = false;
    }
}
