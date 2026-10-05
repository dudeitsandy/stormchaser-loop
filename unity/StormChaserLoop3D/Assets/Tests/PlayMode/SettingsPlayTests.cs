using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// run-screens story 002 in the real scene, against an in-memory save stem: Settings from Pause apply live (camera
/// preset on the rig, brightness on the global Volume), are saved once on close, and survive a reload.
/// </summary>
public class SettingsPlayTests
{
    private const string SceneName = "VerificationScene";
    private MemoryProfileStorage _disk;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _disk = new MemoryProfileStorage();
        ProfileStore.Shared = new ProfileStore(_disk);
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
        AudioListener.volume = 1f;
        Scene empty = SceneManager.CreateScene("SettingsEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    private static int Row(SettingsMenu m, SettingRow row)
    {
        for (int i = 0; i < m.Rows.Count; i++) if (m.Rows[i] == row) return i;
        return -1;
    }

    [UnityTest]
    public IEnumerator FromPause_ChangesApplyLive_SaveOnceOnClose_AndSurviveAReload()
    {
        var run = Object.FindAnyObjectByType<RunManager>();
        var rig = Object.FindAnyObjectByType<ChaseCameraRig>();
        Assert.AreEqual(12f, rig.DefaultPitch, 1e-3f, "SKY default");
        run.StartRun();
        yield return null;
        run.Pause(PauseReason.Manual);
        run.OpenSettings(fromTitle: false);
        var menu = (SettingsMenu)typeof(RunManager)
            .GetField("_settingsMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(run);

        menu.FocusOn(Row(menu, SettingRow.Camera));
        run.ChangeSetting(1);
        Assert.AreEqual(26.57f, rig.DefaultPitch, 1e-3f, "CLASSIC applied live");

        menu.FocusOn(Row(menu, SettingRow.Brightness));
        run.ChangeSetting(1);
        run.ChangeSetting(1);
        Volume global = null;
        foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) if (v.isGlobal) global = v;
        Assert.IsNotNull(global, "the scene has a global Volume");
        Assert.IsTrue(global.profile.TryGet(out ColorAdjustments adjust));
        Assert.AreEqual(SettingsApplier.BrightnessEv(0.2f), adjust.postExposure.value, 1e-4f, "+20 % applied live");

        menu.FocusOn(Row(menu, SettingRow.Master));
        run.ChangeSetting(-1);
        Assert.AreEqual(0.9f, AudioListener.volume, 1e-4f);

        int writes = _disk.Writes;
        run.CloseSettings();
        Assert.AreEqual(writes + 1, _disk.Writes, "saved once on close");
        Assert.AreEqual(RunManager.State.Paused, run.Current, "back to the pause menu");

        var reloaded = new ProfileStore(_disk);
        Assert.AreEqual("CLASSIC", reloaded.Settings.CameraPreset);
        Assert.AreEqual(0.2f, reloaded.Settings.Brightness, 1e-4f);
        Assert.AreEqual(0.9f, reloaded.Settings.MasterVolume, 1e-4f);
    }
}
