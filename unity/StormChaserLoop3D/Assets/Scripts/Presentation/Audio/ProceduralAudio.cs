using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

/// <summary>Synthesized truck, wind, shutter, damage and end-of-run audio without imported assets.</summary>
public sealed class ProceduralAudio : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _effectsVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float _engineVolume = 0.16f;
    [SerializeField, Range(0f, 1f)] private float _windVolume = 0.4f;
    [SerializeField] private float _windRange = 65f;
    [SerializeField, Range(0f, 1f)] private float _skidVolume = 0.24f;
    [SerializeField, Min(0f)] private float _landingThreshold = 2f;
    [SerializeField, Min(0f)] private float _impactCooldown = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _boostVolume = 0.3f;
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.Audio.Update");
    private PlayerVehicle _vehicle;
    private AudioSource _effects, _engine, _wind, _skid, _vehicleEffects, _boost;
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
    private void End(RunSummary summary) => Play(summary.Wrecked ? _wreck : _finish);
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
        float proximity = 0;
        if (running)
            foreach (var disaster in DisasterEntity.Active)
                if (disaster is TornadoController tornado)
                    proximity = Mathf.Max(proximity, Mathf.Clamp01(1f - Vector3.Distance(_vehicle.transform.position,
                        tornado.transform.position) / Mathf.Max(1f, _windRange)) * tornado.Intensity);
        _wind.volume = Mathf.MoveTowards(_wind.volume, proximity * _windVolume, Time.unscaledDeltaTime * 0.5f);
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
