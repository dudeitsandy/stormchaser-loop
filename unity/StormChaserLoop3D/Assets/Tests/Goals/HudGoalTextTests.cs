using NUnit.Framework;

/// <summary>Goals HUD text (run-goals-v1 story 007): every state has a text marker, never colour alone.</summary>
public class HudGoalTextTests
{
    private static GoalDef Bounty(string key) => GoalCatalogue.Find("bounty.compact." + key);
    private static GoalDef Career(string key) => GoalCatalogue.Find("career.compact.heartland." + key);

    [Test]
    public void BountyRow_OpenDoneMissed_EachCarryATextMarker()
    {
        GoalDef rope = Bounty("rope_out");
        Assert.AreEqual("·  GET THE ROPE-OUT", HudController.BountyText(rope, HudController.BountyState.Open, 0));
        Assert.AreEqual("DONE  GET THE ROPE-OUT  +200", HudController.BountyText(rope, HudController.BountyState.Done, 200));
        Assert.AreEqual("MISS  <s>GET THE ROPE-OUT</s>", HudController.BountyText(rope, HudController.BountyState.Missed, 0));
    }

    [Test]
    public void CareerPopup_ShowsShortNameBonusAndNewOnlyOnFirstEver()
    {
        GoalDef air = Career("big_air");
        Assert.AreEqual("GOAL! BIG AIR  +150  NEW",
            HudController.CareerPopupText(air, new GoalCompletion(air.Id, GoalKind.Career, 150, firstEver: true)));
        Assert.AreEqual("GOAL! BIG AIR  +150",
            HudController.CareerPopupText(air, new GoalCompletion(air.Id, GoalKind.Career, 150, firstEver: false)));
        GoalDef pro = Career("score_pro");
        Assert.AreEqual("GOAL! PRO", HudController.CareerPopupText(pro, new GoalCompletion(pro.Id, GoalKind.Career, 0, false)),
                        "score goals pay no bonus, so none is shown");
    }
}
