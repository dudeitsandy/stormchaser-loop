using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shuffle bag for radio songs (S8-09): every song plays once before any repeats, and a song never plays twice in a
/// row, even across a reshuffle. Pure; the random source is injected.
/// </summary>
public sealed class RadioPlaylist
{
    private readonly int _count;
    private readonly System.Random _rng;
    private readonly List<int> _bag = new List<int>();
    private int _last = -1;

    public RadioPlaylist(int count, int seed)
    {
        _count = count;
        _rng = new System.Random(seed);
    }

    /// <summary>Index of the next song, or −1 when there are none.</summary>
    public int Next()
    {
        if (_count <= 0) return -1;
        if (_count == 1) return _last = 0;
        if (_bag.Count == 0)
        {
            for (int i = 0; i < _count; i++) _bag.Add(i);
            for (int i = _bag.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
            if (_bag[_bag.Count - 1] == _last) (_bag[0], _bag[_bag.Count - 1]) = (_bag[_bag.Count - 1], _bag[0]);
        }
        int next = _bag[_bag.Count - 1];
        _bag.RemoveAt(_bag.Count - 1);
        return _last = next;
    }
}

/// <summary>
/// Music ducking under storm warnings (S8-09: "music must never mask the warnings"). A TORNADO WARNING (forming true
/// EF3/EF4) ducks for the siren cycle; a TORNADO EMERGENCY (forming true EF5) ducks until that cell ropes out or ends;
/// the anchor's touchdown alert (Peak, EF ≥ 3) ducks briefly. Pure: time is passed in.
/// <para>Release (X9-04, Andy 2026-10-05: "the radio should come back up with a quick ramp, not snap"): when the duck
/// ends, the music holds the duck for <see cref="ReleaseHoldSeconds"/> (the siren's 3 s tail starts) then eases back
/// over <see cref="ReleaseSeconds"/>, so it finishes with the tail and the two never overlap loudly.</para>
/// </summary>
public sealed class MusicDuck
{
    /// <summary>−10 dB.</summary>
    public const float DuckedGain = 0.316f;
    public const float SirenCycleSeconds = 25f;
    public const float TouchdownSeconds = 8f;
    /// <summary>Codex's siren release is 3.0 s (OutdoorWarningSirens, X9-03/04): hold 1 s, ease 2 s.</summary>
    public const float ReleaseHoldSeconds = 1f;
    public const float ReleaseSeconds = 2f;

    private readonly HashSet<int> _emergencies = new HashSet<int>();
    private float _until = float.NegativeInfinity;
    private float _emergencyEndedAt = float.NegativeInfinity;

    public void OnCellForming(int cellId, int trueEf, float now)
    {
        if (trueEf >= 5) _emergencies.Add(cellId);
        else if (trueEf >= 3) _until = Mathf.Max(_until, now + SirenCycleSeconds);
    }

    public void OnCellPeak(StormCellRole role, int trueEf, float now)
    {
        if (role == StormCellRole.Anchor && trueEf >= 3) _until = Mathf.Max(_until, now + TouchdownSeconds);
    }

    public void OnCellDeclined(int cellId, float now)
    {
        if (_emergencies.Remove(cellId) && _emergencies.Count == 0) _emergencyEndedAt = now;
    }

    public void Reset()
    {
        _emergencies.Clear();
        _until = float.NegativeInfinity;
        _emergencyEndedAt = float.NegativeInfinity;
    }

    /// <summary>A warning, emergency or touchdown alert is live (the siren is sounding).</summary>
    public bool IsDucked(float now) => _emergencies.Count > 0 || now < _until;

    /// <summary>Ducked while live; after it ends, held, then a smooth ease back to full over the siren's tail.</summary>
    public float TargetGain(float now)
    {
        if (IsDucked(now)) return DuckedGain;
        float ended = Mathf.Max(_until, _emergencyEndedAt);
        if (float.IsNegativeInfinity(ended)) return 1f;
        float t = (now - ended - ReleaseHoldSeconds) / ReleaseSeconds;
        if (t <= 0f) return DuckedGain;
        if (t >= 1f) return 1f;
        return Mathf.Lerp(DuckedGain, 1f, t * t * (3f - 2f * t));
    }
}

/// <summary>
/// Radio lite (Sprint 8 S8-09, Andy 2026-10-05): the title loop on the title; on run start a station jingle, then
/// shuffled songs with a rotating jingle between them (drop a <c>jingle_*.ogg</c> or <c>radio_*.ogg</c> in Resources/Music); fades out at run end (Results stay quiet so goal stings read); ducks
/// under warnings. Plays through the MUSIC volume (Settings); MASTER is the listener volume. Pauses with <see cref="AudioListener.pause"/>.
/// Clips load from <c>Resources/Music</c>. <see cref="RunManager"/> drives title / run; storm events drive ducking.
/// </summary>
public sealed class RadioLite : MonoBehaviour
{
    /// <summary>Every clip in Resources/Music whose name starts with this is a radio song: add a song by dropping it in.</summary>
    public const string SongPrefix = "radio_";
    /// <summary>Every clip whose name starts with this, plus the original <c>storm_radio_jingle</c>, is a station jingle.</summary>
    public const string JinglePrefix = "jingle_";
    public const string LegacyJingle = "storm_radio_jingle";

    /// <summary>True for a radio song (never a jingle).</summary>
    public static bool IsSong(string clipName) => clipName.StartsWith(SongPrefix, System.StringComparison.Ordinal);

    /// <summary>True for a station jingle (never a song).</summary>
    public static bool IsJingle(string clipName) =>
        clipName.StartsWith(JinglePrefix, System.StringComparison.Ordinal) || clipName == LegacyJingle;

    [SerializeField] private float _fadeSeconds = 1.2f;
    [SerializeField] private float _duckRampSeconds = 0.5f;

    private enum Mode { Silent, Title, Radio }

    private AudioSource _source;
    private AudioClip _title;
    private readonly List<AudioClip> _songs = new List<AudioClip>();
    private readonly List<AudioClip> _jingles = new List<AudioClip>();
    private RadioPlaylist _playlist;
    private RadioPlaylist _jinglePlaylist;
    private readonly MusicDuck _duck = new MusicDuck();
    private Mode _mode;
    private bool _jingleNext;
    private float _fade;        // 0..1 fade envelope
    private float _fadeTarget;
    private float _duckGain = 1f;
    private float _clock;       // unscaled seconds, for ducking

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _title = Resources.Load<AudioClip>("Music/title_loop");
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Music"))
        {
            if (IsSong(clip.name)) _songs.Add(clip);
            else if (IsJingle(clip.name)) _jingles.Add(clip);
        }
        // Stable order before the shuffles; jingles rotate with no immediate repeat, like songs.
        _songs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        _jingles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        int seed = System.Environment.TickCount;
        _playlist = new RadioPlaylist(_songs.Count, seed);
        _jinglePlaylist = new RadioPlaylist(_jingles.Count, seed ^ 0x5f3759df);
    }

    private void OnEnable()
    {
        GameEvents.RunEnded += OnRunEnded;
        GameEvents.RunForfeited += FadeOut;
        GameEvents.StormCellForming += OnForming;
        GameEvents.StormCellPeak += OnPeak;
        GameEvents.StormCellRopeOut += OnDeclined;
        GameEvents.StormCellEnded += OnDeclined;
    }

    private void OnDisable()
    {
        GameEvents.RunEnded -= OnRunEnded;
        GameEvents.RunForfeited -= FadeOut;
        GameEvents.StormCellForming -= OnForming;
        GameEvents.StormCellPeak -= OnPeak;
        GameEvents.StormCellRopeOut -= OnDeclined;
        GameEvents.StormCellEnded -= OnDeclined;
    }

    /// <summary>Title screen: the title loop, faded in.</summary>
    public void PlayTitle()
    {
        _duck.Reset();
        if (_title == null) return;
        _mode = Mode.Title;
        Play(_title, loop: true);
        _fade = 0f;
        _fadeTarget = 1f;
    }

    /// <summary>Run start: jingle, then shuffled songs.</summary>
    public void StartRadio()
    {
        _duck.Reset();
        _mode = Mode.Radio;
        _jingleNext = true;
        _source.Stop();
        _fade = 1f;
        _fadeTarget = 1f;
        NextTrack();
    }

    private void OnRunEnded(RunSummary _) => FadeOut();

    private void FadeOut()
    {
        _mode = Mode.Silent;
        _fadeTarget = 0f;
    }

    // Only a run's storms duck the music; the title's demo storm never touches the title loop.
    private void OnForming(StormCellInfo c) { if (_mode == Mode.Radio) _duck.OnCellForming(c.CellId, c.EF, _clock); }
    private void OnPeak(StormCellInfo c) { if (_mode == Mode.Radio) _duck.OnCellPeak(c.Role, c.EF, _clock); }
    private void OnDeclined(StormCellInfo c) => _duck.OnCellDeclined(c.CellId, _clock);

    private void Update()
    {
        if (AudioListener.pause) return; // paused: the listener holds the music where it is
        float dt = Time.unscaledDeltaTime;
        _clock += dt;
        _fade = Mathf.MoveTowards(_fade, _fadeTarget, dt / Mathf.Max(0.01f, _fadeSeconds));
        _duckGain = Mathf.MoveTowards(_duckGain, _duck.TargetGain(_clock), dt / Mathf.Max(0.01f, _duckRampSeconds));
        DeviceSettings s = ProfileStore.Shared.Settings;
        _source.volume = _fade * _duckGain * s.MusicVolume; // MASTER is AudioListener.volume (Settings)
        if (_mode == Mode.Silent && _fade <= 0f && _source.isPlaying) _source.Stop();
        if (_mode == Mode.Radio && !_source.isPlaying) NextTrack();
    }

    private void NextTrack()
    {
        if (_jingleNext && _jingles.Count > 0)
        {
            _jingleNext = false;
            Play(_jingles[_jinglePlaylist.Next()], loop: false);
            return;
        }
        int i = _playlist.Next();
        _jingleNext = true;
        if (i >= 0) Play(_songs[i], loop: false);
    }

    private void Play(AudioClip clip, bool loop)
    {
        _source.clip = clip;
        _source.loop = loop;
        _source.Play();
    }
}
