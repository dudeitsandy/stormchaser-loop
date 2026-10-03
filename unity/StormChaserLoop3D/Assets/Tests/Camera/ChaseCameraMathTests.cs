using NUnit.Framework;
using UnityEngine;

/// <summary>
/// vehicle-feel.md Core Rule 8 / F14 / E11 and photo-scoring.md's framing curve (S7-05).
/// </summary>
public class ChaseCameraMathTests
{
    private const float VTop = 21.5f;
    private const float Lag = 10f;
    private const float Sway = 3f;
    private const float MaxOffset = 8f;

    [TestCase(0f, 1f)]
    [TestCase(7.5f, 0.5f)]
    [TestCase(15f, 0f)]
    [TestCase(40f, 0f)]
    [TestCase(-7.5f, 0.5f)]
    public void AimScore_FramingCurve_MatchesClamp01OneMinusThetaOverHalfAngle(float angle, float expected)
    {
        Assert.AreEqual(expected, ScoringSystem.AimScore(angle), 1e-4f);
    }

    [Test]
    public void AimScore_FullStormCamOffset_LandsGoodNotPerfect()
    {
        // F14: the full 8° offset leaves AimScore ≈ 0.47; at the optimal distance that is GOOD.
        float aim = ScoringSystem.AimScore(MaxOffset);
        float quality = ScoringSystem.CalculateQuality(aim, 1f);

        Assert.AreEqual(0.467f, aim, 0.001f);
        Assert.AreEqual(ShotTier.Good, ScoringSystem.GetTier(quality));
    }

    [Test]
    public void AimScore_Centered_AtOptimalDistance_IsPerfect()
    {
        float quality = ScoringSystem.CalculateQuality(ScoringSystem.AimScore(0f), 1f);
        Assert.AreEqual(ShotTier.Perfect, ScoringSystem.GetTier(quality));
    }

    [Test]
    public void StormCamOffset_FastCrossing_ClampsToMaxOffset()
    {
        float right = ChaseCameraMath.StormCamOffset(VTop * 2f, VTop, 0f, Lag, Sway, MaxOffset);
        float left = ChaseCameraMath.StormCamOffset(-VTop * 2f, VTop, 0f, Lag, Sway, MaxOffset);

        Assert.AreEqual(MaxOffset, right, 1e-4f);
        Assert.AreEqual(-MaxOffset, left, 1e-4f);
    }

    [Test]
    public void StormCamOffset_NoCrossing_IsSwayOnly()
    {
        // sin(0.7 t) = 1 at t = π / 1.4.
        float t = Mathf.PI / 1.4f;
        Assert.AreEqual(Sway, ChaseCameraMath.StormCamOffset(0f, VTop, t, Lag, Sway, MaxOffset), 1e-4f);
        Assert.AreEqual(0f, ChaseCameraMath.StormCamOffset(0f, VTop, 0f, Lag, Sway, MaxOffset), 1e-4f);
    }

    [Test]
    public void StormCamOffset_HalfTopSpeedCrossing_IsHalfLag()
    {
        Assert.AreEqual(Lag * 0.5f, ChaseCameraMath.StormCamOffset(VTop * 0.5f, VTop, 0f, Lag, Sway, MaxOffset), 1e-4f);
    }

    [Test]
    public void PerpendicularSpeed_MovingRightAcrossLine_IsPositive()
    {
        Vector3 toFunnel = Vector3.forward * 30f;

        Assert.AreEqual(10f, ChaseCameraMath.PerpendicularSpeed(Vector3.right * 10f, toFunnel), 1e-4f);
        Assert.AreEqual(-10f, ChaseCameraMath.PerpendicularSpeed(Vector3.left * 10f, toFunnel), 1e-4f);
        Assert.AreEqual(0f, ChaseCameraMath.PerpendicularSpeed(Vector3.forward * 10f, toFunnel), 1e-4f);
    }

    [Test]
    public void RestYaw_MovingForward_FollowsVelocity()
    {
        float yaw = ChaseCameraMath.RestYaw(new Vector3(10f, 0f, 10f), Vector3.forward, 0f, 3f);
        Assert.AreEqual(45f, yaw, 1e-3f);
    }

    [Test]
    public void RestYaw_Reversing_StaysBehindFacing()
    {
        // Reversing at 8 m/s must not flip the camera to the front of the truck.
        float yaw = ChaseCameraMath.RestYaw(Vector3.back * 8f, Vector3.forward, 0f, 3f);
        Assert.AreEqual(0f, yaw, 1e-3f);
    }

    [Test]
    public void RestYaw_BelowMinSpeed_UsesFacing()
    {
        float yaw = ChaseCameraMath.RestYaw(Vector3.right * 2f, Vector3.forward, 90f, 3f);
        Assert.AreEqual(0f, yaw, 1e-3f);
    }

    [Test]
    public void RestYaw_FacingNearVertical_KeepsPreviousYaw()
    {
        float yaw = ChaseCameraMath.RestYaw(Vector3.zero, new Vector3(0.05f, 1f, 0f), 123f, 3f);
        Assert.AreEqual(123f, yaw, 1e-3f);
    }

    [TestCase(40f, 29f, true)]
    [TestCase(40f, 30f, false)]
    [TestCase(40f, 35f, false)]
    public void ShouldSwitchTarget_RequiresTwentyFivePercentCloser(float current, float candidate, bool expected)
    {
        Assert.AreEqual(expected, ChaseCameraMath.ShouldSwitchTarget(current, candidate, 0.75f));
    }

    [Test]
    public void DampAngle_AfterDampTime_CoversNinetyNinePercentAlongShortestArc()
    {
        // 350° → 10° is a 20° turn through north, not 340° the long way.
        float yaw = 350f;
        for (int i = 0; i < 100; i++) yaw = ChaseCameraMath.DampAngle(yaw, 10f, 1f, 0.01f);

        Assert.AreEqual(0f, Mathf.DeltaAngle(yaw, 10f), 0.25f);
    }

    [Test]
    public void FlatAngle_IgnoresHeightDifference()
    {
        Assert.AreEqual(0f, ChaseCameraMath.FlatAngle(new Vector3(0f, -0.5f, 1f), new Vector3(0f, 40f, 20f)), 1e-3f);
        Assert.AreEqual(90f, ChaseCameraMath.FlatAngle(Vector3.forward, Vector3.right), 1e-3f);
    }
}
