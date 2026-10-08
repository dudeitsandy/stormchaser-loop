using System.Collections.Generic;
using UnityEngine;

/// <summary>A row in the Settings panel (design/ux/run-screens.md Layout: Settings).</summary>
public enum SettingRow { Camera, Sensitivity, InvertY, Brightness, Master, Effects, Music, Radio, Fullscreen, Resolution, Back }

/// <summary>
/// Settings panel model (run-screens story 002): rows over a <see cref="DeviceSettings"/>, focus, and stepped value
/// changes (◂ ▸). Pure: the caller applies values live (<see cref="SettingsApplier"/>) and saves on close.
/// </summary>
public sealed class SettingsMenu
{
    public static readonly string[] CameraPresets = { "SKY", "CLASSIC", "HIGH" };
    public const float SensitivityMin = 0.25f, SensitivityMax = 2f, SensitivityStep = 0.25f;
    public const float BrightnessMin = -0.5f, BrightnessMax = 0.5f, BrightnessStep = 0.1f;
    public const float VolumeStep = 0.1f;
    /// <summary>SOUND TEST value while nothing previews (Andy 2026-10-08: not "OFF", the radio isn't being switched off).</summary>
    public const string IdleSoundTest = "—";

    private readonly List<SettingRow> _rows = new List<SettingRow>(11);
    private readonly IReadOnlyList<Vector2Int> _resolutions;
    private readonly IReadOnlyList<string> _songs;

    public DeviceSettings Settings { get; }
    public IReadOnlyList<SettingRow> Rows => _rows;
    public int Focus { get; private set; }
    public SettingRow Focused => _rows[Focus];
    /// <summary>SOUND TEST row: the song being previewed (index into the song titles), or −1 for idle ("—"). Never saved.</summary>
    public int PreviewIndex { get; private set; } = -1;

    /// <param name="desktop">False on WebGL: no RESOLUTION row.</param>
    /// <param name="resolutions">Desktop resolutions, smallest first (may be empty).</param>
    /// <param name="songs">Radio song titles for the SOUND TEST row (Andy 2026-10-08); none = no row.</param>
    public SettingsMenu(DeviceSettings settings, bool desktop, IReadOnlyList<Vector2Int> resolutions = null,
                        IReadOnlyList<string> songs = null)
    {
        Settings = settings;
        _resolutions = resolutions ?? new List<Vector2Int>();
        _songs = songs ?? new List<string>();
        _rows.AddRange(new[] { SettingRow.Camera, SettingRow.Sensitivity, SettingRow.InvertY, SettingRow.Brightness,
                               SettingRow.Master, SettingRow.Effects, SettingRow.Music });
        if (_songs.Count > 0) _rows.Add(SettingRow.Radio);
        _rows.Add(SettingRow.Fullscreen);
        if (desktop && _resolutions.Count > 0) _rows.Add(SettingRow.Resolution);
        _rows.Add(SettingRow.Back);
    }

    /// <summary>Moves focus up (−1) or down (+1), wrapping.</summary>
    public void Move(int delta)
    {
        int n = _rows.Count;
        Focus = ((Focus + delta) % n + n) % n;
    }

    public void FocusOn(int index)
    {
        if (index >= 0 && index < _rows.Count) Focus = index;
    }

    /// <summary>Steps the focused value by <paramref name="dir"/> (−1 / +1). Returns the row changed, or null.</summary>
    public SettingRow? Change(int dir)
    {
        DeviceSettings s = Settings;
        switch (Focused)
        {
            case SettingRow.Camera:
                s.CameraPreset = CameraPresets[Wrap(IndexOf(CameraPresets, s.CameraPreset) + dir, CameraPresets.Length)];
                break;
            case SettingRow.Sensitivity:
                s.Sensitivity = Step(s.Sensitivity, dir * SensitivityStep, SensitivityMin, SensitivityMax);
                break;
            case SettingRow.InvertY: s.InvertY = !s.InvertY; break;
            case SettingRow.Brightness:
                s.Brightness = Step(s.Brightness, dir * BrightnessStep, BrightnessMin, BrightnessMax);
                break;
            case SettingRow.Master: s.MasterVolume = Step(s.MasterVolume, dir * VolumeStep, 0f, 1f); break;
            case SettingRow.Effects: s.EffectsVolume = Step(s.EffectsVolume, dir * VolumeStep, 0f, 1f); break;
            case SettingRow.Music: s.MusicVolume = Step(s.MusicVolume, dir * VolumeStep, 0f, 1f); break;
            case SettingRow.Radio:
                // Idle, then each song, wrapping: index −1 .. n−1.
                PreviewIndex = Wrap(PreviewIndex + 1 + dir, _songs.Count + 1) - 1;
                break;
            case SettingRow.Fullscreen: s.Fullscreen = !s.Fullscreen; break;
            case SettingRow.Resolution:
                if (_resolutions.Count == 0) return null;
                int i = Wrap(ResolutionIndex() + dir, _resolutions.Count);
                s.ResolutionWidth = _resolutions[i].x;
                s.ResolutionHeight = _resolutions[i].y;
                break;
            default: return null;
        }
        return Focused;
    }

    public static string Label(SettingRow row)
    {
        switch (row)
        {
            case SettingRow.Camera: return "CAMERA";
            case SettingRow.Sensitivity: return "SENSITIVITY";
            case SettingRow.InvertY: return "INVERT Y";
            case SettingRow.Brightness: return "BRIGHTNESS";
            case SettingRow.Master: return "MASTER VOLUME";
            case SettingRow.Effects: return "EFFECTS";
            case SettingRow.Music: return "MUSIC";
            case SettingRow.Radio: return "SOUND TEST";
            case SettingRow.Fullscreen: return "FULLSCREEN";
            case SettingRow.Resolution: return "RESOLUTION";
            default: return "BACK";
        }
    }

    /// <summary>Value text for a row ("" for BACK).</summary>
    public string Value(SettingRow row)
    {
        DeviceSettings s = Settings;
        switch (row)
        {
            case SettingRow.Camera: return s.CameraPreset;
            case SettingRow.Sensitivity: return $"{s.Sensitivity:0.00}x";
            case SettingRow.InvertY: return s.InvertY ? "ON" : "OFF";
            case SettingRow.Brightness:
                int pct = Mathf.RoundToInt(s.Brightness * 100f);
                return pct > 0 ? $"+{pct}%" : $"{pct}%";
            case SettingRow.Master: return Percent(s.MasterVolume);
            case SettingRow.Effects: return Percent(s.EffectsVolume);
            case SettingRow.Music: return Percent(s.MusicVolume);
            case SettingRow.Radio: return PreviewIndex < 0 ? IdleSoundTest : $"{PreviewIndex + 1:00} {_songs[PreviewIndex]}";
            case SettingRow.Fullscreen: return s.Fullscreen ? "ON" : "OFF";
            case SettingRow.Resolution: return s.ResolutionWidth > 0 ? $"{s.ResolutionWidth}x{s.ResolutionHeight}" : "AUTO";
            default: return "";
        }
    }

    private int ResolutionIndex()
    {
        for (int i = 0; i < _resolutions.Count; i++)
            if (_resolutions[i].x == Settings.ResolutionWidth && _resolutions[i].y == Settings.ResolutionHeight) return i;
        return _resolutions.Count - 1;
    }

    private static string Percent(float v) => $"{Mathf.RoundToInt(v * 100f)}%";

    // Rounded to the step grid so repeated steps never drift (0.1 × 3 ≠ 0.3 in float).
    private static float Step(float v, float delta, float min, float max) =>
        Mathf.Clamp(Mathf.Round((v + delta) * 100f) / 100f, min, max);

    private static int Wrap(int i, int n) => ((i % n) + n) % n;

    private static int IndexOf(string[] a, string v)
    {
        for (int i = 0; i < a.Length; i++) if (a[i] == v) return i;
        return 0;
    }
}
