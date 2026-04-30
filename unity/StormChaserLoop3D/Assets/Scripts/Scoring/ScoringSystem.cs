using UnityEngine;

public static class ScoringSystem
{
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
        float quality = aimScore * 0.6f + distanceScore * 0.4f;
        return quality * efStrength * 100f;
    }
}
