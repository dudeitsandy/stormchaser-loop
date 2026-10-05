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
        Assert.AreEqual(1f, d.TargetGain(35.1f));
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
        d.OnCellDeclined(4);
        Assert.IsTrue(d.IsDucked(500f), "another cell's rope-out doesn't release it");
        d.OnCellDeclined(5);
        Assert.IsFalse(d.IsDucked(500f));
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
}
