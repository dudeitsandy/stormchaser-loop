using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// S7-06 G1-condition instrumentation: physics step ≤ 4 ms under real debris + vehicle in the shipping
/// scene. URL <c>?physProbe=1</c>, desktop <c>-physProbe=1</c>. Same method as <see cref="SpikeMetrics"/>:
/// takes over physics stepping to time each <c>Physics.Simulate</c>, then logs a machine-readable
/// "[PHYS-RESULT]" line after a fixed run (readable from the WebGL browser console). Also counts vehicle
/// impacts and peak awake rigidbodies so a result proves debris was actually in play. Stops the session
/// timer so the run outlasts the measurement window. Never active in normal play.
/// </summary>
[DefaultExecutionOrder(10000)] // last FixedUpdate: every force for this step is applied before Simulate
public sealed class PhysicsBudgetProbe : MonoBehaviour
{
    private const string Arg = "physProbe=1";
    [SerializeField] private float _warmupSeconds = 5f;
    [SerializeField] private float _durationSeconds = 180f;
    [SerializeField] private float _maxPhysicsMs = 4f;

    private readonly Stopwatch _timer = new Stopwatch();
    private SimulationMode _previousMode;
    private float _startTime, _nextLog, _nextBodySample, _maxStepMs, _maxStepAt, _sumMs;
    private int _steps, _stepsOver, _impacts, _peakAwakeBodies;
    private bool _started, _done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);

    private static void Install()
    {
        if (!Requested()) return;
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<PhysicsBudgetProbe>() != null) return;
        new GameObject(nameof(PhysicsBudgetProbe)).AddComponent<PhysicsBudgetProbe>();
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
        _previousMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        GameEvents.VehicleImpact += OnImpact;
        GameEvents.RunStarted += OnRunStarted;
    }

    private void OnDisable()
    {
        Physics.simulationMode = _previousMode;
        GameEvents.VehicleImpact -= OnImpact;
        GameEvents.RunStarted -= OnRunStarted;
    }

    // A wrecked truck or a retry reloads the scene mid-window; report what was measured instead of losing it.
    private void OnDestroy()
    {
        if (_started && !_done && _steps > 0)
            Debug.Log($"[PHYS-RESULT] {Summary(_stepsOver <= 1 ? "PARTIAL_PASS" : "PARTIAL_FAIL")}");
    }

    // The window opens when the run starts (after the title), not at scene load.
    private void OnRunStarted()
    {
        if (_started) return;
        _started = true;
        _startTime = Time.realtimeSinceStartup;
        // A normal run ends at 90 s; the measurement needs the full window, so the run stays open.
        var timer = FindAnyObjectByType<SessionTimer>();
        if (timer != null) timer.Stop();
        Debug.Log($"[PHYS] start: warm-up {_warmupSeconds:F0} s, run {_durationSeconds:F0} s, budget {_maxPhysicsMs:F1} ms");
    }

    private float Elapsed => Time.realtimeSinceStartup - _startTime;
    private bool Recording => _started && !_done && Elapsed >= _warmupSeconds;

    private void OnImpact(ImpactInfo _)
    {
        if (Recording) _impacts++;
    }

    private void FixedUpdate()
    {
        _timer.Restart();
        Physics.Simulate(Time.fixedDeltaTime);
        float ms = (float)_timer.Elapsed.TotalMilliseconds;
        if (!Recording) return;
        if (ms > _maxStepMs) { _maxStepMs = ms; _maxStepAt = Elapsed; }
        if (ms > _maxPhysicsMs) _stepsOver++;
        _sumMs += ms;
        _steps++;
    }

    private void Update()
    {
        if (!_started || _done) return;
        if (Recording && Time.realtimeSinceStartup >= _nextBodySample)
        {
            // 1 Hz and probe-only: the scan cost lands in Update, not in the timed Simulate.
            _nextBodySample = Time.realtimeSinceStartup + 1f;
            int awake = 0;
            foreach (Rigidbody body in FindObjectsByType<Rigidbody>())
                if (!body.isKinematic && !body.IsSleeping()) awake++;
            _peakAwakeBodies = Mathf.Max(_peakAwakeBodies, awake);
        }
        if (Recording && Time.realtimeSinceStartup >= _nextLog)
        {
            _nextLog = Time.realtimeSinceStartup + 15f;
            Debug.Log($"[PHYS] t={Elapsed:F0}s {Summary("RUNNING")}");
        }
        if (Elapsed >= _warmupSeconds + _durationSeconds)
        {
            _done = true;
            // One isolated first-contact spike is allowed, as at G1 (s7-01-spike-results.md); a repeat is not.
            string verdict = _stepsOver == 0 ? "PASS" : _stepsOver == 1 ? "PASS_ONE_SPIKE" : "FAIL";
            Debug.Log($"[PHYS-RESULT] {Summary(verdict)}");
        }
    }

    private string Summary(string verdict)
    {
        float avg = _steps > 0 ? _sumMs / _steps : 0f;
        return "{" +
               $"\"verdict\":\"{verdict}\",\"steps\":{_steps},\"physMaxMs\":{_maxStepMs:F2},\"physMaxAtS\":{_maxStepAt:F0}," +
               $"\"physAvgMs\":{avg:F3},\"stepsOverBudget\":{_stepsOver},\"impacts\":{_impacts},\"peakAwakeBodies\":{_peakAwakeBodies}" +
               "}";
    }
}
