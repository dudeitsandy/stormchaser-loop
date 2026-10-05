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

    /// <summary>
    /// Pacing probe (Andy 2026-10-05: "real dead moments with nothing to do"): over 1000 seeds, when storms are alive
    /// (spawn → end) inside the 180 s run, the quiet time with none alive, the longest quiet gap, the opening wait and the
    /// empty tail after the last storm, per regime. Observations only.
    /// </summary>
    [Test]
    public void PacingQuietTime()
    {
        const float run = 180f;
        var rows = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<float[]>>();
        for (long seed = 1; seed <= 1000; seed++)
        {
            WeatherPlan plan = WeatherPlanner.BuildCompact(seed, 0, DirectorTuning.Defaults, CompactSettings.Defaults,
                                                           StormEfTable.Defaults, Vector2.zero);
            var spans = plan.Cells.Where(c => !c.Dropped).Select(c => new Vector2(c.SpawnTime, Mathf.Min(run, c.EndTime)))
                            .Where(s => s.x < run).OrderBy(s => s.x).ToList();
            float covered = 0f, cursor = 0f, longest = 0f;
            foreach (Vector2 s in spans)
            {
                if (s.x > cursor) longest = Mathf.Max(longest, s.x - cursor);
                if (s.y > cursor) { covered += s.y - Mathf.Max(cursor, s.x); cursor = s.y; }
            }
            float tail = run - cursor;
            longest = Mathf.Max(longest, tail);
            PlannedCell a = plan.Cells.FirstOrDefault(c => c.Role == StormCellRole.Anchor && !c.Dropped);
            float touchdown = a != null ? a.SpawnTime + a.Form : -1f;
            float opening = spans.Count > 0 ? spans[0].x : run;
            string key = plan.Regime.ToString();
            if (!rows.ContainsKey(key)) rows[key] = new System.Collections.Generic.List<float[]>();
            rows[key].Add(new[] { run - covered, longest, opening, tail, touchdown, spans.Count });
            if (!rows.ContainsKey("ALL")) rows["ALL"] = new System.Collections.Generic.List<float[]>();
            rows["ALL"].Add(rows[key][rows[key].Count - 1]);
        }
        string P(System.Collections.Generic.List<float[]> l, int i)
        {
            var v = l.Select(r => r[i]).OrderBy(x => x).ToList();
            return $"{v[(int)(v.Count * 0.1f)]:0}/{v[v.Count / 2]:0}/{v[(int)(v.Count * 0.9f)]:0}";
        }
        string log = "p10/p50/p90 seconds: quiet total | longest gap | opening wait | empty tail | anchor touchdown | storms";
        foreach (var kv in rows.OrderBy(k => k.Key))
            log += System.Environment.NewLine + $"[Pacing] {kv.Key,-10} n={kv.Value.Count,4}  quiet {P(kv.Value, 0)}  longest {P(kv.Value, 1)}  " +
                   $"opening {P(kv.Value, 2)}  tail {P(kv.Value, 3)}  touchdown {P(kv.Value, 4)}  storms {P(kv.Value, 5)}";
        Assert.Pass(log);
    }

    /// <summary>One seed's compact plan, cell by cell (spawn, touchdown, rope, end): lines perf spikes up with storm events.</summary>
    [TestCase(554)]
    public void DumpPlan(long seed)
    {
        WeatherPlan plan = WeatherPlanner.BuildCompact(seed, 0, DirectorTuning.Defaults, CompactSettings.Defaults,
                                                       StormEfTable.Defaults, Vector2.zero);
        string log = $"seed {seed} regime {plan.Regime}";
        foreach (PlannedCell c in plan.Cells)
            log += System.Environment.NewLine + $"[Plan] #{c.Id} {c.Role}{(c.Pacing ? " (pacing)" : "")} EF{c.Ef} " +
                   (c.Dropped ? "dropped" : $"spawn {c.SpawnTime:0.0} touchdown {c.SpawnTime + c.Form:0.0} " +
                                            $"rope {c.SlotFreeTime:0.0} end {c.EndTime:0.0}");
        Assert.Pass(log);
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
