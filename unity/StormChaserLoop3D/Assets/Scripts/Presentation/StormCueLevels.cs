using System;

/// <summary>Shared, bounded world and audio response to the sum of storm wind magnitudes.</summary>
public static class StormCueLevels
{
    /// <summary>Rule 7 exposure; opposite wind directions must never cancel this value.</summary>
    public static float Exposure(float summedWindMagnitude) => float.IsNaN(summedWindMagnitude) ? 0f : Math.Max(0f, Math.Min(1f, summedWindMagnitude / 20f));
    /// <summary>Sky/sun brightness reaches 40 percent of baseline at full exposure.</summary>
    public static float Brightness(float exposure) => 1f - 0.6f * Math.Max(0f, Math.Min(1f, exposure));
    /// <summary>Exposure lengthens gusts without enlarging the fixed card pool.</summary>
    public static float GustLength(float exposure) => 1f + Math.Max(0f, Math.Min(1f, exposure));
}
