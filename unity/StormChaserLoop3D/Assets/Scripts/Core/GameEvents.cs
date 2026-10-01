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

    public RunSummary(float score, int photosTaken, float bestShot, bool wrecked, float previousBest)
    {
        Score = score;
        PhotosTaken = photosTaken;
        BestShot = bestShot;
        Wrecked = wrecked;
        PreviousBest = previousBest;
    }

    public bool IsNewBest => Score > PreviousBest;
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

    public static void RaiseRunStarted() => RunStarted?.Invoke();
    public static void RaisePhotoTaken(PhotoResult result) => PhotoTaken?.Invoke(result);
    public static void RaisePhotoMissed() => PhotoMissed?.Invoke();
    public static void RaiseOutOfFilm() => OutOfFilm?.Invoke();
    public static void RaiseFilmChanged(int remaining, int capacity) => FilmChanged?.Invoke(remaining, capacity);
    public static void RaisePlayerDamaged(int current, int max) => PlayerDamaged?.Invoke(current, max);
    public static void RaiseRunEnded(RunSummary summary) => RunEnded?.Invoke(summary);

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
    }
}
