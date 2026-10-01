using System;
using UnityEngine;

/// <summary>
/// Every vehicle-feel.md Tuning Knob and F13 coefficient, as a plain struct so VehicleModel can use it
/// without touching UnityEngine.Object (ADR-0005). Defaults are the GDD's.
/// </summary>
[Serializable]
public struct VehicleFeelValues
{
    [Header("F13 archetype coefficients")]
    public float TopSpeedBase, TopSpeedPerStar;          // v_top = base + per·s
    public float AccelTimeBase, AccelTimePerStar;        // AccelTime = base − per·s
    public float MassBase, MassPerStar;                  // M = base + per·a
    public float ExposureBase, ExposurePerStar;          // Exposure = base − per·a
    public float LightBase, LightPerStar, SevereRatio;   // Light = base + per·a; Severe = ratio·Light
    public float TrickScaleBase, TrickScalePerStar;
    public float HandbrakeGripBase, HandbrakeGripPerStar;
    public float StyleRefillBase, StyleRefillPerStar;

    [Header("Suspension (F1)")]
    public float SagFraction, DampingRatio, Travel, RestLength;

    [Header("Drive & grip (F2, F2b, F3, F4, F6)")]
    public float EngineCoefficient;      // A_engine = coef · v_top / AccelTime (≈1.28 per GDD)
    public float TorqueFalloffExponent;  // 2.5
    public float BrakeDecel, CoastDecel, ReverseSpeedFraction;
    public float GripStiffness, GripMu;
    public float MaxSteerDeg, HighSpeedSteerFactor;
    public float DownforceCoeff;
    public float AntiRoll, YawStability;
    public float GripRecoveryTime;

    [Header("States (F5, E5, E9)")]
    public float SlideEnterDeg, SlideExitDeg, SlideMinSpeed, SlideExitSpeed;
    public float AirborneGrace;
    public float UpendedDot, UpendedAngularSpeed, AutoRightDelay;
    public float MaxGroundSlopeDeg;

    [Header("Wind (F11)")]
    public float WindResponse, WindForceCapG, WindLeverHeight, WindGripLossAt;

    [Header("Impacts (F10)")]
    public float LandingThresholdMul;

    [Header("Limits")]
    public float MaxAngularSpeed;

    public static VehicleFeelValues Defaults => new VehicleFeelValues
    {
        TopSpeedBase = 14f, TopSpeedPerStar = 2.5f,
        AccelTimeBase = 4.2f, AccelTimePerStar = 0.4f,
        MassBase = 1200f, MassPerStar = 300f,
        ExposureBase = 1.3f, ExposurePerStar = 0.15f,
        LightBase = 8f, LightPerStar = 1.5f, SevereRatio = 1.8f,
        TrickScaleBase = 0.6f, TrickScalePerStar = 0.2f,
        HandbrakeGripBase = 0.45f, HandbrakeGripPerStar = 0.04f,
        StyleRefillBase = 0.7f, StyleRefillPerStar = 0.15f,

        SagFraction = 0.35f, DampingRatio = 0.45f, Travel = 0.35f, RestLength = 0.62f,

        EngineCoefficient = 1.28f, TorqueFalloffExponent = 2.5f,
        BrakeDecel = 14f, CoastDecel = 2.5f, ReverseSpeedFraction = 0.35f,
        GripStiffness = 1.5f, GripMu = 1.1f,
        MaxSteerDeg = 32f, HighSpeedSteerFactor = 0.45f,
        DownforceCoeff = 0.25f,
        AntiRoll = 0.6f, YawStability = 0.5f,
        GripRecoveryTime = 0.25f,

        SlideEnterDeg = 20f, SlideExitDeg = 10f, SlideMinSpeed = 3f, SlideExitSpeed = 2f,
        AirborneGrace = 0.1f,
        UpendedDot = 0.3f, UpendedAngularSpeed = 1.5f, AutoRightDelay = 1.2f,
        MaxGroundSlopeDeg = 60f,

        WindResponse = 1.1f, WindForceCapG = 1.2f, WindLeverHeight = 0.4f, WindGripLossAt = 10f,

        LandingThresholdMul = 1.5f,

        MaxAngularSpeed = 12f,
    };
}

/// <summary>Shared vehicle tuning asset (ADR-0005: one config for all archetypes; archetypes differ only by stars).</summary>
[CreateAssetMenu(fileName = "VehicleFeelConfig", menuName = "StormChaser/VehicleFeelConfig")]
public class VehicleFeelConfig : ScriptableObject
{
    public VehicleFeelValues Values = VehicleFeelValues.Defaults;
}
