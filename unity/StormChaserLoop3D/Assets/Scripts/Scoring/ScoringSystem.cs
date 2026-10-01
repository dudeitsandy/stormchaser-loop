using UnityEngine;

public static class ScoringSystem
{
    /// <summary>Framing quality at or above which a shot is PERFECT.</summary>
    public const float PerfectQuality = 0.85f;
    /// <summary>Framing quality at or above which a shot is GOOD.</summary>
    public const float GoodQuality = 0.5f;

    /// <summary>
    /// Framing quality, 0–1, independent of disaster strength.
    /// Formula: AimScore * 0.6 + DistanceScore * 0.4
    /// </summary>
    public static float CalculateQuality(float aimScore, float distanceScore)
    {
        return Mathf.Clamp01(aimScore) * 0.6f + Mathf.Clamp01(distanceScore) * 0.4f;
    }

    /// <summary>
    /// Calculates photo score from aim quality, distance quality, and EF strength.
    /// Formula: (AimScore * 0.6 + DistanceScore * 0.4) * EFStrength * 100
    ///
    /// Season 2+ extension point — multi-entity composition:
    ///   Replace efStrength with combinedThreatLevel (sum of ThreatClass for all
    ///   entities in frame). Add styleMultiplier as a final factor (Style scoring axis).
    ///   Extended formula: quality * combinedThreat * styleMultiplier * 100
    /// </summary>
    /// <param name="aimScore">0.0–1.0 — how centered the tornado is in the frame</param>
    /// <param name="distanceScore">0.0–1.0 — how close to optimal distance</param>
    /// <param name="efStrength">EFStrength multiplier from TornadoData (EF0=1.0, EF5=4.0)</param>
    public static float CalculatePhotoScore(float aimScore, float distanceScore, float efStrength)
    {
        return CalculateQuality(aimScore, distanceScore) * efStrength * 100f;
    }

    /// <summary>
    /// Tier is based on framing quality, not raw score, so a perfectly framed EF0 still reads PERFECT.
    /// Deviates from S4-07's absolute thresholds (&gt;300 / 150–299), which made PERFECT unreachable below EF3.
    /// </summary>
    /// <summary>
    /// "IN THE WIND" bonus for shooting from inside a disaster's wind field.
    /// Formula: 1 + MaxBonus × clamp01(WindSpeed / FullAt). Distinct from event-system.md's DaredevilMultiplier
    /// (structure-gap event), which will chain on top when events land.
    /// </summary>
    public static float WindMultiplier(float windSpeed, float fullAt, float maxBonus)
    {
        if (fullAt <= 0f) return 1f;
        float t = Mathf.Clamp01(windSpeed / fullAt);
        return 1f + Mathf.Max(0f, maxBonus) * t;
    }

    public static ShotTier GetTier(float quality)
    {
        if (quality >= PerfectQuality) return ShotTier.Perfect;
        if (quality >= GoodQuality) return ShotTier.Good;
        return ShotTier.Glancing;
    }
}
