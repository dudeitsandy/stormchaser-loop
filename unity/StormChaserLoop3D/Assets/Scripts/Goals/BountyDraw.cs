using System.Collections.Generic;

/// <summary>
/// KTVR bounty draw (event-system.md Run Goals, Formulas: Bounty draw): the eligible pool is the v1 bounties the
/// run's Weather Plan can satisfy, judged on non-dropped cells; <see cref="BountiesPerRun"/> are drawn without
/// replacement by a Fisher–Yates shuffle on <see cref="DirectorRng"/> stream 3. Pure: same plan, same bounties,
/// and the draw never touches the plan or any other stream.
/// </summary>
public static class BountyDraw
{
    /// <summary>Director RNG stream reserved for bounties (1 plan, 2 placement, 100+id tracks, 1000+id forecast).</summary>
    public const ulong Stream = 3;
    public const int BountiesPerRun = 3;

    /// <summary>True when <paramref name="plan"/> can satisfy <paramref name="bounty"/>.</summary>
    public static bool Eligible(GoalDef bounty, WeatherPlan plan)
    {
        if (bounty.Need == BountyNeed.Always) return true;
        if (plan == null) return false;
        foreach (PlannedCell c in plan.Cells)
        {
            if (c.Dropped) continue;
            switch (bounty.Need)
            {
                case BountyNeed.AnyCell: return true;
                case BountyNeed.Ef3Plus: if (c.Ef >= 3) return true; break;
                case BountyNeed.Ef5: if (c.Ef >= 5) return true; break;
                case BountyNeed.Anchor: if (c.Role == StormCellRole.Anchor) return true; break;
            }
        }
        return false;
    }

    /// <summary>The run's bounties, in draw order. Empty without a plan (legacy spawner).</summary>
    public static List<GoalDef> Draw(WeatherPlan plan, int count = BountiesPerRun)
    {
        var drawn = new List<GoalDef>(count);
        if (plan == null) return drawn;
        var pool = new List<GoalDef>(GoalCatalogue.Bounties.Count);
        foreach (GoalDef b in GoalCatalogue.Bounties)
            if (Eligible(b, plan)) pool.Add(b);

        var rng = new DirectorRng(plan.Seed, Stream);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.RangeInclusive(0, i);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        for (int i = 0; i < count && i < pool.Count; i++) drawn.Add(pool[i]);
        return drawn;
    }
}
