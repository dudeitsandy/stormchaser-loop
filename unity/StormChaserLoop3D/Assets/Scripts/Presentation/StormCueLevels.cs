using System;
using System.Collections.Generic;

/// <summary>Shared, bounded world and audio response to the sum of storm wind magnitudes.</summary>
public static class StormCueLevels
{
    /// <summary>Normalized siren envelope: bounded attack and a finite release, independent of effects gain.</summary>
    public static float SirenLevel(float current, bool active, float dt, float attackSeconds = 0.5f, float releaseSeconds = 3f)
    {
        current = float.IsNaN(current) ? 0f : Math.Max(0f, Math.Min(1f, current));
        float step = Math.Max(0f, dt) / Math.Max(0.001f, active ? attackSeconds : releaseSeconds);
        return active ? Math.Min(1f, current + step) : Math.Max(0f, current - step);
    }
    /// <summary>Rule 7 exposure; opposite wind directions must never cancel this value.</summary>
    public static float Exposure(float summedWindMagnitude) => float.IsNaN(summedWindMagnitude) ? 0f : Math.Max(0f, Math.Min(1f, summedWindMagnitude / 20f));
    /// <summary>Sky/sun brightness reaches 40 percent of baseline at full exposure.</summary>
    public static float Brightness(float exposure) => 1f - 0.6f * Math.Max(0f, Math.Min(1f, exposure));
    /// <summary>Overcast ambient falls to twenty percent, while direct light retains its separate response.</summary>
    public static float AmbientBrightness(float storminess) => 1f - 0.8f * StorminessTarget(storminess);
    /// <summary>Exposure lengthens gusts without enlarging the fixed card pool.</summary>
    public static float GustLength(float exposure) => 1f + Math.Max(0f, Math.Min(1f, exposure));
    /// <summary>One cell's contribution to run-wide storminess; independent of player distance.</summary>
    public static float StorminessContribution(int ef, float intensity) =>
        (Math.Max(0, Math.Min(5, ef)) + 1f) * Math.Max(0f, Math.Min(1f, intensity)) / 6f;
    /// <summary>Bounds the sum of live-cell contributions.</summary>
    public static float StorminessTarget(float summedContributions) => Math.Max(0f, Math.Min(1f, summedContributions));
    /// <summary>Exponential smoothing with rise/fall time constants; invariant under time subdivision.</summary>
    public static float EaseStorminess(float current, float target, float dt, float riseSeconds = 5f, float fallSeconds = 20f)
    {
        current = StorminessTarget(current);
        target = StorminessTarget(target);
        float seconds = target > current ? riseSeconds : fallSeconds;
        return target + (current - target) * (float)Math.Exp(-Math.Max(0f, dt) / Math.Max(0.001f, seconds));
    }
}

/// <summary>Bounded warning timer plus independent emergency cells; no Unity time or audio dependencies.</summary>
public sealed class OutdoorSirenCycle
{
    private readonly HashSet<int> _seen = new HashSet<int>();
    private readonly HashSet<int> _emergencies = new HashSet<int>();
    private float _remaining;
    /// <summary>Whether an ordinary warning cycle or any unended emergency requires a wail.</summary>
    public bool Active => _remaining > 0f || _emergencies.Count > 0;
    /// <summary>Starts a warning once per cell, or an emergency that survives the ordinary cycle duration.</summary>
    public void Forming(int cellId, int ef, float cycleSeconds)
    {
        if (ef < 3 || !_seen.Add(cellId)) return;
        if (ef >= 5) _emergencies.Add(cellId);
        else _remaining = Math.Max(_remaining, cycleSeconds);
    }
    /// <summary>Ends only the specified emergency; other warnings retain their own duration.</summary>
    public void End(int cellId) => _emergencies.Remove(cellId);
    /// <summary>Consumes game time, so a zero delta leaves the timer paused.</summary>
    public void Tick(float dt) => _remaining = Math.Max(0f, _remaining - Math.Max(0f, dt));
    /// <summary>Clears all cells and timers at run start/end or component disable.</summary>
    public void Reset() { _seen.Clear(); _emergencies.Clear(); _remaining = 0f; }
}
