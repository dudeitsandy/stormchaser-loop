using UnityEngine;
using Unity.Profiling;

/// <summary>Self-installing positional civil-defense sirens driven only by storm lifecycle events.</summary>
public sealed class OutdoorWarningSirens : MonoBehaviour
{
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.Sirens.Update");
    private readonly PresentationEffectsGain _gain = new PresentationEffectsGain();
    [SerializeField, Min(1f)] private float _sirenCycleSeconds = 25f;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.32f;
    [SerializeField, Min(10f)] private float _audibleRange = 230f;
    [SerializeField] private Vector3 _polePosition = new Vector3(35f, 0f, 30f);
    [SerializeField, Min(1f)] private float _fullVolumeDistance = 8f;
    [SerializeField, Min(0.1f)] private float _attackSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float _releaseSeconds = 3f;
    [SerializeField, Min(0f)] private float _attentionHoldSeconds = 5f;
    [SerializeField, Min(0.1f)] private float _settleSeconds = 10f;
    [SerializeField, Range(0f, 1f)] private float _backgroundLevel = 0.25f;
    private readonly OutdoorSirenCycle _cycle = new OutdoorSirenCycle();
    private AudioSource _source;
    private float _level;
    private float _attentionAge;
    private AudioClip _wail;
    private Material _poleMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);
    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<OutdoorWarningSirens>() != null) return;
        new GameObject(nameof(OutdoorWarningSirens)).AddComponent<OutdoorWarningSirens>();
    }
    private void Awake()
    {
        // Smooth 8 s up/down sweep. Its periodic phase integrates frequency, avoiding clicks/chirps.
        const int rate = 22050;
        const float seconds = 8f;
        var samples = new float[(int)(rate * seconds)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float phase = 2f * Mathf.PI * 440f * t - 150f * seconds * Mathf.Sin(2f * Mathf.PI * t / seconds);
            samples[i] = Mathf.Sin(phase) * 0.48f + Mathf.Sin(phase * 2f) * 0.12f + Mathf.Sin(phase * 3f) * 0.04f;
        }
        _wail = AudioClip.Create("OutdoorCivilDefenseWail", samples.Length, 1, rate, false);
        _wail.SetData(samples, 0);
        var template = Resources.Load<Material>("Presentation/VfxCardMaterial");
        var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            _poleMaterial = template != null ? new Material(template) : new Material(shader);
            _poleMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture);
            _poleMaterial.SetColor("_BaseColor", new Color(0.36f, 0.40f, 0.37f, 1f));
        }
        Place(_polePosition);
    }
    private void Place(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out var hit, 150f)) position.y = hit.point.y;
        var pole = new GameObject("OutdoorSirenPole");
        pole.transform.SetParent(transform, false);
        pole.transform.position = position;
        Part(pole.transform, PrimitiveType.Cylinder, new Vector3(0f, 3f, 0f), new Vector3(0.24f, 3f, 0.24f));
        Part(pole.transform, PrimitiveType.Cube, new Vector3(0f, 6.1f, 0f), new Vector3(1.3f, 0.6f, 0.7f));
        var horn = new GameObject("SirenAudio");
        horn.transform.SetParent(pole.transform, false);
        horn.transform.localPosition = Vector3.up * 6.1f;
        var source = horn.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.clip = _wail;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = Mathf.Max(1f, _fullVolumeDistance);
        source.maxDistance = Mathf.Max(source.minDistance + 1f, _audibleRange);
        source.dopplerLevel = 0f;
        _gain.Set(source, 0f);
        _source = source;
    }
    private void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale)
    {
        var part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        var collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        if (_poleMaterial != null) part.GetComponent<Renderer>().sharedMaterial = _poleMaterial;
    }
    private void OnEnable()
    {
        _gain.Enable();
        GameEvents.StormCellForming += Forming;
        GameEvents.StormCellRopeOut += EndEmergency;
        GameEvents.StormCellEnded += EndEmergency;
        GameEvents.RunStarted += Reset;
        GameEvents.RunEnded += EndRun;
    }
    private void Forming(StormCellInfo cell)
    {
        if (_cycle.Forming(cell.CellId, cell.EF, _sirenCycleSeconds)) _attentionAge = 0f;
    }
    private void EndEmergency(StormCellInfo cell) => _cycle.End(cell.CellId);
    private void Update()
    {
        using var sample = UpdateMarker.Auto();
        if (Time.timeScale == 0f)
        {
            _source.Pause();
            return;
        }
        _cycle.Tick(Time.deltaTime);
        bool active = _cycle.Active;
        if (active) _attentionAge += Time.deltaTime;
        float target = StormCueLevels.SirenAttention(_attentionAge, _attentionHoldSeconds, _settleSeconds, _backgroundLevel);
        _level = StormCueLevels.SirenLevel(_level, active, Time.deltaTime, _attackSeconds, _releaseSeconds, target);
        // Resume an interrupted release as well as an active warning; mute never destroys the envelope.
        if (_level > 0f && !_source.isPlaying) { _source.UnPause(); if (!_source.isPlaying) _source.Play(); }
        _gain.Set(_source, _level * _volume);
        if (!active && _level == 0f) _source.Stop();
    }
    private void EndRun(RunSummary summary) => Reset();
    private void Reset()
    {
        _cycle.Reset();
        _level = 0f;
        _attentionAge = 0f;
        if (_source != null) { _source.Stop(); _gain.Set(_source, 0f); }
    }
    private void OnDisable()
    {
        _gain.Disable();
        GameEvents.StormCellForming -= Forming;
        GameEvents.StormCellRopeOut -= EndEmergency;
        GameEvents.StormCellEnded -= EndEmergency;
        GameEvents.RunStarted -= Reset;
        GameEvents.RunEnded -= EndRun;
        Reset();
    }
    private void OnDestroy()
    {
        if (_wail != null) Destroy(_wail);
        if (_poleMaterial != null) Destroy(_poleMaterial);
    }
}
