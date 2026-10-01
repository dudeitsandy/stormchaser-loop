using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

/// <summary>
/// S7-01 G1 gate instrumentation (ADR-0004 Validation #1). Takes over physics stepping to time it,
/// records frame times and GC frames during a fixed-length run, shows a live overlay, and logs a
/// machine-readable "[SPIKE-RESULT]" line (readable from the WebGL browser console).
/// Pass: no frame &gt; 50 ms, no GC spike &gt; 5 ms, physics step ≤ 4 ms.
/// </summary>
[DefaultExecutionOrder(100)] // after TileStreamer.Start, so the start log sees the initial ring
public class SpikeMetrics : MonoBehaviour
{
    [SerializeField] private TileStreamer _streamer;
    [SerializeField] private float _warmupSeconds = 5f;
    [SerializeField] private float _durationSeconds = 180f;
    [SerializeField] private float _maxFrameMs = 50f;
    [SerializeField] private float _maxGcSpikeMs = 5f;
    [SerializeField] private float _maxPhysicsMs = 4f;

    private readonly List<float> _frames = new List<float>(20000);
    private readonly List<float> _sortBuffer = new List<float>(20000);
    private float _nextOverlay;
    private readonly Stopwatch _physicsTimer = new Stopwatch();
    private SimulationMode _previousMode;
    private float _maxPhysicsStepMs, _sumPhysicsMs;
    private int _physicsSteps, _gcFrames, _lastGcCount;
    private float _worstGcFrameMs;
    private float _startTime;
    private bool _done;
    private Label _overlay;
    private float _nextLog;

    private void OnEnable()
    {
        _previousMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
    }

    private void OnDisable() => Physics.simulationMode = _previousMode;

    private void Start()
    {
        _startTime = Time.realtimeSinceStartup;
        _lastGcCount = GC.CollectionCount(0);
        BuildOverlay();
        Debug.Log($"[SPIKE] start: initial ring {_streamer.InitialLoadMs:F0} ms, {_streamer.ActiveTiles} tiles");
    }

    private void FixedUpdate()
    {
        _physicsTimer.Restart();
        Physics.Simulate(Time.fixedDeltaTime);
        float ms = (float)_physicsTimer.Elapsed.TotalMilliseconds;
        if (Recording)
        {
            _maxPhysicsStepMs = Mathf.Max(_maxPhysicsStepMs, ms);
            _sumPhysicsMs += ms;
            _physicsSteps++;
        }
    }

    private bool Recording
    {
        get
        {
            float t = Time.realtimeSinceStartup - _startTime;
            return !_done && t >= _warmupSeconds;
        }
    }

    private void Update()
    {
        if (_done) return;
        float elapsed = Time.realtimeSinceStartup - _startTime;
        float frameMs = Time.unscaledDeltaTime * 1000f;

        int gc = GC.CollectionCount(0);
        bool gcThisFrame = gc != _lastGcCount;
        _lastGcCount = gc;

        if (Recording)
        {
            _frames.Add(frameMs);
            if (gcThisFrame)
            {
                _gcFrames++;
                _worstGcFrameMs = Mathf.Max(_worstGcFrameMs, frameMs);
            }
        }

        // 4 Hz: rebuilding this string every frame was itself a steady GC source.
        if (_overlay != null && Time.realtimeSinceStartup >= _nextOverlay)
        {
            _nextOverlay = Time.realtimeSinceStartup + 0.25f;
            _overlay.text = $"S7-01 SPIKE  t={elapsed:F0}/{_warmupSeconds + _durationSeconds:F0}s  frame {frameMs:F1}ms  " +
                            $"tiles {_streamer.ActiveTiles} q{_streamer.QueueLength}  built {_streamer.TilesBuilt}  " +
                            $"slice≤{_streamer.MaxSliceMs:F1}  bake≤{_streamer.MaxColliderBakeMs:F1}  phys≤{_maxPhysicsStepMs:F2}";
        }

        if (Time.realtimeSinceStartup >= _nextLog && Recording)
        {
            _nextLog = Time.realtimeSinceStartup + 15f;
            Debug.Log($"[SPIKE] t={elapsed:F0}s {Summary(false)}");
        }

        if (elapsed >= _warmupSeconds + _durationSeconds) Finish();
    }

    private void Finish()
    {
        _done = true;
        string result = Summary(true);
        Debug.Log($"[SPIKE-RESULT] {result}");
        if (_overlay != null) _overlay.text = "S7-01 RESULT  " + result;
    }

    private string Summary(bool final)
    {
        if (_frames.Count == 0) return "{}";
        _sortBuffer.Clear();
        _sortBuffer.AddRange(_frames);
        _sortBuffer.Sort();
        List<float> sorted = _sortBuffer;
        float median = sorted[sorted.Count / 2];
        float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
        float max = sorted[sorted.Count - 1];
        int over50 = 0;
        foreach (float f in _frames) if (f > _maxFrameMs) over50++;
        float gcSpike = Mathf.Max(0f, _worstGcFrameMs - median);
        float avgPhys = _physicsSteps > 0 ? _sumPhysicsMs / _physicsSteps : 0f;

        bool pass = max <= _maxFrameMs && gcSpike <= _maxGcSpikeMs && _maxPhysicsStepMs <= _maxPhysicsMs;
        string verdict = final ? (pass ? "\"PASS\"" : "\"FAIL\"") : "\"RUNNING\"";
        return "{" +
               $"\"verdict\":{verdict},\"frames\":{_frames.Count},\"medianMs\":{median:F2},\"p99Ms\":{p99:F2},\"maxMs\":{max:F2}," +
               $"\"framesOver50\":{over50},\"gcFrames\":{_gcFrames},\"gcSpikeMs\":{gcSpike:F2}," +
               $"\"physMaxMs\":{_maxPhysicsStepMs:F2},\"physAvgMs\":{avgPhys:F3}," +
               $"\"sliceMaxMs\":{_streamer.MaxSliceMs:F2},\"colliderBakeMaxMs\":{_streamer.MaxColliderBakeMs:F2}," +
               $"\"tilesBuilt\":{_streamer.TilesBuilt},\"initialLoadMs\":{_streamer.InitialLoadMs:F0}" +
               "}";
    }

    private void BuildOverlay()
    {
        var host = new GameObject("SpikeOverlay");
        host.SetActive(false);
        var doc = host.AddComponent<UIDocument>();
        doc.panelSettings = Resources.Load<PanelSettings>("UI/PanelSettings");
        doc.sortingOrder = 200;
        host.SetActive(true);
        _overlay = new Label { pickingMode = PickingMode.Ignore };
        _overlay.style.position = Position.Absolute;
        _overlay.style.left = 8;
        _overlay.style.top = 8;
        _overlay.style.fontSize = 14;
        _overlay.style.color = Color.white;
        _overlay.style.backgroundColor = new Color(0, 0, 0, 0.6f);
        _overlay.style.paddingLeft = 6;
        _overlay.style.paddingRight = 6;
        doc.rootVisualElement.Add(_overlay);
    }
}
