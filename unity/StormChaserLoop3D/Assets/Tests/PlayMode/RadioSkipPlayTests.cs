using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Radio skip and the Settings RADIO preview (Andy 2026-10-08) in the real scene: a jingle can't be skipped, a song
/// can and the next one differs; the preview plays the chosen song from the pause menu and stops on close.
/// </summary>
public class RadioSkipPlayTests
{
    private const string SceneName = "VerificationScene";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        ProfileStore.Shared = new ProfileStore(new MemoryProfileStorage());
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        ProfileStore.Shared = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Scene empty = SceneManager.CreateScene("RadioSkipEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    [UnityTest]
    public IEnumerator Skip_IgnoredOnAJingle_MovesToADifferentSong_JingleFollows()
    {
        var run = Object.FindAnyObjectByType<RunManager>();
        var radio = Object.FindAnyObjectByType<RadioLite>();
        run.StartRun();
        yield return null;
        Assert.IsTrue(RadioLite.IsJingle(radio.CurrentClip), "a run opens on a jingle");
        Assert.IsFalse(radio.SkipSong(), "jingles can't be skipped");

        // Jump past the jingle to the first song (what Update does when the jingle ends).
        var nextTrack = typeof(RadioLite).GetMethod("NextTrack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        nextTrack.Invoke(radio, null);
        yield return null;
        string first = radio.CurrentClip;
        Assert.IsTrue(RadioLite.IsSong(first));

        string toast = null;
        radio.SongSkipped += t => toast = t;
        Assert.IsTrue(radio.SkipSong());
        Assert.IsTrue(RadioLite.IsSong(radio.CurrentClip));
        Assert.AreNotEqual(first, radio.CurrentClip, "never the same song twice in a row");
        Assert.AreEqual(RadioLite.SongTitle(radio.CurrentClip), toast);
        AudioClip skipped = Resources.Load<AudioClip>("Music/" + first);
        Assert.AreEqual(AudioDataLoadState.Unloaded, skipped.loadState, "a skipped song's audio is released");

        // The rotation resumes after a skip: the skipped-to song is followed by a jingle, then a song (never two jingles).
        nextTrack.Invoke(radio, null);
        yield return null;
        Assert.IsTrue(RadioLite.IsJingle(radio.CurrentClip), "a jingle follows the skipped-to song");
        nextTrack.Invoke(radio, null);
        yield return null;
        Assert.IsTrue(RadioLite.IsSong(radio.CurrentClip), "then a song, never a second jingle");
    }

    [UnityTest]
    public IEnumerator Preview_FromPause_PlaysChosenSong_StopsOnClose()
    {
        var run = Object.FindAnyObjectByType<RunManager>();
        var radio = Object.FindAnyObjectByType<RadioLite>();
        run.StartRun();
        yield return null;
        run.Pause(PauseReason.Manual);
        run.OpenSettings(fromTitle: false);
        var menu = (SettingsMenu)typeof(RunManager)
            .GetField("_settingsMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(run);
        for (int i = 0; i < menu.Rows.Count; i++) if (menu.Rows[i] == SettingRow.Radio) menu.FocusOn(i);
        Assert.AreEqual(SettingRow.Radio, menu.Focused, "the RADIO row is there when songs are");

        run.ChangeSetting(1);
        Assert.AreEqual(menu.Value(SettingRow.Radio), RadioLite.SongTitle(radio.PreviewClip));
        run.ChangeSetting(-1);
        Assert.IsNull(radio.PreviewClip, "back to OFF stops the preview");

        run.ChangeSetting(1);
        run.CloseSettings();
        Assert.IsNull(radio.PreviewClip, "closing Settings stops the preview");
        Assert.AreEqual(RunManager.State.Paused, run.Current);
        yield return null;
    }
}
