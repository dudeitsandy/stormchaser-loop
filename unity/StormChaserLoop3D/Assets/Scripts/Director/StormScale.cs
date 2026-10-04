using UnityEngine;

/// <summary>
/// storm-director.md F3 storm scale: wind, lift and damage radius for one cell, gated by lifecycle phase.
/// Pure math (no Unity objects), so the director, the tornado and tests all share one definition.
/// <para>Wind: W(d) = P · I · (1 − d / (R · I))² inside R · I, split 0.537 inflow / 0.843 swirl (the 7 : 11
/// ratio, |split| ≈ 1). Lift uses vehicle-feel F12 (<see cref="VehicleModel.LiftFraction"/>).</para>
/// </summary>
public static class StormScale
{
    /// <summary>Lifecycle phase as F3 gates it.</summary>
    public enum Phase
    {
        /// <summary>Growing: wind ramps with I; lift and damage are zero.</summary>
        Forming,
        /// <summary>Peak window: I = 1, full lift and damage.</summary>
        Mature,
        /// <summary>Decline: wind, lift and damage radius all scale with I.</summary>
        RopingOut,
        /// <summary>Evicted while Forming (never touched down): only wind scales with I; lift and damage stay 0.</summary>
        FailedTouchdown,
    }

    /// <summary>Inflow share of W (inward pull).</summary>
    public const float InflowRatio = 0.537f;
    /// <summary>Swirl share of W (counter-clockwise from above).</summary>
    public const float SwirlRatio = 0.843f;
    /// <summary>Below this intensity a cell has no wind at all (guards R · I ≈ 0).</summary>
    public const float MinIntensity = 0.01f;

    /// <summary>Wind speed W(d) in m/s at horizontal distance <paramref name="d"/> from the axis.</summary>
    public static float WindSpeed(float peakWind, float windRadius, float intensity, float d)
    {
        if (intensity < MinIntensity) return 0f;
        float r = windRadius * intensity;
        if (r <= 0f || d >= r) return 0f;
        float f = 1f - d / r;
        return peakWind * intensity * f * f;
    }

    /// <summary>
    /// Wind vector on the XZ plane at <paramref name="offsetFromAxis"/>: inflow toward the axis plus swirl.
    /// Zero on the axis itself (direction undefined) and outside R · I.
    /// </summary>
    public static Vector3 Wind(Vector3 offsetFromAxis, float peakWind, float windRadius, float intensity)
    {
        if (intensity < MinIntensity) return Vector3.zero;
        float w = peakWind * intensity;
        return WindField.Vortex(offsetFromAxis, windRadius * intensity, InflowRatio * w, SwirlRatio * w);
    }

    /// <summary>
    /// Lift as a fraction of the vehicle's weight (vehicle-feel F12), gated by phase: zero while Forming or
    /// after a failed touchdown; full F12 when Mature; I · F12 evaluated at radius R · I while Roping Out, so
    /// lift and wind shrink together and there is no lift outside the wind field.
    /// </summary>
    public static float Lift(Phase phase, float exposure, float efStrength, float intensity, float d, float windRadius,
                             float liftCoefficient)
    {
        switch (phase)
        {
            case Phase.Mature:
                return VehicleModel.LiftFraction(exposure, efStrength, 1f, d, windRadius, liftCoefficient);
            case Phase.RopingOut:
                if (intensity < MinIntensity) return 0f;
                return VehicleModel.LiftFraction(exposure, efStrength, intensity, d, windRadius * intensity,
                                                 liftCoefficient);
            default:
                return 0f;
        }
    }

    /// <summary>Damage radius in metres: D when Mature, D · I while Roping Out, zero otherwise.</summary>
    public static float DamageRadius(Phase phase, float damageRadius, float intensity)
    {
        switch (phase)
        {
            case Phase.Mature: return damageRadius;
            case Phase.RopingOut: return damageRadius * Mathf.Clamp01(intensity);
            default: return 0f;
        }
    }

    /// <summary>
    /// Intensity during an early rope-out (cap eviction) that starts from <paramref name="startIntensity"/>:
    /// I = I0 · (1 − t / (I0 · Rope)), so there is no jump and it reaches 0 after I0 · Rope seconds.
    /// </summary>
    public static float EarlyRopeIntensity(float startIntensity, float secondsSinceRopeStart, float ropeSeconds)
    {
        float span = startIntensity * ropeSeconds;
        if (span <= 0f) return 0f;
        return Mathf.Max(0f, startIntensity * (1f - secondsSinceRopeStart / span));
    }
}
