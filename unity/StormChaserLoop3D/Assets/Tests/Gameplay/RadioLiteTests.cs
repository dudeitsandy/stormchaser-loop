using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Sprint 8 S8-09 radio lite: shuffle without immediate repeats, and ducking under warnings.</summary>
public class RadioLiteTests
{
    [Test]
    public void Playlist_PlaysEverySongBeforeARepeat_AndNeverTheSameSongTwiceInARow()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            var p = new RadioPlaylist(4, seed);
            int last = -1;
            for (int round = 0; round < 5; round++)
            {
                var seen = new HashSet<int>();
                for (int i = 0; i < 4; i++)
                {
                    int s = p.Next();
                    Assert.AreNotEqual(last, s, $"seed {seed}: immediate repeat");
                    Assert.IsTrue(seen.Add(s), $"seed {seed}: repeat inside a round");
                    last = s;
                }
            }
        }
    }

    [Test]
    public void Playlist_EdgeCounts()
    {
        Assert.AreEqual(-1, new RadioPlaylist(0, 1).Next());
        var one = new RadioPlaylist(1, 1);
        Assert.AreEqual(0, one.Next());
        Assert.AreEqual(0, one.Next(), "a one-song radio repeats it");
    }

    [Test]
    public void Warning_DucksFiveSeconds_ThenRadioRecoversOverTen_WhileTheWarningIsStillLive()
    {
        // Arrange
        var d = new MusicDuck();
        Assert.AreEqual(1f, d.TargetGain(0f));
        // Act
        d.OnCellForming(cellId: 1, trueEf: 3, now: 10f);
        // Assert: attention hold, then a DJ-break recovery under the still-live siren (X9-04, Andy 2026-10-05)
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(10f));
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(14.9f));
        float mid = d.TargetGain(20f);
        Assert.Greater(mid, MusicDuck.DuckedGain);
        Assert.Less(mid, 1f);
        Assert.IsTrue(d.IsDucked(25.1f), "the warning is still live");
        Assert.AreEqual(1f, d.TargetGain(25.1f), 1e-4f, "radio fully back while the siren sits underneath");
    }

    [Test]
    public void SameCellAgain_DoesNotRestartRecovery_ANewCellReDucks()
    {
        // Arrange: a warning, recovering
        var d = new MusicDuck();
        d.OnCellForming(1, 4, 0f);
        float recovering = d.TargetGain(10f);
        // Act: the same cell again (duplicate lifecycle event)
        d.OnCellForming(1, 4, 9f);
        // Assert
        Assert.AreEqual(recovering, d.TargetGain(10f), 1e-5f, "a duplicate never fabricates a fresh duck");
        // Act: a different cell warns
        d.OnCellForming(2, 3, 12f);
        // Assert
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(12f), "a new warning re-ducks");
    }

    [Test]
    public void Release_RisesSmoothly_NeverJumps()
    {
        // Arrange: duck ends during the attention hold (from the ducked level)
        var d = new MusicDuck();
        d.OnCellPeak(7, StormCellRole.Anchor, 4, 0f); // live 8 s, attention hold 5 s then recovering
        float prev = d.TargetGain(0f);
        // Act / Assert: sampled at 60 Hz until well after release, gain only rises, by small steps
        for (float t = 0f; t <= 16f; t += 1f / 60f)
        {
            float g = d.TargetGain(t);
            Assert.GreaterOrEqual(g, prev - 1e-6f, $"t {t}");
            Assert.Less(g - prev, 0.03f, $"t {t}: a step, not a ramp");
            prev = g;
        }
        Assert.AreEqual(1f, prev, 1e-4f);
    }

    [Test]
    public void Ef2Forming_DoesNotDuck()
    {
        var d = new MusicDuck();
        d.OnCellForming(1, 2, 0f);
        Assert.AreEqual(1f, d.TargetGain(1f));
    }

    [Test]
    public void Emergency_StaysLiveUntilThatCellRopesOut_RadioRecoversUnderIt_NoDipAtTheEnd()
    {
        // Arrange
        var d = new MusicDuck();
        d.OnCellForming(5, 5, 0f);
        // Assert: still live long after, but the radio has come back underneath
        Assert.IsTrue(d.IsDucked(500f), "an EF5 emergency stays live past the siren cycle");
        Assert.AreEqual(1f, d.TargetGain(500f), 1e-4f);
        d.OnCellDeclined(4, 500f);
        Assert.IsTrue(d.IsDucked(500f), "another cell's rope-out doesn't release it");
        // Act
        d.OnCellDeclined(5, 500f);
        // Assert: already recovered, so the end-of-demand release never dips it again
        Assert.IsFalse(d.IsDucked(500f));
        Assert.AreEqual(1f, d.TargetGain(500.5f), 1e-4f);
        Assert.AreEqual(1f, d.TargetGain(503.01f), 1e-4f);
    }

    [Test]
    public void EmergencyEndingDuringTheHold_HoldsThenEasesBackOverTheSirenTail()
    {
        // Arrange: the EF5 ropes out 2 s after forming (still in the attention hold)
        var d = new MusicDuck();
        d.OnCellForming(5, 5, 0f);
        d.OnCellDeclined(5, 2f);
        // Assert: held for 1 s of the siren tail, then eased back over 2 s
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(2.9f));
        float mid = d.TargetGain(4f);
        Assert.Greater(mid, MusicDuck.DuckedGain);
        Assert.Less(mid, 1f);
        Assert.AreEqual(1f, d.TargetGain(5.01f), 1e-4f);
    }

    [Test]
    public void AnchorTouchdown_DucksBriefly_SatelliteDoesNot()
    {
        var d = new MusicDuck();
        d.OnCellPeak(3, StormCellRole.Satellite, 4, 0f);
        Assert.IsFalse(d.IsDucked(1f));
        d.OnCellPeak(4, StormCellRole.Anchor, 4, 0f);
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(1f));
        Assert.IsTrue(d.IsDucked(7.9f));
        Assert.IsFalse(d.IsDucked(8.1f));
    }

    [TestCase("radio_winding_chase", true, false)]
    [TestCase("jingle_01", false, true)]
    [TestCase("storm_radio_jingle", false, true)]
    [TestCase("title_loop", false, false)]
    public void Clips_AreSongsOrJingles_NeverBoth(string name, bool song, bool jingle)
    {
        Assert.AreEqual(song, RadioLite.IsSong(name));
        Assert.AreEqual(jingle, RadioLite.IsJingle(name));
    }

    [TestCase("radio_give_me_a_gravel_road", "GIVE ME A GRAVEL ROAD")]
    [TestCase("radio_geese", "GEESE")]
    public void SongTitle_FromClipName_UppercaseWithSpaces(string clip, string title)
    {
        Assert.AreEqual(title, RadioLite.SongTitle(clip));
    }

    [TestCase(true, "radio_geese", true)]
    [TestCase(true, "jingle_03", false)]
    [TestCase(true, "storm_radio_jingle", false)]
    [TestCase(false, "title_loop", false)]
    [TestCase(false, "radio_geese", false)]
    [TestCase(true, null, false)]
    public void CanSkip_OnlyASongOnTheRunRadio(bool radioOn, string playing, bool expected)
    {
        Assert.AreEqual(expected, RadioLite.CanSkip(radioOn, playing));
    }

    [Test]
    public void JingleRotation_FiveJingles_NeverBackToBack()
    {
        var p = new RadioPlaylist(5, 42);
        int last = -1;
        for (int i = 0; i < 100; i++)
        {
            int j = p.Next();
            Assert.AreNotEqual(last, j);
            last = j;
        }
    }
}
