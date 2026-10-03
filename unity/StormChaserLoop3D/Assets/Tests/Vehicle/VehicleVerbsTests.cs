using NUnit.Framework;
using UnityEngine;

/// <summary>vehicle-feel.md verbs (S7-04): air control F7, jump F8, boost F9, exploit gates E1–E3, E14.</summary>
public class VehicleVerbsTests
{
    private const float Dt = 0.02f;
    private static readonly VehicleFeelValues V = VehicleFeelValues.Defaults;
    private static float RestGap => V.RestLength - V.SagFraction * V.Travel;

    // ---------- F8 jump ----------

    [Test]
    public void Jump_Grounded_GivesJumpSpeedUpward()
    {
        // Arrange
        var model = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.Input.JumpPressed = true;
        var output = new VehicleStepOutput();
        // Act
        model.Step(input, Grounded(Vector3.zero), output);
        // Assert: 5.5 m/s → apex v²/2g ≈ 1.5 m.
        Assert.IsTrue(output.Jumped);
        Assert.AreEqual(V.JumpSpeed, output.JumpVelocity.magnitude, 1e-4f);
        Assert.AreEqual(1f, Vector3.Dot(output.JumpVelocity.normalized, Vector3.up), 1e-4f);
        Assert.AreEqual(1.54f, V.JumpSpeed * V.JumpSpeed / (2f * 9.81f), 0.01f);
    }

    [Test]
    public void Jump_RespectsCooldown()
    {
        // Arrange
        var model = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.Input.JumpPressed = true;
        var output = new VehicleStepOutput();
        WheelContact[] ground = Grounded(Vector3.zero);
        // Act
        model.Step(input, ground, output);
        model.Step(input, ground, output);
        bool second = output.Jumped;
        int steps = Mathf.CeilToInt(V.JumpCooldown / Dt);
        int jumpedAt = -1;
        for (int i = 0; i <= steps && jumpedAt < 0; i++)
        {
            model.Step(input, ground, output);
            if (output.Jumped) jumpedAt = i;
        }
        // Assert
        Assert.IsFalse(second, "no second jump inside the cooldown");
        Assert.GreaterOrEqual(jumpedAt, steps - 2, "not before the cooldown elapses");
    }

    [Test]
    public void Jump_IgnoredWhenAirborneOrCritical()
    {
        // Arrange
        var airborne = NewPickup(out _);
        var critical = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.Input.JumpPressed = true;
        var outA = new VehicleStepOutput();
        var outC = new VehicleStepOutput();
        // Act
        airborne.Step(input, Airborne(), outA);
        input.Damage = DamageStage.Critical;
        critical.Step(input, Grounded(Vector3.zero), outC);
        // Assert (no air jump; E10/E14)
        Assert.IsFalse(outA.Jumped);
        Assert.IsFalse(outC.Jumped);
    }

    // ---------- F9 boost ----------

    [TestCase(0f, 9f)]
    [TestCase(0.5f, 6.75f)]   // 9 · (1 − 0.25)
    [TestCase(1f, 0f)]
    [TestCase(1.2f, 0f)]      // above BoostMaxSpeed boost never brakes
    public void BoostAcceleration_FadesToZeroAtBoostMaxSpeed(float speedFraction, float expected)
    {
        // Arrange
        ArchetypeParams p = ArchetypeParams.Derive(Stars.Pickup, V);
        float vMax = V.BoostMaxSpeedRatio * p.TopSpeed;
        // Act
        float a = VehicleModel.BoostAcceleration(speedFraction * vMax, vMax, V.BoostAccel);
        // Assert
        Assert.AreEqual(expected, a, 1e-3f);
    }

    [Test]
    public void BoostMaxSpeed_PickupClearsRamSpeed()
    {
        // Arrange / Act
        var model = NewPickup(out ArchetypeParams p);
        // Assert: 1.35 × 21.5 = 29.0 m/s ≈ 65 MPH, above the 50 MPH (22.4 m/s) Ram & Unblock threshold.
        Assert.AreEqual(29.025f, model.BoostMaxSpeed, 1e-3f);
        Assert.Greater(model.BoostMaxSpeed, 22.4f);
    }

    [Test]
    public void HoldingBoost_DrainsAndPushesForward()
    {
        // Arrange
        var model = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.Input.Boost = true;
        var output = new VehicleStepOutput();
        // Act: one second of boost at rest.
        for (int i = 0; i < 50; i++) model.Step(input, Grounded(Vector3.zero), output);
        // Assert: −33/s drain, no passive trickle while boosting → ≈ 67 left.
        Assert.IsTrue(model.BoostActive);
        Assert.AreEqual(100f - V.BoostDrain, model.BoostMeter, 0.5f);
        Assert.Greater(Vector3.Dot(output.CenterAcceleration, Vector3.forward), 8f);
    }

    [Test]
    public void Boost_CannotStartBelowMinimum_ButRunsToEmpty()
    {
        // Arrange: drain to empty.
        var model = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.Input.Boost = true;
        var output = new VehicleStepOutput();
        WheelContact[] ground = Grounded(Vector3.zero);
        for (int i = 0; i < 400 && model.BoostMeter > 0f; i++) model.Step(input, ground, output);
        model.Step(input, ground, output); // still held at 0: boost cuts out, trickle resumes
        bool ranToEmpty = !model.BoostActive && model.BoostMeter < 0.1f;
        // Act: release, regen to just under 5, press again.
        input.Input.Boost = false;
        int regenSteps = Mathf.FloorToInt((V.BoostMinStart - 0.5f) / (V.BoostPassiveRegen * Dt));
        for (int i = 0; i < regenSteps; i++) model.Step(input, ground, output);
        input.Input.Boost = true;
        model.Step(input, ground, output);
        // Assert
        Assert.IsTrue(ranToEmpty, "a held boost runs the meter to empty, then stops");
        Assert.IsFalse(model.BoostActive, "boost cannot start below BoostMinStart");
    }

    [Test]
    public void Boost_DisabledWhenCritical()
    {
        // Arrange
        var model = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.Input.Boost = true;
        input.Damage = DamageStage.Critical;
        var output = new VehicleStepOutput();
        // Act
        model.Step(input, Grounded(Vector3.zero), output);
        // Assert
        Assert.IsFalse(model.BoostActive);
        Assert.AreEqual(100f, model.BoostMeter, 0.5f);
    }

    // ---------- E1 / E2 refill gates ----------

    [Test]
    public void AirtimeBelowMinHeight_RefillsOnlyPassively()
    {
        // Arrange: start at 50 so refills are visible; jump-apex clearance 1.5 m (E2).
        float bunnyHop = Refill(clearance: 1.5f);
        float realAir = Refill(clearance: 2.5f);
        ArchetypeParams p = ArchetypeParams.Derive(Stars.Pickup, V);
        // Assert: per second of airtime.
        Assert.AreEqual(V.BoostPassiveRegen, bunnyHop, 0.2f);
        Assert.AreEqual(V.BoostPassiveRegen + V.RefillAir * p.StyleRefillScale, realAir, 0.2f);
    }

    [Test]
    public void NearMissRefill_AddsScaledAmount()
    {
        // Arrange
        var model = NewPickup(out ArchetypeParams p);
        DrainTo(model, 50f);
        float before = model.BoostMeter;
        // Act
        model.AddNearMiss();
        // Assert
        Assert.AreEqual(before + V.RefillNearMiss * p.StyleRefillScale, model.BoostMeter, 1e-3f);
    }

    // ---------- E3 near-miss ----------

    [Test]
    public void NearMiss_PassThroughBandAtSpeed_AwardsOnceThenCoolsDown()
    {
        // Arrange: damage radius 3 m, band to 9 m.
        var t = new NearMissTracker(V.NearMissMargin, V.NearMissMinSpeed, V.NearMissCooldown);
        // Act
        bool first = Pass(t, 0f, speed: 12f, closest: 5f);
        bool again = Pass(t, 1f, speed: 12f, closest: 5f);
        bool later = Pass(t, 1f + V.NearMissCooldown + 0.1f, speed: 12f, closest: 5f);
        // Assert
        Assert.IsTrue(first);
        Assert.IsFalse(again, "same funnel within the cooldown adds zero");
        Assert.IsTrue(later);
    }

    [Test]
    public void NearMiss_TooSlowOrTouchedFunnel_AwardsNothing()
    {
        // Arrange
        var slow = new NearMissTracker(V.NearMissMargin, V.NearMissMinSpeed, V.NearMissCooldown);
        var hit = new NearMissTracker(V.NearMissMargin, V.NearMissMinSpeed, V.NearMissCooldown);
        var forming = new NearMissTracker(V.NearMissMargin, V.NearMissMinSpeed, V.NearMissCooldown);
        // Act / Assert
        Assert.IsFalse(Pass(slow, 0f, speed: 6f, closest: 5f), "E3: requires > 8 m/s");
        Assert.IsFalse(Pass(hit, 0f, speed: 12f, closest: 2f), "inside the damage radius is a hit");
        Assert.IsFalse(Pass(forming, 0f, speed: 12f, closest: 5f, damageRadius: 0f), "harmless funnel");
    }

    // ---------- F7 air control ----------

    [Test]
    public void AirControl_PitchInput_AppliesAirAccelAboutRightAxis()
    {
        // Arrange
        var model = NewPickup(out ArchetypeParams p);
        var input = Input(Vector3.zero);
        input.Input.Air = new Vector2(0f, 1f);
        var output = new VehicleStepOutput();
        // Act
        model.Step(input, Airborne(), output);
        // Assert: 20 rad/s² × TrickScale (Pickup 1.0), nose-down about +X.
        Assert.AreEqual(V.AirAccel * p.TrickScale, Vector3.Dot(output.AngularAcceleration, Vector3.right), 1e-3f);
    }

    [Test]
    public void AirControl_CapsAngularSpeed()
    {
        // Arrange: already spinning at the cap.
        var model = NewPickup(out _);
        var input = Input(Vector3.zero);
        input.AngularVelocity = Vector3.right * V.AirMaxRate;
        input.Input.Air = new Vector2(0f, 1f);
        var output = new VehicleStepOutput();
        // Act
        model.Step(input, Airborne(), output);
        // Assert: no further acceleration along the spin.
        Vector3 next = input.AngularVelocity + output.AngularAcceleration * Dt;
        Assert.LessOrEqual(next.magnitude, V.AirMaxRate + 1e-3f);
    }

    // ---------- helpers ----------

    private static bool Pass(NearMissTracker t, float startTime, float speed, float closest, float damageRadius = 3f)
    {
        bool award = false;
        float[] path = { 20f, 12f, closest, 12f, 20f };
        for (int i = 0; i < path.Length; i++)
        {
            t.BeginStep();
            award |= t.Observe(7, path[i], damageRadius, speed, startTime + i * 0.1f);
            t.EndStep();
        }
        return award;
    }

    // Boost gained per second while airborne at a fixed clearance, starting from a half meter.
    private static float Refill(float clearance)
    {
        var model = NewPickup(out _);
        DrainTo(model, 50f);
        var input = Input(Vector3.zero);
        input.GroundClearance = clearance;
        var output = new VehicleStepOutput();
        WheelContact[] air = Airborne();
        for (int i = 0; i < 10; i++) model.Step(input, air, output); // past the 0.1 s airborne grace
        float before = model.BoostMeter;
        for (int i = 0; i < 50; i++) model.Step(input, air, output);
        return model.BoostMeter - before;
    }

    private static void DrainTo(VehicleModel model, float target)
    {
        var input = Input(Vector3.zero);
        input.Input.Boost = true;
        var output = new VehicleStepOutput();
        WheelContact[] ground = Grounded(Vector3.zero);
        while (model.BoostMeter > target) model.Step(input, ground, output);
    }

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
        Damage = DamageStage.Healthy,
    };

    private static WheelContact[] Airborne() => new WheelContact[VehicleModel.WheelCount];

    private static WheelContact[] Grounded(Vector3 velocity)
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
                GroundDistance = RestGap,
                Point = new Vector3(xs[i], -RestGap, zs[i]),
                Normal = Vector3.up,
                PointVelocity = velocity,
                SurfaceGrip = 1f,
            };
        }
        return c;
    }
}
