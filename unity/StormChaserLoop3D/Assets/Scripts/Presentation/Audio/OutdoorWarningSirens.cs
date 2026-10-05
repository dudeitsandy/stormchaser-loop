using System.Collections.Generic;
using UnityEngine;

/// <summary>Self-installing positional civil-defense sirens driven only by storm lifecycle events.</summary>
public sealed class OutdoorWarningSirens : MonoBehaviour
{
    private readonly PresentationEffectsGain _gain = new PresentationEffectsGain();
    [SerializeField, Min(1f)] private float _sirenCycleSeconds = 25f;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.32f;
    [SerializeField, Min(10f)] private float _audibleRange = 230f;
    [SerializeField] private Vector3[] _polePositions = {
        new Vector3(-55f, 0f, -55f), new Vector3(55f, 0f, -55f),
        new Vector3(-55f, 0f, 55f), new Vector3(55f, 0f, 55f) };
    private readonly OutdoorSirenCycle _cycle = new OutdoorSirenCycle();
    private readonly List<AudioSource> _sources = new List<AudioSource>(4);
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
        for (int i = 0; i < Mathf.Min(4, _polePositions.Length); i++) Place(_polePositions[i]);
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
        source.minDistance = 12f;
        source.maxDistance = _audibleRange;
        source.dopplerLevel = 0f;
        _gain.Set(source, 0f);
        _sources.Add(source);
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
        _cycle.Forming(cell.CellId, cell.EF, _sirenCycleSeconds);
    }
    private void EndEmergency(StormCellInfo cell) => _cycle.End(cell.CellId);
    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            foreach (var source in _sources) source.Pause();
            return;
        }
        _cycle.Tick(Time.deltaTime);
        bool active = _cycle.Active;
        foreach (var source in _sources)
        {
            if (active && !source.isPlaying) { source.UnPause(); if (!source.isPlaying) source.Play(); }
            _gain.Set(source, Mathf.MoveTowards(_gain.Get(source), active ? _volume : 0f, Time.deltaTime * _volume * 2f));
            if (!active && _gain.Get(source) == 0f) source.Stop();
        }
    }
    private void EndRun(RunSummary summary) => Reset();
    private void Reset()
    {
        _cycle.Reset();
        foreach (var source in _sources) { source.Stop(); _gain.Set(source, 0f); }
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
