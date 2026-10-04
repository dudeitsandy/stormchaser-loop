using NUnit.Framework;
using UnityEngine;

/// <summary>
/// storm-director.md F1 (story 001): own RNG stream, regime draw shaped by Heat, anchor EF tables with the
/// Heat 5 floor, Chaos cells with at most one EF5. AC-1, AC-2, AC-5, AC-7.
/// </summary>
public class RegimeDrawTests
{
    private const int Samples = 10000;
    private static readonly DirectorTuning T = DirectorTuning.Defaults;

    // ---------- AC-1: determinism, own stream ----------

    [Test]
    public void Plan_SameSeedTwice_IsByteIdentical_EvenWithUnityRandomCallsBetween()
    {
        // Arrange
        string first = WeatherPlanner.Build(12345, 2, T).Serialize();

        // Act: disturb Unity's global RNG between the two builds
        Random.InitState(999);
        for (int i = 0; i < 1000; i++) Random.value.GetHashCode();
        string second = WeatherPlanner.Build(12345, 2, T).Serialize();

        // Assert
        Assert.AreEqual(first, second);
    }

    [Test]
    public void Rng_SameSeedSameStream_ReproducesSequence_DifferentStreamsDiffer()
    {
        var a = new DirectorRng(42, 1);
        var b = new DirectorRng(42, 1);
        var c = new DirectorRng(42, 2);
        bool anyDifferent = false;
        for (int i = 0; i < 100; i++)
        {
            uint x = a.NextUInt();
            Assert.AreEqual(x, b.NextUInt());
            if (x != c.NextUInt()) anyDifferent = true;
        }
        Assert.IsTrue(anyDifferent, "sub-streams must not mirror each other");
    }

    [Test]
    public void Rng_Floats_StayInUnitInterval_AndIntRangeIsInclusive()
    {
        var rng = new DirectorRng(7, 0);
        bool sawMin = false, sawMax = false;
        for (int i = 0; i < 5000; i++)
        {
            float f = rng.NextFloat01();
            Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            int n = rng.RangeInclusive(3, 8);
            Assert.That(n, Is.InRange(3, 8));
            sawMin |= n == 3;
            sawMax |= n == 8;
        }
        Assert.IsTrue(sawMin && sawMax);
    }

    // ---------- AC-2: regime shares ----------

    [TestCase(0, new[] { .200f, .250f, .300f, .150f, .100f })]
    [TestCase(5, new[] { .007f, .165f, .254f, .345f, .230f })]
    public void RegimeProbabilities_MatchF1(int heat, float[] expected)
    {
        float[] p = RegimeDraw.Probabilities(heat, T);
        for (int r = 0; r < expected.Length; r++) Assert.AreEqual(expected[r], p[r], 0.0015f, $"regime {(Regime)r}");
    }

    [TestCase(0, new[] { .200f, .250f, .300f, .150f, .100f })]
    [TestCase(5, new[] { .007f, .165f, .254f, .345f, .230f })]
    public void RegimeDraw_10kSeeds_SharesWithinOnePointFivePoints(int heat, float[] expected)
    {
        var counts = new int[5];
        for (int seed = 0; seed < Samples; seed++) counts[(int)WeatherPlanner.Build(seed, heat, T).Regime]++;
        for (int r = 0; r < 5; r++)
            Assert.AreEqual(expected[r], counts[r] / (float)Samples, 0.015f, $"regime {(Regime)r}");
    }

    // ---------- Anchor tables ----------

    [Test]
    public void AnchorTable_Heat3Sequence_MatchesWorkedExample()
    {
        // F1 example: Sequence p5 = .03 × 2.2 = .066, taken from EF2 → EF2 .264, EF3 .40, EF4 .27, EF5 .066
        float[] t = RegimeDraw.AnchorTable(Regime.Sequence, 3, T);
        Assert.AreEqual(0.264f, t[2], 1e-4f);
        Assert.AreEqual(0.400f, t[3], 1e-4f);
        Assert.AreEqual(0.270f, t[4], 1e-4f);
        Assert.AreEqual(0.066f, t[5], 1e-4f);
    }

    [TestCase(Regime.Sequence)]
    [TestCase(Regime.Outbreak)]
    public void AnchorTable_Heat5_FloorsSequenceAndOutbreakAtEf4(Regime regime)
    {
        float[] t = RegimeDraw.AnchorTable(regime, 5, T);
        Assert.AreEqual(0f, t[0] + t[1] + t[2] + t[3], 1e-5f);
        Assert.AreEqual(0.91f, t[4], 1e-4f);
        Assert.AreEqual(0.09f, t[5], 1e-4f);
    }

    [TestCase(Regime.Quiet)]
    [TestCase(Regime.LoneGiant)]
    [TestCase(Regime.Sequence)]
    [TestCase(Regime.Outbreak)]
    public void AnchorTables_SumToOne_AtEveryHeat(Regime regime)
    {
        for (int h = 0; h <= 5; h++)
        {
            float sum = 0f;
            foreach (float p in RegimeDraw.AnchorTable(regime, h, T)) sum += p;
            Assert.AreEqual(1f, sum, 1e-4f, $"{regime} heat {h}");
        }
    }

    // ---------- AC-5: EF5 rarity ----------

    [TestCase(0, 0.0439f, 0.006f)]
    [TestCase(5, 0.1385f, 0.011f)]
    public void AnyEf5_10kEpicSeeds_MatchesExactF1Rate(int heat, float expected, float tolerance)
    {
        int withEf5 = 0;
        for (int seed = 0; seed < Samples; seed++)
            if (WeatherPlanner.Build(seed, heat, T).HasEf5) withEf5++;
        Assert.AreEqual(expected, withEf5 / (float)Samples, tolerance);
    }

    [Test]
    public void ExactAnyEf5Rate_MatchesGdd()
    {
        Assert.AreEqual(0.0439f, RegimeDraw.ExactAnyEf5Rate(0, T), 0.0002f);
        Assert.AreEqual(0.1385f, RegimeDraw.ExactAnyEf5Rate(5, T), 0.0002f);
    }

    // ---------- AC-7: Chaos ----------

    [Test]
    public void Chaos_10kPlansAtHeat5_NoAnchor_AndNeverMoreThanOneEf5()
    {
        int chaosPlans = 0;
        for (int seed = 0; seed < Samples * 3 && chaosPlans < Samples; seed++)
        {
            WeatherPlan plan = WeatherPlanner.Build(seed, 5, T);
            if (plan.Regime != Regime.Chaos) continue;
            chaosPlans++;
            Assert.AreEqual(-1, plan.AnchorEf, "Chaos has no anchor");
            int ef5 = 0;
            foreach (int ef in plan.ChaosEfs) if (ef == 5) ef5++;
            Assert.LessOrEqual(ef5, 1);
            Assert.That(plan.ChaosEfs.Count, Is.InRange(3, 8));
        }
        Assert.Greater(chaosPlans, 1000);
    }

    [Test]
    public void ChaosOneEf5Rule_ForcedSecondEf5_BecomesEf4()
    {
        int[] cells = RegimeDraw.ApplyOneEf5Rule(new[] { 5, 2, 5, 5 });
        CollectionAssert.AreEqual(new[] { 5, 2, 4, 4 }, cells);
    }
}
