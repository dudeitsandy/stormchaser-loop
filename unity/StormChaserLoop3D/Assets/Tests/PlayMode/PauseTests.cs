using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>run-screens story 001 in the real scene: pause freezes the run, resume continues, quit forfeits.</summary>
public class PauseTests
{
    private const string SceneName = "VerificationScene";
    private RunManager _run;
    private SessionTimer _timer;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
        _run = Object.FindAnyObjectByType<RunManager>();
        _timer = Object.FindAnyObjectByType<SessionTimer>();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    [UnityTest]
    public IEnumerator Pause_FreezesTimeAndTimer_ResumeContinuesFromTheSameTime()
    {
        _run.StartRun();
        yield return new WaitForSeconds(0.5f);
        bool? pausedEvent = null;
        System.Action<bool, PauseReason> onPause = (p, _) => pausedEvent = p;
        GameEvents.PauseChanged += onPause;
        try
        {
            _run.Pause(PauseReason.Manual);
            float frozen = _timer.TimeRemaining;
            Assert.AreEqual(RunManager.State.Paused, _run.Current);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.AreEqual(true, pausedEvent);

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(frozen, _timer.TimeRemaining, 1e-4f, "the timer does not run while paused");

            _run.Resume();
            Assert.AreEqual(RunManager.State.Running, _run.Current);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            Assert.AreEqual(false, pausedEvent);
            Assert.AreEqual(frozen, _timer.TimeRemaining, 0.05f, "resumes with the same time left");
        }
        finally { GameEvents.PauseChanged -= onPause; }
    }

    [UnityTest]
    public IEnumerator FocusLoss_AutoPauses_AndNeverAutoResumes()
    {
        _run.StartRun();
        yield return null;
        _run.HandleFocusChange(focused: false, ignore: false);
        Assert.AreEqual(RunManager.State.Paused, _run.Current);
        _run.HandleFocusChange(focused: true, ignore: false);
        yield return null;
        Assert.AreEqual(RunManager.State.Paused, _run.Current, "regaining focus needs a press to resume");
    }

    [UnityTest]
    public IEnumerator Pause_IsIgnoredOnTheTitle()
    {
        Assert.AreEqual(RunManager.State.Title, _run.Current);
        _run.Pause(PauseReason.Manual);
        Assert.AreEqual(RunManager.State.Title, _run.Current);
        yield return null;
    }

    [UnityTest]
    public IEnumerator QuitRun_Forfeits_NoRunEnd_NothingBanked_BackOnTitle()
    {
        float bestBefore = BestScoreStore.Load();
        bool ended = false, forfeited = false;
        System.Action<RunSummary> onEnd = _ => ended = true;
        System.Action onForfeit = () => forfeited = true;
        GameEvents.RunEnded += onEnd;
        GameEvents.RunForfeited += onForfeit;
        try
        {
            _run.StartRun();
            yield return null;
            _run.Pause(PauseReason.Manual);
            _run.ForfeitRun();
            yield return null;
            yield return null;
            Assert.IsTrue(forfeited);
            Assert.IsFalse(ended, "a forfeit is not a run end");
            Assert.AreEqual(bestBefore, BestScoreStore.Load(), "nothing banked");
            var reloaded = Object.FindAnyObjectByType<RunManager>();
            Assert.AreEqual(RunManager.State.Title, reloaded.Current, "back on the title");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }
        finally
        {
            GameEvents.RunEnded -= onEnd;
            GameEvents.RunForfeited -= onForfeit;
        }
    }
}
