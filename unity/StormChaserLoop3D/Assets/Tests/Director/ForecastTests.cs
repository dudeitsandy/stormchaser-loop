using NUnit.Framework;
using UnityEngine;

/// <summary>storm-director.md F4 forecast estimate (story 007): AC-14, AC-15, AC-16.</summary>
public class ForecastTests
{
    // ---------- AC-14 ----------

    [Test]
    public void InsideExactRange_ShownEqualsTrue_ForEveryEfAndBias()
    {
        for (int ef = 0; ef <= 5; ef++)
        for (float b = -3f; b <= 3f; b += 0.25f)
        for (float d = 0f; d <= 150f; d += 5f)
            Assert.AreEqual(ef, StormForecast.RawShownEf(ef, b, d), $"EF{ef} b={b} d={d}");
    }

    [Test]
    public void AtAnyDistance_ErrorIsAtMostOne()
    {
        for (int ef = 0; ef <= 5; ef++)
        for (float b = -3f; b <= 3f; b += 0.25f)
        for (float d = 0f; d <= 2000f; d += 25f)
            Assert.LessOrEqual(Mathf.Abs(StormForecast.RawShownEf(ef, b, d) - ef), 1, $"EF{ef} b={b} d={d}");
    }

    // ---------- AC-15 ----------

    [Test]
    public void SameCellAndDistance_ThousandEvaluations_SameValue()
    {
        var f = new StormForecast(42);
        int first = f.ShownEf(3, 2, 640f);
        for (int i = 0; i < 1000; i++) Assert.AreEqual(first, f.ShownEf(3, 2, 640f));
    }

    [Test]
    public void DistanceOscillatingAcrossBoundary_NeverFlickers_ThenFlipsTenMetresPast()
    {
        // Find a cell whose bias puts an EF2 flip boundary (b·σ = ±0.5) inside the error band.
        var f = new StormForecast(7);
        int id = 0;
        float bias = 0f;
        for (; id < 500; id++)
        {
            f.BiasesFor(id, out bias, out _);
            if (Mathf.Abs(bias) > 1f) break;
        }
        float boundary = 150f + 0.5f / (0.7f * Mathf.Abs(bias)) * 650f;
        int before = f.ShownEf(id, 2, boundary - 30f);
        for (int i = 0; i < 50; i++)
            Assert.AreEqual(before, f.ShownEf(id, 2, boundary + (i % 2 == 0 ? 5f : -5f)), $"oscillation {i}");
        int after = f.ShownEf(id, 2, boundary + 12f);
        Assert.AreNotEqual(before, after, "10 m past the boundary the estimate updates");
    }

    [Test]
    public void TenThousandEf2CellsAt800m_AboutFortySevenPointFivePercentWrong()
    {
        var f = new StormForecast(2026);
        int wrong = 0;
        for (int id = 0; id < 10000; id++) if (f.ShownEf(id, 2, 800f) != 2) wrong++;
        Assert.AreEqual(0.475f, wrong / 10000f, 0.015f);
    }

    // ---------- AC-16 ----------

    [Test]
    public void Eta_IsNonNegativeMultipleOfFive_AndWithinErrorBounds()
    {
        for (float bt = -3f; bt <= 3f; bt += 0.5f)
        for (float t = 0f; t <= 120f; t += 7f)
        for (float d = 0f; d <= 1200f; d += 50f)
        {
            float eta = StormForecast.ShownEta(t, bt, d);
            Assert.GreaterOrEqual(eta, 0f);
            Assert.AreEqual(0f, Mathf.Repeat(eta, 5f), 1e-3f);
            float bound = d <= 150f ? 0.05f * t + 2.5f : 0.45f * t + 2.5f;
            Assert.LessOrEqual(Mathf.Abs(eta - t), bound + 1e-3f, $"t={t} bt={bt} d={d}");
        }
    }

    [Test]
    public void PanelText_MarksUncertaintyAndStatus()
    {
        var far = new ForecastRow { Bearing = "NW", ShownEf = 3, Uncertain = true, Distance = 412f, Status = ForecastStatus.Forming, EtaSeconds = 45f };
        var near = new ForecastRow { Bearing = "E", ShownEf = 4, Uncertain = false, Distance = 90f, Status = ForecastStatus.OnGround, EtaSeconds = 20f };
        var rope = new ForecastRow { Bearing = "S", ShownEf = 2, Uncertain = true, Distance = 300f, Status = ForecastStatus.RopingOut, EtaSeconds = -1f };
        Assert.AreEqual("NW  ~EF3  412 m  PEAK ~45s", HudController.ForecastText(far));
        Assert.AreEqual("E   EF4  90 m  ON GROUND ~20s", HudController.ForecastText(near));
        StringAssert.EndsWith("ROPING OUT", HudController.ForecastText(rope));
    }

    [Test]
    public void Gdd_WorkedExample()
    {
        // EF4 at 500 m, b = −1.4 → shows EF3; true ETA 60 s, b_t = +1.2 → ≈ 72.7 s → 75 s.
        Assert.AreEqual(3, StormForecast.RawShownEf(4, -1.4f, 500f));
        Assert.AreEqual(75f, StormForecast.ShownEta(60f, 1.2f, 500f));
    }
}
