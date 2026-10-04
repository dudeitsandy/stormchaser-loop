using NUnit.Framework;

/// <summary>design/ux/run-screens.md pause menu model (run-screens story 001).</summary>
public class PauseMenuTests
{
    [Test]
    public void Desktop_HasFourEntries_WebGL_HasNoQuitToDesktop()
    {
        CollectionAssert.AreEqual(
            new[] { PauseItem.Resume, PauseItem.Settings, PauseItem.QuitRun, PauseItem.QuitToDesktop },
            new PauseMenu(canQuitToDesktop: true).Items);
        CollectionAssert.DoesNotContain(new PauseMenu(canQuitToDesktop: false).Items, PauseItem.QuitToDesktop);
    }

    [Test]
    public void FocusStartsOnResume_AndWrapsBothWays()
    {
        var menu = new PauseMenu(true);
        Assert.AreEqual(0, menu.Focus);
        menu.Move(-1);
        Assert.AreEqual(3, menu.Focus);
        menu.Move(1);
        Assert.AreEqual(0, menu.Focus);
    }

    [Test]
    public void SelectResume_Resumes_SelectSettings_ShowsSettings()
    {
        var menu = new PauseMenu(true);
        Assert.AreEqual(PauseAction.Resume, menu.Select());
        menu.Move(1);
        Assert.AreEqual(PauseAction.ShowSettings, menu.Select());
    }

    [Test]
    public void QuitRun_OpensConfirm_DefaultKeepPlaying_MashedSelectNeverQuits()
    {
        var menu = new PauseMenu(true);
        menu.Move(2);
        Assert.AreEqual(PauseAction.None, menu.Select(), "first press only opens the confirm");
        Assert.AreEqual(PauseItem.QuitRun, menu.Confirming);
        Assert.IsFalse(menu.ConfirmQuitFocused, "default focus is KEEP PLAYING");
        Assert.AreEqual(PauseAction.None, menu.Select(), "a second mashed press keeps playing");
        Assert.IsNull(menu.Confirming);
    }

    [Test]
    public void ConfirmQuit_Forfeits_ConfirmQuitToDesktop_QuitsToDesktop()
    {
        var menu = new PauseMenu(true);
        menu.Move(2);
        menu.Select();
        menu.Move(1);
        Assert.AreEqual(PauseAction.ForfeitToTitle, menu.Select());

        menu.Move(1); // focus QUIT TO DESKTOP
        Assert.AreEqual(PauseItem.QuitToDesktop, menu.Items[menu.Focus]);
        menu.Select();
        menu.Move(1);
        Assert.AreEqual(PauseAction.QuitToDesktop, menu.Select());
    }

    [Test]
    public void Back_InConfirm_ReturnsToMenu_InMenu_Resumes()
    {
        var menu = new PauseMenu(true);
        menu.Move(2);
        menu.Select();
        Assert.AreEqual(PauseAction.None, menu.Back());
        Assert.IsNull(menu.Confirming);
        Assert.AreEqual(PauseAction.Resume, menu.Back());
    }
}
