using UnityEngine;

/// <summary>Speed / Armor / Trick star ratings (vision-1.0 archetype table; Armor ✗ = 0).</summary>
[System.Serializable]
public struct Stars
{
    [Range(1, 5)] public int Speed;
    [Range(0, 5)] public int Armor;
    [Range(1, 5)] public int Trick;

    public Stars(int speed, int armor, int trick)
    {
        Speed = speed;
        Armor = armor;
        Trick = trick;
    }

    public static Stars Pickup => new Stars(3, 3, 2);
}

/// <summary>Runtime vehicle parameters derived from stars (vehicle-feel.md F13) plus F1 spring constants.</summary>
public struct ArchetypeParams
{
    public const float Gravity = 9.81f;

    public float Mass;
    public float TopSpeed;
    public float AccelTime;
    /// <summary>F2 A_engine (m/s²).</summary>
    public float EngineAccel;
    public float ReverseSpeed;
    public float Exposure;
    public float LightImpact;
    public float SevereImpact;
    public float TrickScale;
    public float HandbrakeGrip;
    public float StyleRefillScale;
    /// <summary>F1 per-wheel spring stiffness (N/m).</summary>
    public float SpringK;
    /// <summary>F1 per-wheel damper (N·s/m).</summary>
    public float DamperC;

    /// <summary>F13 + F1. Pure: same stars and values → same parameters.</summary>
    public static ArchetypeParams Derive(Stars s, in VehicleFeelValues v)
    {
        var p = new ArchetypeParams
        {
            TopSpeed = v.TopSpeedBase + v.TopSpeedPerStar * s.Speed,
            AccelTime = v.AccelTimeBase - v.AccelTimePerStar * s.Speed,
            Mass = v.MassBase + v.MassPerStar * s.Armor,
            Exposure = v.ExposureBase - v.ExposurePerStar * s.Armor,
            LightImpact = v.LightBase + v.LightPerStar * s.Armor,
            TrickScale = v.TrickScaleBase + v.TrickScalePerStar * s.Trick,
            HandbrakeGrip = v.HandbrakeGripBase - v.HandbrakeGripPerStar * s.Trick,
            StyleRefillScale = v.StyleRefillBase + v.StyleRefillPerStar * s.Trick,
        };
        p.SevereImpact = v.SevereRatio * p.LightImpact;
        p.EngineAccel = v.EngineCoefficient * p.TopSpeed / Mathf.Max(0.1f, p.AccelTime);
        p.ReverseSpeed = v.ReverseSpeedFraction * p.TopSpeed;
        SpringConstants(p.Mass, v.SagFraction, v.Travel, v.DampingRatio, out p.SpringK, out p.DamperC);
        return p;
    }

    /// <summary>F1: k = (M·g/4)/(Sag·Travel); c = 2·ζ·√(k·M/4).</summary>
    public static void SpringConstants(float mass, float sagFraction, float travel, float dampingRatio, out float k, out float c)
    {
        float quarter = mass * 0.25f;
        k = quarter * Gravity / Mathf.Max(1e-4f, sagFraction * travel);
        c = 2f * dampingRatio * Mathf.Sqrt(k * quarter);
    }
}
