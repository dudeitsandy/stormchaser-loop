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
/// </summary>
public sealed class MusicDuck
{
    /// <summary>−10 dB.</summary>
    public const float DuckedGain = 0.316f;
    public const float SirenCycleSeconds = 25f;
    public const float TouchdownSeconds = 8f;

    private readonly HashSet<int> _emergencies = new HashSet<int>();
    private float _until = float.NegativeInfinity;

    public void OnCellForming(int cellId, int trueEf, float now)
    {
        if (trueEf >= 5) _emergencies.Add(cellId);
        else if (trueEf >= 3) _until = Mathf.Max(_until, now + SirenCycleSeconds);
    }

    public void OnCellPeak(StormCellRole role, int trueEf, float now)
    {
        if (role == StormCellRole.Anchor && trueEf >= 3) _until = Mathf.Max(_until, now + TouchdownSeconds);
    }

    public void OnCellDeclined(int cellId) => _emergencies.Remove(cellId);

    public void Reset()
    {
        _emergencies.Clear();
        _until = float.NegativeInfinity;
    }

    public bool IsDucked(float now) => _emergencies.Count > 0 || now < _until;

    public float TargetGain(float now) => IsDucked(now) ? DuckedGain : 1f;
}

/// <summary>
/// Radio lite (Sprint 8 S8-09, Andy 2026-10-05): the title loop on the title; on run start a station jingle, then
/// shuffled songs with the jingle between them; fades out at run end (Results stay quiet so goal stings read); ducks
/// under warnings. Plays through the MUSIC volume (Settings) × master. Pauses with <see cref="AudioListener.pause"/>.
/// Clips load from <c>Resources/Music</c>. <see cref="RunManager"/> drives title / run; storm events drive ducking.
/// </summary>
public sealed class RadioLite : MonoBehaviour
{
    /// <summary>Every clip in Resources/Music whose name starts with this is a radio song: add a song by dropping it in.</summary>
    public const string SongPrefix = "radio_";

    [SerializeField] private float _fadeSeconds = 1.2f;
    [SerializeField] private float _duckRampSeconds = 0.5f;

    private enum Mode { Silent, Title, Radio }

    private AudioSource _source;
    private AudioClip _title, _jingle;
    private readonly List<AudioClip> _songs = new List<AudioClip>();
    private RadioPlaylist _playlist;
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
        _jingle = Resources.Load<AudioClip>("Music/storm_radio_jingle");
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Music"))
            if (clip.name.StartsWith(SongPrefix, System.StringComparison.Ordinal)) _songs.Add(clip);
        _songs.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // stable order before the shuffle
        _playlist = new RadioPlaylist(_songs.Count, System.Environment.TickCount);
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

    private void OnForming(StormCellInfo c) => _duck.OnCellForming(c.CellId, c.EF, _clock);
    private void OnPeak(StormCellInfo c) => _duck.OnCellPeak(c.Role, c.EF, _clock);
    private void OnDeclined(StormCellInfo c) => _duck.OnCellDeclined(c.CellId);

    private void Update()
    {
        if (AudioListener.pause) return; // paused: the listener holds the music where it is
        float dt = Time.unscaledDeltaTime;
        _clock += dt;
        _fade = Mathf.MoveTowards(_fade, _fadeTarget, dt / Mathf.Max(0.01f, _fadeSeconds));
        _duckGain = Mathf.MoveTowards(_duckGain, _duck.TargetGain(_clock), dt / Mathf.Max(0.01f, _duckRampSeconds));
        DeviceSettings s = ProfileStore.Shared.Settings;
        _source.volume = _fade * _duckGain * s.MusicVolume * s.MasterVolume;
        if (_mode == Mode.Silent && _fade <= 0f && _source.isPlaying) _source.Stop();
        if (_mode == Mode.Radio && !_source.isPlaying) NextTrack();
    }

    private void NextTrack()
    {
        if (_jingleNext && _jingle != null)
        {
            _jingleNext = false;
            Play(_jingle, loop: false);
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
