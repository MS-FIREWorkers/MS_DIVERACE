/*
    Debug_HanaueManager
    20261021  hanaue sho
    デバッグ用のマネージャー
 */
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Debug_HanaueManager : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Elapsed Time -----
    // --------------------------------------------------
    [Header("ElapsedTime")]
    [SerializeField] private float _elapsedTime = 0f;
    [SerializeField] private bool _isElapsed = false;

    // --------------------------------------------------
    // ----- PlayerCharacter -----
    // --------------------------------------------------
    [Header("PlayerCharacter")]
    [SerializeField] private Player_CharacterController _player;
    public float FallDistance { get { return _player.transform.position.y; } }

    // --------------------------------------------------
    // ----- Wrap -----
    // --------------------------------------------------
    private float _wrapTime = 0.0f; 
    private float _beforeWrap = 0.0f;
    private float _nextWrapPosition = -2000.0f;

    void Start()
    {
        
    }
    void Update()
    {
        // 時間計測
        if (_isElapsed)
        {
            _elapsedTime += Time.deltaTime;
        }

        // リセットボタン
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            // 現在のシーンを再ロード
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // スタートボタン
        if (_player != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            _isElapsed = true;
            _player.StartPlay();
        }

        // ラップ記録
        if (FallDistance <= _nextWrapPosition)
        {
            _wrapTime = _elapsedTime - _beforeWrap;
            _beforeWrap = _elapsedTime; // 記録

            // 次の記録場所を更新
            _nextWrapPosition -= 2000.0f;
        }

    }
}
