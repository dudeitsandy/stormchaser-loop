using System;

/// <summary>
/// Shared audio levels from Settings (design/ux/run-screens.md). MASTER is <c>AudioListener.volume</c>; MUSIC is applied
/// by <see cref="RadioLite"/>; EFFECTS is read by every non-music source (presentation lane) as a multiplier.
/// </summary>
public static class GameAudio
{
    private static float _effects = 1f;
    private static bool _attractMute;

    /// <summary>
    /// 0..1 effects level. Presentation audio multiplies its source volumes by this. Reads 0 while
    /// <see cref="AttractMute"/> is on (the setting itself is kept).
    /// </summary>
    public static float EffectsVolume
    {
        get => _attractMute ? 0f : _effects;
        set
        {
            float v = value < 0f ? 0f : value > 1f ? 1f : value;
            if (Math.Abs(v - _effects) < 1e-6f) return;
            _effects = v;
            EffectsVolumeChanged?.Invoke(EffectsVolume);
        }
    }

    /// <summary>Title attract mode: silences every effects source (title music plays on its own channel).</summary>
    public static bool AttractMute
    {
        get => _attractMute;
        set
        {
            if (_attractMute == value) return;
            _attractMute = value;
            EffectsVolumeChanged?.Invoke(EffectsVolume);
        }
    }

    /// <summary>Raised when the effects level changes (for sources that cache their volume).</summary>
    public static event Action<float> EffectsVolumeChanged;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _effects = 1f;
        _attractMute = false;
        EffectsVolumeChanged = null;
    }
}
