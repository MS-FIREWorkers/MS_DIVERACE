
// ==================================================
// File: PostEffectManager.Common.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: ポストエフェクトの共通停止処理と実行状態取得API
// ==================================================

// ==================================================
// PostEffectManager
// ==================================================

public partial class PostEffectManager
{
    // ==================================================
    // Static Public API
    // ==================================================

    // ----- Stop All Effects -----

    public static void StopAllEffects()
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager._runtimeController?.StopAll();
    }

    // ----- Stop Effects By ID -----

    public static int StopEffectsById(string effectId)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return 0;

        if (manager._runtimeController == null)
            return 0;

        return manager._runtimeController.StopById(effectId);
    }

    // ----- Is Effect Playing -----

    public static bool IsEffectPlaying<T>(string effectId) where T : CustomPostEffect
    {
        if (!TryGetManager(out PostEffectManager manager))
            return false;

        if (manager._runtimeController == null)
            return false;

        return manager._runtimeController.IsPlaying<T>(effectId);
    }
}
