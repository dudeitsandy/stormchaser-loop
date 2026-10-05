using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies the user effects gain while preserving each source's independent fade level.</summary>
public sealed class PresentationEffectsGain
{
    private readonly Dictionary<AudioSource, float> _levels = new Dictionary<AudioSource, float>();
    private bool _enabled;
    /// <summary>Returns the mix level before the user's effects gain.</summary>
    public float Get(AudioSource source) => _levels.TryGetValue(source, out float level) ? level : 0f;
    /// <summary>Updates the mix level and applies the current effects setting exactly once.</summary>
    public void Set(AudioSource source, float level)
    {
        level = Mathf.Clamp01(level);
        _levels[source] = level;
        source.volume = level * GameAudio.EffectsVolume;
    }
    /// <summary>Subscribes to live settings changes, including changes while paused.</summary>
    public void Enable()
    {
        if (_enabled) return;
        _enabled = true;
        GameAudio.EffectsVolumeChanged += Apply;
        Apply(GameAudio.EffectsVolume);
    }
    /// <summary>Detaches the settings listener when its owner is disabled.</summary>
    public void Disable()
    {
        if (!_enabled) return;
        _enabled = false;
        GameAudio.EffectsVolumeChanged -= Apply;
    }
    private void Apply(float gain)
    {
        foreach (var pair in _levels)
            if (pair.Key != null) pair.Key.volume = pair.Value * gain;
    }
}
