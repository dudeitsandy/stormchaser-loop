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
    [Tooltip("Arcade brake assist (m/s²) applied to the body while braking on >= 2 wheels, outside the tire friction budget (Rocket League feel).")]
    public float BrakeAssistDecel;
    public float GripStiffness, GripMu;
    public float MaxSteerDeg, HighSpeedSteerFactor;
    public float DownforceCoeff;
    public float AntiRoll, YawStability;
    public float GripRecoveryTime;
    [Tooltip("Corrective roll acceleration beyond RollStabilizeAngle (grounded). 0 = off.")]
    public float RollStabilization, RollStabilizeAngleDeg;

    [Header("States (F5, E5, E9)")]
    public float SlideEnterDeg, SlideExitDeg, SlideMinSpeed, SlideExitSpeed;
    [Tooltip("Steering factor at top speed while Sliding (1 = full lock, so counter-steer can catch a drift). Grip driving uses HighSpeedSteerFactor.")]
    public float SlideSteerFactor;
    [Tooltip("Arcade drift drive (m/s²) along the direction of travel while Sliding on throttle, fading to 0 at top speed: a powered slide carries speed instead of scrubbing it.")]
    public float DriftDriveAccel;
    [Tooltip("Counter-steer assist (1/s): while Sliding off the e-brake, steering against the rotation damps the yaw rate by this × |steer|, so a driver can catch and hold a drift.")]
    public float CounterSteerAssist;
    public float AirborneGrace;
    public float UpendedDot, UpendedAngularSpeed, AutoRightDelay;
    public float MaxGroundSlopeDeg;

    [Header("Wind (F11)")]
    public float WindResponse, WindForceCapG, WindLeverHeight, WindGripLossAt;
    [Tooltip("Steering bias toward the wind per (exposed wind / WindGripLossAt). Small tornadoes tug the wheel.")]
    public float WindSteer, WindSteerMax;

    [Header("Lift & toss (F12, F12b)")]
    public float LiftCoefficient, TossThreshold, TossUpSpeed, TossSwirlFraction;

    [Header("Air control & jump (F7, F8)")]
    public float AirAccel, AirMaxRate;
    public float JumpSpeed, JumpCooldown;
    [Tooltip("Jump direction: 0 = vehicle up, 1 = world up.")]
    public float JumpWorldUpBlend;
    [Tooltip("Gravity multiplier while airborne and rising (not Tossed). Playtest 2026-10-03: jump too floaty.")]
    public float AirGravityMul;
    [Tooltip("Gravity multiplier while airborne and falling (not Tossed).")]
    public float FallGravityMul;

    [Header("Boost (F9) and style refills")]
    public float BoostAccel, BoostMaxSpeedRatio;          // F_boost = M·A·(1 − (v/(ratio·v_top))²)
    public float BoostDrain, BoostPassiveRegen, BoostMinStart;
    public float RefillSlide, RefillAir, RefillNearMiss;  // × StyleRefillScale
    [Header("Exploit gates (E1–E3)")]
    public float MinSlideRefillSpeed, MinAirtimeHeight;
    public float NearMissCooldown, NearMissMinSpeed;
    [Tooltip("Near-miss band: from the funnel's damage radius out to damage radius + this (m).")]
    public float NearMissMargin;
    [Tooltip("Shortest slide / counted airtime (s) that raises a StyleEvent.")]
    public float MinStyleSeconds;

    [Header("Impacts (F10)")]
    public float LandingThresholdMul;
    [Tooltip("Impacts slower than this (m/s, after E13 mass scaling) are not reported: resting contact and scrapes.")]
    public float ImpactReportMin;
    [Tooltip("F10 collision/landing HP cost. Off until the vehicle-damage.md HP redesign (S8-C2); impacts still raise VehicleImpact.")]
    public bool ImpactsCostHp;

    [Header("Knockback & recovery")]
    public float KnockbackSpreadSeconds, KnockbackGripScale;
    public float StuckSeconds;
    [Tooltip("Body tilt (degrees from upright) that counts as wedged for stuck recovery, even with wheels down.")]
    public float StuckTiltDeg;
    [Tooltip("Pitch beyond this (degrees) is levelled while any wheel touches (nose-first landings).")]
    public float PitchStabilizeAngleDeg;
    [Tooltip("Critical damage stage: engine power multiplier (limp mode). Boost and jump stay disabled.")]
    public float CriticalPowerScale;

    [Header("Limits")]
    public float MaxAngularSpeed;

    public static VehicleFeelValues Defaults => new VehicleFeelValues
    {
        TopSpeedBase = 14f, TopSpeedPerStar = 2.5f,
        AccelTimeBase = 4.2f, AccelTimePerStar = 0.4f,
        MassBase = 1200f, MassPerStar = 300f,
        ExposureBase = 1.3f, ExposurePerStar = 0.15f,
        // S8-C1 (2026-10-04, "three bumps end a run"): Pickup Light 12.5 → 15.5 m/s (≈ 72 % of top speed, so bumps
        // are free) and Severe 22.5 → 23.25 m/s (above unboosted top speed: 2 HP needs boost or a throw).
        LightBase = 11f, LightPerStar = 1.5f, SevereRatio = 1.5f,
        TrickScaleBase = 0.6f, TrickScalePerStar = 0.2f,
        HandbrakeGripBase = 0.45f, HandbrakeGripPerStar = 0.04f,
        StyleRefillBase = 0.7f, StyleRefillPerStar = 0.15f,

        // Playtest 2026-10-01 pass 2: pass 1 (0.28 / 0.6) was "too stiff" — midpoint toward the GDD's 0.35 / 0.45.
        // RestLength keeps the body origin ≈ 0.5 m above ground: 0.5 + Sag·Travel.
        SagFraction = 0.32f, DampingRatio = 0.52f, Travel = 0.35f, RestLength = 0.61f,

        EngineCoefficient = 1.28f, TorqueFalloffExponent = 2.5f,
        BrakeDecel = 14f, CoastDecel = 2.5f, ReverseSpeedFraction = 0.35f,
        // Playtest 0.7.0 (2026-10-03): "stops too slowly" vs Rocket League. Tire braking is grip-limited to
        // ≈ GripMu·g ≈ 11.3 m/s², so the assist adds 8 on top: full-speed stop ≈ 1.1 s / 12 m.
        BrakeAssistDecel = 8f,
        // Playtest 2026-10-01: GDD 1.5 / 1.1 / yaw 0.5 "a little too loose"; pass 1 2.0 / 1.25 / 0.7 "too stiff".
        GripStiffness = 1.7f, GripMu = 1.15f,
        MaxSteerDeg = 32f, HighSpeedSteerFactor = 0.45f,
        DownforceCoeff = 0.25f,
        AntiRoll = 0.6f, YawStability = 0.6f,
        // S9-02a ("drift needs more driver feel"): 0.25 → 0.4 s so letting off the e-brake flows into the slide.
        GripRecoveryTime = 0.4f,
        RollStabilization = 0.6f, RollStabilizeAngleDeg = 25f,

        SlideEnterDeg = 20f, SlideExitDeg = 10f, SlideMinSpeed = 3f, SlideExitSpeed = 2f,
        // S9-02a: baseline drifts bled 16 → 4–9 m/s in 1.5 s even on throttle, and counter-steer got only 45 % lock.
        SlideSteerFactor = 0.7f, DriftDriveAccel = 6f, CounterSteerAssist = 4f,
        AirborneGrace = 0.1f,
        UpendedDot = 0.3f, UpendedAngularSpeed = 1.5f, AutoRightDelay = 1.2f,
        MaxGroundSlopeDeg = 60f,

        WindResponse = 1.1f, WindForceCapG = 1.2f, WindLeverHeight = 0.2f, WindGripLossAt = 10f,
        WindSteer = 0.35f, WindSteerMax = 0.5f,
        // Playtest 2026-10-01: "more powerful tornadoes should throw". GDD coefficient 0.8 only let EF5 toss;
        // 1.35 → EF3 lifts (never tosses), EF4 tosses at ≈ its damage-radius edge, EF5 from ≈ 8.5 m.
        LiftCoefficient = 1.35f, TossThreshold = 0.7f, TossUpSpeed = 8f, TossSwirlFraction = 0.5f,

        AirAccel = 20f, AirMaxRate = 4.5f,
        // Playtest 2026-10-03 ("jump is a little floaty"): heavier air, same 1.5 m apex (v = √(2·1.5g·1.5)).
        // S9-02a (Andy 2026-10-04, still "a touch floaty"): heavier again at the same ≈ 1.5 m apex, v = √(2·1.8g·1.5).
        JumpSpeed = 7.3f, JumpCooldown = 0.8f, JumpWorldUpBlend = 0.5f,
        AirGravityMul = 1.8f, FallGravityMul = 2.4f,

        BoostAccel = 9f, BoostMaxSpeedRatio = 1.35f,
        BoostDrain = 33f, BoostPassiveRegen = 4f, BoostMinStart = 5f,
        RefillSlide = 18f, RefillAir = 14f, RefillNearMiss = 25f,
        // S9-02a: 1.8 m was above a full jump's ≈ 1.5 m apex, so jumps never counted (rg-airtime-evidence.md). 0.6 m
        // lets a full jump count ≈ 0.5 s (AIR pop + refill) while curb hops don't; big_air (1.0 s) stays toss-only.
        MinSlideRefillSpeed = 6f, MinAirtimeHeight = 0.6f,
        NearMissCooldown = 3f, NearMissMinSpeed = 8f, NearMissMargin = 6f,
        MinStyleSeconds = 0.5f,

        LandingThresholdMul = 1.5f,
        ImpactReportMin = 3f,
        ImpactsCostHp = true, // S8-C1: back on with the higher thresholds and the 6 HP Pickup

        KnockbackSpreadSeconds = 0.15f, KnockbackGripScale = 0.35f,
        // Playtest 2026-10-03: nose-first landings wedged the truck with rear wheels down; you had to jump out.
        StuckSeconds = 1f, StuckTiltDeg = 35f, PitchStabilizeAngleDeg = 30f,
        // Playtest 2026-10-01: momentum-only Critical soft-locked a stopped truck for the rest of the run.
        CriticalPowerScale = 0.4f,

        MaxAngularSpeed = 12f,
    };
}

/// <summary>Shared vehicle tuning asset (ADR-0005: one config for all archetypes; archetypes differ only by stars).</summary>
[CreateAssetMenu(fileName = "VehicleFeelConfig", menuName = "StormChaser/VehicleFeelConfig")]
public class VehicleFeelConfig : ScriptableObject
{
    public VehicleFeelValues Values = VehicleFeelValues.Defaults;
}
