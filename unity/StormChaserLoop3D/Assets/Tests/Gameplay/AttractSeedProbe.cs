using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>Probe (Explicit): lists showcase seeds for the title attract mode (anchor EF4+, on the ground early).</summary>
[Explicit, Category("Probe")]
public class AttractSeedProbe
{
    private string _log = "";
    [Test]
    public void ListShowcaseSeeds()
    {
        int found = 0;
        for (long seed = 1; seed <= 5000 && found < 15; seed++)
        {
            WeatherPlan plan = WeatherPlanner.BuildCompact(seed, 0, DirectorTuning.Defaults, CompactSettings.Defaults,
                                                           StormEfTable.Defaults, Vector2.zero);
            PlannedCell a = plan.Cells.FirstOrDefault(c => c.Role == StormCellRole.Anchor && !c.Dropped);
            if (a == null || a.Ef < 4) continue;
            float touchdown = a.SpawnTime + a.Form;
            float dist = a.Position.magnitude;
            if (dist < 45f || dist > 95f) continue;
            found++;
            _log += System.Environment.NewLine + $"[Attract] seed {seed} regime {plan.Regime} anchor EF{a.Ef} spawn {a.SpawnTime:0.0}s touchdown {touchdown:0.0}s dist {dist:0}m cells {plan.Cells.Count}";
        }
        Assert.Pass($"{found} candidates: " + _log);
    }

    /// <summary>Smoke seeds for run-goals story 008: an EF4+ anchor whose peak window falls early in the 180 s run.</summary>
    [Test]
    public void ListGoalSmokeSeeds()
    {
        int found = 0;
        for (long seed = 1; seed <= 5000 && found < 12; seed++)
        {
            WeatherPlan plan = WeatherPlanner.BuildCompact(seed, 0, DirectorTuning.Defaults, CompactSettings.Defaults,
                                                           StormEfTable.Defaults, Vector2.zero);
            PlannedCell a = plan.Cells.FirstOrDefault(c => c.Role == StormCellRole.Anchor && !c.Dropped);
            if (a == null || a.Ef < 4 || a.FailedTouchdown) continue;
            float peakStart = a.SpawnTime + a.Form, peakEnd = peakStart + a.Mature;
            if (peakStart > 75f || peakEnd - peakStart < 15f) continue;
            int ef3Plus = plan.Cells.Count(c => !c.Dropped && c.Ef >= 3);
            found++;
            _log += System.Environment.NewLine + $"[Smoke] seed {seed} regime {plan.Regime} anchor EF{a.Ef} spawn {a.SpawnTime:0}s " +
                    $"peak {peakStart:0}-{peakEnd:0}s rope {peakEnd:0}-{peakEnd + a.Rope:0}s dist {a.Position.magnitude:0}m " +
                    $"cells {plan.Cells.Count(c => !c.Dropped)} ef3+ {ef3Plus}";
        }
        Assert.Pass($"{found} candidates: " + _log);
    }
}
