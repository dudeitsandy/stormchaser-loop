using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

/// <summary>Synthesized truck, wind, shutter, damage and end-of-run audio without imported assets.</summary>
public sealed class ProceduralAudio : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _effectsVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float _engineVolume = 0.16f;
    [SerializeField, Range(0f, 1f)] private float _windVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _rumbleVolume = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _stormAlertVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _skidVolume = 0.24f;
    [SerializeField, Min(0f)] private float _landingThreshold = 2f;
    [SerializeField, Min(0f)] private float _impactCooldown = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _boostVolume = 0.3f;
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.Audio.Update");
    private PlayerVehicle _vehicle;
    private AudioSource _effects, _engine, _wind, _skid, _vehicleEffects, _boost, _rumble, _stormAlert;
    private AudioClip _formingAlert, _emergencyAlert, _peakAlert;
    private readonly StormCellCueTracker _stormCells = new StormCellCueTracker();
    private readonly Queue<(int CellId, float Level, bool Emergency)> _stormQueue = new Queue<(int, float, bool)>();
    private int _playingCellId = -1;
    private bool _playingPeak;
    private AudioClip _shutter, _dryShutter, _hit, _finish, _wreck, _landing, _crunch, _toss, _boostStart;
    private float _load, _lastImpact = float.NegativeInfinity;
    private bool _wasBoosting;
    private readonly List<AudioClip> _clips = new List<AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install); // AfterSceneLoad alone skips reloads

    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() != null && Object.FindAnyObjectByType<ProceduralAudio>() == null)
            new GameObject(nameof(ProceduralAudio)).AddComponent<ProceduralAudio>();
    }
    private void Awake()
    {
        _effects = Source(false);
        _engine = Source(true);
        _wind = Source(true);
        _skid = Source(true);
        _vehicleEffects = Source(false);
        _boost = Source(true);
        _rumble = Source(true);
        _stormAlert = Source(false);
        _shutter = Clip("Shutter", 0.15f, 0);
        _dryShutter = Clip("DryShutter", 0.055f, 6);
        _hit = Clip("Damage", 0.35f, 1);
        _finish = Clip("RunComplete", 0.8f, 2);
        _wreck = Clip("Wreck", 0.8f, 3);
        _engine.clip = Clip("EngineLoop", 1f, 4);
        _wind.clip = Clip("WindLoop", 2f, 5);
        _skid.clip = Clip("TireSkidLoop", 1f, 7);
        _landing = Clip("SuspensionLanding", 0.25f, 8);
        _crunch = Clip("MetalImpact", 0.45f, 9);
        _toss = Clip("WindToss", 0.5f, 10);
        _boost.clip = Clip("BoostRoarLoop", 1f, 11);
        _boostStart = Clip("BoostIgnition", 0.22f, 12);
        _rumble.clip = Clip("EF5LowRumble", 2f, 13);
        _formingAlert = Clip("BroadcastWarningRadio", 3.2f, 14);
        _emergencyAlert = Clip("BroadcastEmergencyRadio", 4.2f, 16);
        _peakAlert = Clip("AnchorTouchdownAlert", 0.5f, 15);
    }
    private void Start()
    {
        _vehicle = FindAnyObjectByType<PlayerVehicle>();
    }
    private AudioSource Source(bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0;
        source.volume = 0;
        return source;
    }
    private void OnEnable()
    {
        _engine.Play();
        _wind.Play();
        _skid.Play();
        GameEvents.PhotoTaken += Photo;
        GameEvents.PhotoMissed += Shutter;
        GameEvents.OutOfFilm += DryShutter;
        GameEvents.PlayerDamaged += Hit;
        GameEvents.RunEnded += End;
        GameEvents.Landed += Landed;
        GameEvents.VehicleImpact += Impact;
        GameEvents.Tossed += Tossed;
        GameEvents.StormCellForming += CellForming;
        GameEvents.StormCellPeak += CellPeak;
        GameEvents.StormCellRopeOut += CellRopeOut;
        GameEvents.StormCellEnded += CellEnded;
        GameEvents.RunStarted += ClearStormCues;
    }
    private void OnDisable()
    {
        GameEvents.PhotoTaken -= Photo;
        GameEvents.PhotoMissed -= Shutter;
        GameEvents.OutOfFilm -= DryShutter;
        GameEvents.PlayerDamaged -= Hit;
        GameEvents.RunEnded -= End;
        GameEvents.Landed -= Landed;
        GameEvents.VehicleImpact -= Impact;
        GameEvents.Tossed -= Tossed;
        GameEvents.StormCellForming -= CellForming;
        GameEvents.StormCellPeak -= CellPeak;
        GameEvents.StormCellRopeOut -= CellRopeOut;
        GameEvents.StormCellEnded -= CellEnded;
        GameEvents.RunStarted -= ClearStormCues;
        ClearStormCues();
        if (_engine != null) _engine.Stop();
        if (_wind != null) _wind.Stop();
        if (_skid != null) _skid.Stop();
        if (_vehicleEffects != null) _vehicleEffects.Stop();
        if (_boost != null) { _boost.Stop(); _boost.volume = 0f; }
        _wasBoosting = false;
    }
    private void Photo(PhotoResult result) => Shutter();
    private void Shutter() => Play(_shutter);
    private void DryShutter() => Play(_dryShutter);
    private void Hit(int current, int maximum) => Play(_hit);
    private void End(RunSummary summary) { ClearStormCues(); Play(summary.Wrecked ? _wreck : _finish); }
    private void ClearStormCues()
    {
        _stormCells.Clear();
        _stormQueue.Clear();
        _playingCellId = -1;
        _playingPeak = false;
        if (_stormAlert != null) _stormAlert.Stop();
        if (_rumble != null) { _rumble.Stop(); _rumble.volume = 0f; }
    }
    private void QueueStormCue(StormCellInfo cell)
    {
        if (!Running || _stormQueue.Count >= 8) return;
        float distance = Vector3.Distance(_vehicle.transform.position, cell.Position);
        _stormQueue.Enqueue((cell.CellId, Mathf.Clamp01(40f / Mathf.Max(40f, distance)) * 0.65f + 0.15f, cell.EF >= 5));
    }
    private void CellForming(StormCellInfo cell) { if (_stormCells.Forming(cell)) QueueStormCue(cell); }
    private void CellPeak(StormCellInfo cell)
    {
        if (!_stormCells.Peak(cell) || !Running) return;
        // Touchdown is immediate and preempts radio; do not delay it behind the queue.
        _stormAlert.Stop();
        _stormAlert.clip = _peakAlert;
        _playingCellId = cell.CellId;
        _playingPeak = true;
        _stormAlert.volume = _stormAlertVolume;
        _stormAlert.Play();
    }
    private void CellRopeOut(StormCellInfo cell)
    {
        _stormCells.RopeOut(cell);
        if (_playingCellId == cell.CellId && !_playingPeak) _stormAlert.Stop();
    }
    private void CellEnded(StormCellInfo cell)
    {
        _stormCells.Ended(cell);
        if (_playingCellId == cell.CellId) _stormAlert.Stop();
    }
    private void Play(AudioClip clip)
    {
        _effects.volume = _effectsVolume;
        _effects.PlayOneShot(clip);
    }
    private bool Running => _vehicle != null && _vehicle.InputEnabled && Time.timeScale > 0f;
    private void VehicleSound(AudioClip clip, float level, float pitch)
    {
        if (!Running) return;
        _vehicleEffects.pitch = pitch;
        _vehicleEffects.volume = _effectsVolume;
        _vehicleEffects.PlayOneShot(clip, level);
    }
    private void Landed(float verticalSpeed)
    {
        float severity = VehicleFeedbackLevels.Landing(verticalSpeed, _landingThreshold);
        if (severity <= 0f) return;
        VehicleSound(_landing, Mathf.Lerp(0.2f, 0.8f, severity), Mathf.Lerp(1.15f, 0.75f, severity));
    }
    private void Impact(ImpactInfo impact)
    {
        if (!Running || impact.Speed < 2f || Time.unscaledTime - _lastImpact < _impactCooldown) return;
        _lastImpact = Time.unscaledTime;
        float severity = VehicleFeedbackLevels.Impact(impact.Speed, impact.HpLoss);
        VehicleSound(_crunch, Mathf.Lerp(0.25f, 1f, severity), Mathf.Lerp(1.2f, 0.65f, severity));
    }
    private void Tossed() => VehicleSound(_toss, 0.65f, 1f);
    private void Update()
    {
        using var sample = UpdateMarker.Auto();
        bool running = Running;
        float speed = running ? Mathf.Clamp01(Mathf.Abs(_vehicle.CurrentSpeed) / Mathf.Max(1f, _vehicle.MaxSpeed)) : 0;
        float absoluteSpeed = running ? Mathf.Abs(_vehicle.CurrentSpeed) : 0f;
        _load = Mathf.MoveTowards(_load, running ? Mathf.Clamp01(_vehicle.EngineLoad) : 0f, Time.unscaledDeltaTime * 3f);
        bool boosting = running && _vehicle.BoostActive;
        if (boosting && !_wasBoosting) { _boost.Play(); Play(_boostStart); }
        _wasBoosting = boosting;
        _boost.volume = Mathf.MoveTowards(_boost.volume, boosting ? _boostVolume : 0f, Time.unscaledDeltaTime * 3f);
        _boost.pitch = Mathf.Lerp(0.9f, 1.2f, speed);
        if (!boosting && _boost.volume <= 0f && _boost.isPlaying) _boost.Stop();
        float airborne = running && (_vehicle.State == VehicleState.Airborne || _vehicle.State == VehicleState.Tossed) ? 1f : 0f;
        _engine.pitch = Mathf.MoveTowards(_engine.pitch, Mathf.Lerp(0.8f, 2.1f, speed) + _load * 0.15f + airborne * 0.1f,
            Time.unscaledDeltaTime * 3f);
        _engine.volume = Mathf.MoveTowards(_engine.volume, running ? _engineVolume * (0.4f + speed * 0.5f + _load * 0.1f) : 0,
            Time.unscaledDeltaTime);
        float slip = running ? VehicleFeedbackLevels.Skid(_vehicle.State, _vehicle.GroundedWheels,
            absoluteSpeed, _vehicle.SlipAngle) : 0f;
        _skid.volume = Mathf.MoveTowards(_skid.volume, slip * _skidVolume, Time.unscaledDeltaTime * 2f);
        _skid.pitch = Mathf.Lerp(0.85f, 1.25f, speed);
        float exposure = running ? StormEnvironmentCues.Exposure : 0f;
        _wind.volume = Mathf.MoveTowards(_wind.volume, exposure * _windVolume, Time.unscaledDeltaTime * 0.5f);
        float rumble = running ? StormEnvironmentCues.Ef5Exposure : 0f;
        if (rumble > 0f && !_rumble.isPlaying) _rumble.Play();
        _rumble.volume = Mathf.MoveTowards(_rumble.volume, rumble * _rumbleVolume, Time.unscaledDeltaTime * 0.5f);
        if (_rumble.volume == 0f && _rumble.isPlaying) _rumble.Stop();
        if (!running) { _stormQueue.Clear(); _stormAlert.Stop(); }
        else if (!_stormAlert.isPlaying && _stormQueue.Count > 0)
        {
            var cue = _stormQueue.Dequeue();
            if (_stormCells.IsForming(cue.CellId))
            {
                _stormAlert.clip = cue.Emergency ? _emergencyAlert : _formingAlert;
                _playingCellId = cue.CellId;
                _playingPeak = false;
                _stormAlert.volume = _stormAlertVolume * cue.Level;
                _stormAlert.Play();
            }
        }
    }
    private AudioClip Clip(string name, float seconds, int kind)
    {
        const int rate = 22050;
        int count = Mathf.RoundToInt(seconds * rate);
        var samples = new float[count];
        var random = new System.Random(173 + kind);
        float filtered = 0;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float noise = (float)random.NextDouble() * 2f - 1f;
            filtered = Mathf.Lerp(filtered, noise, 0.08f);
            float value;
            switch (kind)
            {
                case 0: value = noise * Mathf.Exp(-t * 45) + noise * 0.5f * Mathf.Exp(-Mathf.Abs(t - 0.065f) * 150); break;
                case 1: value = (noise * 0.4f + Mathf.Sin(2 * Mathf.PI * 65 * t) * 0.6f) * Mathf.Exp(-t * 13); break;
                case 2: value = Mathf.Sin(2 * Mathf.PI * (t < 0.25f ? 440 : t < 0.5f ? 550 : 660) * t) * 0.35f; break;
                case 3: value = Mathf.Sin(2 * Mathf.PI * (180 * t - 65 * t * t)) * 0.4f + filtered * 0.3f; break;
                case 4: value = Mathf.Sin(2 * Mathf.PI * 55 * t) * 0.45f + Mathf.Sin(2 * Mathf.PI * 110 * t) * 0.2f; break;
                case 6: value = (noise * 0.3f + Mathf.Sin(2 * Mathf.PI * 1200 * t) * 0.2f) * Mathf.Exp(-t * 100); break;
                case 7: value = filtered * 0.7f + Mathf.Sin(2 * Mathf.PI * 880 * t) * (0.12f + noise * 0.06f); break;
                case 8: value = (Mathf.Sin(2 * Mathf.PI * (90 * t - 80 * t * t)) * 0.7f + filtered * 0.3f) * Mathf.Exp(-t * 18); break;
                case 9: value = (noise * 0.5f + Mathf.Sin(2 * Mathf.PI * 173 * t) * 0.2f + Mathf.Sin(2 * Mathf.PI * 317 * t) * 0.2f) * Mathf.Exp(-t * 10); break;
                case 10: value = filtered * 2f * Mathf.Sin(Mathf.PI * t / seconds); break;
                case 11: value = filtered * 1.5f + Mathf.Sin(2 * Mathf.PI * 80 * t) * 0.3f; break;
                case 12: value = filtered * 2f * Mathf.Sin(Mathf.PI * t / seconds) + noise * 0.2f * Mathf.Exp(-t * 30f); break;
                case 13: value = Mathf.Sin(2 * Mathf.PI * 35 * t) * 0.45f + Mathf.Sin(2 * Mathf.PI * 55 * t) * 0.2f + filtered * 0.25f; break;
                case 14:
                case 16:
                    float attentionSeconds = kind == 16 ? 1.7f : 0.7f;
                    if (t < attentionSeconds)
                    {
                        float pulseTime = Mathf.Repeat(t, kind == 16 ? 0.42f : 0.23f);
                        float tone = kind == 16 ? 853f : (t < 0.23f ? 660f : t < 0.46f ? 880f : 990f);
                        float envelope = Mathf.Clamp01(pulseTime / 0.012f) * Mathf.Clamp01((0.20f - pulseTime) / 0.025f);
                        value = (Mathf.Sin(2f * Mathf.PI * tone * t) * 0.38f
                            + (kind == 16 ? Mathf.Sin(2f * Mathf.PI * 960f * t) * 0.26f : 0f)) * envelope;
                        break;
                    }
                    // Filtered radio chatter texture and a distant institutional two-tone siren; no spoken EF.
                    float radioTime = t - attentionSeconds;
                    float carrier = radioTime < 1.25f ? 420f : 560f;
                    float chatter = Mathf.Max(0f, Mathf.Sin(t * 31f)) * Mathf.Sin(2 * Mathf.PI * 190 * t);
                    value = (Mathf.Sin(2 * Mathf.PI * carrier * t) * 0.22f + chatter * 0.16f + filtered * 0.55f)
                        * Mathf.Clamp01(radioTime / 0.02f);
                    break;
                case 15: value = (Mathf.Sin(2 * Mathf.PI * 880 * t) + Mathf.Sin(2 * Mathf.PI * 1320 * t) * 0.3f) * Mathf.Exp(-t * 5f) * 0.45f; break;
                default: value = filtered * 2f; break;
            }
            if (kind != 4)
                value *= Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((seconds - t) / 0.04f);
            samples[i] = Mathf.Clamp(value, -1f, 1f);
        }
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(samples, 0);
        _clips.Add(clip);
        return clip;
    }
    private void OnDestroy()
    {
        foreach (var clip in _clips) if (clip != null) Destroy(clip);
    }
}
