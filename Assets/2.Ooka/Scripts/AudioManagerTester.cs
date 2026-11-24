using UnityEngine;
using UnityEngine.InputSystem;

// ==================================================
// ----- Audio Manager Tester -----
// ==================================================

// AudioManagerの動作確認用スクリプト
// 動作確認が完了したら削除してよい
//
// 1キー：BGM Aをクロスフェードで再生
// 2キー：BGM Bをクロスフェードで再生
// 3キー：BGM Bをフェードアウト完了後にフェードインで再生
// 4キー：BGMをフェードアウトして停止
// 5キー：SEを2Dで再生
// 6キー：SEをこのオブジェクトの位置から3Dで再生
// 7キー：Time.timeScaleを0 / 1で切り替え（ポーズ中もフェードが進むか確認する）
// 8キー：AudioIdで指定したBGMをクロスフェードで再生
// 9キー：AudioIdで指定したSEを2Dで再生
// 0キー：AudioIdで指定したSEをこのオブジェクトの位置から3Dで再生

public class AudioManagerTester : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("テスト用AudioData")]
    [Tooltip("1キーで再生するBGM")]
    [SerializeField] private AudioData _bgmA;

    [Tooltip("2キー・3キーで再生するBGM")]
    [SerializeField] private AudioData _bgmB;

    [Tooltip("5キー・6キーで再生するSE")]
    [SerializeField] private AudioData _se;

    // [Header("テスト用AudioId")]
    // [Tooltip("8キーで再生するBGMのAudioId")]
    // [SerializeField] private AudioId _bgmId = AudioId.BgmTest;

    // [Tooltip("9キー・0キーで再生するSEのAudioId")]
    // [SerializeField] private AudioId _seId = AudioId.PlayerJump;

    [Header("フェード時間（秒）")]
    [Tooltip("BGMのフェードイン・フェードアウトに使用する時間")]
    [SerializeField] private float _fadeTime = 2f;


    // ==================================================
    // ----- Unity Standard Methods -----
    // ==================================================

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || AudioManager.Instance == null)
        {
            return;
        }

        // BGM Aをクロスフェードで再生
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            AudioManager.Instance.PlayBGM(_bgmA, _fadeTime, _fadeTime);
        }

        // BGM Bをクロスフェードで再生
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            AudioManager.Instance.PlayBGM(_bgmB, _fadeTime, _fadeTime);
        }

        // BGM Bをフェードアウト完了後にフェードインで再生
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            AudioManager.Instance.PlayBGM(_bgmB, _fadeTime, _fadeTime, waitFadeOut: true);
        }

        // BGMをフェードアウトして停止
        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            AudioManager.Instance.StopBGM(_fadeTime);
        }

        // SEを2Dで再生
        if (keyboard.digit5Key.wasPressedThisFrame)
        {
            AudioManager.Instance.PlaySE(_se);
        }

        // SEをこのオブジェクトの位置から3Dで再生
        if (keyboard.digit6Key.wasPressedThisFrame)
        {
            AudioManager.Instance.PlaySEAt(_se, transform.position);
        }

        // Time.timeScaleを0 / 1で切り替え
        if (keyboard.digit7Key.wasPressedThisFrame)
        {
            Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
        }

        // AudioIdで指定したBGMをクロスフェードで再生
        if (keyboard.digit8Key.wasPressedThisFrame)
        {
            // AudioManager.Instance.PlayBGM(_bgmId, _fadeTime, _fadeTime);
        }

        // AudioIdで指定したSEを2Dで再生
        if (keyboard.digit9Key.wasPressedThisFrame)
        {
            // AudioManager.Instance.PlaySE(_seId);
        }

        // AudioIdで指定したSEをこのオブジェクトの位置から3Dで再生
        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            // AudioManager.Instance.PlaySEAt(_seId, transform.position);
        }
    }

    private void OnDisable()
    {
        // Time.timeScaleを0のまま終了しないように1へ戻す
        Time.timeScale = 1f;
    }
}
