using System;
using UnityEngine;

/// <summary>Quality tier of a photo, derived from framing quality (not raw score) so every EF class can earn PERFECT.</summary>
public enum ShotTier { Glancing, Good, Perfect }

/// <summary>Everything presentation code needs to react to a photo being taken.</summary>
public readonly struct PhotoResult
{
    public readonly float Score;
    public readonly float AimScore;
    public readonly float DistanceScore;
    public readonly float Quality;
    public readonly ShotTier Tier;
    public readonly DisasterEntity Subject;
    public readonly Vector3 SubjectPosition;
    /// <summary>Diminishing-returns multiplier for repeat shots of the same subject (1 = fresh).</summary>
    public readonly float RepeatMultiplier;
    /// <summary>"IN THE WIND" bonus multiplier (1 = no wind).</summary>
    public readonly float WindMultiplier;

    public PhotoResult(float score, float aimScore, float distanceScore, float quality, ShotTier tier,
        DisasterEntity subject, Vector3 subjectPosition, float repeatMultiplier = 1f, float windMultiplier = 1f)
    {
        Score = score;
        AimScore = aimScore;
        DistanceScore = distanceScore;
        Quality = quality;
        Tier = tier;
        Subject = subject;
        SubjectPosition = subjectPosition;
        RepeatMultiplier = repeatMultiplier;
        WindMultiplier = windMultiplier;
    }

    /// <summary>True if the wind bonus applied meaningfully (show "IN THE WIND").</summary>
    public bool InTheWind => WindMultiplier > 1.05f;
    /// <summary>True if this shot was heavily discounted as a repeat (show "SAME SHOT").</summary>
    public bool IsStaleRepeat => RepeatMultiplier < 0.6f;
}

/// <summary>End-of-run tally handed to the results screen.</summary>
public readonly struct RunSummary
{
    public readonly float Score;
    public readonly int PhotosTaken;
    public readonly float BestShot;
    public readonly bool Wrecked;
    public readonly float PreviousBest;
    /// <summary>Storm Director facts for the results screen (default when the legacy spawner ran).</summary>
    public readonly StormRunInfo Storm;

    public RunSummary(float score, int photosTaken, float bestShot, bool wrecked, float previousBest,
                      StormRunInfo storm = default)
    {
        Score = score;
        PhotosTaken = photosTaken;
        BestShot = bestShot;
        Wrecked = wrecked;
        PreviousBest = previousBest;
        Storm = storm;
    }

    public bool IsNewBest => Score > PreviousBest;
}

/// <summary>Run-level storm facts for the results screen (storm-director.md UI Requirements, story 009).</summary>
public readonly struct StormRunInfo
{
    /// <summary>False when no director ran (legacy spawner): the results screen omits the storm block.</summary>
    public readonly bool Valid;
    public readonly long Seed;
    public readonly string BuildVersion;
    public readonly string Regime;
    /// <summary>The anchor's EF when it never reached Mature ("The big one got away"), otherwise −1.</summary>
    public readonly int BigOneGotAwayEf;
    /// <summary>True when the replay link named a different build version.</summary>
    public readonly bool VersionMismatch;

    public StormRunInfo(long seed, string buildVersion, string regime, int bigOneGotAwayEf, bool versionMismatch)
    {
        Valid = true;
        Seed = seed;
        BuildVersion = buildVersion;
        Regime = regime;
        BigOneGotAwayEf = bigOneGotAwayEf;
        VersionMismatch = versionMismatch;
    }
}

/// <summary>Role of a storm cell in the Weather Plan (storm-director.md Rule 3).</summary>
public enum StormCellRole { Anchor, CoAnchor, Satellite }

/// <summary>
/// Payload for the storm-cell lifecycle events (storm-director.md Rule 11, agreed with Codex 2026-10-02).
/// <see cref="Position"/> is the cell's position at that transition; per-frame values are a query surface.
/// </summary>
public readonly struct StormCellInfo
{
    public readonly int CellId;
    /// <summary>True EF rating 0–5 (the forecast's ±1 estimate is separate).</summary>
    public readonly int EF;
    public readonly StormCellRole Role;
    public readonly Vector3 Position;

    public StormCellInfo(int cellId, int ef, StormCellRole role, Vector3 position)
    {
        CellId = cellId;
        EF = ef;
        Role = role;
        Position = position;
    }
}

/// <summary>
/// Cross-system event hub. Gameplay raises, presentation (UI, audio, VFX) listens.
/// Presentation code must never call the Raise methods.
/// </summary>
public static class GameEvents
{
    /// <summary>Fired when a run begins (after the title screen is dismissed).</summary>
    public static event Action RunStarted;
    /// <summary>Fired for every photo that had a subject.</summary>
    public static event Action<PhotoResult> PhotoTaken;
    /// <summary>Fired when the shutter is pressed with no disaster in range (still uses a frame of film).</summary>
    public static event Action PhotoMissed;
    /// <summary>Fired when the shutter is pressed with no film left (no frame used, nothing scored).</summary>
    public static event Action OutOfFilm;
    /// <summary>Fired whenever film count changes, and at run start. Args: remaining, capacity.</summary>
    public static event Action<int, int> FilmChanged;
    /// <summary>Fired when the player vehicle takes damage. Args: current HP, max HP.</summary>
    public static event Action<int, int> PlayerDamaged;
    /// <summary>Fired once when the run ends (timer or wreck).</summary>
    public static event Action<RunSummary> RunEnded;

    // Vehicle events (ADR-0005; vehicle-feel.md Interactions). Presentation listens; only gameplay raises.
    /// <summary>Style moment: drift seconds, airtime seconds, or a near-miss (amount = 1).</summary>
    public static event Action<StyleKind, float> StyleEvent;
    /// <summary>All wheels back down after being airborne. Arg: vertical touchdown speed (m/s).</summary>
    public static event Action<float> Landed;
    /// <summary>A collision strong enough to report (severity, HP loss, kind, point).</summary>
    public static event Action<ImpactInfo> VehicleImpact;
    /// <summary>Wind lift crossed the toss threshold (vehicle-feel.md F12b).</summary>
    public static event Action Tossed;

    public static void RaiseRunStarted() => RunStarted?.Invoke();
    public static void RaisePhotoTaken(PhotoResult result) => PhotoTaken?.Invoke(result);
    public static void RaisePhotoMissed() => PhotoMissed?.Invoke();
    public static void RaiseOutOfFilm() => OutOfFilm?.Invoke();
    public static void RaiseFilmChanged(int remaining, int capacity) => FilmChanged?.Invoke(remaining, capacity);
    public static void RaisePlayerDamaged(int current, int max) => PlayerDamaged?.Invoke(current, max);
    public static void RaiseRunEnded(RunSummary summary) => RunEnded?.Invoke(summary);
    public static void RaiseStyleEvent(StyleKind kind, float amount) => StyleEvent?.Invoke(kind, amount);
    public static void RaiseLanded(float verticalSpeed) => Landed?.Invoke(verticalSpeed);
    public static void RaiseVehicleImpact(ImpactInfo impact) => VehicleImpact?.Invoke(impact);
    public static void RaiseTossed() => Tossed?.Invoke();

    // Storm Director lifecycle (storm-director.md Rule 11): Forming → Peak → RopeOut → Ended for a full
    // life; evicted while Forming: Forming → RopeOut → Ended; left the world or run ended: Ended only.
    // Every spawned cell raises Ended exactly once; a cell that never spawns raises nothing.
    /// <summary>A cell started Forming (visible, growing; no damage or lift yet).</summary>
    public static event Action<StormCellInfo> StormCellForming;
    /// <summary>A cell reached Mature (touchdown; for the anchor, the peak alert).</summary>
    public static event Action<StormCellInfo> StormCellPeak;
    /// <summary>A cell began Roping Out (normal decline or cap eviction).</summary>
    public static event Action<StormCellInfo> StormCellRopeOut;
    /// <summary>A cell is gone (Done, left the world, or the run ended).</summary>
    public static event Action<StormCellInfo> StormCellEnded;

    public static void RaiseStormCellForming(StormCellInfo cell) => StormCellForming?.Invoke(cell);
    public static void RaiseStormCellPeak(StormCellInfo cell) => StormCellPeak?.Invoke(cell);
    public static void RaiseStormCellRopeOut(StormCellInfo cell) => StormCellRopeOut?.Invoke(cell);
    public static void RaiseStormCellEnded(StormCellInfo cell) => StormCellEnded?.Invoke(cell);

    // Static events survive play sessions when domain reload is disabled; clear them on entry.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        RunStarted = null;
        PhotoTaken = null;
        PhotoMissed = null;
        OutOfFilm = null;
        FilmChanged = null;
        PlayerDamaged = null;
        RunEnded = null;
        StyleEvent = null;
        Landed = null;
        VehicleImpact = null;
        Tossed = null;
        StormCellForming = null;
        StormCellPeak = null;
        StormCellRopeOut = null;
        StormCellEnded = null;
    }
}
