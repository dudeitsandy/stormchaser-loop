using System;

/// <summary>
/// Shared audio levels from Settings (design/ux/run-screens.md). MASTER is <c>AudioListener.volume</c>; MUSIC is applied
/// by <see cref="RadioLite"/>; EFFECTS is read by every non-music source (presentation lane) as a multiplier.
/// </summary>
public static class GameAudio
{
    private static float _effects = 1f;

    /// <summary>0..1 effects level. Presentation audio multiplies its source volumes by this.</summary>
    public static float EffectsVolume
    {
        get => _effects;
        set
        {
            float v = value < 0f ? 0f : value > 1f ? 1f : value;
            if (Math.Abs(v - _effects) < 1e-6f) return;
            _effects = v;
            EffectsVolumeChanged?.Invoke(v);
        }
    }

    /// <summary>Raised when the effects level changes (for sources that cache their volume).</summary>
    public static event Action<float> EffectsVolumeChanged;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _effects = 1f;
        EffectsVolumeChanged = null;
    }
}
