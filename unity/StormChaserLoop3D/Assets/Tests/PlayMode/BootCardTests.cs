using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Ghostweave Labs boot card (Andy 2026-10-05): shows once per app session before the title, then the title and its
/// attract mode; any press skips it, and that press doesn't also start a run.
/// </summary>
public class BootCardTests
{
    private const string SceneName = "VerificationScene";
    private Keyboard _keyboard;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        BootCard.AllowInBatchmode = true;
        BootCard.ResetSessionForTests();
        TitleAttract.AllowInBatchmode = true;
        ProfileStore.Shared = new ProfileStore(new MemoryProfileStorage());
        _keyboard = InputSystem.AddDevice<Keyboard>();
        yield return LoadScene();
    }

    private static IEnumerator LoadScene()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        BootCard.AllowInBatchmode = false;
        TitleAttract.AllowInBatchmode = false;
        ProfileStore.Shared = null;
        GameAudio.AttractMute = false;
        Time.timeScale = 1f;
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        Scene empty = SceneManager.CreateScene("BootCardEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    [UnityTest]
    public IEnumerator Card_ShowsFirst_ThenTheTitle_AndNeverAgainThisSession()
    {
        // Arrange
        var card = Object.FindAnyObjectByType<BootCard>();
        // Assert: the card is up, the title's attract mode isn't, and game audio is silent
        Assert.IsTrue(card.IsPlaying);
        Assert.IsFalse(TitleAttract.Active);
        Assert.AreEqual(0f, GameAudio.EffectsVolume);
        // Act: let it play out (≈ 2.6 s)
        yield return new WaitForSecondsRealtime(3f);
        // Assert: title + attract
        Assert.IsFalse(card.IsPlaying);
        Assert.IsTrue(TitleAttract.Active);
        Assert.AreEqual(RunManager.State.Title, Object.FindAnyObjectByType<RunManager>().Current);
        // Act: a second scene load in the same app session (Retry / back to title)
        yield return LoadScene();
        // Assert: straight to the title
        Assert.IsFalse(Object.FindAnyObjectByType<BootCard>().IsPlaying);
        Assert.IsTrue(TitleAttract.Active);
    }

    [UnityTest]
    public IEnumerator AnyPress_SkipsToTheTitle_WithoutStartingARun()
    {
        // Arrange
        var card = Object.FindAnyObjectByType<BootCard>();
        var run = Object.FindAnyObjectByType<RunManager>();
        Assert.IsTrue(card.IsPlaying);
        yield return new WaitForSecondsRealtime(0.5f);
        // Act: press and release Space
        InputSystem.QueueStateEvent(_keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Space));
        yield return null;
        InputSystem.QueueStateEvent(_keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return null;
        // Assert: skipped to the title, and the same press didn't start a run
        Assert.IsFalse(card.IsPlaying);
        Assert.IsTrue(TitleAttract.Active);
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.AreEqual(RunManager.State.Title, run.Current, "the skip press must not also start a run");
    }
}
