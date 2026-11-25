/*
    LevelScene_Timer
    20261119  hanaue sho
    レベルシーンの時間計測
 */
using UnityEngine;

public class LevelScene_Timer : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Timer -----
    // --------------------------------------------------
    private float _elapsedTime = 0.0f;
    private bool _isRunning = false;

    public float ElapsedTime => _elapsedTime;
    public bool IsRunning => _isRunning;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        _elapsedTime += Time.deltaTime;
    }

    // --------------------------------------------------
    // ----- Public Events -----
    // --------------------------------------------------
    public void StartTimer()
    {
        _isRunning = true;
    }
    public void StopTimer()
    {
        _isRunning = false;
    }
    public void ResetTimer()
    {
        _elapsedTime = 0.0f;
        _isRunning = false;
    }
}
