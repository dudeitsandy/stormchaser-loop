using System.Collections.Generic;
using NUnit.Framework;

/// <summary>event-system.md Run Goals Rule 6 / Formulas: Reward (run-goals-v1 story 006).</summary>
public class RunRewardsTests
{
    private const string C = "career.compact.heartland.";

    private static List<GoalCompletion> Done(params string[] ids)
    {
        var list = new List<GoalCompletion>();
        foreach (string id in ids)
            list.Add(new GoalCompletion(id, id.StartsWith("career") ? GoalKind.Career : GoalKind.Bounty, 150, true));
        return list;
    }

    [Test]
    public void FourRecorded_PlusOneNew_EarnsTheKtvrLivery()
    {
        var earned = RunRewards.Earned(4, Done(C + "big_air"), _ => false, _ => false);
        CollectionAssert.AreEqual(new[] { "livery.ktvr" }, earned);
    }

    [Test]
    public void FourRecorded_PlusARepeat_EarnsNothing()
    {
        var earned = RunRewards.Earned(4, Done(C + "big_air"), id => id == C + "big_air", _ => false);
        CollectionAssert.IsEmpty(earned, "a goal already in the record doesn't count twice");
    }

    [Test]
    public void BountiesDoNotCount_TowardTheCareerReward()
    {
        CollectionAssert.IsEmpty(RunRewards.Earned(4, Done("bounty.compact.rope_out"), _ => false, _ => false));
    }

    [Test]
    public void AlreadyOwned_IsNeverGrantedTwice()
    {
        CollectionAssert.IsEmpty(RunRewards.Earned(9, Done(C + "front_page"), _ => false, id => id == "livery.ktvr"));
    }

    [Test]
    public void TwoNewInOneRun_FromThree_ReachesFive()
    {
        var earned = RunRewards.Earned(3, Done(C + "big_air", C + "near_misses", C + "big_air"), _ => false, _ => false);
        CollectionAssert.AreEqual(new[] { "livery.ktvr" }, earned);
        CollectionAssert.IsEmpty(RunRewards.Earned(3, Done(C + "big_air", C + "big_air"), _ => false, _ => false),
                                 "the same goal twice in a list counts once");
    }
}
