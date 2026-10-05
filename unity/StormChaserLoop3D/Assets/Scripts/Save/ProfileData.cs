using System;
using System.Collections.Generic;

/// <summary>
/// Save &amp; Profile **M1 stem** (Andy 2026-10-04; `save-profile.md` header note): the one profile file. Disposable:
/// <see cref="SchemaVersion"/> 0, and the full Save &amp; Profile build may reset it. Stable string IDs only, so a later
/// migration stays possible. JsonUtility-serialized.
/// </summary>
[Serializable]
public sealed class ProfileData
{
    /// <summary>0 = M1 stem (disposable).</summary>
    public int SchemaVersion;
    public float BestScore;
    /// <summary>True once the PlayerPrefs best score has been imported (one time).</summary>
    public bool ImportedPlayerPrefsBest;
    public List<AccomplishmentEntry> Accomplishments = new List<AccomplishmentEntry>();
    /// <summary>Unlock IDs (e.g. <c>livery.ktvr</c>). Unknown IDs are kept, never interpreted.</summary>
    public List<string> Unlocks = new List<string>();
    /// <summary>Chosen livery (<c>LastLoadout.livery</c> in save-profile.md terms); empty = stock.</summary>
    public string Livery = "";
    /// <summary>Dev only: launches counted by <c>?savecheck=1</c> to prove persistence across page reloads.</summary>
    public int SaveCheckLaunches;
}

/// <summary>One goal's record (event-system.md Run Goals Rule 5): first completion and a running count.</summary>
[Serializable]
public sealed class AccomplishmentEntry
{
    public string Id;
    public string Mode;
    public long FirstSeed;
    /// <summary>ISO 8601 UTC.</summary>
    public string FirstDate;
    public string FirstVersion;
    public int Count;
}

/// <summary>The device file (Settings), also part of the M1 stem; owned by run-screens story 002.</summary>
[Serializable]
public sealed class DeviceSettings
{
    public int SchemaVersion;
    public string CameraPreset = "SKY";
    public float Sensitivity = 1f;
    public bool InvertY;
    /// <summary>−0.5 … +0.5 around today's look, default 0 (accessibility Basic).</summary>
    public float Brightness;
    public float MasterVolume = 1f, EffectsVolume = 1f, MusicVolume = 0.8f;
    public bool Fullscreen = true;
    public int ResolutionWidth, ResolutionHeight;
}
