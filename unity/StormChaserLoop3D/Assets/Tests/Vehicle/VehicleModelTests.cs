using NUnit.Framework;
using UnityEngine;

/// <summary>vehicle-feel.md Unit acceptance criteria + ADR-0005 stability bounds (S7-03).</summary>
public class VehicleModelTests
{
    private const float Dt = 0.02f;
    private static readonly VehicleFeelValues V = VehicleFeelValues.Defaults;

    // ---------- F13 archetype derivation ----------

    [Test]
    public void PickupStars_DeriveGddValues()
    {
        // Arrange / Act
        ArchetypeParams p = ArchetypeParams.Derive(Stars.Pickup, V);
        // Assert
        Assert.AreEqual(21.5f, p.TopSpeed, 1e-4f);
        Assert.AreEqual(3.0f, p.AccelTime, 1e-4f);
        Assert.AreEqual(2100f, p.Mass, 1e-3f);
        Assert.AreEqual(0.85f, p.Exposure, 1e-4f);
        Assert.AreEqual(12.5f, p.LightImpact, 1e-4f);
        Assert.AreEqual(22.5f, p.SevereImpact, 1e-4f);
        Assert.AreEqual(1.0f, p.TrickScale, 1e-4f);
        Assert.AreEqual(0.37f, p.HandbrakeGrip, 1e-4f);
        Assert.AreEqual(1.0f, p.StyleRefillScale, 1e-4f);
    }

    [Test]
    public void MotorcycleAndMonsterTruck_MatchGddExtremes()
    {
        ArchetypeParams moto = ArchetypeParams.Derive(new Stars(5, 0, 5), V);
        ArchetypeParams monster = ArchetypeParams.Derive(new Stars(2, 5, 3), V);
        Assert.AreEqual(26.5f, moto.TopSpeed, 1e-4f);
        Assert.AreEqual(1200f, moto.Mass, 1e-3f);
        Assert.AreEqual(1.3f, moto.Exposure, 1e-4f);
        Assert.AreEqual(8f, moto.LightImpact, 1e-4f);
        Assert.AreEqual(2700f, monster.Mass, 1e-3f);
        Assert.AreEqual(0.55f, monster.Exposure, 1e-4f);
        Assert.AreEqual(15.5f, monster.LightImpact, 1e-4f);
    }

    // ---------- F1 suspension ----------

    [Test]
    public void PickupSpringConstants_MatchGddWorkedExample()
    {
        // GDD F1 worked example uses Sag 0.35, ζ 0.45 (shipped defaults are tuned stiffer; see config).
        ArchetypeParams.SpringConstants(2100f, 0.35f, 0.35f, 0.45f, out float k, out float c);
        Assert.AreEqual(42000f, k, 200f);  // GDD: k ≈ 42,000 N/m
        Assert.AreEqual(4300f, c, 150f);   // GDD: c ≈ 4,300 N·s/m
    }

    [TestCase(1, 0, 1)]
    [TestCase(3, 3, 2)]
    [TestCase(2, 5, 3)]
    [TestCase(5, 0, 5)]
    public void SuspensionAt50Hz_IsWithinStabilityBounds(int speed, int armor, int trick)
    {
        // ADR-0005 guideline: c·dt/(M/4) < 1 and k·dt²/(M/4) < 0.5
        ArchetypeParams p = ArchetypeParams.Derive(new Stars(speed, armor, trick), V);
        float quarter = p.Mass * 0.25f;
        Assert.Less(p.DamperC * Dt / quarter, 1f);
        Assert.Less(p.SpringK * Dt * Dt / quarter, 0.5f);
    }

    [Test]
    public void PickupAtRestSag_SuspensionCarriesBodyWeightWithin3Percent()
    {
        // Arrange: all four wheels at 35 % of travel, body still.
        var model = NewPickup(out ArchetypeParams p);
        WheelContact[] contacts = Contacts(V.RestLength - V.SagFraction * V.Travel, Vector3.zero);
        var output = new VehicleStepOutput();
        var input = Input(Vector3.zero);
        // Act: second step so the damper sees zero compression velocity.
        model.Step(input, contacts, output);
        model.Step(input, contacts, output);
        // Assert
        float vertical = 0f;
        for (int i = 0; i < VehicleModel.WheelCount; i++) vertical += output.WheelForce[i].y;
        Assert.AreEqual(p.Mass * ArchetypeParams.Gravity, vertical, p.Mass * ArchetypeParams.Gravity * 0.03f);
    }

    // ---------- F2 drive ----------

    [Test]
    public void TorqueCurve_FullAtRestZeroAtTopSpeed()
    {
        Assert.AreEqual(1f, VehicleModel.TorqueCurve(0f, 21.5f), 1e-5f);
        Assert.AreEqual(0f, VehicleModel.TorqueCurve(21.5f, 21.5f), 1e-5f);
        Assert.AreEqual(1f - Mathf.Pow(0.5f, 2.5f), VehicleModel.TorqueCurve(10.75f, 21.5f), 1e-5f);
    }

    [Test]
    public void EngineCoefficient_Reaches90PercentInAccelTime()
    {
        // Integrate dv/dt = A·(1 − (v/vTop)^2.5) with GDD F2 coefficient 1.28.
        ArchetypeParams p = ArchetypeParams.Derive(Stars.Pickup, V);
        float v = 0f, t = 0f;
        while (v < 0.9f * p.TopSpeed && t < 10f)
        {
            v += p.EngineAccel * VehicleModel.TorqueCurve(v, p.TopSpeed) * 0.001f;
            t += 0.001f;
        }
        Assert.AreEqual(p.AccelTime, t, 0.3f);
    }

    [Test]
    public void CriticalDamage_ThrottleDrivesAtLimpPower()
    {
        // Arrange: grounded at rest, full throttle, Healthy vs Critical.
        float Drive(DamageStage stage, float throttle, float brake, out bool disabled)
        {
            var model = NewPickup(out _);
            WheelContact[] contacts = Contacts(V.RestLength - V.SagFraction * V.Travel, Vector3.zero);
            var output = new VehicleStepOutput();
            var input = Input(Vector3.zero);
            input.Damage = stage;
            input.Input.Throttle = throttle;
            input.Input.Brake = brake;
            model.Step(input, contacts, output);
            disabled = model.Disabled;
            float forward = 0f;
            for (int i = 0; i < VehicleModel.WheelCount; i++) forward += output.WheelForce[i].z;
            return forward;
        }
        // Act
        float healthy = Drive(DamageStage.Healthy, 1f, 0f, out _);
        float critical = Drive(DamageStage.Critical, 1f, 0f, out bool disabled);
        float reverse = Drive(DamageStage.Critical, 0f, 1f, out _);
        // Assert: limp mode moves both ways at CriticalPowerScale of healthy power (playtest 2026-10-01).
        Assert.IsTrue(disabled);
        Assert.Greater(critical, 0f, "Critical must still drive forward");
        Assert.AreEqual(healthy * V.CriticalPowerScale, critical, healthy * 0.01f);
        Assert.Less(reverse, 0f, "Critical must still reverse");
    }

    // ---------- F3 grip ----------

    [Test]
    public void SidewaysSlide_WheelForcesStayInsideFrictionCircle()
    {
        // Arrange: body sliding sideways at 8 m/s on full-grip ground.
        var model = NewPickup(out ArchetypeParams p);
        Vector3 vel = new Vector3(8f, 0f, 0f);
        WheelContact[] contacts = Contacts(V.RestLength - V.SagFraction * V.Travel, vel);
        var output = new VehicleStepOutput();
        // Act
        model.Step(Input(vel), contacts, output);
        model.Step(Input(vel), contacts, output);
        // Assert: horizontal force per wheel ≤ μ·load (load ≈ vertical component on flat ground).
        for (int i = 0; i < VehicleModel.WheelCount; i++)
        {
            Vector3 f = output.WheelForce[i];
            float horizontal = new Vector2(f.x, f.z).magnitude;
            Assert.LessOrEqual(horizontal, V.GripMu * f.y + 1f, $"wheel {i}");
            Assert.Less(f.x, 0f, $"wheel {i} should resist the slide");
        }
    }

    // ---------- F4 steering ----------

    [Test]
    public void Steering_NarrowsAtSpeedAndWhenDamaged()
    {
        WheelContact[] contacts = Contacts(V.RestLength - V.SagFraction * V.Travel, Vector3.zero);
        var output = new VehicleStepOutput();

        var slow = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.Input.Steer = 1f;
        slow.Step(input, contacts, output);
        Assert.AreEqual(V.MaxSteerDeg, slow.SteerAngleDeg, 1e-3f);

        var fast = NewPickup(out ArchetypeParams p);
        var fastInput = Input(new Vector3(0f, 0f, p.TopSpeed));
        fastInput.Input.Steer = 1f;
        fast.Step(fastInput, Contacts(V.RestLength - 0.12f, new Vector3(0f, 0f, p.TopSpeed)), output);
        Assert.AreEqual(V.MaxSteerDeg * V.HighSpeedSteerFactor, fast.SteerAngleDeg, 1e-2f);

        var damaged = NewPickup(out _);
        input.Damage = DamageStage.Damaged;
        damaged.Step(input, contacts, output);
        Assert.AreEqual(V.MaxSteerDeg * 0.75f, damaged.SteerAngleDeg, 1e-3f);
    }

    // ---------- States ----------

    [Test]
    public void NoContactsPastGrace_BecomesAirborne_ThenLandingReportsSpeed()
    {
        var model = NewPickup(out _);
        var output = new VehicleStepOutput();
        WheelContact[] air = new WheelContact[VehicleModel.WheelCount];
        var falling = Input(new Vector3(0f, -6f, 5f));
        for (int i = 0; i < 7; i++) model.Step(falling, air, output); // 0.14 s > 0.1 s grace
        Assert.AreEqual(VehicleState.Airborne, model.State);

        model.Step(falling, Contacts(V.RestLength - 0.2f, new Vector3(0f, -6f, 5f)), output);
        Assert.IsTrue(output.Landed);
        Assert.AreEqual(6f, output.LandedSpeed, 1e-3f);
        Assert.AreNotEqual(VehicleState.Airborne, model.State);
    }

    [Test]
    public void OneWheelGrounded_KeepsPreviousState()
    {
        var model = NewPickup(out _);
        var output = new VehicleStepOutput();
        WheelContact[] contacts = Contacts(V.RestLength - 0.12f, Vector3.zero);
        model.Step(Input(Vector3.zero), contacts, output);
        Assert.AreEqual(VehicleState.Grounded, model.State);
        for (int i = 1; i < VehicleModel.WheelCount; i++) contacts[i].Grounded = false;
        for (int i = 0; i < 20; i++) model.Step(Input(Vector3.zero), contacts, output);
        Assert.AreEqual(VehicleState.Grounded, model.State);
    }

    [Test]
    public void HandbrakeAtSpeed_EntersSliding_ButNotWhenNearlyStopped()
    {
        WheelContact[] contacts = Contacts(V.RestLength - 0.12f, new Vector3(0f, 0f, 15f));
        var output = new VehicleStepOutput();
        var model = NewPickup(out _);
        var input = Input(new Vector3(0f, 0f, 15f));
        input.Input.Handbrake = true;
        model.Step(input, contacts, output);
        Assert.AreEqual(VehicleState.Sliding, model.State);

        var slowModel = NewPickup(out _);
        var slow = Input(new Vector3(0f, 0f, 1f));
        slow.Input.Handbrake = true;
        slowModel.Step(slow, Contacts(V.RestLength - 0.12f, new Vector3(0f, 0f, 1f)), output);
        Assert.AreEqual(VehicleState.Grounded, slowModel.State);
    }

    [Test]
    public void UpsideDownAndStill_RequestsAutoRightAfterDelay()
    {
        var model = NewPickup(out _);
        var output = new VehicleStepOutput();
        WheelContact[] none = new WheelContact[VehicleModel.WheelCount];
        var input = Input(Vector3.zero);
        input.Rotation = Quaternion.Euler(0f, 0f, 180f);
        bool requested = false;
        int steps = Mathf.CeilToInt(V.AutoRightDelay / Dt) + 1;
        for (int i = 0; i < steps; i++)
        {
            model.Step(input, none, output);
            requested |= output.AutoRight;
        }
        Assert.AreEqual(VehicleState.Upended, model.State);
        Assert.IsTrue(requested);
    }

    // ---------- F10 impacts ----------

    [TestCase(12.4f, 0)]
    [TestCase(12.5f, 1)]
    [TestCase(22.4f, 1)]
    [TestCase(22.5f, 2)]
    public void PickupImpactSeverity_MapsToHpLoss(float speed, int expected)
    {
        Assert.AreEqual(expected, VehicleModel.ImpactHpLoss(speed, 12.5f, 22.5f));
    }

    [Test]
    public void CleanLandingFrom8m_DealsNoDamage()
    {
        float severity = VehicleModel.LandingSeverity(12.5f, V.LandingThresholdMul);
        Assert.AreEqual(0, VehicleModel.ImpactHpLoss(severity, 12.5f, 22.5f));
    }

    // ---------- F11 wind ----------

    [Test]
    public void StillAir_ProducesNoWindForce()
    {
        Vector3 a = VehicleModel.WindAcceleration(Vector3.zero, new Vector3(0f, 0f, 20f), 0.85f, 1.1f, 1.2f);
        Assert.AreEqual(Vector3.zero, a);
    }

    [Test]
    public void Crosswind_PushesAlongWindUntilBodyMatchesIt()
    {
        Vector3 wind = new Vector3(8f, 0f, 0f);
        Vector3 parked = VehicleModel.WindAcceleration(wind, Vector3.zero, 0.85f, 1.1f, 1.2f);
        Vector3 matched = VehicleModel.WindAcceleration(wind, new Vector3(8f, 0f, 3f), 0.85f, 1.1f, 1.2f);
        Assert.AreEqual(1.1f * 0.85f * 8f, parked.x, 1e-4f);
        Assert.AreEqual(Vector3.zero, matched);
    }

    [Test]
    public void ExtremeWind_IsCappedAtConfiguredG()
    {
        Vector3 a = VehicleModel.WindAcceleration(new Vector3(200f, 0f, 0f), Vector3.zero, 1.3f, 1.1f, 1.2f);
        Assert.AreEqual(1.2f * ArchetypeParams.Gravity, a.magnitude, 1e-3f);
    }

    // ---------- F12 lift (implemented as a pure function now; applied in S7-06) ----------

    [Test]
    public void Ef3_NeverReachesTossThresholdForPickup()
    {
        for (float d = 0f; d < 20f; d += 0.25f)
            Assert.Less(VehicleModel.LiftFraction(0.85f, 2.5f, 1f, d, 29f), 0.7f, $"d={d}");
    }

    [Test]
    public void Ef5_TossesPickupAt5mButNot6m()
    {
        Assert.GreaterOrEqual(VehicleModel.LiftFraction(0.85f, 4f, 1f, 5f, 38f), 0.7f);
        Assert.Less(VehicleModel.LiftFraction(0.85f, 4f, 1f, 6f, 38f), 0.7f);
    }

    [Test]
    public void TunedLift_Ef3NeverTosses_Ef4TossesAtDamageEdge_Ef5From8m()
    {
        // Arrange: shipped coefficient (playtest 2026-10-01).
        float k = V.LiftCoefficient;
        // Act / Assert: EF3 (strength 2.5, R 29) never reaches the threshold.
        for (float d = 0f; d < 15f; d += 0.25f)
            Assert.Less(VehicleModel.LiftFraction(0.85f, 2.5f, 1f, d, 29f, k), V.TossThreshold, $"EF3 d={d}");
        // EF4 (strength 3, R 32, damage radius ≈ 3.5 m) tosses at 3.4 m, not at 4.5 m.
        Assert.GreaterOrEqual(VehicleModel.LiftFraction(0.85f, 3f, 1f, 3.4f, 32f, k), V.TossThreshold);
        Assert.Less(VehicleModel.LiftFraction(0.85f, 3f, 1f, 4.5f, 32f, k), V.TossThreshold);
        // EF5 (strength 4, R 38) tosses from 8 m.
        Assert.GreaterOrEqual(VehicleModel.LiftFraction(0.85f, 4f, 1f, 8f, 38f, k), V.TossThreshold);
    }

    [Test]
    public void LiftAboveThreshold_TossesOnce_UntilLanded()
    {
        // Arrange
        var model = NewPickup(out _);
        var output = new VehicleStepOutput();
        WheelContact[] contacts = Contacts(V.RestLength - 0.12f, Vector3.zero);
        var input = Input(Vector3.zero);
        input.LiftFraction = 1f;
        input.LiftEFStrength = 4f;
        input.Wind = new Vector3(10f, 0f, 0f);
        // Act
        model.Step(input, contacts, output);
        bool first = output.Toss;
        Vector3 v = output.TossVelocity;
        model.Step(input, contacts, output);
        // Assert
        Assert.IsTrue(first);
        Assert.Greater(v.y, 5f);
        Assert.IsFalse(output.Toss, "Toss must not repeat before landing");
        Assert.AreEqual(VehicleState.Tossed, model.State);
    }

    [Test]
    public void CrosswindFromLeft_BiasesSteeringTowardWind()
    {
        // Arrange: parked, wind blowing toward -x (to the truck's left), no steer input.
        var model = NewPickup(out _);
        var output = new VehicleStepOutput();
        var input = Input(Vector3.zero);
        input.Wind = new Vector3(-6f, 0f, 0f);
        // Act
        model.Step(input, Contacts(V.RestLength - 0.12f, Vector3.zero), output);
        // Assert: negative steer = left, capped at WindSteerMax.
        Assert.Less(model.WindSteerBias, 0f);
        Assert.GreaterOrEqual(model.WindSteerBias, -V.WindSteerMax);
    }

    // ---------- vehicle-damage.md stages ----------

    [TestCase(3, 3, DamageStage.Healthy)]
    [TestCase(2, 3, DamageStage.Damaged)]
    [TestCase(1, 3, DamageStage.Critical)]
    [TestCase(1, 1, DamageStage.Healthy)]
    public void HpToStage_FollowsVehicleDamageThresholds(int current, int max, DamageStage expected)
    {
        Assert.AreEqual(expected, VehicleHealth.StageFor(current, max));
    }

    // ---------- helpers ----------

    private static VehicleModel NewPickup(out ArchetypeParams p)
    {
        p = ArchetypeParams.Derive(Stars.Pickup, V);
        return new VehicleModel(p, V, WheelLayout.Pickup);
    }

    private static VehicleStepInput Input(Vector3 velocity) => new VehicleStepInput
    {
        Dt = Dt,
        Rotation = Quaternion.identity,
        Velocity = velocity,
        AngularVelocity = Vector3.zero,
        CenterOfMassWorld = Vector3.zero,
        Wind = Vector3.zero,
        Damage = DamageStage.Healthy,
    };

    private static WheelContact[] Contacts(float groundDistance, Vector3 velocity)
    {
        WheelLayout l = WheelLayout.Pickup;
        var c = new WheelContact[VehicleModel.WheelCount];
        float[] xs = { -l.HalfTrack, l.HalfTrack, -l.HalfTrack, l.HalfTrack };
        float[] zs = { l.FrontAxleZ, l.FrontAxleZ, l.RearAxleZ, l.RearAxleZ };
        for (int i = 0; i < c.Length; i++)
        {
            c[i] = new WheelContact
            {
                Grounded = true,
                GroundDistance = groundDistance,
                Point = new Vector3(xs[i], -groundDistance, zs[i]),
                Normal = Vector3.up,
                PointVelocity = velocity,
                SurfaceGrip = 1f,
                SurfaceDrag = 0f,
            };
        }
        return c;
    }
}
