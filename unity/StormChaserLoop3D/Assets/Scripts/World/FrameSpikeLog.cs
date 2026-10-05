using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// S9-04 diagnostics (M1 "no frame > 50 ms"): when a frame runs long, logs what happened around it, so the heavy
/// frames can be pinned on GC, storm spawns / phase changes, impacts, audio or debris before anything is tuned. URL
/// <c>?spikeLog=1</c>, desktop <c>-spikeLog=1</c>; machine-readable "[SPIKE]" lines (the WebGL console is captured by
/// <c>tools/perf/webgl-frametime.mjs</c>). Observations only. Never active in normal play.
/// </summary>
[DefaultExecutionOrder(-10000)] // first Update: the frame interval measured here is the previous frame's cost
public sealed class FrameSpikeLog : MonoBehaviour
{
    private const string Arg = "spikeLog=1";
    [Tooltip("Frames longer than this (ms, frame-to-frame interval) are logged. 30 catches every 2+ vsync frame at 60 Hz.")]
    [SerializeField] private float _thresholdMs = 30f;
    [Tooltip("Seconds between [SPIKE-SUMMARY] lines.")]
    [SerializeField] private float _summaryEvery = 30f;

    private readonly List<(int frame, string what)> _events = new List<(int, string)>();
    private readonly StringBuilder _sb = new StringBuilder(256);
    private int _lastGc0, _lastGc1, _lastGc2, _spikes, _gcFrames, _frames, _skipFrame = -1;
    private long _lastHeap;
    private float _startTime, _nextSummary, _worstMs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);

    private static void Install()
    {
        if (!Requested() || Object.FindAnyObjectByType<FrameSpikeLog>() != null) return;
        new GameObject(nameof(FrameSpikeLog)).AddComponent<FrameSpikeLog>();
    }

    private static bool Requested()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg == "-" + Arg) return true;
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token == Arg) return true;
        return false;
    }

    private void OnEnable()
    {
        GameEvents.StormCellForming += OnForming;
        GameEvents.StormCellPeak += OnPeak;
        GameEvents.StormCellRopeOut += OnRopeOut;
        GameEvents.StormCellEnded += OnEnded;
        GameEvents.VehicleImpact += OnImpact;
        GameEvents.Landed += OnLanded;
        GameEvents.Tossed += OnTossed;
        GameEvents.PhotoTaken += OnPhoto;
        GameEvents.GoalCompleted += OnGoal;
        GameEvents.RunStarted += OnRunStarted;
        _lastGc0 = GC.CollectionCount(0);
        _lastGc1 = GC.CollectionCount(1);
        _lastGc2 = GC.CollectionCount(2);
        _lastHeap = GC.GetTotalMemory(false);
        _startTime = Time.realtimeSinceStartup;
        _nextSummary = _startTime + _summaryEvery;
        Debug.Log($"[SPIKE] start: threshold {_thresholdMs:0} ms");
    }

    private void OnDisable()
    {
        GameEvents.StormCellForming -= OnForming;
        GameEvents.StormCellPeak -= OnPeak;
        GameEvents.StormCellRopeOut -= OnRopeOut;
        GameEvents.StormCellEnded -= OnEnded;
        GameEvents.VehicleImpact -= OnImpact;
        GameEvents.Landed -= OnLanded;
        GameEvents.Tossed -= OnTossed;
        GameEvents.PhotoTaken -= OnPhoto;
        GameEvents.GoalCompleted -= OnGoal;
        GameEvents.RunStarted -= OnRunStarted;
    }

    private void OnForming(StormCellInfo c) => Note($"forming #{c.CellId} EF{c.EF}");
    private void OnPeak(StormCellInfo c) => Note($"peak #{c.CellId} EF{c.EF}");
    private void OnRopeOut(StormCellInfo c) => Note($"ropeout #{c.CellId} EF{c.EF}");
    private void OnEnded(StormCellInfo c) => Note($"ended #{c.CellId} EF{c.EF}");
    private void OnImpact(ImpactInfo i) => Note($"impact {i.Kind} {i.HpLoss}hp");
    private void OnLanded(float v) => Note($"landed {v:0.0}");
    private void OnTossed() => Note("tossed");
    private void OnPhoto(PhotoResult _) => Note("photo");
    private void OnGoal(GoalCompletion _) => Note("goal");
    private void OnRunStarted() => Note("runstarted");

    private void Note(string what) => _events.Add((Time.frameCount, what));

    private void Update()
    {
        float ms = Time.unscaledDeltaTime * 1000f;
        int frame = Time.frameCount;
        _frames++;
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        long heap = GC.GetTotalMemory(false);
        int gcRan = (gc0 - _lastGc0) + (gc1 - _lastGc1) + (gc2 - _lastGc2);
        if (gcRan > 0) _gcFrames++;

        if (ms > _thresholdMs && frame != _skipFrame)
        {
            _spikes++;
            _worstMs = Mathf.Max(_worstMs, ms);
            _sb.Clear();
            _sb.Append("[SPIKE] t=").Append((Time.realtimeSinceStartup - _startTime).ToString("0.0"))
               .Append("s frame=").Append(ms.ToString("0.0")).Append("ms gc=").Append(gcRan)
               .Append(" heapDeltaKB=").Append(((heap - _lastHeap) / 1024).ToString())
               .Append(" heapMB=").Append((heap / (1024 * 1024)).ToString())
               .Append(" events=[");
            bool any = false;
            foreach ((int f, string what) in _events)
            {
                if (f < frame - 1) continue;
                if (any) _sb.Append(", ");
                _sb.Append(what);
                any = true;
            }
            _sb.Append("] ").Append(SceneCounts());
            Debug.Log(_sb.ToString());
            _skipFrame = frame + 1; // the counting above can itself cost a little; don't blame the next frame on it
        }

        if (Time.realtimeSinceStartup >= _nextSummary)
        {
            _nextSummary += _summaryEvery;
            Debug.Log($"[SPIKE-SUMMARY] t={Time.realtimeSinceStartup - _startTime:0}s frames={_frames} spikes={_spikes} " +
                      $"worst={_worstMs:0.0}ms gcFrames={_gcFrames} heapMB={heap / (1024 * 1024)}");
        }

        _events.RemoveAll(e => e.frame < frame - 1);
        _lastGc0 = gc0;
        _lastGc1 = gc1;
        _lastGc2 = gc2;
        _lastHeap = heap;
    }

    // Only on a spike: live funnels, playing audio sources and awake rigidbodies (the usual suspects).
    private static string SceneCounts()
    {
        int funnels = Object.FindObjectsByType<TornadoController>(FindObjectsSortMode.None).Length;
        int playing = 0;
        foreach (AudioSource a in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (a.isPlaying) playing++;
        int awake = 0, bodies = 0;
        foreach (Rigidbody rb in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            bodies++;
            if (!rb.IsSleeping()) awake++;
        }
        return $"funnels={funnels} audioPlaying={playing} bodiesAwake={awake}/{bodies}";
    }
}
