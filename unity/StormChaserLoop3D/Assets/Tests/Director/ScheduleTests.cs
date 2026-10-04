using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// storm-director.md F2 / Rule 12 compact schedule and plan-time cap resolution (story 002): AC-6, AC-6b,
/// AC-21 and the AC-9 cap rules at CapTotal = 2.
/// </summary>
public class ScheduleTests
{
    private const int Samples = 10000;
    private static readonly DirectorTuning T = DirectorTuning.Defaults;
    private static readonly CompactSettings C = CompactSettings.Defaults;
    private static readonly StormEfTable Ef = StormEfTable.Defaults;

    private static WeatherPlan Plan(int seed, int heat) => WeatherPlanner.BuildCompact(seed, heat, T, C, Ef, Vector2.zero);

    private static IEnumerable<WeatherPlan> Plans(int heat)
    {
        for (int seed = 0; seed < Samples; seed++) yield return Plan(seed, heat);
    }

    // ---------- AC-6: composition ----------

    [TestCase(0)]
    [TestCase(5)]
    public void NonChaosPlans_OneAnchor_SatellitesCappedBelowAnchor_QuietNoEf5(int heat)
    {
        foreach (WeatherPlan p in Plans(heat).Where(p => p.Regime != Regime.Chaos))
        {
            var anchors = p.Cells.Where(c => c.Role == StormCellRole.Anchor).ToList();
            Assert.AreEqual(1, anchors.Count, $"seed {p.Seed}");
            int anchorEf = anchors[0].Ef;
            foreach (PlannedCell s in p.Cells.Where(c => c.Role == StormCellRole.Satellite))
            {
                Assert.LessOrEqual(s.Ef, 3, $"seed {p.Seed}: satellite EF4+");
                Assert.LessOrEqual(s.Ef, anchorEf, $"seed {p.Seed}: satellite above anchor");
            }
            if (p.Regime == Regime.Quiet) Assert.IsFalse(p.Cells.Any(c => c.Ef == 5), $"seed {p.Seed}: Quiet EF5");
        }
    }

    [Test]
    public void Heat5_SequenceAndOutbreak_HaveOneEf4CoAnchor_AnchorAtLeastEf4_OthersNone()
    {
        foreach (WeatherPlan p in Plans(5))
        {
            var co = p.Cells.Where(c => c.Role == StormCellRole.CoAnchor).ToList();
            if (p.Regime == Regime.Sequence || p.Regime == Regime.Outbreak)
            {
                Assert.AreEqual(1, co.Count, $"seed {p.Seed}");
                Assert.AreEqual(4, co[0].Ef);
                Assert.GreaterOrEqual(p.Cells.First(c => c.Role == StormCellRole.Anchor).Ef, 4, $"seed {p.Seed}");
            }
            else Assert.AreEqual(0, co.Count, $"seed {p.Seed}: {p.Regime}");
        }
        foreach (WeatherPlan p in Plans(0)) Assert.IsFalse(p.Cells.Any(c => c.Role == StormCellRole.CoAnchor));
    }

    [Test]
    public void Heat5_Sequence_CoAnchorTakesLastSlot_WithSMinusOneSatellites()
    {
        int checkedPlans = 0;
        foreach (WeatherPlan p in Plans(5).Where(p => p.Regime == Regime.Sequence))
        {
            PlannedCell anchor = p.Cells.First(c => c.Role == StormCellRole.Anchor);
            PlannedCell co = p.Cells.First(c => c.Role == StormCellRole.CoAnchor);
            Assert.AreEqual(anchor.DesiredTime - p.SequenceDelta, co.DesiredTime, 1e-3f, $"seed {p.Seed}");
            Assert.AreEqual(p.SatellitesDrawn - 1, p.Cells.Count(c => c.Role == StormCellRole.Satellite) + p.SatellitesDroppedEarly,
                            $"seed {p.Seed}");
            checkedPlans++;
        }
        Assert.Greater(checkedPlans, 100);
    }

    // ---------- AC-6b: draws ----------

    [Test]
    public void Sequence_SatelliteEfs_RiseOneStepToEfTop()
    {
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, WeatherPlanner.SequenceEfs(3, 4));
        CollectionAssert.AreEqual(new[] { 0, 0, 1 }, WeatherPlanner.SequenceEfs(3, 2));
        foreach (WeatherPlan p in Plans(0).Where(p => p.Regime == Regime.Sequence))
        {
            int anchorEf = p.Cells.First(c => c.Role == StormCellRole.Anchor).Ef;
            int[] expected = WeatherPlanner.SequenceEfs(p.SatellitesDrawn, anchorEf);
            int[] actual = p.Cells.Where(c => c.Role == StormCellRole.Satellite).OrderBy(c => c.DesiredTime).Select(c => c.Ef).ToArray();
            CollectionAssert.AreEqual(expected.Skip(expected.Length - actual.Length).ToArray(), actual, $"seed {p.Seed}");
        }
    }

    [Test]
    public void Outbreak_SatelliteEfUniformBelowCap_AndCountUniform()
    {
        var efCounts = new int[4];
        int efTotal = 0;
        var sCounts = new Dictionary<int, int>();
        int plans = 0;
        foreach (WeatherPlan p in Plans(0).Where(p => p.Regime == Regime.Outbreak))
        {
            plans++;
            sCounts[p.SatellitesDrawn] = sCounts.TryGetValue(p.SatellitesDrawn, out int n) ? n + 1 : 1;
            if (p.Cells.First(c => c.Role == StormCellRole.Anchor).Ef != 4) continue;
            foreach (PlannedCell s in p.Cells.Where(c => c.Role == StormCellRole.Satellite)) { efCounts[s.Ef]++; efTotal++; }
        }
        for (int ef = 0; ef < 4; ef++) Assert.AreEqual(0.25f, efCounts[ef] / (float)efTotal, 0.05f, $"EF{ef} of {efTotal}");
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, sCounts.Keys);
        foreach (int s in sCounts.Keys) Assert.AreEqual(0.5f, sCounts[s] / (float)plans, 0.04f, $"S={s}");
    }

    // ---------- AC-21: compact shape ----------

    [TestCase(0)]
    [TestCase(5)]
    public void Compact_DurationPeakWindowDistancesLifetimesAndCounts(int heat)
    {
        foreach (WeatherPlan p in Plans(heat))
        {
            Assert.AreEqual(180f, p.Duration);
            foreach (PlannedCell c in p.Cells)
            {
                float d = c.Position.magnitude;
                if (c.Role == StormCellRole.Satellite) Assert.That(d, Is.InRange(40f - 1e-3f, 85f + 1e-3f), $"seed {p.Seed} sat");
                else Assert.That(d, Is.InRange(60f - 1e-3f, 85f + 1e-3f), $"seed {p.Seed} {c.Role}");
                Assert.LessOrEqual(Mathf.Abs(c.Position.x), 85f + 1e-3f);
                Assert.LessOrEqual(Mathf.Abs(c.Position.y), 85f + 1e-3f);
                Assert.AreEqual(Mathf.Max(4f, 0.25f * Ef.Form[c.Ef]), c.Form, 1e-4f);
                Assert.AreEqual(0.5f * Ef.Mature[c.Ef], c.Mature, 1e-4f);
                Assert.AreEqual(0.5f * Ef.Rope[c.Ef], c.Rope, 1e-4f);
            }
            PlannedCell anchor = p.Cells.FirstOrDefault(c => c.Role == StormCellRole.Anchor);
            if (anchor != null) Assert.That(anchor.SpawnTime + anchor.Form, Is.InRange(72f - 1e-3f, 117f + 1e-3f), $"seed {p.Seed}");
            if (p.Regime == Regime.Sequence) Assert.That(p.SatellitesDrawn, Is.InRange(1, 3));
            if (p.Regime == Regime.Outbreak) Assert.That(p.SatellitesDrawn, Is.InRange(1, 2));
            if (p.Regime == Regime.Chaos) Assert.That(p.Cells.Count, Is.InRange(3, 5));
        }
    }

    // ---------- Cap resolution (AC-9 rules at CapTotal 2) ----------

    [TestCase(0)]
    [TestCase(5)]
    public void Caps_NeverMoreThanTwoHoldingSlots_WaitsInTenSecondSteps_DropsAfterThirty(int heat)
    {
        foreach (WeatherPlan p in Plans(heat))
        {
            var spawned = p.Cells.Where(c => !c.Dropped).ToList();
            foreach (PlannedCell c in spawned)
            {
                float t = c.SpawnTime;
                int holding = spawned.Count(o => o.SpawnTime <= t + 1e-4f && o.SlotFreeTime > t + 1e-4f);
                Assert.LessOrEqual(holding, 2, $"seed {p.Seed} at t={t}");
                float wait = c.SpawnTime - c.DesiredTime;
                Assert.AreEqual(0f, Mathf.Repeat(wait + 1e-3f, 10f), 2e-3f, $"seed {p.Seed}: waits are 10 s steps");
                Assert.LessOrEqual(wait, 30f + 1e-3f);
            }
            foreach (PlannedCell d in p.Cells.Where(c => c.Dropped)) Assert.AreEqual(StormCellRole.Satellite, d.Role);
        }
    }

    [TestCase(0)]
    [TestCase(5)]
    public void Caps_AnchorsNeverWaitOrDrop_EvictLowestSatelliteInstead(int heat)
    {
        int evictions = 0;
        foreach (WeatherPlan p in Plans(heat))
        {
            foreach (PlannedCell a in p.Cells.Where(c => c.Role != StormCellRole.Satellite && p.Regime != Regime.Chaos))
            {
                Assert.IsFalse(a.Dropped);
                Assert.AreEqual(a.DesiredTime, a.SpawnTime, 1e-4f, $"seed {p.Seed}: anchor waited");
            }
            foreach (PlannedCell e in p.Cells.Where(c => c.EarlyRopeTime >= 0f))
            {
                evictions++;
                Assert.AreEqual(StormCellRole.Satellite, e.Role);
                Assert.IsTrue(p.Cells.Any(a => a.Role != StormCellRole.Satellite && Mathf.Abs(a.SpawnTime - e.EarlyRopeTime) < 1e-4f),
                              $"seed {p.Seed}: eviction coincides with an anchor arrival");
                float age = e.EarlyRopeTime - e.SpawnTime;
                float expectedI0 = age < e.Form ? age / e.Form : 1f;
                Assert.AreEqual(expectedI0, e.EarlyRopeStartIntensity, 1e-4f, "rope-out starts from the current I");
                Assert.AreEqual(age < e.Form, e.FailedTouchdown);
            }
        }
        if (heat == 5) Assert.Greater(evictions, 0, "Heat 5 compact should evict satellites for the co-anchor");
    }

    [Test]
    public void Plan_SameSeed_IsByteIdentical()
    {
        Assert.AreEqual(Plan(777, 3).Serialize(), Plan(777, 3).Serialize());
        Assert.AreNotEqual(Plan(777, 3).Serialize(), Plan(778, 3).Serialize());
    }
    [Test]
    public void Serialize_PlansDifferingOnlyInLifecycle_AreDifferent()
    {
        string Of(float mature, float ropeIntensity)
        {
            var plan = new WeatherPlan { Seed = 9, Duration = 180f, Compact = true };
            plan.Cells.Add(new PlannedCell { Id = 0, Ef = 3, Role = StormCellRole.Anchor, Form = 10f, Mature = mature,
                                             Rope = 8f, EarlyRopeTime = 40f, EarlyRopeStartIntensity = ropeIntensity });
            return plan.Serialize();
        }
        Assert.AreNotEqual(Of(30f, 1f), Of(31f, 1f), "Mature length");
        Assert.AreNotEqual(Of(30f, 1f), Of(30f, 0.5f), "early rope-out start intensity");
    }
}
