using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>Weather regimes (storm-director.md Rule 2), in table order.</summary>
public enum Regime { Quiet, LoneGiant, Sequence, Outbreak, Chaos }

/// <summary>
/// F1 tuning values (storm-director.md Formulas, Tuning Knobs). Placeholders pending playtest; edit here or
/// in a data asset, never in the draw code.
/// </summary>
[Serializable]
public class DirectorTuning
{
    [Tooltip("Base regime weights w0_r: Quiet, Lone Giant, Sequence, Outbreak, Chaos.")]
    public float[] BaseWeights = { 0.20f, 0.25f, 0.30f, 0.15f, 0.10f };
    [Tooltip("Heat slopes s_r: w_r(H) = w0_r · exp(s_r · H).")]
    public float[] HeatSlopes = { -0.55f, 0.05f, 0.10f, 0.30f, 0.30f };
    [Tooltip("Base EF5 share p5_0 per regime (Chaos: per cell).")]
    public float[] BaseEf5Share = { 0f, 0.10f, 0.03f, 0.03f, 0.01f };
    [Tooltip("EF5 share grows by this × Heat: p5 = p5_0 · (1 + k · H).")]
    public float Ef5HeatGrowth = 0.4f;

    /// <summary>Anchor EF0–EF5 tables for Quiet, Lone Giant, Sequence, Outbreak (Chaos has no anchor).</summary>
    public float[][] AnchorTables =
    {
        new[] { 0.40f, 0.40f, 0.20f, 0f, 0f, 0f },
        new[] { 0f, 0f, 0f, 0.55f, 0.35f, 0.10f },
        new[] { 0f, 0f, 0.30f, 0.40f, 0.27f, 0.03f },
        new[] { 0f, 0f, 0.30f, 0.45f, 0.22f, 0.03f },
    };

    [Tooltip("Chaos per-cell EF0–EF5 table (Andy, 2026-10-02).")]
    public float[] ChaosTable = { 0.20f, 0.25f, 0.27f, 0.21f, 0.06f, 0.01f };

    [Tooltip("Epic Chaos cell count range (inclusive). Compact uses 3..5 (story 002).")]
    public int ChaosMinCells = 3, ChaosMaxCells = 8;

    /// <summary>The GDD's defaults.</summary>
    public static DirectorTuning Defaults => new DirectorTuning();
}

/// <summary>storm-director.md F1: regime draw and anchor / Chaos EF tables. Pure functions.</summary>
public static class RegimeDraw
{
    public const int MaxHeat = 5;

    /// <summary>P_r = w_r / Σ w with w_r = w0_r · exp(s_r · H).</summary>
    public static float[] Probabilities(int heat, DirectorTuning t)
    {
        var w = new float[5];
        float sum = 0f;
        for (int r = 0; r < 5; r++)
        {
            w[r] = t.BaseWeights[r] * Mathf.Exp(t.HeatSlopes[r] * heat);
            sum += w[r];
        }
        for (int r = 0; r < 5; r++) w[r] /= sum;
        return w;
    }

    /// <summary>EF5 share for a regime at Heat H: p5_0 · (1 + 0.4 H).</summary>
    public static float Ef5Share(Regime regime, int heat, DirectorTuning t) =>
        t.BaseEf5Share[(int)regime] * (1f + t.Ef5HeatGrowth * heat);

    /// <summary>
    /// Anchor EF0–EF5 table at Heat H: the EF5 share grows with Heat, taken from the table's lowest tier; at
    /// Heat 5, Sequence and Outbreak move all EF2/EF3 mass onto EF4 (anchor floor, Rule 3).
    /// </summary>
    public static float[] AnchorTable(Regime regime, int heat, DirectorTuning t)
    {
        if (regime == Regime.Chaos) throw new ArgumentException("Chaos has no anchor table", nameof(regime));
        float[] table = (float[])t.AnchorTables[(int)regime].Clone();
        float added = Ef5Share(regime, heat, t) - table[5];
        if (added > 0f)
        {
            int lowest = Array.FindIndex(table, p => p > 0f);
            table[lowest] -= added;
            table[5] += added;
        }
        if (heat >= MaxHeat && (regime == Regime.Sequence || regime == Regime.Outbreak))
        {
            for (int ef = 0; ef < 4; ef++)
            {
                table[4] += table[ef];
                table[ef] = 0f;
            }
        }
        return table;
    }

    /// <summary>Chaos per-cell table at Heat H: EF5 share × (1 + 0.4 H), added mass taken from EF0.</summary>
    public static float[] ChaosTable(int heat, DirectorTuning t)
    {
        float[] table = (float[])t.ChaosTable.Clone();
        float added = Ef5Share(Regime.Chaos, heat, t) - table[5];
        table[0] -= added;
        table[5] += added;
        return table;
    }

    /// <summary>"At most one EF5 per run": in spawn order, any EF5 after the first becomes EF4 (AC-7).</summary>
    public static int[] ApplyOneEf5Rule(int[] cellsInSpawnOrder)
    {
        var result = (int[])cellsInSpawnOrder.Clone();
        bool seen = false;
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] != 5) continue;
            if (seen) result[i] = 4;
            seen = true;
        }
        return result;
    }

    /// <summary>
    /// Exact P(any EF5 in a run) before cap resolution (AC-5): Σ P_r · p5(r, H) for anchored regimes, plus
    /// P_Chaos · mean over N of 1 − (1 − p5_C)^N.
    /// </summary>
    public static float ExactAnyEf5Rate(int heat, DirectorTuning t)
    {
        float[] p = Probabilities(heat, t);
        float rate = 0f;
        for (int r = 0; r < 4; r++) rate += p[r] * AnchorTable((Regime)r, heat, t)[5];
        float pc = ChaosTable(heat, t)[5];
        float chaos = 0f;
        int n = t.ChaosMaxCells - t.ChaosMinCells + 1;
        for (int cells = t.ChaosMinCells; cells <= t.ChaosMaxCells; cells++) chaos += 1f - Mathf.Pow(1f - pc, cells);
        return rate + p[(int)Regime.Chaos] * chaos / n;
    }
}

/// <summary>
/// The Weather Plan so far (story 001: regime, anchor EF, Chaos cells). Story 002 adds the cell schedule.
/// Plain data; <see cref="Serialize"/> is byte-stable for replay checks (AC-1).
/// </summary>
public sealed class WeatherPlan
{
    public ulong Seed;
    public int Heat;
    public Regime Regime;
    /// <summary>Anchor EF 0–5, or −1 for Chaos (no anchor).</summary>
    public int AnchorEf = -1;
    /// <summary>Chaos cells' EFs in spawn order (empty for other regimes).</summary>
    public readonly List<int> ChaosEfs = new List<int>();
    /// <summary>True for a compact-mode plan (Rule 12).</summary>
    public bool Compact;
    /// <summary>Run length T in seconds (0 until a schedule is built).</summary>
    public float Duration;
    /// <summary>Scheduled cells (story 002); empty for an F1-only plan.</summary>
    public readonly List<PlannedCell> Cells = new List<PlannedCell>();
    /// <summary>Satellite count S as drawn (before the Heat 5 co-anchor takes a slot or any drop).</summary>
    public int SatellitesDrawn;
    /// <summary>Sequence slots dropped for spawning before 5 s.</summary>
    public int SatellitesDroppedEarly;
    /// <summary>Opener / closer satellites the compact pacing fill added (0–2).</summary>
    public int PacingCells;
    /// <summary>Sequence spacing Δ in seconds (Sequence plans only).</summary>
    public float SequenceDelta;

    /// <summary>True when any drawn cell is EF5 (before cap resolution).</summary>
    public bool HasEf5 => AnchorEf == 5 || ChaosEfs.Contains(5);

    /// <summary>Deterministic text form: fixed field order, invariant culture.</summary>
    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append("seed=").Append(Seed.ToString(CultureInfo.InvariantCulture))
          .Append(";heat=").Append(Heat.ToString(CultureInfo.InvariantCulture))
          .Append(";regime=").Append(Regime)
          .Append(";anchor=").Append(AnchorEf.ToString(CultureInfo.InvariantCulture))
          .Append(";chaos=");
        for (int i = 0; i < ChaosEfs.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(ChaosEfs[i].ToString(CultureInfo.InvariantCulture));
        }
        sb.Append(";T=").Append(F(Duration)).Append(Compact ? ";compact" : "")
          .Append(";S=").Append(SatellitesDrawn.ToString(CultureInfo.InvariantCulture))
          .Append('-').Append(SatellitesDroppedEarly.ToString(CultureInfo.InvariantCulture))
          .Append(";delta=").Append(F(SequenceDelta)).Append(";cells=");
        foreach (PlannedCell c in Cells)
        {
            sb.Append('[').Append(c.Id.ToString(CultureInfo.InvariantCulture)).Append(',').Append(c.Role)
              .Append(",EF").Append(c.Ef.ToString(CultureInfo.InvariantCulture))
              .Append(',').Append(F(c.Position.x)).Append(',').Append(F(c.Position.y))
              .Append(",t").Append(F(c.DesiredTime)).Append('>').Append(F(c.SpawnTime))
              .Append(c.Dropped ? ",dropped" : "").Append(c.Pacing ? ",pacing" : "")
              .Append(",life").Append(F(c.Form)).Append('/').Append(F(c.Mature)).Append('/').Append(F(c.Rope))
              .Append(c.EarlyRopeTime >= 0f ? ",rope@" + F(c.EarlyRopeTime) + "x" + F(c.EarlyRopeStartIntensity) : "")
              .Append(']');
        }
        return sb.ToString();
    }

    private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
}

/// <summary>Builds a <see cref="WeatherPlan"/> from a seed and Heat (story 001 scope: F1 draws).</summary>
public static partial class WeatherPlanner
{
    /// <summary>RNG sub-stream for plan composition (tracks and forecast use their own streams).</summary>
    public const ulong PlanStream = 1;
    /// <summary>RNG sub-stream for spawn points, so rerolls never shift the plan's other draws.</summary>
    public const ulong PlacementStream = 2;

    /// <summary>Builds the plan for run <paramref name="seed"/> at Cataclysm Heat <paramref name="heat"/> (0–5).</summary>
    public static WeatherPlan Build(long seed, int heat, DirectorTuning t)
    {
        heat = Mathf.Clamp(heat, 0, RegimeDraw.MaxHeat);
        ulong s = unchecked((ulong)seed);
        var rng = new DirectorRng(s, PlanStream);
        var plan = new WeatherPlan { Seed = s, Heat = heat };
        Compose(plan, rng, heat, t, t.ChaosMinCells, t.ChaosMaxCells);
        return plan;
    }

    /// <summary>F1 draws into <paramref name="plan"/>: regime, then the anchor EF or the Chaos cells.</summary>
    private static void Compose(WeatherPlan plan, DirectorRng rng, int heat, DirectorTuning t, int chaosMin, int chaosMax)
    {
        plan.Regime = (Regime)rng.Pick(RegimeDraw.Probabilities(heat, t));
        if (plan.Regime == Regime.Chaos)
        {
            int n = rng.RangeInclusive(chaosMin, chaosMax);
            float[] table = RegimeDraw.ChaosTable(heat, t);
            var cells = new int[n];
            for (int i = 0; i < n; i++) cells[i] = rng.Pick(table);
            plan.ChaosEfs.AddRange(RegimeDraw.ApplyOneEf5Rule(cells));
        }
        else
        {
            plan.AnchorEf = rng.Pick(RegimeDraw.AnchorTable(plan.Regime, heat, t));
        }
    }
}
