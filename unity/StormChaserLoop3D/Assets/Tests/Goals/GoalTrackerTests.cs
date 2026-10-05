using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>event-system.md Run Goals Rules 4, 7 and Formulas (run-goals-v1 story 003).</summary>
public class GoalTrackerTests
{
    private const string C = "career.compact.heartland.";
    private const string B = "bounty.compact.";
    private static readonly GoalTuning T = GoalTuning.Defaults;

    private readonly List<GoalCompletion> _done = new List<GoalCompletion>();
    private readonly List<string> _failed = new List<string>();

    private GoalTracker Tracker(params string[] bountyKeys) => Tracker(_ => false, bountyKeys);

    private GoalTracker Tracker(System.Func<string, bool> before, params string[] bountyKeys)
    {
        _done.Clear();
        _failed.Clear();
        var bounties = bountyKeys.Select(k => GoalCatalogue.Find(B + k)).ToList();
        return new GoalTracker(bounties, T, before, _done.Add, _failed.Add);
    }

    private static PhotoFacts Photo(float distance, int ef = 2, StormScale.Phase phase = StormScale.Phase.Mature,
                                    ShotTier tier = ShotTier.Good, int cell = 1, bool anchor = false) =>
        new PhotoFacts { Distance = distance, SubjectEf = ef, SubjectPhase = phase, Tier = tier, SubjectCellId = cell, SubjectIsAnchor = anchor };

    private bool Done(string id) => _done.Any(c => c.Id == id);

    // ---------- thresholds at the boundary ----------

    [TestCase(2.99f, false)]
    [TestCase(3.0f, true)]
    public void StormDrift_AtSlideEnd_ThreeSecondsWithAStormWithin60m(float seconds, bool expected)
    {
        GoalTracker t = Tracker();
        t.OnStyle(StyleKind.Drift, seconds, 59f, 1f);
        Assert.AreEqual(expected, Done(C + "storm_drift"));
    }

    [Test]
    public void StormDrift_StormFartherThan60m_DoesNotCount()
    {
        GoalTracker t = Tracker();
        t.OnStyle(StyleKind.Drift, 5f, 60.5f, 1f);
        Assert.IsFalse(Done(C + "storm_drift"));
    }

    [TestCase(0.99f, false)]
    [TestCase(1.0f, true)]
    public void BigAir_CountedSecondsAtTheMeasuredThreshold(float counted, bool expected)
    {
        GoalTracker t = Tracker();
        t.OnStyle(StyleKind.Airtime, counted, float.PositiveInfinity, 1f);
        Assert.AreEqual(expected, Done(C + "big_air"));
    }

    [TestCase(19.9f, true)]
    [TestCase(20.0f, false)]
    public void PointBlank_StrictlyUnder20m(float distance, bool expected)
    {
        GoalTracker t = Tracker();
        t.OnPhoto(Photo(distance), 1f);
        Assert.AreEqual(expected, Done(C + "point_blank"));
    }

    [TestCase(9.9f, true)]
    [TestCase(10.1f, false)]
    public void DoubleNearMiss_SecondWithinTenSeconds(float gap, bool expected)
    {
        GoalTracker t = Tracker("double_near_miss");
        t.OnStyle(StyleKind.NearMiss, 1f, 10f, 5f);
        t.OnStyle(StyleKind.NearMiss, 1f, 10f, 5f + gap);
        Assert.AreEqual(expected, Done(B + "double_near_miss"));
    }

    [Test]
    public void NearMisses_ThreeInARun()
    {
        GoalTracker t = Tracker();
        t.OnStyle(StyleKind.NearMiss, 1f, 10f, 10f);
        t.OnStyle(StyleKind.NearMiss, 1f, 10f, 40f);
        Assert.IsFalse(Done(C + "near_misses"));
        t.OnStyle(StyleKind.NearMiss, 1f, 10f, 80f);
        Assert.IsTrue(Done(C + "near_misses"));
    }

    // ---------- score tiers ----------

    [Test]
    public void ScoreTiers_1499MeetsRookieOnly_1500MeetsRookieAndPro_NoBonus()
    {
        GoalTracker t = Tracker();
        t.OnRunEnd(1499f, endedOnTimer: true);
        Assert.IsTrue(Done(C + "score_rookie"));
        Assert.IsFalse(Done(C + "score_pro"));

        t = Tracker();
        t.OnRunEnd(1500f, endedOnTimer: true);
        Assert.IsTrue(Done(C + "score_rookie"));
        Assert.IsTrue(Done(C + "score_pro"));
        Assert.IsFalse(Done(C + "score_sick"));
        Assert.AreEqual(0, t.BonusTotal, "score goals pay no bonus");
    }

    // ---------- bonuses ----------

    [Test]
    public void AGoalMetTwiceInOneRun_PaysOnce()
    {
        GoalTracker t = Tracker();
        t.OnPhoto(Photo(10f), 1f);
        t.OnPhoto(Photo(10f), 2f);
        Assert.AreEqual(1, _done.Count(c => c.Id == C + "point_blank"));
        Assert.AreEqual(150, t.BonusTotal);
    }

    [Test]
    public void OnePhoto_MeetingFrontPageEf4PeakPointBlankAndABounty_PaysAllFour()
    {
        GoalTracker t = Tracker("point_blank_ef5");
        t.OnPhoto(Photo(10f, ef: 5, phase: StormScale.Phase.Mature, tier: ShotTier.Perfect, anchor: true), 1f);
        CollectionAssert.AreEquivalent(
            new[] { C + "point_blank", C + "ef4_peak", C + "front_page", B + "point_blank_ef5" },
            _done.Select(c => c.Id));
        Assert.AreEqual(150 * 3 + 500, t.BonusTotal);
    }

    [Test]
    public void UndrawnBounty_NeverCompletes()
    {
        GoalTracker t = Tracker("rope_out");
        t.OnPhoto(Photo(10f, ef: 5, tier: ShotTier.Perfect, anchor: true), 1f);
        Assert.IsFalse(Done(B + "point_blank_ef5"), "point_blank_ef5 was not drawn this run");
    }

    // ---------- events ----------

    [Test]
    public void GoalCompleted_CarriesKindBonusAndFirstEver()
    {
        GoalTracker t = Tracker(id => id == C + "big_air", "rope_out");
        t.OnStyle(StyleKind.Airtime, 1.2f, float.PositiveInfinity, 1f);
        t.OnPhoto(Photo(50f, phase: StormScale.Phase.RopingOut), 2f);
        GoalCompletion air = _done.Single(c => c.Id == C + "big_air");
        GoalCompletion rope = _done.Single(c => c.Id == B + "rope_out");
        Assert.AreEqual(GoalKind.Career, air.Kind);
        Assert.AreEqual(150, air.Bonus);
        Assert.IsFalse(air.FirstEver, "already in the record");
        Assert.AreEqual(GoalKind.Bounty, rope.Kind);
        Assert.AreEqual(200, rope.Bonus);
        Assert.IsTrue(rope.FirstEver);
    }

    [Test]
    public void WarningCell_ShotWithinWindow_Completes()
    {
        GoalTracker t = Tracker("warning_cell");
        t.OnCellForming(cellId: 2, trueEf: 2, time: 5f);   // EF2: no warning
        t.OnCellForming(cellId: 4, trueEf: 3, time: 10f);  // the warning cell
        t.OnPhoto(Photo(80f, ef: 3, phase: StormScale.Phase.Forming, cell: 4), 49f);
        Assert.IsTrue(Done(B + "warning_cell"));
        CollectionAssert.IsEmpty(_failed);
    }

    [Test]
    public void WarningCell_WindowCloses_RaisesBountyFailedOnce_ALaterShotDoesNotCount()
    {
        GoalTracker t = Tracker("warning_cell");
        t.OnCellForming(4, 4, 10f);
        t.Tick(0.1f, 50.05f, float.PositiveInfinity);
        t.Tick(0.1f, 50.15f, float.PositiveInfinity);
        CollectionAssert.AreEqual(new[] { B + "warning_cell" }, _failed);
        t.OnPhoto(Photo(30f, ef: 4, cell: 4), 51f);
        Assert.IsFalse(Done(B + "warning_cell"));
        t.OnRunEnd(0f, true);
        Assert.AreEqual(1, _failed.Count, "fails once");
    }

    [Test]
    public void WarningCell_NoWarningAllRun_FailsAtRunEnd()
    {
        GoalTracker t = Tracker("warning_cell");
        t.OnRunEnd(0f, endedOnTimer: true);
        CollectionAssert.AreEqual(new[] { B + "warning_cell" }, _failed);
    }

    [Test]
    public void WarningCell_EndsBeforeTheShot_Fails()
    {
        GoalTracker t = Tracker("warning_cell");
        t.OnCellForming(4, 3, 10f);
        t.OnCellEnded(4);
        CollectionAssert.AreEqual(new[] { B + "warning_cell" }, _failed);
    }

    // ---------- the rest of the pool ----------

    [Test]
    public void CloseCall_FiveSecondsInTotalWithin30mOfAnEf3OnTheGround()
    {
        GoalTracker t = Tracker("close_call");
        for (int i = 0; i < 49; i++) t.Tick(0.1f, i * 0.1f, 25f);
        Assert.IsFalse(Done(B + "close_call"));
        t.Tick(0.1f, 30f, 100f); // stepping away does not reset the total
        t.Tick(0.1f, 30.1f, 29f);
        Assert.IsTrue(Done(B + "close_call"));
    }

    [Test]
    public void BeforeTouchdown_AnchorWhileForming_DriftBy_DriftEndingWithin40m()
    {
        GoalTracker t = Tracker("before_touchdown", "drift_by");
        t.OnPhoto(Photo(90f, ef: 4, phase: StormScale.Phase.Forming, anchor: false), 1f);
        Assert.IsFalse(Done(B + "before_touchdown"), "not the anchor");
        t.OnPhoto(Photo(90f, ef: 4, phase: StormScale.Phase.Forming, anchor: true), 2f);
        Assert.IsTrue(Done(B + "before_touchdown"));
        t.OnStyle(StyleKind.Drift, 0.6f, 41f, 3f);
        Assert.IsFalse(Done(B + "drift_by"));
        t.OnStyle(StyleKind.Drift, 0.6f, 40f, 4f);
        Assert.IsTrue(Done(B + "drift_by"));
    }

    [Test]
    public void TossSurvivor_NeedsATossAndATimerEnd()
    {
        GoalTracker t = Tracker();
        t.OnTossed();
        t.OnRunEnd(0f, endedOnTimer: false);
        Assert.IsFalse(Done(C + "toss_survivor"), "wrecked");

        t = Tracker();
        t.OnTossed();
        t.OnRunEnd(0f, endedOnTimer: true);
        Assert.IsTrue(Done(C + "toss_survivor"));
    }
}
