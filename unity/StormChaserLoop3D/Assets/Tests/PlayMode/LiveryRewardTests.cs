using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// run-goals-v1 story 006 in the real scene, against an in-memory save stem: the fifth career goal earns the KTVR
/// livery in that run's single checkpoint write, it is worn next run, and it survives a reload.
/// </summary>
public class LiveryRewardTests
{
    private const string SceneName = "VerificationScene";
    private const string C = "career.compact.heartland.";
    private MemoryProfileStorage _disk;
    private RunSummary? _summary;
    private void OnEnd(RunSummary s) => _summary = s;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _disk = new MemoryProfileStorage();
        var seeded = new ProfileStore(_disk);
        var four = new[] { "score_rookie", "point_blank", "storm_drift", "near_misses" }
            .Select(k => new GoalCompletion(C + k, GoalKind.Career, 150, true)).ToList();
        seeded.RecordRunComplete(four, "compact", 1, "test", DateTime.UtcNow, 0f);
        ProfileStore.Shared = new ProfileStore(_disk);
        _summary = null;
        GameEvents.RunEnded += OnEnd;
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        GameEvents.RunEnded -= OnEnd;
        ProfileStore.Shared = null;
        Time.timeScale = 1f;
        Scene empty = SceneManager.CreateScene("LiveryRewardEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    [UnityTest]
    public IEnumerator FifthCareerGoal_UnlocksAndEquipsTheKtvrLivery_InOneWrite_AndItSurvivesAReload()
    {
        var run = UnityEngine.Object.FindAnyObjectByType<RunManager>();
        int writesBefore = _disk.Writes;
        run.StartRun();
        yield return null;
        GameEvents.RaiseStyleEvent(StyleKind.Airtime, 1.2f); // big_air: the fifth career goal
        yield return null;
        typeof(RunManager).GetMethod("EndRun", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(run, new object[] { false });
        yield return null;

        Assert.IsTrue(_summary.HasValue);
        CollectionAssert.Contains(_summary.Value.Goals.NewUnlocks.ToList(), "livery.ktvr", "Results gets the unlock");
        Assert.IsTrue(_summary.Value.Goals.Saved);
        Assert.AreEqual(writesBefore + 1, _disk.Writes, "one checkpoint write");

        var reloaded = new ProfileStore(_disk);
        Assert.IsTrue(reloaded.IsUnlocked("livery.ktvr"));
        Assert.AreEqual("livery.ktvr", reloaded.Data.Livery, "a newly earned paint job is worn next run");
        Assert.AreEqual(5, reloaded.CareerCount("compact", "heartland"));
    }

    [UnityTest]
    public IEnumerator ARunWithoutANewCareerGoal_EarnsNothing()
    {
        var run = UnityEngine.Object.FindAnyObjectByType<RunManager>();
        run.StartRun();
        yield return null;
        typeof(RunManager).GetMethod("EndRun", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(run, new object[] { true });
        yield return null;
        CollectionAssert.IsEmpty(_summary.Value.Goals.NewUnlocks.ToList());
        Assert.IsFalse(new ProfileStore(_disk).IsUnlocked("livery.ktvr"));
    }
}
