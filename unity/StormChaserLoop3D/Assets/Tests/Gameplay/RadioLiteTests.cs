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
    public void Warning_DucksForTheSirenCycle_ThenRestores()
    {
        var d = new MusicDuck();
        Assert.AreEqual(1f, d.TargetGain(0f));
        d.OnCellForming(cellId: 1, trueEf: 3, now: 10f);
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(10f));
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(34.9f));
        // X9-04: hold through the first second of the siren tail, then ease back over 2 s (no snap).
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(35.9f));
        float mid = d.TargetGain(37f);
        Assert.Greater(mid, MusicDuck.DuckedGain);
        Assert.Less(mid, 1f);
        Assert.AreEqual(1f, d.TargetGain(38.01f));
    }

    [Test]
    public void Release_RisesSmoothly_NeverJumps()
    {
        // Arrange
        var d = new MusicDuck();
        d.OnCellForming(1, 3, 0f);
        float prev = d.TargetGain(25f);
        // Act / Assert: sampled at 60 Hz through the release, gain only rises, by small steps
        for (float t = 25f; t <= 29f; t += 1f / 60f)
        {
            float g = d.TargetGain(t);
            Assert.GreaterOrEqual(g, prev - 1e-6f, $"t {t}");
            Assert.Less(g - prev, 0.03f, $"t {t}: a step, not a ramp");
            prev = g;
        }
        Assert.AreEqual(1f, prev);
    }

    [Test]
    public void Ef2Forming_DoesNotDuck()
    {
        var d = new MusicDuck();
        d.OnCellForming(1, 2, 0f);
        Assert.AreEqual(1f, d.TargetGain(1f));
    }

    [Test]
    public void Emergency_DucksUntilThatCellRopesOut()
    {
        var d = new MusicDuck();
        d.OnCellForming(5, 5, 0f);
        Assert.IsTrue(d.IsDucked(500f), "an EF5 emergency holds the duck past the siren cycle");
        d.OnCellDeclined(4, 500f);
        Assert.IsTrue(d.IsDucked(500f), "another cell's rope-out doesn't release it");
        d.OnCellDeclined(5, 500f);
        Assert.IsFalse(d.IsDucked(500f));
        Assert.AreEqual(MusicDuck.DuckedGain, d.TargetGain(500.5f), "held through the siren tail's first second");
        Assert.AreEqual(1f, d.TargetGain(503.01f));
    }

    [Test]
    public void AnchorTouchdown_DucksBriefly_SatelliteDoesNot()
    {
        var d = new MusicDuck();
        d.OnCellPeak(StormCellRole.Satellite, 4, 0f);
        Assert.IsFalse(d.IsDucked(1f));
        d.OnCellPeak(StormCellRole.Anchor, 4, 0f);
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
