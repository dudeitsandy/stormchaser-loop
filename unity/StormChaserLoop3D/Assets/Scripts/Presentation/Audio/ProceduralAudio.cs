using System.Collections.Generic;
using UnityEngine;

/// <summary>Synthesized truck, wind, shutter, damage and end-of-run audio without imported assets.</summary>
public sealed class ProceduralAudio : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _effectsVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float _engineVolume = 0.16f;
    [SerializeField, Range(0f, 1f)] private float _windVolume = 0.4f;
    [SerializeField] private float _windRange = 65f;
    private PlayerVehicle _vehicle;
    private AudioSource _effects, _engine, _wind;
    private AudioClip _shutter, _dryShutter, _hit, _finish, _wreck;
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
        _shutter = Clip("Shutter", 0.15f, 0);
        _dryShutter = Clip("DryShutter", 0.055f, 6);
        _hit = Clip("Damage", 0.35f, 1);
        _finish = Clip("RunComplete", 0.8f, 2);
        _wreck = Clip("Wreck", 0.8f, 3);
        _engine.clip = Clip("EngineLoop", 1f, 4);
        _wind.clip = Clip("WindLoop", 2f, 5);
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
        GameEvents.PhotoTaken += Photo;
        GameEvents.PhotoMissed += Shutter;
        GameEvents.OutOfFilm += DryShutter;
        GameEvents.PlayerDamaged += Hit;
        GameEvents.RunEnded += End;
    }
    private void OnDisable()
    {
        GameEvents.PhotoTaken -= Photo;
        GameEvents.PhotoMissed -= Shutter;
        GameEvents.OutOfFilm -= DryShutter;
        GameEvents.PlayerDamaged -= Hit;
        GameEvents.RunEnded -= End;
        if (_engine != null) _engine.Stop();
        if (_wind != null) _wind.Stop();
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
    private void Update()
    {
        bool running = _vehicle != null && _vehicle.InputEnabled;
        float speed = running ? Mathf.Clamp01(Mathf.Abs(_vehicle.CurrentSpeed) / Mathf.Max(1f, _vehicle.MaxSpeed)) : 0;
        _engine.pitch = Mathf.Lerp(0.8f, 2.1f, speed);
        _engine.volume = Mathf.MoveTowards(_engine.volume, running ? _engineVolume * (0.4f + speed * 0.6f) : 0,
            Time.unscaledDeltaTime);
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
