/*
    CameraShake_Instance
    20261018  hanaue sho
    カメラシェイクの再生状態・波形計算
 */
using UnityEngine;

public class CameraShake_Instance
{
    // --------------------------------------------------
    // ----- Data -----
    // --------------------------------------------------
    private readonly CameraShake_Data _data;


    // --------------------------------------------------
    // ----- Runtime -----
    // --------------------------------------------------
    private float _elapsedTime;
    private readonly Vector3 _positionSeed;
    private readonly Vector3 _rotationSeed;
    private readonly float _fovSeed;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public CameraShakeGroup Group { get; }
    public bool IsFinished { get; private set; }
    public CameraShakeOffset CurrentOffset { get; private set; }

    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public CameraShake_Instance(CameraShake_Data data)
    {
        _data = data;
        Group = _data.Group;

        _elapsedTime = 0.0f;
        IsFinished = false;
        CurrentOffset = default;

        // 各成分を異なる位相で開始
        _positionSeed = CreateSeed();
        _rotationSeed = CreateSeed();
        _fovSeed = Random.Range(0.0f, 1000.0f);
    }


    // --------------------------------------------------
    // ----- Update -----
    // --------------------------------------------------
    public void Update(float deltaTime)
    {
        if (IsFinished)
        {
            return;
        }

        _elapsedTime += Mathf.Max(0.0f, deltaTime);

        float duration = Mathf.Max(0.0f, _data.TotalDuration);

        // 再生終了
        if (_elapsedTime >= duration)
        {
            IsFinished = true;
            CurrentOffset = default;
            return;
        }

        // パターンごとの計算
        switch (_data.Pattern)
        {
            case CameraShakePattern.Sine:
                CurrentOffset = CalculateOscillation(false);
                break;
            case CameraShakePattern.PerlinNoise:
                CurrentOffset = CalculateOscillation(true);
                break;
            case CameraShakePattern.FovPulse:
                CurrentOffset = CalculateFovPulse();
                break;
            default:
                CurrentOffset = default;
                break;
        }

    }

    // --------------------------------------------------
    // ----- Oscillation -----
    // --------------------------------------------------
    private CameraShakeOffset CalculateOscillation(bool useNoise)
    {
        float fade = CalculateFade();

        CameraShakeOffset offset = new CameraShakeOffset();

        // Position
        offset.Position = CalculateVectorWave(
            _data.PositionAmplitude,
            _data.PositionFrequency,
            _positionSeed,
            useNoise
            ) * fade;

        // Rotation
        offset.Rotation = CalculateVectorWave(
            _data.RotationAmplitude,
            _data.RotationFrequency,
            _rotationSeed,
            useNoise
            ) * fade;

        // FOV
        offset.Fov = CalculateWave(
            _data.FovAmplitude,
            _data.FovFrequency,
            _fovSeed,
            useNoise
            ) * fade;

        return offset;
    }

    // --------------------------------------------------
    // ----- Fade -----
    // --------------------------------------------------
    private float CalculateFade()
    {
        float duration = _data.Duration;

        float fadeIn = Mathf.Max(0.0f, _data.FadeIn);
        float fadeOut = Mathf.Max(0.0f, _data.FadeOut);

        float inRatio = fadeIn > 0.0f ? Mathf.Clamp01(_elapsedTime / fadeIn) : 1.0f;

        float remainingTime = duration - _elapsedTime;

        float outRatio = fadeOut > 0.0f ? Mathf.Clamp01(remainingTime / fadeOut) : 1.0f;

        return Mathf.Min(inRatio, outRatio);
    }

    // --------------------------------------------------
    // ----- Calculate Wave -----
    // --------------------------------------------------
    private Vector3 CalculateVectorWave(Vector3 amplitude, Vector3 frequency, Vector3 seed, bool useNoise)
    {
        return new Vector3(
            CalculateWave(amplitude.x, frequency.x, seed.x, useNoise),
            CalculateWave(amplitude.y, frequency.y, seed.y, useNoise),
            CalculateWave(amplitude.z, frequency.z, seed.z, useNoise)
            );
    }
    private float CalculateWave(float amplitude, float frequency, float seed, bool useNoise)
    {
        if (Mathf.Approximately(amplitude, 0.0f))
        {
            return 0.0f;
        }

        float t = _elapsedTime * Mathf.Max(0.0f, frequency);

        float wave;
        if (useNoise)
        {
            // ０～１のノイズをおおむね－１～１へ変換
            wave = Mathf.PerlinNoise(seed, t + seed) * 2.0f - 1.0f;
        }
        else
        {
            // seed を初期位相として使用
            wave = Mathf.Sin((t + seed) * Mathf.PI * 2.0f);
        }

        return amplitude * wave;
    }

    // --------------------------------------------------
    // ----- FOv Pulse -----
    // --------------------------------------------------
    private CameraShakeOffset CalculateFovPulse()
    {
        float attack  = Mathf.Max(0.0f, _data.FovAttackTime);
        float hold    = Mathf.Max(0.0f, _data.FovHoldTime);
        float release = Mathf.Max(0.0f, _data.FovReleaseTime);

        float t = _elapsedTime;

        float ratio;
        // Attack
        if (t < attack)
        {
            ratio = attack > 0.0f ? t / attack : 1.0f;
        }
        else if (t < attack + hold)
        {
            ratio = 1.0f;
        }
        else 
        {
            float releaseTime = t - attack - hold;

            ratio = release > 0.0f ? 1.0f - releaseTime / release : 0.0f;
        }

        return new CameraShakeOffset
        {
            Position = Vector3.zero,
            Rotation = Vector3.zero,
            Fov = _data.FovAmplitude * Mathf.Clamp01(ratio)
        };
    }

    // --------------------------------------------------
    // ----- Seed -----
    // --------------------------------------------------
    private static Vector3 CreateSeed()
    {
        return new Vector3(
                           Random.Range(0.0f, 1000.0f),
                           Random.Range(0.0f, 1000.0f),
                           Random.Range(0.0f, 1000.0f)
                          );
    }

}
