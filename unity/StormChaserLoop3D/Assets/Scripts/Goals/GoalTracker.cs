using System;
using System.Collections.Generic;

/// <summary>
/// Run Goals thresholds (event-system.md Run Goals, Formulas: Goal thresholds; Tuning Knobs). Data, never literals
/// in the tracker.
/// </summary>
[Serializable]
public struct GoalTuning
{
    public float ScoreRookie, ScorePro, ScoreSick;
    public float PointBlankDistance;
    public float DriftSeconds, DriftStormRange;
    public float BigAirSeconds;
    public int NearMissGoal;
    public float TimedBountyWindow;
    public float PointBlankEf5Distance;
    public float CloseCallSeconds, CloseCallRange;
    public float NearMissPairWindow;
    public float DriftByRange;

    public static GoalTuning Defaults => new GoalTuning
    {
        ScoreRookie = 750f, ScorePro = 1500f, ScoreSick = 3000f,
        PointBlankDistance = 20f,
        DriftSeconds = 3f, DriftStormRange = 60f,
        // Measured 2026-10-04 (production/qa/evidence/rg-airtime-evidence.md): EF4 toss 1.12 s, EF5 1.36 s, jumps 0.
        BigAirSeconds = 1.0f,
        NearMissGoal = 3,
        TimedBountyWindow = 40f,
        PointBlankEf5Distance = 12f,
        CloseCallSeconds = 5f, CloseCallRange = 30f,
        NearMissPairWindow = 10f,
        DriftByRange = 40f,
    };
}

/// <summary>What a photo was of, as the goals need it (filled by the runner from <see cref="PhotoResult"/>).</summary>
public struct PhotoFacts
{
    public ShotTier Tier;
    /// <summary>Horizontal distance to the subject (m).</summary>
    public float Distance;
    /// <summary>Subject EF 0–5, or −1 for no subject.</summary>
    public int SubjectEf;
    public StormScale.Phase SubjectPhase;
    /// <summary>Director cell ID of the subject, or −1.</summary>
    public int SubjectCellId;
    public bool SubjectIsAnchor;
}

/// <summary>
/// Run Goals evaluation (event-system.md Run Goals Rules 2–4, 7; Formulas): decides which career goals and drawn
/// bounties a run completes, from plain inputs. Each goal completes and pays at most once per run; completions and
/// timed-bounty failures go out through the callbacks (the runner forwards them to <see cref="GameEvents"/>).
/// Pure C#, no Unity objects.
/// </summary>
public sealed class GoalTracker
{
    private readonly GoalTuning _t;
    private readonly HashSet<string> _activeBounties = new HashSet<string>();
    private readonly HashSet<string> _done = new HashSet<string>();
    private readonly HashSet<string> _failed = new HashSet<string>();
    private readonly List<GoalCompletion> _completions = new List<GoalCompletion>();
    private readonly Func<string, bool> _hasCompletedBefore;
    private readonly Action<GoalCompletion> _onCompleted;
    private readonly Action<string> _onBountyFailed;

    private int _nearMisses;
    private float _lastNearMissTime = float.NegativeInfinity;
    private bool _tossed;
    private float _closeCallSeconds;
    private int _warningCellId = -1;
    private float _warningStart;
    private bool _runEnded;

    /// <param name="bounties">This run's drawn bounties (<see cref="BountyDraw.Draw"/>).</param>
    /// <param name="hasCompletedBefore">True when the accomplishments record already holds the ID (for FirstEver).</param>
    public GoalTracker(IEnumerable<GoalDef> bounties, GoalTuning tuning, Func<string, bool> hasCompletedBefore,
                       Action<GoalCompletion> onCompleted, Action<string> onBountyFailed)
    {
        _t = tuning;
        if (bounties != null) foreach (GoalDef b in bounties) _activeBounties.Add(b.Id);
        _hasCompletedBefore = hasCompletedBefore ?? (_ => false);
        _onCompleted = onCompleted;
        _onBountyFailed = onBountyFailed;
    }

    /// <summary>Completions so far this run, in order.</summary>
    public IReadOnlyList<GoalCompletion> Completions => _completions;
    /// <summary>Sum of this run's goal bonuses (added to the run score by the runner).</summary>
    public int BonusTotal { get; private set; }
    /// <summary>Drawn bounties whose window failed this run.</summary>
    public IReadOnlyCollection<string> FailedBounties => _failed;
    public bool IsDone(string id) => _done.Contains(id);

    // ---------- inputs ----------

    /// <summary>A photo with a subject.</summary>
    public void OnPhoto(PhotoFacts p, float time)
    {
        if (p.SubjectEf < 0) return;
        if (p.Distance < _t.PointBlankDistance) Complete("career", "point_blank");
        if (p.SubjectEf >= 4 && p.SubjectPhase == StormScale.Phase.Mature) Complete("career", "ef4_peak");
        if (p.Tier == ShotTier.Perfect && p.SubjectIsAnchor) Complete("career", "front_page");

        if (p.SubjectEf >= 5 && p.Distance < _t.PointBlankEf5Distance) Complete("bounty", "point_blank_ef5");
        if (p.SubjectIsAnchor && p.SubjectPhase == StormScale.Phase.Forming) Complete("bounty", "before_touchdown");
        if (p.SubjectPhase == StormScale.Phase.RopingOut) Complete("bounty", "rope_out");
        if (_warningCellId >= 0 && p.SubjectCellId == _warningCellId && time - _warningStart <= _t.TimedBountyWindow)
            Complete("bounty", "warning_cell");
    }

    /// <summary>
    /// A style moment (drift fires at slide end with its duration; airtime fires on landing with counted seconds;
    /// near miss with amount 1). <paramref name="nearestTornadoDistance"/> is measured when the moment fires.
    /// </summary>
    public void OnStyle(StyleKind kind, float amount, float nearestTornadoDistance, float time)
    {
        switch (kind)
        {
            case StyleKind.Drift:
                if (amount >= _t.DriftSeconds && nearestTornadoDistance <= _t.DriftStormRange) Complete("career", "storm_drift");
                if (nearestTornadoDistance <= _t.DriftByRange) Complete("bounty", "drift_by");
                break;
            case StyleKind.Airtime:
                if (amount >= _t.BigAirSeconds) Complete("career", "big_air");
                break;
            case StyleKind.NearMiss:
                _nearMisses++;
                if (_nearMisses >= _t.NearMissGoal) Complete("career", "near_misses");
                if (time - _lastNearMissTime <= _t.NearMissPairWindow) Complete("bounty", "double_near_miss");
                _lastNearMissTime = time;
                break;
        }
    }

    /// <summary>The truck was tossed by a tornado.</summary>
    public void OnTossed() => _tossed = true;

    /// <summary>A cell started Forming. The first true-EF3+ one is the TORNADO WARNING cell the timed bounty names.</summary>
    public void OnCellForming(int cellId, int trueEf, float time)
    {
        if (_warningCellId >= 0 || trueEf < 3 || !_activeBounties.Contains(Id("bounty", "warning_cell"))) return;
        _warningCellId = cellId;
        _warningStart = time;
    }

    /// <summary>A cell ended. If it was the warning cell and its bounty is still open, the bounty fails.</summary>
    public void OnCellEnded(int cellId)
    {
        if (cellId == _warningCellId) Fail("warning_cell");
    }

    /// <summary>
    /// Per frame: the timed window, and <c>close_call</c> time. <paramref name="nearestEf3PlusOnGroundDistance"/> is the
    /// distance to the nearest EF3+ tornado in Mature or RopingOut (∞ when none).
    /// </summary>
    public void Tick(float dt, float time, float nearestEf3PlusOnGroundDistance)
    {
        if (_warningCellId >= 0 && time - _warningStart > _t.TimedBountyWindow) Fail("warning_cell");
        if (nearestEf3PlusOnGroundDistance <= _t.CloseCallRange)
        {
            _closeCallSeconds += dt;
            // Summed frame times drift low (50 × 0.1f = 4.9999995), so allow a hair of float error.
            if (_closeCallSeconds + 1e-4f >= _t.CloseCallSeconds) Complete("bounty", "close_call");
        }
    }

    /// <summary>
    /// Run end (timer or wreck; a forfeit never calls this). Judges score tiers on the final score including goal
    /// bonuses, and <c>toss_survivor</c>; an open timed bounty fails.
    /// </summary>
    public void OnRunEnd(float finalScore, bool endedOnTimer)
    {
        if (_runEnded) return;
        _runEnded = true;
        if (finalScore >= _t.ScoreRookie) Complete("career", "score_rookie");
        if (finalScore >= _t.ScorePro) Complete("career", "score_pro");
        if (finalScore >= _t.ScoreSick) Complete("career", "score_sick");
        if (_tossed && endedOnTimer) Complete("career", "toss_survivor");
        Fail("warning_cell"); // still open at run end (window running, or no warning ever fired): no-op if done
    }

    // ---------- internals ----------

    private static string Id(string kind, string key) =>
        kind == "career" ? $"career.{GoalCatalogue.Mode}.{GoalCatalogue.Map}.{key}" : $"bounty.{GoalCatalogue.Mode}.{key}";

    private void Complete(string kind, string key)
    {
        string id = Id(kind, key);
        if (_done.Contains(id) || _failed.Contains(id)) return;
        if (kind == "bounty" && !_activeBounties.Contains(id)) return;
        GoalDef def = GoalCatalogue.Find(id);
        if (def == null) return;
        _done.Add(id);
        var c = new GoalCompletion(id, def.Kind, def.Bonus, !_hasCompletedBefore(id));
        BonusTotal += def.Bonus;
        _completions.Add(c);
        _onCompleted?.Invoke(c);
    }

    private void Fail(string key)
    {
        string id = Id("bounty", key);
        if (!_activeBounties.Contains(id) || _done.Contains(id) || _failed.Contains(id)) return;
        _failed.Add(id);
        _onBountyFailed?.Invoke(id);
    }
}
