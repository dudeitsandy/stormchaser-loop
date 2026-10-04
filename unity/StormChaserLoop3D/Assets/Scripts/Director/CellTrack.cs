using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>F5 track tuning (storm-director.md). Placeholders pending playtest.</summary>
[Serializable]
public class TrackTuning
{
    [Tooltip("Fixed integration substep in seconds (frame-rate independence, AC-23).")]
    public float Substep = 0.05f;
    [Tooltip("Noise sample rate: n = 2 · noise(seed, NoiseRate · t) − 1.")]
    public float NoiseRate = 0.25f;
    [Tooltip("Initial heading within ± this many degrees of the bearing to world centre.")]
    public float InitialHeadingSpread = 60f;
    [Tooltip("Track speed multiplier at Heat 1+ (Cataclysm rank 1).")]
    public float HeatSpeedMultiplier = 1.25f;
    [Tooltip("Jog rate λ0 per second at end of life.")]
    public float JogRate = 1f / 12f;
    [Tooltip("Jogs only start after this fraction of the lifetime.")]
    public float JogStartFraction = 0.6f;
    public Vector2 JogTurnDeg = new Vector2(40f, 110f);
    public float JogTelegraph = 1f;
    public float JogTurnSeconds = 1.5f;
    public float JogBurstSeconds = 3f;
    public float JogBurstMultiplier = 1.5f;
    public float JogRefractory = 4.5f;
    [Tooltip("Jog speed cap: 0.7 × Pickup top speed.")]
    public float SpeedCap = 15.05f;

    public static TrackTuning Defaults => new TrackTuning();
}

/// <summary>One pre-rolled late-life jog: a lean telegraph, then a turn with a speed burst.</summary>
public readonly struct TrackJog
{
    /// <summary>Telegraph (funnel lean) starts, cell age in seconds.</summary>
    public readonly float Start;
    /// <summary>Turn starts (Start + telegraph).</summary>
    public readonly float TurnStart;
    /// <summary>Signed heading change in degrees.</summary>
    public readonly float TurnDeg;

    public TrackJog(float start, float turnStart, float turnDeg)
    {
        Start = start;
        TurnStart = turnStart;
        TurnDeg = turnDeg;
    }
}

/// <summary>
/// One cell's track (storm-director.md F5): hash-noise wander, Heat speed multiplier, intensity-scaled speed,
/// pre-rolled late-life jogs and compact edge steer-back, integrated in fixed substeps so a seed replays the
/// same on any frame rate. Takes no player input (Rule 5: no homing).
/// </summary>
public sealed class CellTrack
{
    /// <summary>RNG sub-stream base for tracks (each cell adds its id).</summary>
    public const ulong TrackStreamBase = 100;

    private readonly TrackTuning _t;
    private readonly PlannedCell _cell;
    private readonly float _baseSpeed;
    private readonly float _turnRate;
    private readonly Vector2 _centre;
    private readonly float _halfExtent;
    private readonly uint _noiseSeed;
    private readonly List<TrackJog> _jogs = new List<TrackJog>();
    private float _steps; // substeps taken
    private float _jogApplied; // heading already applied for the jog in progress

    /// <summary>Current position on the ground plane (x, z).</summary>
    public Vector2 Position { get; private set; }
    /// <summary>Current heading in degrees (0 = +Z).</summary>
    public float HeadingDeg { get; private set; }
    /// <summary>Age the track has been integrated to, in seconds.</summary>
    public float Age => _steps * _t.Substep;
    /// <summary>Plan-time lifetime (after cap resolution): early rope-out shortens Rope to I0 · Rope.</summary>
    public float Lifetime { get; }
    /// <summary>Pre-rolled jogs in order.</summary>
    public IReadOnlyList<TrackJog> Jogs => _jogs;

    public CellTrack(PlannedCell cell, long runSeed, int heat, float trackSpeed, float turnRateDeg, Vector2 worldCentre,
                     float halfExtent, TrackTuning tuning)
    {
        _t = tuning;
        _cell = cell;
        _baseSpeed = trackSpeed * (heat >= 1 ? tuning.HeatSpeedMultiplier : 1f);
        _turnRate = turnRateDeg;
        _centre = worldCentre;
        _halfExtent = halfExtent;
        Lifetime = cell.EarlyRopeTime >= 0f
            ? cell.EarlyRopeTime - cell.SpawnTime + cell.EarlyRopeStartIntensity * cell.Rope
            : cell.Form + cell.Mature + cell.Rope;

        var rng = new DirectorRng(unchecked((ulong)runSeed), TrackStreamBase + (ulong)Math.Max(0, cell.Id));
        _noiseSeed = rng.NextUInt();
        Position = cell.Position;
        Vector2 toCentre = worldCentre - cell.Position;
        float bearing = toCentre.sqrMagnitude > 1e-6f ? Mathf.Atan2(toCentre.x, toCentre.y) * Mathf.Rad2Deg : 0f;
        HeadingDeg = bearing + rng.Range(-tuning.InitialHeadingSpread, tuning.InitialHeadingSpread);
        PreRollJogs(rng);
    }

    /// <summary>Integrates the track to <paramref name="age"/> in fixed substeps (never backwards).</summary>
    public void AdvanceTo(float age)
    {
        int target = Mathf.FloorToInt(age / _t.Substep + 1e-4f);
        while (_steps < target) Step();
    }

    /// <summary>True while a jog's 1 s lean telegraph plays; <paramref name="lean"/> is the turn's sign (−1 / +1).</summary>
    public bool IsTelegraphingAt(float age, out float lean)
    {
        foreach (TrackJog jog in _jogs)
        {
            if (age >= jog.Start && age < jog.TurnStart)
            {
                lean = Mathf.Sign(jog.TurnDeg);
                return true;
            }
        }
        lean = 0f;
        return false;
    }

    /// <summary>Lifecycle intensity at <paramref name="age"/> (linear Form ramp, 1 Mature, linear Rope decline).</summary>
    public float IntensityAt(float age)
    {
        PlannedCell c = _cell;
        if (c.EarlyRopeTime >= 0f)
        {
            float ropeAge = c.EarlyRopeTime - c.SpawnTime;
            if (age >= ropeAge) return StormScale.EarlyRopeIntensity(c.EarlyRopeStartIntensity, age - ropeAge, c.Rope);
        }
        if (age < c.Form) return Mathf.Clamp01(age / c.Form);
        if (age < c.Form + c.Mature) return 1f;
        return Mathf.Clamp01(1f - (age - c.Form - c.Mature) / c.Rope);
    }

    private void Step()
    {
        float dt = _t.Substep;
        float age = Age;

        // Wander: dθ/dt = n(t) · ω, n in [−1, 1] from seeded hash noise.
        float n = 2f * HashNoise.Value(_noiseSeed, _t.NoiseRate * age) - 1f;
        float heading = HeadingDeg + n * _turnRate * dt;

        float speed = _baseSpeed * Mathf.Lerp(0.4f, 1f, IntensityAt(age));
        foreach (TrackJog jog in _jogs)
        {
            float into = age - jog.TurnStart;
            if (into < 0f || into >= _t.JogBurstSeconds) continue;
            // Eased turn: apply the change in smoothstep(0..1) over the turn window.
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((into + dt) / _t.JogTurnSeconds));
            float before = into <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(into / _t.JogTurnSeconds));
            heading += jog.TurnDeg * (eased - before);
            speed *= _t.JogBurstMultiplier;
            break;
        }
        speed = Mathf.Min(speed, _t.SpeedCap);

        // Compact edge steer-back: outside the arena, head for the centre.
        Vector2 p = Position;
        if (Mathf.Abs(p.x - _centre.x) > _halfExtent || Mathf.Abs(p.y - _centre.y) > _halfExtent)
        {
            Vector2 back = _centre - p;
            heading = Mathf.Atan2(back.x, back.y) * Mathf.Rad2Deg;
        }

        HeadingDeg = heading;
        float rad = heading * Mathf.Deg2Rad;
        Position = p + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * (speed * dt);
        _steps++;
    }

    // Poisson jogs by thinning over the late-life window, rate λ0 · clamp01((u − 0.6) / 0.4), with refractory.
    private void PreRollJogs(DirectorRng rng)
    {
        if (Lifetime <= 0f || _t.JogRate <= 0f) return;
        float t = _t.JogStartFraction * Lifetime;
        float lastStart = float.NegativeInfinity;
        while (true)
        {
            float u01 = Mathf.Max(1e-7f, rng.NextFloat01());
            t += -Mathf.Log(u01) / _t.JogRate;
            if (t >= Lifetime) break;
            float u = t / Lifetime;
            float rate = Mathf.Clamp01((u - _t.JogStartFraction) / (1f - _t.JogStartFraction));
            bool accept = rng.NextFloat01() < rate;
            float turn = rng.Range(_t.JogTurnDeg.x, _t.JogTurnDeg.y) * (rng.NextFloat01() < 0.5f ? -1f : 1f);
            if (!accept || t - lastStart < _t.JogRefractory) continue;
            _jogs.Add(new TrackJog(t, t + _t.JogTelegraph, turn));
            lastStart = t;
        }
    }
}

/// <summary>Seeded 1D value noise in [0, 1] (smooth, deterministic on every platform; not Mathf.PerlinNoise).</summary>
public static class HashNoise
{
    public static float Value(uint seed, float x)
    {
        float fl = Mathf.Floor(x);
        int i = (int)fl;
        float f = x - fl;
        float a = Hash01(seed, i), b = Hash01(seed, i + 1);
        float s = f * f * (3f - 2f * f);
        return a + (b - a) * s;
    }

    private static float Hash01(uint seed, int i)
    {
        uint h = unchecked(seed ^ ((uint)i * 0x9E3779B1u));
        h ^= h >> 16;
        h = unchecked(h * 0x7FEB352Du);
        h ^= h >> 15;
        h = unchecked(h * 0x846CA68Bu);
        h ^= h >> 16;
        return (h >> 8) * (1f / 16777216f);
    }
}
