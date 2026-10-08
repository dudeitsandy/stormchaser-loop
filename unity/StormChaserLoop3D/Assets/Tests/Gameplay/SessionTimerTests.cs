using NUnit.Framework;

/// <summary>Final countdown ticks (Andy 2026-10-08): one per HUD second from the countdown start down to 1.</summary>
public class SessionTimerTests
{
    private const int Countdown = 5;

    [TestCase(5.01f, 4.99f, 5)]
    [TestCase(1.01f, 0.99f, 1)]
    [TestCase(4.50f, 4.40f, -1)]
    [TestCase(6.01f, 5.99f, -1)]
    [TestCase(5.00f, 4.99f, -1)]
    public void CountdownTickCrossed_FiresOnEnteringAShownSecond(float before, float after, int expected)
    {
        Assert.AreEqual(expected, SessionTimer.CountdownTickCrossed(before, after, Countdown));
    }

    [Test]
    public void CountdownTickCrossed_LongFrameSkippingSeconds_ReportsTheLatest()
    {
        Assert.AreEqual(3, SessionTimer.CountdownTickCrossed(5.2f, 2.9f, Countdown));
    }

    [Test]
    public void CountdownTickCrossed_StepThroughARun_FiresFiveToOneOnce()
    {
        var ticks = new System.Collections.Generic.List<int>();
        float t = 7f;
        while (t > 0.02f)
        {
            float next = t - 0.0167f;
            int tick = SessionTimer.CountdownTickCrossed(t, next, Countdown);
            if (tick > 0) ticks.Add(tick);
            t = next;
        }
        CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, ticks);
    }
}
