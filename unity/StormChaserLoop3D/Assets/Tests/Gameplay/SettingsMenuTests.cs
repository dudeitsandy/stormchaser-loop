using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Settings panel model (run-screens story 002; design/ux/run-screens.md Layout: Settings).</summary>
public class SettingsMenuTests
{
    private static readonly List<Vector2Int> Res = new List<Vector2Int> { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) };

    private static SettingsMenu Menu(bool desktop = true) => new SettingsMenu(new DeviceSettings(), desktop, Res);

    private static void FocusRow(SettingsMenu m, SettingRow row)
    {
        for (int i = 0; i < m.Rows.Count; i++) if (m.Rows[i] == row) { m.FocusOn(i); return; }
        Assert.Fail($"no {row} row");
    }

    [Test]
    public void Desktop_HasResolution_WebGL_DoesNot_BothEndWithBack()
    {
        CollectionAssert.Contains(Menu(true).Rows, SettingRow.Resolution);
        CollectionAssert.DoesNotContain(Menu(false).Rows, SettingRow.Resolution);
        Assert.AreEqual(SettingRow.Back, Menu(true).Rows[Menu(true).Rows.Count - 1]);
        CollectionAssert.Contains(Menu(false).Rows, SettingRow.Brightness);
    }

    [Test]
    public void RadioRow_OnlyWithSongs_SitsUnderMusic()
    {
        CollectionAssert.DoesNotContain(Menu().Rows, SettingRow.Radio);
        var m = new SettingsMenu(new DeviceSettings(), true, Res, new[] { "GEESE", "SOCK ME HOME" });
        int music = -1, radio = -1;
        for (int i = 0; i < m.Rows.Count; i++)
        {
            if (m.Rows[i] == SettingRow.Music) music = i;
            if (m.Rows[i] == SettingRow.Radio) radio = i;
        }
        Assert.AreEqual(music + 1, radio);
    }

    [Test]
    public void SoundTestRow_StartsIdle_StepsThroughNumberedSongs_WrapsBothWays()
    {
        var m = new SettingsMenu(new DeviceSettings(), true, Res, new[] { "GEESE", "SOCK ME HOME" });
        FocusRow(m, SettingRow.Radio);
        Assert.AreEqual("SOUND TEST", SettingsMenu.Label(SettingRow.Radio));
        Assert.AreEqual(SettingsMenu.IdleSoundTest, m.Value(SettingRow.Radio));
        Assert.AreEqual(SettingRow.Radio, m.Change(1));
        Assert.AreEqual("01 GEESE", m.Value(SettingRow.Radio));
        Assert.AreEqual(0, m.PreviewIndex);
        m.Change(1);
        Assert.AreEqual("02 SOCK ME HOME", m.Value(SettingRow.Radio));
        m.Change(1);
        Assert.AreEqual(-1, m.PreviewIndex, "wraps back to idle after the last song");
        m.Change(-1);
        Assert.AreEqual("02 SOCK ME HOME", m.Value(SettingRow.Radio), "left from idle goes to the last song");
    }

    [Test]
    public void Brightness_StepsInTens_ClampsAtPlusMinusFifty_WithoutFloatDrift()
    {
        SettingsMenu m = Menu();
        FocusRow(m, SettingRow.Brightness);
        for (int i = 0; i < 5; i++) m.Change(1);
        Assert.AreEqual(0.5f, m.Settings.Brightness);
        Assert.AreEqual("+50%", m.Value(SettingRow.Brightness));
        m.Change(1);
        Assert.AreEqual(0.5f, m.Settings.Brightness, "clamped");
        for (int i = 0; i < 12; i++) m.Change(-1);
        Assert.AreEqual(-0.5f, m.Settings.Brightness);
        Assert.AreEqual("-50%", m.Value(SettingRow.Brightness));
    }

    [Test]
    public void BrightnessEv_HalvesAtMinus50_ZeroAtDefault()
    {
        Assert.AreEqual(-1f, SettingsApplier.BrightnessEv(-0.5f), 1e-5f);
        Assert.AreEqual(0f, SettingsApplier.BrightnessEv(0f), 1e-6f);
        Assert.AreEqual(Mathf.Log(1.5f, 2f), SettingsApplier.BrightnessEv(0.5f), 1e-5f);
    }

    [Test]
    public void Camera_CyclesSkyClassicHigh_Volumes_ClampZeroToHundred()
    {
        SettingsMenu m = Menu();
        Assert.AreEqual("SKY", m.Value(SettingRow.Camera), "default is the new low camera");
        FocusRow(m, SettingRow.Camera);
        m.Change(1);
        Assert.AreEqual("CLASSIC", m.Settings.CameraPreset);
        m.Change(1);
        m.Change(1);
        Assert.AreEqual("SKY", m.Settings.CameraPreset, "wraps");

        FocusRow(m, SettingRow.Master);
        for (int i = 0; i < 15; i++) m.Change(-1);
        Assert.AreEqual(0f, m.Settings.MasterVolume);
        Assert.AreEqual("0%", m.Value(SettingRow.Master));
    }

    [Test]
    public void Toggles_AndResolution_Change_BackChangesNothing()
    {
        SettingsMenu m = Menu();
        FocusRow(m, SettingRow.InvertY);
        m.Change(1);
        Assert.IsTrue(m.Settings.InvertY);
        FocusRow(m, SettingRow.Resolution);
        m.Change(1);
        Assert.AreEqual("1280x720", m.Value(SettingRow.Resolution), "wraps from AUTO (treated as the largest) to the first");
        FocusRow(m, SettingRow.Back);
        Assert.IsNull(m.Change(1));
    }

    [Test]
    public void AttractMute_SilencesEffects_KeepsTheSetting_AndRestores()
    {
        float heard = -1f;
        System.Action<float> on = v => heard = v;
        GameAudio.EffectsVolumeChanged += on;
        try
        {
            GameAudio.EffectsVolume = 0.7f;
            GameAudio.AttractMute = true;
            Assert.AreEqual(0f, GameAudio.EffectsVolume);
            Assert.AreEqual(0f, heard, "sources hear the mute");
            GameAudio.AttractMute = false;
            Assert.AreEqual(0.7f, GameAudio.EffectsVolume, 1e-5f, "the player's setting is kept");
            Assert.AreEqual(0.7f, heard, 1e-5f);
        }
        finally
        {
            GameAudio.EffectsVolumeChanged -= on;
            GameAudio.AttractMute = false;
            GameAudio.EffectsVolume = 1f;
        }
    }
}
