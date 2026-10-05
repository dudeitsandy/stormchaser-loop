using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// run-goals-v1 story 004 in the real scene: the goal runner fed by live GameEvents, bonuses into the score, goals in
/// RunSummary, wreck and forfeit rules. Style moments and photos are raised the way the truck and camera raise them.
/// </summary>
public class GoalTrackerPlayTests
{
    private const string SceneName = "VerificationScene";
    private const string C = "career.compact.heartland.";
    private RunManager _run;
    private PlayerVehicle _truck;
    private DisasterSpawner _spawner;
    private ScoreAccumulator _score;
    private readonly List<GoalCompletion> _completed = new List<GoalCompletion>();
    private RunSummary? _summary;

    private void OnGoal(GoalCompletion c) => _completed.Add(c);
    private void OnEnd(RunSummary s) => _summary = s;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        ProfileStore.Shared = new ProfileStore(new MemoryProfileStorage()); // fresh record: NEW tags are deterministic
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
        _run = Object.FindAnyObjectByType<RunManager>();
        _truck = Object.FindAnyObjectByType<PlayerVehicle>();
        _spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        _score = Object.FindAnyObjectByType<ScoreAccumulator>();
        _completed.Clear();
        _summary = null;
        GameEvents.GoalCompleted += OnGoal;
        GameEvents.RunEnded += OnEnd;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        GameEvents.GoalCompleted -= OnGoal;
        GameEvents.RunEnded -= OnEnd;
        ProfileStore.Shared = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Scene empty = SceneManager.CreateScene("GoalTrackerEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    private void EndRun(bool wrecked) =>
        typeof(RunManager).GetMethod("EndRun", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_run, new object[] { wrecked });

    // Replaces the run's plan with one EF2 anchor 40 m north of the truck, Mature after 0.5 s.
    private IEnumerator SpawnAnchorNearTruck()
    {
        const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        var prefab = (TornadoController)typeof(DisasterSpawner).GetField("_tornadoPrefab", Hidden).GetValue(_spawner);
        var roster = ((List<DisasterSpawner.RosterEntry>)typeof(DisasterSpawner).GetField("_roster", Hidden).GetValue(_spawner))
                     .Select(e => e.Data).ToList();
        Vector3 p = _truck.transform.position + new Vector3(0f, 0f, 40f);
        var plan = new WeatherPlan { Seed = 7, Heat = 0, Regime = Regime.LoneGiant, AnchorEf = 2, Duration = 180f, Compact = true };
        plan.Cells.Add(new PlannedCell
        {
            Id = 0, Ef = 2, Role = StormCellRole.Anchor, Position = new Vector2(p.x, p.z),
            DesiredTime = 0.05f, SpawnTime = 0.05f, Form = 0.5f, Mature = 60f, Rope = 5f,
        });
        _spawner.Director.BeginWithPlan(plan, prefab, roster);
        yield return new WaitForSeconds(1f); // game time: spawn at 0.05 s, Mature from 0.55 s
        Assert.AreEqual(1, _spawner.Director.LiveCells.Count, "the anchor is live");
    }

    [UnityTest]
    public IEnumerator DriftBesideAStorm_AndAPerfectShotOfTheAnchor_CompleteBoth_PayBonuses_ReachTheSummary()
    {
        _run.StartRun();
        _truck.InputEnabled = false;
        yield return SpawnAnchorNearTruck();
        TornadoController anchor = _spawner.Director.LiveCells[0].Tornado;
        float before = _score.TotalScore;

        GameEvents.RaiseStyleEvent(StyleKind.Drift, 3.2f);
        GameEvents.RaisePhotoTaken(new PhotoResult(120f, 1f, 1f, 1f, ShotTier.Perfect, anchor, anchor.transform.position));
        yield return null;

        Assert.AreEqual(1, _completed.Count(c => c.Id == C + "storm_drift"), "one GoalCompleted for storm_drift");
        Assert.AreEqual(1, _completed.Count(c => c.Id == C + "front_page"), "one GoalCompleted for front_page");
        // The run's drawn bounties are random (no ?seed), so a drawn drift_by can also complete here.
        Assert.AreEqual(before + _completed.Sum(c => c.Bonus), _score.TotalScore, 1e-3f, "every bonus added to the score");
        Assert.GreaterOrEqual(_score.TotalScore - before, 300f, "at least the two career bonuses");

        EndRun(wrecked: false);
        yield return null;
        Assert.IsTrue(_summary.HasValue);
        var ids = _summary.Value.Goals.Completions.Select(c => c.Id).ToList();
        CollectionAssert.Contains(ids, C + "storm_drift");
        CollectionAssert.Contains(ids, C + "front_page");
        Assert.AreEqual(3, _summary.Value.Goals.Bounties.Count, "the run drew its three bounties");
    }

    [UnityTest]
    public IEnumerator WreckedRun_KeepsGoalsDoneBeforeTheWreck_ButNotTossSurvivor()
    {
        _run.StartRun();
        GameEvents.RaiseTossed();
        GameEvents.RaiseStyleEvent(StyleKind.NearMiss, 1f);
        GameEvents.RaiseStyleEvent(StyleKind.NearMiss, 1f);
        GameEvents.RaiseStyleEvent(StyleKind.NearMiss, 1f);
        yield return null;
        EndRun(wrecked: true);
        yield return null;
        var ids = _summary.Value.Goals.Completions.Select(c => c.Id).ToList();
        CollectionAssert.Contains(ids, C + "near_misses");
        CollectionAssert.DoesNotContain(ids, C + "toss_survivor");
    }

    [UnityTest]
    public IEnumerator TimerEnd_AfterAToss_CompletesTossSurvivor()
    {
        _run.StartRun();
        GameEvents.RaiseTossed();
        yield return null;
        EndRun(wrecked: false);
        yield return null;
        CollectionAssert.Contains(_summary.Value.Goals.Completions.Select(c => c.Id).ToList(), C + "toss_survivor");
    }

    // run-goals-v1 story 007: the HUD bounty list and career pop-up react to the goal events (text, not pixels:
    // batchmode never reaches end-of-frame, so the visual evidence is a WebGL capture).
    [UnityTest]
    public IEnumerator Hud_ListsTheRunsBounties_MarksDoneAndMissed_AndPopsACareerGoal()
    {
        _run.StartRun();
        _truck.InputEnabled = false;
        yield return null;
        var goals = Object.FindAnyObjectByType<GoalRunner>();
        var hud = Object.FindAnyObjectByType<HudController>();
        const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        var rows = (Dictionary<string, UnityEngine.UIElements.Label>)typeof(HudController).GetField("_bountyRows", Hidden).GetValue(hud);
        var popup = (UnityEngine.UIElements.Label)typeof(HudController).GetField("_goalPopup", Hidden).GetValue(hud);
        Assert.AreEqual(3, rows.Count, "one row per drawn bounty");

        GoalDef done = GoalCatalogue.Find(goals.Bounties[0]);
        GameEvents.RaiseGoalCompleted(new GoalCompletion(done.Id, GoalKind.Bounty, done.Bonus, true));
        GameEvents.RaiseBountyFailed(goals.Bounties[1]);
        GameEvents.RaiseStyleEvent(StyleKind.Airtime, 1.2f); // big_air: a real career completion
        yield return null;

        StringAssert.StartsWith("DONE  ", rows[goals.Bounties[0]].text);
        StringAssert.StartsWith("·  ", rows[goals.Bounties[2]].text);
        StringAssert.StartsWith("MISS  ", rows[goals.Bounties[1]].text, "the HUD shows what BountyFailed reports");
        Assert.AreEqual("GOAL! BIG AIR  +150  NEW", popup.text);
        Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex, popup.style.display.value);
    }

    [UnityTest]
    public IEnumerator QuitToTitle_ForfeitsTheRunsGoals_NoSummary()
    {
        _run.StartRun();
        GameEvents.RaiseStyleEvent(StyleKind.Airtime, 1.2f);
        yield return null;
        Assert.IsTrue(_completed.Any(c => c.Id == C + "big_air"), "completed in the run");
        _run.Pause(PauseReason.Manual);
        _run.ForfeitRun();
        yield return null;
        yield return null;
        Assert.IsFalse(_summary.HasValue, "a forfeit produces no run summary, so nothing reaches the record");
    }
}
