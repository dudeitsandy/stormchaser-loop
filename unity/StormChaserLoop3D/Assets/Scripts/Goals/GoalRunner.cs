using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs Run Goals in a live run (event-system.md Run Goals; run-goals-v1 story 004): draws the run's bounties from
/// the Storm Director's plan, feeds <see cref="GoalTracker"/> from existing <see cref="GameEvents"/>, adds bonuses
/// to the score and raises <see cref="GameEvents.GoalCompleted"/> / <see cref="GameEvents.BountyFailed"/>.
/// <see cref="RunManager"/> drives its lifetime; it holds no references to presentation.
/// </summary>
public sealed class GoalRunner : MonoBehaviour
{
    [SerializeField] private GoalTuning _tuning = GoalTuning.Defaults;

    private ScoreAccumulator _score;
    private DisasterSpawner _spawner;
    private PlayerVehicle _vehicle;
    private GoalTracker _tracker;
    private readonly List<string> _bountyIds = new List<string>(3);
    private float _time;
    private bool _running;

    /// <summary>
    /// True when the accomplishments record already holds an ID (FirstEver). Defaults to "never" until the
    /// Save &amp; Profile stem supplies it (run-goals-v1 story 005).
    /// </summary>
    public Func<string, bool> HasCompletedBefore { get; set; } = _ => false;

    /// <summary>This run's drawn bounties (empty on the legacy spawner).</summary>
    public IReadOnlyList<string> Bounties => _bountyIds;
    /// <summary>This run's tracker (null before the first run).</summary>
    public GoalTracker Tracker => _tracker;

    /// <summary>Wires the run's systems. Called once by <see cref="RunManager"/>.</summary>
    public void Bind(ScoreAccumulator score, DisasterSpawner spawner, PlayerVehicle vehicle)
    {
        _score = score;
        _spawner = spawner;
        _vehicle = vehicle;
    }

    /// <summary>Starts goal tracking for a run; call after the director has built its plan.</summary>
    public void BeginRun()
    {
        Unsubscribe();
        WeatherPlan plan = _spawner != null && _spawner.Director != null ? _spawner.Director.Plan : null;
        List<GoalDef> bounties = BountyDraw.Draw(plan, BountyDraw.BountiesPerRun);
        _bountyIds.Clear();
        foreach (GoalDef b in bounties) _bountyIds.Add(b.Id);
        _tracker = new GoalTracker(bounties, _tuning, HasCompletedBefore, OnCompleted, GameEvents.RaiseBountyFailed);
        _time = 0f;
        _running = true;
        GameEvents.PhotoTaken += OnPhoto;
        GameEvents.StyleEvent += OnStyle;
        GameEvents.Tossed += OnTossed;
        GameEvents.StormCellForming += OnForming;
        GameEvents.StormCellEnded += OnEnded;
    }

    /// <summary>Run end (timer or wreck): score tiers and toss_survivor are judged; returns the results facts.</summary>
    public RunGoalsInfo EndRun(float finalScore, bool endedOnTimer)
    {
        if (_tracker == null) return default;
        if (_running) _tracker.OnRunEnd(finalScore, endedOnTimer);
        _running = false;
        Unsubscribe();
        return new RunGoalsInfo(new List<GoalCompletion>(_tracker.Completions), new List<string>(_bountyIds),
                                new List<string>(_tracker.FailedBounties));
    }

    /// <summary>Forfeit: stops tracking; nothing is judged or recorded.</summary>
    public void StopRun()
    {
        _running = false;
        Unsubscribe();
    }

    private void OnDisable() => Unsubscribe();

    private void Unsubscribe()
    {
        GameEvents.PhotoTaken -= OnPhoto;
        GameEvents.StyleEvent -= OnStyle;
        GameEvents.Tossed -= OnTossed;
        GameEvents.StormCellForming -= OnForming;
        GameEvents.StormCellEnded -= OnEnded;
    }

    private void Update()
    {
        if (!_running) return;
        float dt = Time.deltaTime;
        _time += dt;
        _tracker.Tick(dt, _time, NearestTornado(ef3PlusOnGround: true));
    }

    private void OnCompleted(GoalCompletion c)
    {
        if (_score != null && c.Bonus > 0) _score.AddBonus(c.Bonus);
        GameEvents.RaiseGoalCompleted(c);
    }

    private void OnPhoto(PhotoResult r)
    {
        var tornado = r.Subject as TornadoController;
        var facts = new PhotoFacts { Tier = r.Tier, SubjectEf = -1, SubjectCellId = -1 };
        if (tornado != null)
        {
            facts.SubjectEf = ParseEf(tornado.EFRating);
            facts.SubjectPhase = tornado.ScalePhase;
            facts.Distance = Flat(r.SubjectPosition);
            LiveStormCell cell = CellOf(tornado);
            if (cell != null)
            {
                facts.SubjectCellId = cell.Cell.Id;
                facts.SubjectIsAnchor = cell.Cell.Role == StormCellRole.Anchor;
            }
        }
        _tracker.OnPhoto(facts, _time);
    }

    private void OnStyle(StyleKind kind, float amount) => _tracker.OnStyle(kind, amount, NearestTornado(false), _time);
    private void OnTossed() => _tracker.OnTossed();
    private void OnForming(StormCellInfo c) => _tracker.OnCellForming(c.CellId, c.EF, _time);
    private void OnEnded(StormCellInfo c) => _tracker.OnCellEnded(c.CellId);

    private LiveStormCell CellOf(TornadoController tornado)
    {
        StormDirector director = _spawner != null ? _spawner.Director : null;
        if (director == null) return null;
        IReadOnlyList<LiveStormCell> cells = director.LiveCells;
        for (int i = 0; i < cells.Count; i++)
            if (cells[i].Tornado == tornado) return cells[i];
        return null;
    }

    // Horizontal distance from the truck to the nearest tornado (optionally EF3+ in Mature or RopingOut).
    private float NearestTornado(bool ef3PlusOnGround)
    {
        float best = float.PositiveInfinity;
        IReadOnlyList<DisasterEntity> active = DisasterEntity.Active;
        for (int i = 0; i < active.Count; i++)
        {
            if (!(active[i] is TornadoController t) || t == null) continue;
            if (ef3PlusOnGround)
            {
                StormScale.Phase phase = t.ScalePhase;
                if (ParseEf(t.EFRating) < 3 || (phase != StormScale.Phase.Mature && phase != StormScale.Phase.RopingOut)) continue;
            }
            best = Mathf.Min(best, Flat(t.transform.position));
        }
        return best;
    }

    private float Flat(Vector3 p)
    {
        if (_vehicle == null) return float.PositiveInfinity;
        Vector3 d = p - _vehicle.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    /// <summary>"EF3" → 3; anything unparseable → 0.</summary>
    public static int ParseEf(string rating) =>
        rating != null && rating.Length >= 3 && char.IsDigit(rating[2]) ? rating[2] - '0' : 0;
}
