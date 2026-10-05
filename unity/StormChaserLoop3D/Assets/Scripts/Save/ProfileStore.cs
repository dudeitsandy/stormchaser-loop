using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Save &amp; Profile **M1 stem** (run-goals-v1 story 005; `save-profile.md` header note): loads the profile file once,
/// keeps it in memory across scene reloads, and writes it at the run-complete checkpoint as one write (best score,
/// accomplishments and unlocks together). Quitting a run writes nothing. A failed write keeps everything in memory and
/// the next checkpoint writes it again. Not the full save-profile.md design: no slots, checksums, .bak, migrations or
/// ReadOnly handling.
/// </summary>
public sealed class ProfileStore
{
    public const string ProfileFile = "profile.json";
    public const string DeviceFile = "device.json";

    private static ProfileStore _shared;
    private readonly IProfileStorage _storage;
    private readonly Func<float> _legacyBest;

    /// <summary>The live store (file-backed). Tests replace it with a memory-backed one.</summary>
    public static ProfileStore Shared
    {
        get => _shared ?? (_shared = new ProfileStore(new FileProfileStorage(), BestScoreStore.Load));
        set => _shared = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _shared = null;

    public ProfileData Data { get; private set; }
    public DeviceSettings Settings { get; private set; }
    /// <summary>True when the last profile write failed (Results shows the retry toast; progress is kept in memory).</summary>
    public bool LastWriteFailed { get; private set; }

    /// <param name="legacyBest">The pre-stem PlayerPrefs best score, imported once.</param>
    public ProfileStore(IProfileStorage storage, Func<float> legacyBest = null)
    {
        _storage = storage;
        _legacyBest = legacyBest;
        Data = Read<ProfileData>(ProfileFile) ?? new ProfileData();
        Settings = Read<DeviceSettings>(DeviceFile) ?? new DeviceSettings();
        Data.Accomplishments ??= new List<AccomplishmentEntry>();
        Data.Unlocks ??= new List<string>();
        Data.Livery ??= "";
        if (!Data.ImportedPlayerPrefsBest && _legacyBest != null)
        {
            Data.BestScore = Mathf.Max(Data.BestScore, _legacyBest());
            Data.ImportedPlayerPrefsBest = true;
        }
    }

    // ---------- queries ----------

    /// <summary>True when the record already holds <paramref name="goalId"/> (FirstEver is its negation).</summary>
    public bool HasCompleted(string goalId) => Find(goalId) != null;

    public bool IsUnlocked(string unlockId) => Data.Unlocks.Contains(unlockId);

    /// <summary>Distinct career goals with a recorded first completion, for one mode and map.</summary>
    public int CareerCount(string mode, string map)
    {
        string prefix = $"career.{mode}.{map}.";
        int n = 0;
        foreach (AccomplishmentEntry e in Data.Accomplishments)
            if (e.Id != null && e.Id.StartsWith(prefix, StringComparison.Ordinal)) n++;
        return n;
    }

    // ---------- checkpoints ----------

    /// <summary>
    /// Run complete (timer or wreck): records the run's completions, raises the best score, adds any unlocks the
    /// caller decided (story 006), and writes once. Returns false when the write failed (progress kept in memory).
    /// </summary>
    public bool RecordRunComplete(IReadOnlyList<GoalCompletion> completions, string mode, long seed, string version,
                                  DateTime utcNow, float finalScore, IEnumerable<string> newUnlocks = null)
    {
        if (completions != null)
            foreach (GoalCompletion c in completions)
            {
                AccomplishmentEntry e = Find(c.Id);
                if (e == null)
                {
                    e = new AccomplishmentEntry { Id = c.Id, Mode = mode, FirstSeed = seed, FirstVersion = version,
                                                  FirstDate = utcNow.ToString("o") };
                    Data.Accomplishments.Add(e);
                }
                e.Count++;
            }
        if (finalScore > Data.BestScore) Data.BestScore = finalScore;
        if (newUnlocks != null)
            foreach (string id in newUnlocks)
                if (!string.IsNullOrEmpty(id) && !Data.Unlocks.Contains(id)) Data.Unlocks.Add(id);
        return WriteProfile();
    }

    /// <summary>Saves the chosen livery (title paint toggle).</summary>
    public bool SetLivery(string id)
    {
        Data.Livery = id ?? "";
        return WriteProfile();
    }

    /// <summary>Saves Settings (on panel close).</summary>
    public bool SaveSettings() => _storage.TryWrite(DeviceFile, JsonUtility.ToJson(Settings));

    /// <summary>Dev persistence check (<c>?savecheck=1</c>): counts this launch and writes.</summary>
    public int CountSaveCheckLaunch()
    {
        Data.SaveCheckLaunches++;
        WriteProfile();
        return Data.SaveCheckLaunches;
    }

    private bool WriteProfile()
    {
        LastWriteFailed = !_storage.TryWrite(ProfileFile, JsonUtility.ToJson(Data));
        return !LastWriteFailed;
    }

    private AccomplishmentEntry Find(string id)
    {
        foreach (AccomplishmentEntry e in Data.Accomplishments) if (e.Id == id) return e;
        return null;
    }

    private T Read<T>(string name) where T : class
    {
        if (!_storage.TryRead(name, out string text) || string.IsNullOrEmpty(text)) return null;
        try { return JsonUtility.FromJson<T>(text); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] {name} unreadable, starting fresh (stem saves are disposable): {e.Message}");
            return null;
        }
    }
}
