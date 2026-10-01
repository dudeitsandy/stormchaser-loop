using System.Collections.Generic;

/// <summary>
/// Diminishing returns for photographing the same subject repeatedly.
/// Each shot adds 1 "heat" to that subject; multiplier = DecayPerShot ^ heat; heat drains at 1 per RecoverySeconds.
/// Pure C#, no Unity dependencies.
/// </summary>
public class RepeatPenalty
{
    private struct Entry
    {
        public float Heat;
        public float LastTime;
    }

    private readonly Dictionary<object, Entry> _entries = new Dictionary<object, Entry>();

    /// <summary>Multiplier applied per unit of heat (0.5 = each rapid repeat is worth half the last).</summary>
    public float DecayPerShot { get; }
    /// <summary>Seconds for one unit of heat to drain.</summary>
    public float RecoverySeconds { get; }

    public RepeatPenalty(float decayPerShot, float recoverySeconds)
    {
        DecayPerShot = decayPerShot < 0f ? 0f : decayPerShot > 1f ? 1f : decayPerShot;
        RecoverySeconds = recoverySeconds > 0.01f ? recoverySeconds : 0.01f;
    }

    /// <summary>Score multiplier a shot of <paramref name="subjectId"/> would get at <paramref name="now"/>.</summary>
    public float GetMultiplier(object subjectId, float now)
    {
        float heat = CurrentHeat(subjectId, now);
        return heat <= 0f ? 1f : (float)System.Math.Pow(DecayPerShot, heat);
    }

    /// <summary>Records a shot of <paramref name="subjectId"/>, adding one unit of heat.</summary>
    public void Record(object subjectId, float now)
    {
        _entries[subjectId] = new Entry { Heat = CurrentHeat(subjectId, now) + 1f, LastTime = now };
    }

    private float CurrentHeat(object subjectId, float now)
    {
        if (!_entries.TryGetValue(subjectId, out Entry e)) return 0f;
        float heat = e.Heat - (now - e.LastTime) / RecoverySeconds;
        return heat > 0f ? heat : 0f;
    }
}
