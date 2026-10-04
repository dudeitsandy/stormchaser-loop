using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// storm-director.md Rule 7 telegraph hooks (story 008): the environmental intensity <c>e</c> presentation
/// reads, and the KTVR News crawl text. Pure functions plus one live query.
/// </summary>
public static class StormTelegraph
{
    /// <summary>Summed per-cell wind magnitude at which every environmental cue is at maximum (m/s).</summary>
    public const float FullScaleWind = 20f;
    /// <summary>Warnings fire for a forming cell at or above this true EF.</summary>
    public const int WarningMinEf = 3;

    private static readonly string[] Compass = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

    /// <summary>e = clamp01(Σ|W_i| / 20): magnitudes add without cancelling, so a weak cell never masks a strong one.</summary>
    public static float EnvironmentIntensity(float summedWindMagnitude) =>
        Mathf.Clamp01(summedWindMagnitude / FullScaleWind);

    /// <summary>Live <c>e</c> at <paramref name="position"/>: the sum of each disaster's wind magnitude there.</summary>
    public static float EnvironmentIntensity(Vector3 position) => EnvironmentIntensity(SummedWind(position, DisasterEntity.Active));

    /// <summary>Σ |W_i(position)| over <paramref name="disasters"/> (the physics vector sum is separate).</summary>
    public static float SummedWind(Vector3 position, IReadOnlyList<DisasterEntity> disasters)
    {
        float sum = 0f;
        for (int i = 0; i < disasters.Count; i++)
            if (disasters[i] != null) sum += disasters[i].GetWindAt(position).magnitude;
        return sum;
    }

    /// <summary>8-point compass bearing from <paramref name="from"/> to <paramref name="to"/> (+Z = north, +X = east).</summary>
    public static string Bearing(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        float deg = Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
        return Compass[Mathf.RoundToInt(deg / 45f) % 8];
    }

    /// <summary>
    /// KTVR News crawl for a forming cell, or null below EF3. EF3–EF4 is a TORNADO WARNING; a true EF5 escalates
    /// to TORNADO EMERGENCY (Andy, 2026-10-04). Never contains an EF number or any digit (Rule 7).
    /// </summary>
    public static string FormingCrawl(int trueEf, string bearing)
    {
        if (trueEf < WarningMinEf) return null;
        return trueEf >= 5
            ? $"★ TORNADO EMERGENCY ★ VIOLENT TORNADO FORMING · BEARING {bearing} · TAKE COVER NOW ★"
            : $"★ TORNADO WARNING ★ SEVERE CELL FORMING · BEARING {bearing} ★";
    }

    /// <summary>Crawl for the anchor's touchdown (its Peak).</summary>
    public static string TouchdownCrawl(int trueEf, string bearing) =>
        trueEf >= 5
            ? $"★ TORNADO EMERGENCY ★ VIOLENT TORNADO ON THE GROUND · BEARING {bearing} ★"
            : $"★ TORNADO WARNING ★ TORNADO ON THE GROUND · BEARING {bearing} ★";

    /// <summary>True for crawls that use the red emergency style.</summary>
    public static bool IsEmergency(int trueEf) => trueEf >= 5;
}
