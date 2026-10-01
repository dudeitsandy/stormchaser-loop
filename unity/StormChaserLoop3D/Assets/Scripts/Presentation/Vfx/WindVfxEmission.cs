using UnityEngine;

/// <summary>Frame-rate-independent emission budget for pooled wind cards.</summary>
public static class WindVfxEmission
{
    /// <summary>Returns this frame's emission count, preserving fractional cards between frames.</summary>
    public static int Advance(float windSpeed, float deltaTime, float fullWindSpeed, float cardsPerSecond, ref float remainder)
    {
        float strength = Mathf.Clamp01((windSpeed - 0.5f) / Mathf.Max(0.1f, fullWindSpeed - 0.5f));
        remainder += strength * Mathf.Max(0, cardsPerSecond) * Mathf.Max(0, deltaTime);
        int count = Mathf.FloorToInt(remainder);
        remainder -= count;
        return count;
    }
}
