using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// storm-director.md story 006 in the real scene: StormCell lifecycle events (AC-20), tracks independent of
/// the player (AC-17), pause (AC-22) and per-frame cost (AC-28). Plans are hand-made so each path is exact.
/// </summary>
public class StormDirectorPlayTests
{
    private const string SceneName = "VerificationScene";
    private readonly List<string> _events = new List<string>();
    private readonly List<StormCellInfo> _payloads = new List<StormCellInfo>();
    private StormDirector _director;
    private TornadoController _prefab;
    private List<TornadoData> _roster;

    private void OnForming(StormCellInfo c) { _events.Add($"Forming:{c.CellId}"); _payloads.Add(c); }
    private void OnPeak(StormCellInfo c) { _events.Add($"Peak:{c.CellId}"); _payloads.Add(c); }
    private void OnRopeOut(StormCellInfo c) { _events.Add($"RopeOut:{c.CellId}"); _payloads.Add(c); }
    private void OnEnded(StormCellInfo c) { _events.Add($"Ended:{c.CellId}"); _payloads.Add(c); }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
        var spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        spawner.Stop();
        const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        _prefab = (TornadoController)typeof(DisasterSpawner).GetField("_tornadoPrefab", Hidden).GetValue(spawner);
        _roster = ((List<DisasterSpawner.RosterEntry>)typeof(DisasterSpawner).GetField("_roster", Hidden).GetValue(spawner))
                  .Select(e => e.Data).ToList();
        _director = new GameObject("TestDirector").AddComponent<StormDirector>();
        _events.Clear();
        _payloads.Clear();
        GameEvents.StormCellForming += OnForming;
        GameEvents.StormCellPeak += OnPeak;
        GameEvents.StormCellRopeOut += OnRopeOut;
        GameEvents.StormCellEnded += OnEnded;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        GameEvents.StormCellForming -= OnForming;
        GameEvents.StormCellPeak -= OnPeak;
        GameEvents.StormCellRopeOut -= OnRopeOut;
        GameEvents.StormCellEnded -= OnEnded;
        Time.timeScale = 1f;
        Scene empty = SceneManager.CreateScene("StormDirectorEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    private static PlannedCell Cell(int id, int ef, StormCellRole role, float spawn, float form, float mature, float rope) =>
        new PlannedCell
        {
            Id = id, Ef = ef, Role = role, Position = new Vector2(20f + 10f * id, 30f),
            DesiredTime = spawn, SpawnTime = spawn, Form = form, Mature = mature, Rope = rope,
        };

    private static WeatherPlan PlanOf(params PlannedCell[] cells)
    {
        var plan = new WeatherPlan { Seed = 1, Heat = 0, Regime = Regime.Quiet, AnchorEf = 1, Duration = 180f, Compact = true };
        plan.Cells.AddRange(cells);
        return plan;
    }

    private IEnumerator RunFor(float seconds)
    {
        for (float t = 0f; t < seconds; t += 0.05f)
        {
            _director.Tick(0.05f);
            yield return null;
        }
    }

    // ---------- AC-20: lifecycle events ----------

    [UnityTest]
    public IEnumerator FullLife_RaisesFormingPeakRopeOutEnded_OnceEachInOrder_AndDestroysTheTornado()
    {
        _director.enabled = false; // ticked manually
        _director.BeginWithPlan(PlanOf(Cell(7, 2, StormCellRole.Anchor, 0.1f, 1f, 1f, 1f)), _prefab, _roster);
        yield return RunFor(3.6f);

        CollectionAssert.AreEqual(new[] { "Forming:7", "Peak:7", "RopeOut:7", "Ended:7" }, _events);
        foreach (StormCellInfo info in _payloads)
        {
            Assert.AreEqual(7, info.CellId);
            Assert.AreEqual(2, info.EF);
            Assert.AreEqual(StormCellRole.Anchor, info.Role);
        }
        Assert.AreEqual(0, _director.LiveCells.Count);
        yield return null;
        Assert.IsFalse(Object.FindObjectsByType<TornadoController>().Any(t => t.DirectorDriven), "tornado destroyed at Ended");
    }

    // ---------- Codex review 2026-10-04: long ticks, callback state, restart ----------

    [UnityTest]
    public IEnumerator SingleLongTick_RaisesEveryCrossedTransitionInOrder()
    {
        _director.enabled = false;
        _director.BeginWithPlan(PlanOf(Cell(7, 2, StormCellRole.Anchor, 0.1f, 1f, 1f, 1f)), _prefab, _roster);
        _director.Tick(3.7f); // spawns and jumps straight past Mature and rope-out to the end
        yield return null;

        CollectionAssert.AreEqual(new[] { "Forming:7", "Peak:7", "RopeOut:7", "Ended:7" }, _events);
        Assert.IsTrue(_director.AnchorPeaked, "a full life peaked even though no frame landed in Mature");
    }

    [UnityTest]
    public IEnumerator EarlyRopeAfterTouchdown_JumpedInOneTick_StillRaisesPeakBeforeRopeOut()
    {
        _director.enabled = false;
        PlannedCell cell = Cell(4, 3, StormCellRole.Anchor, 0.1f, 1f, 10f, 2f);
        cell.EarlyRopeTime = 1.5f; // 1.4 s into its life: touched down, then evicted
        _director.BeginWithPlan(PlanOf(cell), _prefab, _roster);
        _director.Tick(0.5f);
        _director.Tick(1.5f); // Forming → past the early rope-out, never ticked while Mature
        yield return null;

        CollectionAssert.AreEqual(new[] { "Forming:4", "Peak:4", "RopeOut:4" }, _events);
    }

    [UnityTest]
    public IEnumerator PeakAndRopeOutListeners_SeeTheTornadoAlreadyInItsNewPhase()
    {
        _director.enabled = false;
        _director.BeginWithPlan(PlanOf(Cell(2, 2, StormCellRole.Anchor, 0.1f, 1f, 1f, 1f)), _prefab, _roster);
        var seen = new List<StormScale.Phase>();
        void Capture(StormCellInfo c) => seen.Add(_director.LiveCells[0].Tornado.ScalePhase);
        GameEvents.StormCellPeak += Capture;
        GameEvents.StormCellRopeOut += Capture;
        try
        {
            _director.Tick(0.2f);
            _director.Tick(1.0f); // age 1.1: Mature
            _director.Tick(1.0f); // age 2.1: RopingOut
        }
        finally
        {
            GameEvents.StormCellPeak -= Capture;
            GameEvents.StormCellRopeOut -= Capture;
        }
        yield return null;

        CollectionAssert.AreEqual(new[] { StormScale.Phase.Mature, StormScale.Phase.RopingOut }, seen);
    }

    [UnityTest]
    public IEnumerator SameSceneRestart_RemovesThePreviousRunsTornadoes()
    {
        _director.enabled = false;
        WeatherPlan plan = PlanOf(Cell(0, 3, StormCellRole.Anchor, 0.1f, 1f, 30f, 2f));
        _director.BeginWithPlan(plan, _prefab, _roster);
        _director.Tick(0.5f);
        _director.EndRun();
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<TornadoController>().Count(t => t.DirectorDriven),
                        "the ended run's tornado stays for the results tableau");

        _director.BeginWithPlan(PlanOf(Cell(0, 3, StormCellRole.Anchor, 5f, 1f, 30f, 2f)), _prefab, _roster);
        yield return null;
        Assert.AreEqual(0, Object.FindObjectsByType<TornadoController>().Count(t => t.DirectorDriven),
                        "a restart removes it before new cells spawn");
    }

    [UnityTest]
    public IEnumerator EvictedWhileForming_RaisesFormingRopeOutEnded_NoPeak_AndNeverHarms()
    {
        _director.enabled = false;
        PlannedCell cell = Cell(3, 5, StormCellRole.Satellite, 0.1f, 2f, 10f, 2f);
        cell.EarlyRopeTime = 0.6f; // 0.5 s into a 2 s Form: I0 = 0.25, failed touchdown
        cell.EarlyRopeStartIntensity = 0.25f;
        _director.BeginWithPlan(PlanOf(cell), _prefab, _roster);
        float maxDamage = 0f;
        for (float t = 0f; t < 1.5f; t += 0.05f)
        {
            _director.Tick(0.05f);
            if (_director.LiveCells.Count > 0) maxDamage = Mathf.Max(maxDamage, _director.LiveCells[0].Tornado.DamageRadius);
            yield return null;
        }

        CollectionAssert.AreEqual(new[] { "Forming:3", "RopeOut:3", "Ended:3" }, _events);
        Assert.AreEqual(0f, maxDamage, "a failed touchdown never becomes harmful");
    }

    [UnityTest]
    public IEnumerator RunEnd_WithTwoLiveCells_RaisesExactlyOneEndedEach_AndNothingAfter()
    {
        _director.enabled = false;
        _director.BeginWithPlan(PlanOf(Cell(0, 1, StormCellRole.Anchor, 0.1f, 5f, 30f, 5f),
                                       Cell(1, 0, StormCellRole.Satellite, 0.2f, 5f, 30f, 5f)), _prefab, _roster);
        yield return RunFor(0.5f);
        _director.EndRun();
        yield return RunFor(1f);

        CollectionAssert.AreEquivalent(new[] { "Forming:0", "Forming:1", "Ended:0", "Ended:1" }, _events);
    }

    [UnityTest]
    public IEnumerator CapDroppedCell_RaisesNothing()
    {
        _director.enabled = false;
        PlannedCell dropped = Cell(4, 1, StormCellRole.Satellite, 0.1f, 1f, 1f, 1f);
        dropped.Dropped = true;
        _director.BeginWithPlan(PlanOf(dropped), _prefab, _roster);
        yield return RunFor(4f);
        Assert.IsEmpty(_events);
    }

    // ---------- AC-22: pause ----------

    [UnityTest]
    public IEnumerator Pause_TenSeconds_LeavesAgeAndPositionUnchanged()
    {
        _director.BeginWithPlan(PlanOf(Cell(0, 2, StormCellRole.Anchor, 0.05f, 5f, 60f, 5f)), _prefab, _roster);
        yield return new WaitForSeconds(1f);
        LiveStormCell cell = _director.LiveCells[0];
        float age = cell.Age;
        Vector3 pos = cell.Position;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(10f);
        float pausedAge = cell.Age;
        Vector3 pausedPos = cell.Position;
        Time.timeScale = 1f;

        Assert.AreEqual(age, pausedAge, 1e-4f);
        Assert.AreEqual(0f, Vector3.Distance(pos, pausedPos), 1e-4f);
    }

    // ---------- AC-17: tracks ignore the player ----------

    [UnityTest]
    public IEnumerator Seed777_IdleAndDriving_CellPositionsMatch()
    {
        _director.enabled = false;
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        List<Vector3> idle = null, driving = null;
        yield return Sample(truck, drive: false, s => idle = s);
        yield return Sample(truck, drive: true, s => driving = s);

        Assert.Greater(idle.Count, 10, "seed 777 should spawn cells within 60 s");
        Assert.AreEqual(idle.Count, driving.Count);
        for (int i = 0; i < idle.Count; i++)
            Assert.AreEqual(0f, Vector3.Distance(idle[i], driving[i]), 0.01f, $"sample {i}");
    }

    private IEnumerator Sample(PlayerVehicle truck, bool drive, Action<List<Vector3>> report)
    {
        var samples = new List<Vector3>();
        truck.InputEnabled = false;
        if (drive) truck.GetComponent<Rigidbody>().linearVelocity = truck.transform.forward * 15f;
        _director.Begin(_prefab, _roster, Vector3.zero, 777);
        for (int step = 0; step < 120; step++) // 60 s in 0.5 s ticks
        {
            _director.Tick(0.5f);
            foreach (LiveStormCell c in _director.LiveCells) samples.Add(c.Position);
            if (drive && step % 4 == 0) truck.GetComponent<Rigidbody>().linearVelocity = Quaternion.Euler(0f, step * 7f, 0f) * Vector3.forward * 15f;
            yield return null;
        }
        _director.EndRun();
        foreach (TornadoController t in Object.FindObjectsByType<TornadoController>()) Object.Destroy(t.gameObject);
        yield return null;
        report(samples);
    }

    // ---------- AC-25 / AC-4: results facts (story 009) ----------

    [UnityTest]
    public IEnumerator RunEndsBeforeAnchorIsMature_RunInfoSaysTheBigOneGotAway()
    {
        _director.enabled = false;
        _director.BeginWithPlan(PlanOf(Cell(0, 4, StormCellRole.Anchor, 0.1f, 5f, 30f, 5f)), _prefab, _roster);
        yield return RunFor(1f);
        _director.EndRun();

        StormRunInfo info = _director.RunInfo();
        Assert.IsTrue(info.Valid);
        Assert.AreEqual(4, info.BigOneGotAwayEf);
        Assert.AreEqual("QUIET DAY", info.Regime);
        Assert.AreEqual(Application.version, info.BuildVersion);
    }

    [UnityTest]
    public IEnumerator AnchorPeaked_RunInfoHasNoGotAway()
    {
        _director.enabled = false;
        _director.BeginWithPlan(PlanOf(Cell(0, 3, StormCellRole.Anchor, 0.1f, 0.5f, 30f, 5f)), _prefab, _roster);
        yield return RunFor(1.5f);
        _director.EndRun();
        Assert.AreEqual(-1, _director.RunInfo().BigOneGotAwayEf);
    }

    [UnityTest]
    public IEnumerator RunManagerEnd_SummaryCarriesSeedVersionAndRegime()
    {
        var run = Object.FindAnyObjectByType<RunManager>();
        var spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        RunSummary? summary = null;
        System.Action<RunSummary> onEnd = s => summary = s;
        GameEvents.RunEnded += onEnd;
        try
        {
            run.StartRun();
            yield return null;
            typeof(RunManager).GetMethod("EndRun", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(run, new object[] { false });
            yield return null;
        }
        finally
        {
            GameEvents.RunEnded -= onEnd;
        }
        Assert.IsTrue(summary.HasValue);
        Assert.IsTrue(summary.Value.Storm.Valid, "the director ran this run");
        Assert.AreEqual(unchecked((long)spawner.Director.Plan.Seed), summary.Value.Storm.Seed);
        Assert.AreEqual(StormDirector.RegimeName(spawner.Director.Plan.Regime), summary.Value.Storm.Regime);
    }

    // ---------- AC-28: per-frame cost ----------

    [UnityTest]
    public IEnumerator Tick_WithLiveCells_UnderPointOneMs_AndNoGarbage()
    {
        _director.enabled = false;
        _director.BeginWithPlan(PlanOf(Cell(0, 4, StormCellRole.Anchor, 0.05f, 2f, 200f, 5f),
                                       Cell(1, 2, StormCellRole.Satellite, 0.05f, 2f, 200f, 5f)), _prefab, _roster);
        yield return RunFor(3f); // both spawned and Mature: no events during the measured window

        const int frames = 600;
        var watch = new Stopwatch(); // allocated before the baseline so only director garbage is counted
        long before = GC.GetAllocatedBytesForCurrentThread();
        watch.Start();
        for (int i = 0; i < frames; i++) _director.Tick(1f / 60f);
        watch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        double msPerTick = watch.Elapsed.TotalMilliseconds / frames;
        Debug.Log($"[Director] {msPerTick:F4} ms per tick, {allocated} B allocated over {frames} ticks");
        Assert.LessOrEqual(msPerTick, 0.10);
        Assert.AreEqual(0, allocated);
    }
}
