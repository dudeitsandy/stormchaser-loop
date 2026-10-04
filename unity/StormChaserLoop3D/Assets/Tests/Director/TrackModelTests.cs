using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// storm-director.md F5 track motion (story 005): no player input, jogs pre-rolled late in life with a 1 s
/// telegraph and 4.5 s refractory, jog speed caps, and frame-rate independent substeps. AC-17 (Unit), AC-18, AC-23.
/// </summary>
public class TrackModelTests
{
    private static readonly TrackTuning Tuning = TrackTuning.Defaults;
    private static readonly float[] TrackSpeed = { 3f, 4f, 5f, 6.5f, 8f, 8f };
    private static readonly float[] TurnRate = { 24f, 20f, 16f, 12f, 9f, 7f };

    private static PlannedCell Cell(int ef, float form = 10f, float mature = 45f, float rope = 15f) =>
        new PlannedCell { Id = ef, Ef = ef, Form = form, Mature = mature, Rope = rope, Position = new Vector2(40f, 30f) };

    private static CellTrack Track(PlannedCell cell, long seed, int heat) =>
        new CellTrack(cell, seed, heat, TrackSpeed[cell.Ef], TurnRate[cell.Ef], Vector2.zero, 90f, Tuning);

    // ---------- AC-17 (Unit companion): never reads the player ----------

    [Test]
    public void TrackModelApi_TakesNoPlayerOrTransformParameter()
    {
        foreach (MethodBase m in typeof(CellTrack).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                 .Cast<MethodBase>().Concat(typeof(CellTrack).GetConstructors()))
        foreach (ParameterInfo p in m.GetParameters())
        {
            Assert.IsFalse(typeof(Component).IsAssignableFrom(p.ParameterType), $"{m.Name}({p.ParameterType.Name})");
            Assert.IsFalse(typeof(GameObject).IsAssignableFrom(p.ParameterType), $"{m.Name}({p.ParameterType.Name})");
        }
    }

    // ---------- AC-18: jogs ----------

    [TestCase(0)]
    [TestCase(3)]
    public void Jogs_StartLateInLife_TelegraphOneSecondBeforeTurn_RefractoryFourPointFive(int heat)
    {
        int jogs = 0;
        for (int seed = 0; seed < 400; seed++)
        for (int ef = 0; ef <= 5; ef++)
        {
            PlannedCell cell = Cell(ef);
            CellTrack track = Track(cell, seed, heat);
            float life = track.Lifetime;
            float previous = float.NegativeInfinity;
            foreach (TrackJog jog in track.Jogs)
            {
                jogs++;
                Assert.GreaterOrEqual(jog.Start / life, 0.6f - 1e-4f, "jogs start at u ≥ 0.6");
                Assert.AreEqual(1.0f, jog.TurnStart - jog.Start, 0.05f, "tilt telegraph 1 s before the turn");
                Assert.GreaterOrEqual(jog.Start - previous, 4.5f - 1e-4f, "refractory");
                Assert.That(Mathf.Abs(jog.TurnDeg), Is.InRange(40f, 110f));
                previous = jog.Start;
            }
        }
        Assert.Greater(jogs, 100, "the sample should contain jogs");
    }

    [TestCase(0, 12f)]
    [TestCase(1, 15.05f)]
    [TestCase(5, 15.05f)]
    public void Speed_NeverExceedsJogCap(int heat, float cap)
    {
        float fastest = 0f;
        for (int seed = 0; seed < 60; seed++)
        {
            CellTrack track = Track(Cell(5), seed, heat);
            Vector2 last = track.Position;
            for (float age = 0.05f; age <= track.Lifetime; age += 0.05f)
            {
                track.AdvanceTo(age);
                fastest = Mathf.Max(fastest, (track.Position - last).magnitude / 0.05f);
                last = track.Position;
            }
        }
        Debug.Log($"[Track] heat {heat} fastest {fastest:F2} m/s");
        Assert.LessOrEqual(fastest, cap + 0.01f);
    }

    [Test]
    public void Jog_TelegraphWindow_IsReportedForPresentation()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            CellTrack track = Track(Cell(4), seed, 0);
            if (track.Jogs.Count == 0) continue;
            TrackJog jog = track.Jogs[0];
            Assert.IsTrue(track.IsTelegraphingAt(jog.Start + 0.5f, out float lean));
            Assert.AreEqual(Mathf.Sign(jog.TurnDeg), Mathf.Sign(lean));
            Assert.IsFalse(track.IsTelegraphingAt(jog.TurnStart + 0.1f, out _));
            return;
        }
        Assert.Fail("no jog found in 200 seeds");
    }

    // ---------- AC-23: frame-rate independence ----------

    [Test]
    public void Positions_At5And60Fps_MatchAtAges10_30_60()
    {
        PlannedCell cell = Cell(3, form: 20f, mature: 60f, rope: 20f);
        CellTrack slow = Track(cell, 777, 2);
        CellTrack fast = Track(cell, 777, 2);
        foreach (float checkpoint in new[] { 10f, 30f, 60f })
        {
            StepTo(slow, checkpoint, 1f / 5f);
            StepTo(fast, checkpoint, 1f / 60f);
            Assert.AreEqual(slow.Position.x, fast.Position.x, 0.01f, $"x at {checkpoint}");
            Assert.AreEqual(slow.Position.y, fast.Position.y, 0.01f, $"z at {checkpoint}");
        }
    }

    private static void StepTo(CellTrack track, float target, float frame)
    {
        float age = track.Age;
        while (age < target - 1e-5f)
        {
            age = Mathf.Min(target, age + frame);
            track.AdvanceTo(age);
        }
    }

    // ---------- Shape ----------

    [Test]
    public void SameSeed_SameTrack_DifferentSeed_DifferentTrack()
    {
        CellTrack a = Track(Cell(2), 5, 0), b = Track(Cell(2), 5, 0), c = Track(Cell(2), 6, 0);
        a.AdvanceTo(40f); b.AdvanceTo(40f); c.AdvanceTo(40f);
        Assert.AreEqual(a.Position, b.Position);
        Assert.AreNotEqual(a.Position, c.Position);
    }

    [Test]
    public void InitialHeading_WithinSixtyDegreesOfWorldCentre()
    {
        for (int seed = 0; seed < 500; seed++)
        {
            CellTrack track = Track(Cell(1), seed, 0);
            float toCentre = Mathf.Atan2(-40f, -30f) * Mathf.Rad2Deg;
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(track.HeadingDeg, toCentre)), 60f + 1e-3f);
        }
    }

    [Test]
    public void CompactSteerBack_KeepsCellsInsideTheArena()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            CellTrack track = Track(Cell(5, form: 11f, mature: 300f, rope: 15f), seed, 5);
            for (float age = 1f; age <= track.Lifetime; age += 1f)
            {
                track.AdvanceTo(age);
                Assert.LessOrEqual(Mathf.Abs(track.Position.x), 90f + 3f, $"seed {seed} age {age}");
                Assert.LessOrEqual(Mathf.Abs(track.Position.y), 90f + 3f, $"seed {seed} age {age}");
            }
        }
    }
}
