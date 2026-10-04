using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A spawned storm cell the director is running: plan entry, track, tornado and lifecycle state.</summary>
public sealed class LiveStormCell
{
    public PlannedCell Cell;
    public CellTrack Track;
    public TornadoController Tornado;
    /// <summary>Seconds since spawn.</summary>
    public float Age;
    public StormScale.Phase Phase;
    public float Intensity;
    public bool PeakRaised, RopeOutRaised, Ended;

    /// <summary>World position (y = 0).</summary>
    public Vector3 Position => new Vector3(Track.Position.x, 0f, Track.Position.y);
    public StormCellInfo Info => new StormCellInfo(Cell.Id, Cell.Ef, Cell.Role, Position);
}

/// <summary>
/// The Storm Director runtime (storm-director.md, story 006): builds the compact Weather Plan at run start,
/// spawns each cell at its planned time, drives its tornado from the track model and lifecycle, and raises
/// the <c>StormCell*</c> events (Rule 11). Holds no references to downstream systems; HUD and presentation
/// read <see cref="LiveCells"/> or the events. Game time only, so it pauses with the game.
/// </summary>
public sealed class StormDirector : MonoBehaviour
{
    public enum DirectorState { Idle, Running, Complete }

    [SerializeField] private DirectorTuning _tuning = DirectorTuning.Defaults;
    [SerializeField] private CompactSettings _compact = CompactSettings.Defaults;
    [SerializeField] private TrackTuning _track = TrackTuning.Defaults;
    [Tooltip("Cataclysm Heat 0–5 until Save & Profile supplies it.")]
    [Range(0, 5)] [SerializeField] private int _heat;
    [Tooltip("Compact edge steer-back half extent (m).")]
    [SerializeField] private float _steerBackHalfExtent = 90f;

    private readonly List<LiveStormCell> _live = new List<LiveStormCell>(8);
    private TornadoController _prefab;
    private readonly TornadoData[] _dataByEf = new TornadoData[6];
    private int _nextPlanned;
    private List<PlannedCell> _order;

    public DirectorState State { get; private set; } = DirectorState.Idle;
    /// <summary>The current run's plan (null before the first run).</summary>
    public WeatherPlan Plan { get; private set; }
    /// <summary>Seconds of game time since the run started.</summary>
    public float RunTime { get; private set; }
    /// <summary>Cells currently spawned and not yet ended (read-only for HUD / presentation).</summary>
    public IReadOnlyList<LiveStormCell> LiveCells => _live;

    /// <summary>Starts a run: builds the compact plan from <paramref name="seed"/> around <paramref name="origin"/>.</summary>
    public void Begin(TornadoController prefab, IReadOnlyList<TornadoData> roster, Vector3 origin, long seed)
    {
        StormEfTable table = BindData(prefab, roster);
        WeatherPlan plan = WeatherPlanner.BuildCompact(seed, _heat, _tuning, _compact, table, new Vector2(origin.x, origin.z));
        BeginWithPlan(plan, prefab, roster);
    }

    /// <summary>Starts a run from a given plan (tests and future Arcade scenarios).</summary>
    public void BeginWithPlan(WeatherPlan plan, TornadoController prefab, IReadOnlyList<TornadoData> roster)
    {
        if (State == DirectorState.Running) EndRun();
        BindData(prefab, roster);
        Plan = plan;
        RunTime = 0f;
        _live.Clear();
        _order = new List<PlannedCell>(plan.Cells.Count);
        foreach (PlannedCell c in plan.Cells)
            if (!c.Dropped) _order.Add(c); // a cap-dropped cell never spawns and raises nothing (Rule 11)
        _order.Sort((a, b) => a.SpawnTime != b.SpawnTime ? a.SpawnTime.CompareTo(b.SpawnTime) : a.Id.CompareTo(b.Id));
        _nextPlanned = 0;
        State = DirectorState.Running;
    }

    /// <summary>Run over (timer, wreck, quit): every live cell raises Ended once; nothing else spawns.</summary>
    public void EndRun()
    {
        if (State != DirectorState.Running) return;
        for (int i = 0; i < _live.Count; i++) EndCell(_live[i], destroy: false);
        _live.Clear();
        State = DirectorState.Complete;
    }

    private void Update()
    {
        if (State == DirectorState.Running) Tick(Time.deltaTime);
    }

    /// <summary>Advances the director by <paramref name="dt"/> seconds of game time.</summary>
    public void Tick(float dt)
    {
        if (State != DirectorState.Running || dt <= 0f) return;
        RunTime += dt;

        while (_nextPlanned < _order.Count && _order[_nextPlanned].SpawnTime <= RunTime)
            Spawn(_order[_nextPlanned++]);

        for (int i = _live.Count - 1; i >= 0; i--)
        {
            LiveStormCell cell = _live[i];
            if (cell.Ended) { _live.RemoveAt(i); continue; }
            if (cell.Tornado == null) { EndCell(cell, destroy: false); _live.RemoveAt(i); continue; }
            Advance(cell);
            if (cell.Ended) _live.RemoveAt(i);
        }
    }

    private void Spawn(PlannedCell planned)
    {
        TornadoData data = _dataByEf[planned.Ef];
        if (_prefab == null || data == null) return;
        var track = new CellTrack(planned, unchecked((long)Plan.Seed), Plan.Heat, data.MoveSpeed, data.TurnRateDeg,
                                  Vector2.zero, _steerBackHalfExtent, _track);
        Vector3 pos = new Vector3(planned.Position.x, 0f, planned.Position.y);
        TornadoController tornado = Instantiate(_prefab, pos, Quaternion.identity);
        tornado.Initialize(data, null);
        var cell = new LiveStormCell { Cell = planned, Track = track, Tornado = tornado, Phase = StormScale.Phase.Forming };
        // Already late (a long frame)? Catch the track up to the true age before the first frame.
        cell.Age = Mathf.Max(0f, RunTime - planned.SpawnTime);
        tornado.SetDirectorState(pos, StormScale.Phase.Forming, 0f, 0f);
        _live.Add(cell);
        GameEvents.RaiseStormCellForming(cell.Info);
        Advance(cell);
    }

    private void Advance(LiveStormCell cell)
    {
        PlannedCell p = cell.Cell;
        cell.Age = RunTime - p.SpawnTime;
        cell.Track.AdvanceTo(cell.Age);
        cell.Intensity = cell.Track.IntensityAt(cell.Age);
        cell.Phase = PhaseAt(p, cell.Age);

        if (cell.Phase == StormScale.Phase.Mature && !cell.PeakRaised)
        {
            cell.PeakRaised = true;
            GameEvents.RaiseStormCellPeak(cell.Info);
        }
        bool declining = cell.Phase == StormScale.Phase.RopingOut || cell.Phase == StormScale.Phase.FailedTouchdown;
        if (declining && !cell.RopeOutRaised)
        {
            cell.RopeOutRaised = true;
            GameEvents.RaiseStormCellRopeOut(cell.Info);
        }

        cell.Track.IsTelegraphingAt(cell.Age, out float lean);
        cell.Tornado.SetDirectorState(cell.Position, cell.Phase, cell.Intensity, lean);

        if (cell.Age >= p.EndTime - p.SpawnTime) EndCell(cell, destroy: true);
    }

    /// <summary>F3 phase of a planned cell at an age (early rope-out and failed touchdown included).</summary>
    public static StormScale.Phase PhaseAt(PlannedCell p, float age)
    {
        if (p.EarlyRopeTime >= 0f && age >= p.EarlyRopeTime - p.SpawnTime)
            return p.FailedTouchdown ? StormScale.Phase.FailedTouchdown : StormScale.Phase.RopingOut;
        if (age < p.Form) return StormScale.Phase.Forming;
        if (age < p.Form + p.Mature) return StormScale.Phase.Mature;
        return StormScale.Phase.RopingOut;
    }

    private static void EndCell(LiveStormCell cell, bool destroy)
    {
        if (cell.Ended) return;
        cell.Ended = true;
        GameEvents.RaiseStormCellEnded(cell.Info);
        if (destroy && cell.Tornado != null) Destroy(cell.Tornado.gameObject);
    }

    private StormEfTable BindData(TornadoController prefab, IReadOnlyList<TornadoData> roster)
    {
        _prefab = prefab;
        Array.Clear(_dataByEf, 0, _dataByEf.Length);
        var table = StormEfTable.Defaults;
        foreach (TornadoData d in roster)
        {
            if (d == null || d.EFRating == null || d.EFRating.Length < 3) continue;
            int ef = d.EFRating[2] - '0';
            if (ef < 0 || ef > 5) continue;
            _dataByEf[ef] = d;
            table.Form[ef] = d.FormSeconds;
            table.Mature[ef] = d.MatureSeconds;
            table.Rope[ef] = d.RopeSeconds;
        }
        return table;
    }

    /// <summary>
    /// Run seed: URL <c>?seed=N</c> or desktop <c>-seed=N</c> to replay a run, otherwise a fresh one. The only
    /// non-deterministic input to a run (Rule 10).
    /// </summary>
    public static long ResolveSeed()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith("-seed=", StringComparison.Ordinal) && long.TryParse(arg.Substring(6), out long s)) return s;
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token.StartsWith("seed=", StringComparison.Ordinal) && long.TryParse(token.Substring(5), out long s)) return s;
        return (long)(uint)Environment.TickCount ^ ((long)DateTime.UtcNow.Ticks & 0x7FFFFFFF);
    }
}
