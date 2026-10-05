using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Save &amp; Profile M1 stem (run-goals-v1 story 005): round trip, one write per run, write failure, import.</summary>
public class AccomplishmentsTests
{
    private const string Big = "career.compact.heartland.big_air";
    private const string Rope = "bounty.compact.rope_out";
    private static readonly DateTime Now = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

    private static List<GoalCompletion> Run(params string[] ids)
    {
        var list = new List<GoalCompletion>();
        foreach (string id in ids) list.Add(new GoalCompletion(id, id.StartsWith("career") ? GoalKind.Career : GoalKind.Bounty, 150, true));
        return list;
    }

    [Test]
    public void RoundTrip_SaveThenLoad_IsIdentical_AndSchemaIsTheDisposableStem()
    {
        var disk = new MemoryProfileStorage();
        var a = new ProfileStore(disk);
        a.RecordRunComplete(Run(Big, Rope), "compact", 554, "0.7.9", Now, 1650f, new[] { "livery.ktvr" });
        a.SetLivery("livery.ktvr");

        var b = new ProfileStore(disk);
        Assert.AreEqual(0, b.Data.SchemaVersion);
        Assert.AreEqual(1650f, b.Data.BestScore);
        Assert.IsTrue(b.HasCompleted(Big));
        Assert.IsTrue(b.HasCompleted(Rope));
        Assert.IsTrue(b.IsUnlocked("livery.ktvr"));
        Assert.AreEqual("livery.ktvr", b.Data.Livery);
        AccomplishmentEntry e = b.Data.Accomplishments.Find(x => x.Id == Big);
        Assert.AreEqual(554, e.FirstSeed);
        Assert.AreEqual("0.7.9", e.FirstVersion);
        Assert.AreEqual(1, e.Count);
        Assert.AreEqual(JsonUtility.ToJson(a.Data), JsonUtility.ToJson(b.Data));
    }

    [Test]
    public void SecondCompletion_KeepsTheFirstSeedAndCounts()
    {
        var store = new ProfileStore(new MemoryProfileStorage());
        store.RecordRunComplete(Run(Big), "compact", 1, "0.7.9", Now, 0f);
        store.RecordRunComplete(Run(Big), "compact", 2, "0.8.0", Now.AddDays(1), 0f);
        AccomplishmentEntry e = store.Data.Accomplishments.Find(x => x.Id == Big);
        Assert.AreEqual(1, e.FirstSeed);
        Assert.AreEqual(2, e.Count);
        Assert.AreEqual(1, store.CareerCount("compact", "heartland"));
    }

    [Test]
    public void UnknownGoalAndUnlockIds_SurviveLoadAndResave()
    {
        var disk = new MemoryProfileStorage();
        var a = new ProfileStore(disk);
        a.RecordRunComplete(Run("career.compact.heartland.from_the_future"), "compact", 1, "9.9", Now, 0f, new[] { "lens.future" });
        var b = new ProfileStore(disk);
        b.RecordRunComplete(Run(Big), "compact", 2, "0.7.9", Now, 0f);
        var c = new ProfileStore(disk);
        Assert.IsTrue(c.HasCompleted("career.compact.heartland.from_the_future"));
        Assert.IsTrue(c.IsUnlocked("lens.future"));
    }

    [Test]
    public void OldOrMissingSave_LoadsEmpty_AndImportsThePlayerPrefsBestOnce()
    {
        var disk = new MemoryProfileStorage();
        disk.Files[ProfileStore.ProfileFile] = "{\"SchemaVersion\":0,\"BestScore\":0}"; // a save from before goals
        var store = new ProfileStore(disk, () => 1249f);
        Assert.AreEqual(0, store.Data.Accomplishments.Count);
        Assert.AreEqual(1249f, store.Data.BestScore, "legacy best imported");
        store.RecordRunComplete(Run(), "compact", 1, "0.7.9", Now, 10f);
        var again = new ProfileStore(disk, () => 5000f);
        Assert.AreEqual(1249f, again.Data.BestScore, "imported once only");
    }

    [Test]
    public void RunComplete_IsOneWrite()
    {
        var disk = new MemoryProfileStorage();
        var store = new ProfileStore(disk);
        store.RecordRunComplete(Run(Big, Rope), "compact", 1, "0.7.9", Now, 900f, new[] { "livery.ktvr" });
        Assert.AreEqual(1, disk.Writes);
    }

    [Test]
    public void FailedWrite_KeepsProgressInMemory_AndTheNextRunWritesItAll()
    {
        var disk = new MemoryProfileStorage { FailWrites = true };
        var store = new ProfileStore(disk);
        Assert.IsFalse(store.RecordRunComplete(Run(Big), "compact", 1, "0.7.9", Now, 900f));
        Assert.IsTrue(store.LastWriteFailed);
        Assert.IsTrue(store.HasCompleted(Big), "kept in memory");

        disk.FailWrites = false;
        Assert.IsTrue(store.RecordRunComplete(Run(Rope), "compact", 2, "0.7.9", Now, 100f));
        Assert.IsFalse(store.LastWriteFailed);
        var reloaded = new ProfileStore(disk);
        Assert.IsTrue(reloaded.HasCompleted(Big), "the earlier run's progress reached disk with the next write");
        Assert.IsTrue(reloaded.HasCompleted(Rope));
        Assert.AreEqual(900f, reloaded.Data.BestScore);
    }

    [Test]
    public void DeviceSettings_SaveAndLoad()
    {
        var disk = new MemoryProfileStorage();
        var a = new ProfileStore(disk);
        a.Settings.Brightness = 0.25f;
        a.Settings.InvertY = true;
        a.Settings.CameraPreset = "HIGH";
        Assert.IsTrue(a.SaveSettings());
        var b = new ProfileStore(disk);
        Assert.AreEqual(0.25f, b.Settings.Brightness);
        Assert.IsTrue(b.Settings.InvertY);
        Assert.AreEqual("HIGH", b.Settings.CameraPreset);
    }

    [Test]
    public void UnreadableFile_StartsFresh()
    {
        var disk = new MemoryProfileStorage();
        disk.Files[ProfileStore.ProfileFile] = "not json {";
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
        var store = new ProfileStore(disk);
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        Assert.AreEqual(0, store.Data.Accomplishments.Count);
    }
}
