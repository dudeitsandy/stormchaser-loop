using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>event-system.md Run Goals Rules 1–3 and Formulas: Bounty draw (run-goals-v1 story 002).</summary>
public class GoalCatalogueTests
{
    private static WeatherPlan Plan(long seed, int heat) => WeatherPlanner.BuildCompact(
        seed, heat, DirectorTuning.Defaults, CompactSettings.Defaults, StormEfTable.Defaults, Vector2.zero);

    [Test]
    public void Catalogue_HasTenCareerGoalsAndSevenBounties_WithStableUniqueIds()
    {
        Assert.AreEqual(10, GoalCatalogue.Career.Count);
        Assert.AreEqual(7, GoalCatalogue.Bounties.Count);
        foreach (GoalDef g in GoalCatalogue.Career) StringAssert.StartsWith("career.compact.heartland.", g.Id);
        foreach (GoalDef g in GoalCatalogue.Bounties) StringAssert.StartsWith("bounty.compact.", g.Id);
        var ids = GoalCatalogue.Career.Concat(GoalCatalogue.Bounties).Select(g => g.Id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
        Assert.AreSame(GoalCatalogue.Career[7], GoalCatalogue.Find("career.compact.heartland.big_air"));
        Assert.IsNull(GoalCatalogue.Find("career.compact.heartland.unknown"));
    }

    [Test]
    public void ScoreGoals_PayNoBonus_StyleAndStormGoals_PayTheCareerBonus()
    {
        foreach (GoalDef g in GoalCatalogue.Career)
            Assert.AreEqual(g.Type == GoalType.Score ? 0 : 150, g.Bonus, g.Id);
    }

    [Test]
    public void ShortNames_FitSixteen_BountyText_FitsTwentyEight_WithNoEfOrDigit()
    {
        foreach (GoalDef g in GoalCatalogue.Career.Concat(GoalCatalogue.Bounties))
            Assert.LessOrEqual(g.ShortName.Length, 16, g.Id);
        foreach (GoalDef b in GoalCatalogue.Bounties)
        {
            Assert.LessOrEqual(b.Text.Length, 28, b.Id);
            Assert.IsFalse(Regex.IsMatch(b.Text, @"\d"), b.Text);
            StringAssert.DoesNotContain("EF", b.Text);
        }
    }

    [Test]
    public void SameSeed_SameBountiesInTheSameOrder()
    {
        for (long seed = 1; seed <= 50; seed++)
        {
            var a = BountyDraw.Draw(Plan(seed, 0)).Select(b => b.Id).ToList();
            var b2 = BountyDraw.Draw(Plan(seed, 0)).Select(b => b.Id).ToList();
            CollectionAssert.AreEqual(a, b2, $"seed {seed}");
        }
    }

    [TestCase(0)]
    [TestCase(5)]
    public void Seeds1To1000_AlwaysDrawThree_NeverOneThePlanCannotSatisfy(int heat)
    {
        var seen = new HashSet<string>();
        for (long seed = 1; seed <= 1000; seed++)
        {
            WeatherPlan plan = Plan(seed, heat);
            List<GoalDef> drawn = BountyDraw.Draw(plan);
            Assert.AreEqual(3, drawn.Count, $"seed {seed}");
            Assert.AreEqual(3, drawn.Select(d => d.Id).Distinct().Count(), $"seed {seed}: drawn without replacement");
            bool hasEf5 = plan.Cells.Any(c => !c.Dropped && c.Ef >= 5);
            foreach (GoalDef b in drawn)
            {
                Assert.IsTrue(BountyDraw.Eligible(b, plan), $"seed {seed}: {b.Id}");
                if (b.Key == "point_blank_ef5") Assert.IsTrue(hasEf5, $"seed {seed}: point-blank EF5 without an EF5");
                seen.Add(b.Id);
            }
        }
        foreach (string always in new[] { "bounty.compact.rope_out", "bounty.compact.double_near_miss", "bounty.compact.drift_by" })
            Assert.IsTrue(seen.Contains(always), $"{always} is drawn at least once in 1,000 seeds");
    }

    [Test]
    public void Draw_UsesItsOwnStream_ThePlanIsUnchanged()
    {
        WeatherPlan plan = Plan(554, 2);
        string before = plan.Serialize();
        BountyDraw.Draw(plan);
        Assert.AreEqual(before, plan.Serialize(), "the draw does not touch the plan");
        Assert.AreEqual(before, Plan(554, 2).Serialize(), "a plan built without a draw is identical");
    }

    [Test]
    public void NoPlan_DrawsNothing()
    {
        Assert.AreEqual(0, BountyDraw.Draw(null).Count);
    }
}
