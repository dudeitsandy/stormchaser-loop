using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Retry/title reloads must rebuild the same world and run loop as a cold boot (0.6.0 itch bug).</summary>
public class RunRestartTests
{
    private const string SceneName = "ArtTest";

    [TearDown]
    public void TearDown() => Time.timeScale = 1f;

    [UnityTest]
    public IEnumerator Retry_AfterRun_RebuildsWorldAndTimerCounts()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
        string coldBoot = Snapshot();
        Debug.Log($"[Restart] cold boot: {coldBoot}");

        Object.FindAnyObjectByType<RunManager>().StartRun();
        yield return new WaitForSeconds(1f);

        // Same path the results screen takes on "any button": RunManager.Reload(skipTitle: true).
        typeof(RunManager).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { true });
        yield return null;
        yield return null;
        yield return null;

        string retry = Snapshot();
        Debug.Log($"[Restart] after retry: {retry}");
        var run = Object.FindAnyObjectByType<RunManager>();
        var timer = Object.FindAnyObjectByType<SessionTimer>();
        Assert.AreEqual(RunManager.State.Running, run.Current, "Retry should start a run");
        float before = timer.TimeRemaining;
        yield return new WaitForSeconds(1f);
        Assert.Less(timer.TimeRemaining, before - 0.5f, "Timer should count down after retry");
        Assert.AreEqual(coldBoot, retry, "Retry should rebuild everything a cold boot builds");
    }

    private static string Snapshot()
    {
        return $"scatter={Has<EnvironmentScatter>()} pip={Has<PipViewfinder>()} feedback={Has<PhotoFeedback>()} " +
               $"offscreen={Has<OffscreenIndicator>()} wind={Has<WindCardVfx>()} audio={Has<ProceduralAudio>()}";
    }

    private static bool Has<T>() where T : Object => Object.FindAnyObjectByType<T>() != null;
}
