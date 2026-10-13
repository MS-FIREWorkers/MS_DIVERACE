//using UnityEngine;

//[CreateAssetMenu(fileName = "ContinuousHapticSettings", menuName = "DualSense Haptics/Continuous")]
//public class ContinuousHapticSettings : ScriptableObject
//{
//    [Header("Amplitude")]
//    [Range(0f, 1f)] public float MinAmplitude = 0.05f;
//    [Range(0f, 1f)] public float MaxAmplitude = 0.7f;

//    [Header("Wave")]
//    [Min(1f)] public float FrequencyHz = 50f;
//    public HapticWaveform Waveform = HapticWaveform.Noise;
//    [Min(0f)] public float NoiseGain = 2.5f;
//}

//[CreateAssetMenu(fileName = "DirectionalHapticSettings", menuName = "DualSense Haptics/Directional")]
//public class DirectionalHapticSettings : ScriptableObject
//{
//    [Header("Amplitude")]
//    [Range(0f, 1f)] public float MinAmplitude = 0.15f;
//    [Range(0f, 1f)] public float MaxAmplitude = 0.6f;

//    [Header("Wave")]
//    [Min(1f)] public float FrequencyHz = 50f;
//    [Range(0f, 1f)] public float SecondaryWeight = 0.35f;
//}

//[CreateAssetMenu(fileName = "RepeatingPulseHapticSettings", menuName = "DualSense Haptics/Repeating Pulse")]
//public class RepeatingPulseHapticSettings : ScriptableObject
//{
//    [Range(0f, 1f)] public float Amplitude = 0.15f;
//    [Min(1f)] public float FrequencyHz = 50f;
//    [Min(0.01f)] public float PulseDuration = 0.08f;
//    [Min(0.01f)] public float Interval = 0.35f;
//}

//[CreateAssetMenu(fileName = "PulseHapticSettings", menuName = "DualSense Haptics/Pulse")]
//public class PulseHapticSettings : ScriptableObject
//{
//    [Range(0f, 1f)] public float Amplitude = 0.25f;
//    [Min(1f)] public float FrequencyHz = 50f;
//    [Min(0.01f)] public float Duration = 0.12f;
//}

//[CreateAssetMenu(fileName = "PassingHapticSettings", menuName = "DualSense Haptics/Passing")]
//public class PassingHapticSettings : ScriptableObject
//{
//    [Range(0f, 1f)] public float Amplitude = 0.25f;
//    [Min(1f)] public float FrequencyHz = 50f;
//    [Min(0.01f)] public float Duration = 0.6f;
//    [Range(0f, 1f)] public float SecondaryWeight = 0.35f;
//}