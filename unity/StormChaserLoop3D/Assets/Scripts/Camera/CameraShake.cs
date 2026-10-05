using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Crash-weight shake values (Andy 2026-10-05: crashes, flips and tosses need tactile feedback; kept low).</summary>
[Serializable]
public struct CameraShakeTuning
{
    [Tooltip("Light impact (1 HP): peak tilt (deg), peak offset (m), seconds.")]
    public float LightTiltDeg, LightOffset, LightSeconds;
    [Tooltip("Severe impact (2+ HP): peak tilt (deg), peak offset (m), seconds.")]
    public float SevereTiltDeg, SevereOffset, SevereSeconds;
    [Tooltip("Landing: no shake below MinSpeed (m/s, a normal jump lands at ≈ 8); scales to the max at MaxSpeed.")]
    public float LandingMinSpeed, LandingMaxSpeed, LandingTiltMin, LandingTiltMax, LandingOffsetMin, LandingOffsetMax, LandingSeconds;
    [Tooltip("Toss launch: peak tilt (deg), peak offset (m), seconds.")]
    public float TossTiltDeg, TossOffset, TossSeconds;
    [Tooltip("Cap on the summed shake, however many kicks overlap.")]
    public float MaxTiltDeg, MaxOffset;

    public static CameraShakeTuning Defaults => new CameraShakeTuning
    {
        LightTiltDeg = 0.4f, LightOffset = 0.04f, LightSeconds = 0.25f,
        SevereTiltDeg = 0.9f, SevereOffset = 0.10f, SevereSeconds = 0.35f,
        LandingMinSpeed = 9f, LandingMaxSpeed = 16f, LandingTiltMin = 0.3f, LandingTiltMax = 0.7f,
        LandingOffsetMin = 0.03f, LandingOffsetMax = 0.08f, LandingSeconds = 0.3f,
        TossTiltDeg = 0.6f, TossOffset = 0.06f, TossSeconds = 0.4f,
        MaxTiltDeg = 1.2f, MaxOffset = 0.12f,
    };
}

/// <summary>What a kick shakes: impacts roll and slide sideways, landings pitch and drop, tosses do both.</summary>
public enum ShakeKind { Impact, Landing, Toss }

/// <summary>
/// Decaying camera jolts for crash weight (Sprint 9). Each kick is a fixed-frequency oscillation under a
/// (1 − t/d)² envelope; overlapping kicks add, then the sum is capped. Deterministic: no random phases.
/// Pure C#, so the envelope and cap are EditMode-testable.
/// </summary>
public sealed class CameraShake
{
    private struct Kick
    {
        public ShakeKind Kind;
        public float Tilt, Offset, Duration, Age;
    }

    // Distinct frequencies per axis so a jolt reads as a knock, not a regular wobble (Hz).
    private const float RollHz = 13f, PitchHz = 11f, SideHz = 9f, DropHz = 8f;

    private readonly List<Kick> _kicks = new List<Kick>();
    private readonly CameraShakeTuning _t;

    public CameraShake(CameraShakeTuning tuning) => _t = tuning;

    /// <summary>Live kicks (finished ones are removed by <see cref="Advance"/>).</summary>
    public int ActiveKicks => _kicks.Count;

    /// <summary>A reported collision: no shake without HP loss (scrapes, fences), light at 1 HP, severe at 2+.</summary>
    public void OnImpact(int hpLoss)
    {
        if (hpLoss <= 0) return;
        if (hpLoss == 1) Add(ShakeKind.Impact, _t.LightTiltDeg, _t.LightOffset, _t.LightSeconds);
        else Add(ShakeKind.Impact, _t.SevereTiltDeg, _t.SevereOffset, _t.SevereSeconds);
    }

    /// <summary>A touchdown at <paramref name="speed"/> m/s: nothing below the minimum, then scaled to the max.</summary>
    public void OnLanding(float speed)
    {
        if (speed < _t.LandingMinSpeed) return;
        float k = Mathf.InverseLerp(_t.LandingMinSpeed, _t.LandingMaxSpeed, speed);
        Add(ShakeKind.Landing, Mathf.Lerp(_t.LandingTiltMin, _t.LandingTiltMax, k),
            Mathf.Lerp(_t.LandingOffsetMin, _t.LandingOffsetMax, k), _t.LandingSeconds);
    }

    /// <summary>The truck is thrown by a funnel.</summary>
    public void OnToss() => Add(ShakeKind.Toss, _t.TossTiltDeg, _t.TossOffset, _t.TossSeconds);

    /// <summary>Drops every kick (run start, teleport).</summary>
    public void Clear() => _kicks.Clear();

    private void Add(ShakeKind kind, float tilt, float offset, float duration)
    {
        if (duration <= 0f) return;
        _kicks.Add(new Kick { Kind = kind, Tilt = tilt, Offset = offset, Duration = duration });
    }

    /// <summary>
    /// Advances every kick by <paramref name="dt"/> and returns the summed, capped shake: tilt as camera-local
    /// Euler degrees (pitch, 0, roll) and offset in camera-local metres (side, drop, 0).
    /// </summary>
    public void Advance(float dt, out Vector3 tiltDeg, out Vector3 offset)
    {
        tiltDeg = Vector3.zero;
        offset = Vector3.zero;
        for (int i = _kicks.Count - 1; i >= 0; i--)
        {
            Kick k = _kicks[i];
            k.Age += Mathf.Max(0f, dt);
            if (k.Age >= k.Duration) { _kicks.RemoveAt(i); continue; }
            _kicks[i] = k;
            float env = Envelope(k.Age, k.Duration);
            float a = k.Age * 2f * Mathf.PI;
            bool rolls = k.Kind != ShakeKind.Landing, pitches = k.Kind != ShakeKind.Impact;
            if (rolls) tiltDeg.z += k.Tilt * env * Mathf.Sin(a * RollHz);
            if (pitches) tiltDeg.x += k.Tilt * env * Mathf.Sin(a * PitchHz);
            if (rolls) offset.x += k.Offset * env * Mathf.Sin(a * SideHz);
            if (pitches) offset.y -= k.Offset * env * Mathf.Abs(Mathf.Sin(a * DropHz)); // drops, never bobs up
        }
        tiltDeg = Vector3.ClampMagnitude(tiltDeg, _t.MaxTiltDeg);
        offset = Vector3.ClampMagnitude(offset, _t.MaxOffset);
    }

    /// <summary>(1 − t/d)²: full at the hit, gone at <paramref name="duration"/>, no step at either end of the fade.</summary>
    public static float Envelope(float age, float duration)
    {
        if (duration <= 0f || age >= duration) return 0f;
        float r = 1f - Mathf.Max(0f, age) / duration;
        return r * r;
    }
}
