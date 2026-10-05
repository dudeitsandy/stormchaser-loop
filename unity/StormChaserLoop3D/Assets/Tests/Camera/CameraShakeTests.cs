using NUnit.Framework;
using UnityEngine;

/// <summary>Crash-weight camera shake (Sprint 9, Andy 2026-10-05): low-end jolts on impacts, hard landings, tosses.</summary>
public class CameraShakeTests
{
    private const float Dt = 1f / 60f;
    private static readonly CameraShakeTuning T = CameraShakeTuning.Defaults;

    /// <summary>Largest tilt and offset over the whole kick, sampled per frame.</summary>
    private static (float tilt, float offset) Peak(CameraShake shake, float seconds = 1f)
    {
        float tilt = 0f, offset = 0f;
        for (float t = 0f; t < seconds; t += Dt)
        {
            shake.Advance(Dt, out Vector3 r, out Vector3 o);
            tilt = Mathf.Max(tilt, r.magnitude);
            offset = Mathf.Max(offset, o.magnitude);
        }
        return (tilt, offset);
    }

    [Test]
    public void Impact_NoHpLoss_DoesNotShake()
    {
        // Arrange
        var shake = new CameraShake(T);
        // Act: scrapes and fences report impacts without HP loss
        shake.OnImpact(0);
        // Assert
        Assert.AreEqual(0, shake.ActiveKicks);
    }

    [Test]
    public void SevereImpact_ShakesHarderThanLight_BothWithinTheirPeaks()
    {
        // Arrange
        var light = new CameraShake(T);
        var severe = new CameraShake(T);
        // Act
        light.OnImpact(1);
        severe.OnImpact(2);
        (float lightTilt, float lightOffset) = Peak(light);
        (float severeTilt, float severeOffset) = Peak(severe);
        // Assert
        Assert.Greater(severeTilt, lightTilt);
        Assert.Greater(severeOffset, lightOffset);
        Assert.LessOrEqual(lightTilt, T.LightTiltDeg + 1e-4f);
        Assert.LessOrEqual(severeTilt, T.SevereTiltDeg + 1e-4f);
        Assert.Greater(severeTilt, 0.5f * T.SevereTiltDeg, "noticeable, not lost under the envelope");
    }

    [Test]
    public void NormalJumpLanding_DoesNotShake_HardLandingDoes()
    {
        // Arrange: a full jump lands at ≈ 8.4 m/s (√(2 · 2.4 g · 1.5 m)); a toss comes down far harder.
        var jump = new CameraShake(T);
        var hard = new CameraShake(T);
        // Act
        jump.OnLanding(8.4f);
        hard.OnLanding(16f);
        // Assert
        Assert.AreEqual(0, jump.ActiveKicks);
        Assert.AreEqual(1, hard.ActiveKicks);
        Assert.LessOrEqual(Peak(hard).tilt, T.LandingTiltMax + 1e-4f);
    }

    [Test]
    public void Kick_FadesToZero_AndIsRemovedAfterItsDuration()
    {
        // Arrange
        var shake = new CameraShake(T);
        shake.OnToss();
        // Act: just past the toss duration
        for (float t = 0f; t < T.TossSeconds + 2f * Dt; t += Dt) shake.Advance(Dt, out _, out _);
        shake.Advance(Dt, out Vector3 tilt, out Vector3 offset);
        // Assert
        Assert.AreEqual(0, shake.ActiveKicks);
        Assert.AreEqual(Vector3.zero, tilt);
        Assert.AreEqual(Vector3.zero, offset);
        Assert.AreEqual(1f, CameraShake.Envelope(0f, 0.3f), 1e-6f);
        Assert.AreEqual(0f, CameraShake.Envelope(0.3f, 0.3f), 1e-6f);
    }

    [Test]
    public void ManyOverlappingKicks_StayUnderTheCap()
    {
        // Arrange: a pile-up (two severe hits, a toss and a hard landing in the same frame)
        var shake = new CameraShake(T);
        shake.OnImpact(2);
        shake.OnImpact(2);
        shake.OnToss();
        shake.OnLanding(20f);
        // Act
        (float tilt, float offset) = Peak(shake);
        // Assert
        Assert.LessOrEqual(tilt, T.MaxTiltDeg + 1e-4f);
        Assert.LessOrEqual(offset, T.MaxOffset + 1e-4f);
    }

    [Test]
    public void Clear_DropsEveryKick()
    {
        // Arrange
        var shake = new CameraShake(T);
        shake.OnImpact(2);
        shake.OnToss();
        // Act
        shake.Clear();
        shake.Advance(Dt, out Vector3 tilt, out _);
        // Assert
        Assert.AreEqual(0, shake.ActiveKicks);
        Assert.AreEqual(Vector3.zero, tilt);
    }
}
