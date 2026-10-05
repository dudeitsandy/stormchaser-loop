using System;
using System.Collections.Generic;

/// <summary>
/// Run Goals rewards (event-system.md Run Goals Rule 6, Formulas: Reward): <c>livery.ktvr</c> is granted when the count
/// of career goals with a recorded first completion reaches <see cref="RewardThreshold"/>, checked at every run-complete
/// checkpoint. Pure.
/// </summary>
public static class RunRewards
{
    public const string KtvrLivery = "livery.ktvr";
    /// <summary>Career goals needed for the KTVR paint job (5 of 10).</summary>
    public const int RewardThreshold = 5;

    /// <summary>
    /// Unlocks this run earns. <paramref name="recordedCareer"/> is the career count already in the record;
    /// <paramref name="completions"/> are this run's; <paramref name="alreadyCompleted"/> and <paramref name="owned"/> read
    /// the record before this run.
    /// </summary>
    public static List<string> Earned(int recordedCareer, IReadOnlyList<GoalCompletion> completions,
                                      Func<string, bool> alreadyCompleted, Func<string, bool> owned)
    {
        int career = recordedCareer;
        var counted = new HashSet<string>();
        if (completions != null)
            foreach (GoalCompletion c in completions)
                if (c.Kind == GoalKind.Career && !alreadyCompleted(c.Id) && counted.Add(c.Id)) career++;
        var earned = new List<string>(1);
        if (career >= RewardThreshold && !owned(KtvrLivery)) earned.Add(KtvrLivery);
        return earned;
    }
}
