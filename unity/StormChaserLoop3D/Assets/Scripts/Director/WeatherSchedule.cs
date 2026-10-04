using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Compact-mode schedule settings (storm-director.md Rule 12, F2 compact). Placeholders pending playtest.</summary>
[Serializable]
public class CompactSettings
{
    [Tooltip("Run length T in seconds (fixed in compact mode).")]
    public float Duration = 180f;
    [Tooltip("Anchor reaches Mature at this fraction range of T.")]
    public Vector2 PeakFraction = new Vector2(0.40f, 0.65f);
    [Tooltip("Sequence spacing Δ in seconds.")]
    public Vector2 SequenceDelta = new Vector2(17f, 28f);
    [Tooltip("Satellite counts S (inclusive) per regime.")]
    public Vector2Int QuietSatellites = new Vector2Int(1, 3);
    public Vector2Int LoneGiantSatellites = new Vector2Int(1, 3);
    public Vector2Int SequenceSatellites = new Vector2Int(1, 3);
    public Vector2Int OutbreakSatellites = new Vector2Int(1, 2);
    public Vector2Int ChaosCells = new Vector2Int(3, 5);
    [Tooltip("Quiet / Lone Giant satellite spawn window as fractions of T.")]
    public Vector2 LooseSpawnFraction = new Vector2(0.05f, 0.75f);
    [Tooltip("Chaos spawn window: seconds from 5 s to this fraction of T.")]
    public float ChaosSpawnEndFraction = 0.8f;
    [Tooltip("Outbreak satellites spawn within ± this many seconds of the anchor.")]
    public float OutbreakSpread = 15f;
    [Tooltip("Outbreak cells stay within this distance of the anchor (m).")]
    public float OutbreakRadius = 120f;
    [Tooltip("Spawn distance from P0 (m).")]
    public Vector2 AnchorDistance = new Vector2(60f, 85f);
    public Vector2 SatelliteDistance = new Vector2(40f, 85f);
    [Tooltip("Spawn square half extent (m): the only invalid spot in compact mode.")]
    public float SpawnHalfExtent = 85f;
    [Tooltip("Sequence slots earlier than this are dropped (s).")]
    public float EarliestSpawn = 5f;
    [Tooltip("Cells holding a slot at once (CapTotal).")]
    public int Cap = 2;
    [Tooltip("Over-cap cells wait in steps of this many seconds, up to MaxWait, then drop.")]
    public float CapDelayStep = 10f;
    public float CapMaxWait = 30f;
    [Tooltip("Compact lifecycle: Form × FormScale (≥ MinForm), Mature and Rope × LifeScale.")]
    public float FormScale = 0.25f;
    public float MinForm = 4f;
    public float LifeScale = 0.5f;

    /// <summary>The GDD's compact defaults.</summary>
    public static CompactSettings Defaults => new CompactSettings();
}

/// <summary>Per-EF lifecycle seconds (storm-director.md F3); runtime fills it from the TornadoData assets.</summary>
[Serializable]
public class StormEfTable
{
    public float[] Form = { 20f, 25f, 30f, 35f, 40f, 45f };
    public float[] Mature = { 30f, 40f, 50f, 60f, 75f, 90f };
    public float[] Rope = { 10f, 12f, 15f, 20f, 25f, 30f };

    /// <summary>The F3 table.</summary>
    public static StormEfTable Defaults => new StormEfTable();
}

/// <summary>One storm cell in the Weather Plan: what, where, when, and how the cap resolved it.</summary>
public sealed class PlannedCell
{
    public int Id;
    public int Ef;
    public StormCellRole Role;
    /// <summary>Spawn position on the ground plane (x, z).</summary>
    public Vector2 Position;
    /// <summary>When the schedule wanted it (before any cap delay).</summary>
    public float DesiredTime;
    /// <summary>When it actually spawns (desired + cap delay). Meaningless when <see cref="Dropped"/>.</summary>
    public float SpawnTime;
    public float Form, Mature, Rope;
    /// <summary>Dropped by the cap after waiting the maximum: never spawns, raises nothing.</summary>
    public bool Dropped;
    /// <summary>Time an anchor's arrival forced this cell to rope out early, or −1.</summary>
    public float EarlyRopeTime = -1f;
    /// <summary>Intensity the early rope-out starts from (F3: no jump).</summary>
    public float EarlyRopeStartIntensity = 1f;

    /// <summary>True when evicted while still Forming: it never touches down (no Peak, no damage).</summary>
    public bool FailedTouchdown => EarlyRopeTime >= 0f && EarlyRopeTime - SpawnTime < Form;

    /// <summary>When the cell stops holding a cap slot: the start of its rope-out (scheduled or early).</summary>
    public float SlotFreeTime => EarlyRopeTime >= 0f ? EarlyRopeTime : SpawnTime + Form + Mature;

    /// <summary>When the cell is Done.</summary>
    public float EndTime => EarlyRopeTime >= 0f
        ? EarlyRopeTime + EarlyRopeStartIntensity * Rope
        : SpawnTime + Form + Mature + Rope;
}

/// <summary>Compact schedule (F2) and plan-time cap resolution.</summary>
public static partial class WeatherPlanner
{
    /// <summary>Sequence satellite EFs in spawn order: EF_k = max(0, EF_top − (S′ − k)), EF_top = min(EF_a − 1, 3).</summary>
    public static int[] SequenceEfs(int placed, int anchorEf)
    {
        int top = Mathf.Min(anchorEf - 1, 3);
        var efs = new int[Mathf.Max(0, placed)];
        for (int k = 1; k <= efs.Length; k++) efs[k - 1] = Mathf.Max(0, top - (efs.Length - k));
        return efs;
    }

    /// <summary>
    /// Builds a compact-mode Weather Plan (Rule 12): F1 draws, then the F2 compact schedule, spawn points around
    /// <paramref name="origin"/> (P0) and plan-time cap resolution. Never reads the player.
    /// </summary>
    public static WeatherPlan BuildCompact(long seed, int heat, DirectorTuning t, CompactSettings c, StormEfTable ef,
                                           Vector2 origin)
    {
        heat = Mathf.Clamp(heat, 0, RegimeDraw.MaxHeat);
        ulong s = unchecked((ulong)seed);
        var rng = new DirectorRng(s, PlanStream);
        var plan = new WeatherPlan { Seed = s, Heat = heat, Duration = c.Duration, Compact = true };
        Compose(plan, rng, heat, t, c.ChaosCells.x, c.ChaosCells.y);

        var place = new DirectorRng(s, PlacementStream);
        if (plan.Regime == Regime.Chaos) ScheduleChaos(plan, rng, place, c, ef, origin);
        else ScheduleAnchored(plan, rng, place, c, ef, origin);

        ResolveCaps(plan, c);
        for (int i = 0; i < plan.Cells.Count; i++) plan.Cells[i].Id = i;
        return plan;
    }

    private static void ScheduleAnchored(WeatherPlan plan, DirectorRng rng, DirectorRng place, CompactSettings c,
                                         StormEfTable ef, Vector2 origin)
    {
        float T = c.Duration;
        PlannedCell anchor = NewCell(plan.AnchorEf, StormCellRole.Anchor, c, ef);
        float peak = Mathf.Min(rng.Range(c.PeakFraction.x, c.PeakFraction.y) * T, T - 30f);
        anchor.DesiredTime = peak - anchor.Form;
        anchor.Position = PlacePoint(place, origin, c.AnchorDistance, c, null, 0f);
        plan.Cells.Add(anchor);

        bool coAnchor = plan.Heat >= RegimeDraw.MaxHeat && (plan.Regime == Regime.Sequence || plan.Regime == Regime.Outbreak);
        int a = plan.AnchorEf;
        switch (plan.Regime)
        {
            case Regime.Quiet:
            case Regime.LoneGiant:
            {
                Vector2Int range = plan.Regime == Regime.Quiet ? c.QuietSatellites : c.LoneGiantSatellites;
                int cap = plan.Regime == Regime.Quiet ? Mathf.Min(2, a) : Mathf.Min(2, a - 2);
                plan.SatellitesDrawn = rng.RangeInclusive(range.x, range.y);
                for (int i = 0; i < plan.SatellitesDrawn; i++)
                {
                    PlannedCell sat = NewCell(rng.RangeInclusive(0, Mathf.Max(0, cap)), StormCellRole.Satellite, c, ef);
                    sat.DesiredTime = rng.Range(c.LooseSpawnFraction.x * T, c.LooseSpawnFraction.y * T);
                    sat.Position = PlacePoint(place, origin, c.SatelliteDistance, c, null, 0f);
                    plan.Cells.Add(sat);
                }
                break;
            }
            case Regime.Sequence:
            {
                plan.SatellitesDrawn = rng.RangeInclusive(c.SequenceSatellites.x, c.SequenceSatellites.y);
                plan.SequenceDelta = rng.Range(c.SequenceDelta.x, c.SequenceDelta.y);
                int placed = coAnchor ? plan.SatellitesDrawn - 1 : plan.SatellitesDrawn;
                int[] efs = SequenceEfs(placed, a);
                for (int k = 1; k <= placed; k++)
                {
                    float time = anchor.DesiredTime - (plan.SatellitesDrawn - k + 1) * plan.SequenceDelta;
                    if (time < c.EarliestSpawn) { plan.SatellitesDroppedEarly++; continue; }
                    PlannedCell sat = NewCell(efs[k - 1], StormCellRole.Satellite, c, ef);
                    sat.DesiredTime = time;
                    sat.Position = PlacePoint(place, origin, c.SatelliteDistance, c, null, 0f);
                    plan.Cells.Add(sat);
                }
                if (coAnchor)
                {
                    PlannedCell co = NewCell(4, StormCellRole.CoAnchor, c, ef);
                    co.DesiredTime = anchor.DesiredTime - plan.SequenceDelta;
                    co.Position = PlacePoint(place, origin, c.AnchorDistance, c, null, 0f);
                    plan.Cells.Add(co);
                }
                break;
            }
            case Regime.Outbreak:
            {
                int cap = Mathf.Min(a - 1, 3);
                plan.SatellitesDrawn = rng.RangeInclusive(c.OutbreakSatellites.x, c.OutbreakSatellites.y);
                for (int i = 0; i < plan.SatellitesDrawn; i++)
                {
                    PlannedCell sat = NewCell(rng.RangeInclusive(0, Mathf.Max(0, cap)), StormCellRole.Satellite, c, ef);
                    sat.DesiredTime = anchor.DesiredTime + rng.Range(-c.OutbreakSpread, c.OutbreakSpread);
                    sat.Position = PlacePoint(place, origin, c.SatelliteDistance, c, anchor, c.OutbreakRadius);
                    plan.Cells.Add(sat);
                }
                if (coAnchor)
                {
                    PlannedCell co = NewCell(4, StormCellRole.CoAnchor, c, ef);
                    co.DesiredTime = anchor.DesiredTime + rng.Range(-c.OutbreakSpread, c.OutbreakSpread);
                    co.Position = PlacePoint(place, origin, c.AnchorDistance, c, anchor, c.OutbreakRadius);
                    plan.Cells.Add(co);
                }
                break;
            }
        }
    }

    private static void ScheduleChaos(WeatherPlan plan, DirectorRng rng, DirectorRng place, CompactSettings c,
                                      StormEfTable ef, Vector2 origin)
    {
        var cells = new List<PlannedCell>();
        foreach (int e in plan.ChaosEfs)
        {
            PlannedCell cell = NewCell(e, StormCellRole.Satellite, c, ef);
            cell.DesiredTime = rng.Range(c.EarliestSpawn, c.ChaosSpawnEndFraction * c.Duration);
            cell.Position = PlacePoint(place, origin, c.SatelliteDistance, c, null, 0f);
            cells.Add(cell);
        }
        // The one-EF5 rule runs in spawn order, so re-apply it once times are known.
        cells.Sort((x, y) => x.DesiredTime.CompareTo(y.DesiredTime));
        int[] efs = RegimeDraw.ApplyOneEf5Rule(cells.ConvertAll(x => x.Ef).ToArray());
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].Ef != efs[i]) cells[i] = Retime(NewCell(efs[i], StormCellRole.Satellite, c, ef), cells[i]);
            plan.Cells.Add(cells[i]);
        }
        plan.ChaosEfs.Clear();
        plan.ChaosEfs.AddRange(efs);
    }

    private static PlannedCell Retime(PlannedCell fresh, PlannedCell old)
    {
        fresh.DesiredTime = old.DesiredTime;
        fresh.Position = old.Position;
        return fresh;
    }

    private static PlannedCell NewCell(int efRating, StormCellRole role, CompactSettings c, StormEfTable ef)
    {
        int e = Mathf.Clamp(efRating, 0, 5);
        return new PlannedCell
        {
            Ef = e,
            Role = role,
            Form = Mathf.Max(c.MinForm, c.FormScale * ef.Form[e]),
            Mature = c.LifeScale * ef.Mature[e],
            Rope = c.LifeScale * ef.Rope[e],
        };
    }

    /// <summary>
    /// Spawn point at a distance band from P0, inside the spawn square and (when <paramref name="near"/> is
    /// set) within <paramref name="maxFromNear"/> of it. Rerolls up to 8 times, then falls back to a point on
    /// the anchor's bearing (Edge Cases: deterministic, never fails).
    /// </summary>
    private static Vector2 PlacePoint(DirectorRng rng, Vector2 origin, Vector2 band, CompactSettings c,
                                      PlannedCell near, float maxFromNear)
    {
        Vector2 p = origin;
        for (int attempt = 0; attempt <= 8; attempt++)
        {
            float bearing = rng.Range(0f, 2f * Mathf.PI);
            float dist = rng.Range(band.x, band.y);
            p = origin + new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing)) * dist;
            if (Valid(p, c) && (near == null || Vector2.Distance(p, near.Position) <= maxFromNear)) return p;
        }
        if (near != null)
        {
            Vector2 dir = (near.Position - origin).normalized;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
            p = origin + dir * Mathf.Clamp(Vector2.Distance(origin, p), band.x, band.y);
        }
        p.x = Mathf.Clamp(p.x, -c.SpawnHalfExtent, c.SpawnHalfExtent);
        p.y = Mathf.Clamp(p.y, -c.SpawnHalfExtent, c.SpawnHalfExtent);
        return p;
    }

    private static bool Valid(Vector2 p, CompactSettings c) =>
        Mathf.Abs(p.x) <= c.SpawnHalfExtent && Mathf.Abs(p.y) <= c.SpawnHalfExtent;

    /// <summary>
    /// Plan-time cap resolution (F2 CapDelay). A cell holds a slot from spawn until its rope-out starts. Over-cap
    /// satellites wait in 10 s steps and drop after 30 s; anchors and co-anchors never wait: the lowest-EF
    /// satellite holding a slot ropes out early instead, from its current intensity.
    /// </summary>
    private static void ResolveCaps(WeatherPlan plan, CompactSettings c)
    {
        var queue = new List<(float time, PlannedCell cell, float waited)>();
        foreach (PlannedCell cell in plan.Cells) queue.Add((cell.DesiredTime, cell, 0f));
        var holding = new List<PlannedCell>();

        while (queue.Count > 0)
        {
            int next = 0;
            for (int i = 1; i < queue.Count; i++)
                if (Earlier(queue[i], queue[next])) next = i;
            (float time, PlannedCell cell, float waited) = queue[next];
            queue.RemoveAt(next);
            holding.RemoveAll(h => h.SlotFreeTime <= time + 1e-4f);

            bool anchorClass = cell.Role != StormCellRole.Satellite;
            if (holding.Count >= c.Cap && anchorClass)
            {
                PlannedCell victim = null;
                foreach (PlannedCell h in holding)
                    if (h.Role == StormCellRole.Satellite && (victim == null || h.Ef < victim.Ef)) victim = h;
                if (victim != null)
                {
                    float age = time - victim.SpawnTime;
                    victim.EarlyRopeTime = time;
                    victim.EarlyRopeStartIntensity = age < victim.Form ? age / victim.Form : 1f;
                    holding.Remove(victim);
                }
            }
            if (holding.Count < c.Cap || anchorClass)
            {
                cell.SpawnTime = time;
                holding.Add(cell);
            }
            else if (waited + c.CapDelayStep <= c.CapMaxWait + 1e-4f)
            {
                queue.Add((time + c.CapDelayStep, cell, waited + c.CapDelayStep));
            }
            else
            {
                cell.Dropped = true;
            }
        }
    }

    // Earlier time first; at the same time anchors before satellites, then the lowest EF (it drops first).
    private static bool Earlier((float time, PlannedCell cell, float waited) a, (float time, PlannedCell cell, float waited) b)
    {
        if (Mathf.Abs(a.time - b.time) > 1e-4f) return a.time < b.time;
        bool aAnchor = a.cell.Role != StormCellRole.Satellite, bAnchor = b.cell.Role != StormCellRole.Satellite;
        if (aAnchor != bAnchor) return aAnchor;
        return a.cell.Ef < b.cell.Ef;
    }
}
