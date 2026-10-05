using NUnit.Framework;

/// <summary>Run screens text (run-screens stories 004 and 005; design/ux/run-screens.md).</summary>
public class RunScreensTextTests
{
    [Test]
    public void CareerStrip_FirstLaunch_Progress_AndOwnedPaint()
    {
        Assert.AreEqual("CAREER 0/10  ·  5 GOALS UNLOCK KTVR PAINT", RunScreens.CareerStripText(0, 10, false, false));
        Assert.AreEqual("CAREER 4/10  ·  1 MORE: KTVR PAINT", RunScreens.CareerStripText(4, 10, false, false));
        Assert.AreEqual("CAREER 6/10  ·  PAINT: KTVR  (L / X)", RunScreens.CareerStripText(6, 10, true, true));
        Assert.AreEqual("CAREER 6/10  ·  PAINT: STOCK  (L / X)", RunScreens.CareerStripText(6, 10, true, false));
    }

    [Test]
    public void CareerPage_ScoreGoalsShowTheirThreshold()
    {
        Assert.AreEqual("PRO SCORE 1,500", RunScreens.CareerGoalText(GoalCatalogue.Find("career.compact.heartland.score_pro")));
        Assert.AreEqual("BIG AIR", RunScreens.CareerGoalText(GoalCatalogue.Find("career.compact.heartland.big_air")));
    }

    [Test]
    public void ResultsGoalLine_ShowsShortNameAndBonus_ScoreGoalsNoBonus()
    {
        GoalDef air = GoalCatalogue.Find("career.compact.heartland.big_air");
        Assert.AreEqual("DONE  BIG AIR  +150", RunScreens.ResultsGoalText(air, new GoalCompletion(air.Id, GoalKind.Career, 150, true)));
        GoalDef pro = GoalCatalogue.Find("career.compact.heartland.score_pro");
        Assert.AreEqual("DONE  PRO", RunScreens.ResultsGoalText(pro, new GoalCompletion(pro.Id, GoalKind.Career, 0, true)));
    }
}
