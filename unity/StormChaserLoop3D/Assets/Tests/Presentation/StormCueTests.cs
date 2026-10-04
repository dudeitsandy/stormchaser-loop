using NUnit.Framework;

public class StormCueTests
{
    [TestCase(0f, 0f)]
    [TestCase(10f, 0.5f)]
    [TestCase(20f, 1f)]
    [TestCase(40f, 1f)]
    [TestCase(-10f, 0f)]
    public void Exposure_IsBoundedAndUsesTwentyMeterPerSecondScale(float magnitude, float expected)
    {
        Assert.That(StormCueLevels.Exposure(magnitude), Is.EqualTo(expected));
    }
    [Test]
    public void OpposedCellMagnitudes_DoNotCancel()
    {
        Assert.That(StormCueLevels.Exposure(2.72f + 20.24f), Is.EqualTo(1f));
        Assert.That(StormCueLevels.Brightness(1f), Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(StormCueLevels.Brightness(0f), Is.EqualTo(1f));
        Assert.That(StormCueLevels.GustLength(1f), Is.EqualTo(2f));
    }
    [TestCase(0, false)]
    [TestCase(2, false)]
    [TestCase(3, true)]
    [TestCase(5, true)]
    public void FormingCue_OnlyEfThreeAndHigherAndOnlyOnce(int ef, bool expected)
    {
        var tracker = new StormCellCueTracker();
        var cell = new StormCellInfo(1, ef, StormCellRole.Satellite, default);
        Assert.That(tracker.Forming(cell), Is.EqualTo(expected));
        Assert.That(tracker.Forming(cell), Is.False);
    }
    [TestCase(StormCellRole.Anchor, true)]
    [TestCase(StormCellRole.CoAnchor, false)]
    [TestCase(StormCellRole.Satellite, false)]
    public void SharpPeakAlert_OnlyForRealAnchorTouchdown(StormCellRole role, bool expected)
    {
        var tracker = new StormCellCueTracker();
        var cell = new StormCellInfo(2, 5, role, default);
        tracker.Forming(cell);
        Assert.That(tracker.Peak(cell), Is.EqualTo(expected));
        Assert.That(tracker.Peak(cell), Is.False);
        Assert.That(tracker.RopeOut(cell), Is.False, "A real peak must not be treated as failed touchdown.");
    }
    [Test]
    public void FailedTouchdown_AndDirectEndCannotFabricatePeakOrPlayQueuedRadio()
    {
        var tracker = new StormCellCueTracker();
        var failed = new StormCellInfo(3, 5, StormCellRole.Anchor, default);
        tracker.Forming(failed);
        Assert.That(tracker.RopeOut(failed), Is.True);
        Assert.That(tracker.IsForming(failed.CellId), Is.False);
        Assert.That(tracker.Peak(failed), Is.False);
        tracker.Ended(failed);
        Assert.That(tracker.Forming(failed), Is.False);
        var exited = new StormCellInfo(4, 4, StormCellRole.Anchor, default);
        tracker.Forming(exited);
        tracker.Ended(exited);
        Assert.That(tracker.Peak(exited), Is.False);
        Assert.That(tracker.IsForming(exited.CellId), Is.False);
        tracker.Clear();
        Assert.That(tracker.Forming(exited), Is.True, "Run reset must allow replayed cell IDs.");
    }
    [Test]
    public void EarlyRetraction_NeverExtendsBeyondLastFormingLength()
    {
        float prior = 0.18f;
        var start = FunnelLifecycleVisual.FailedTouchdown(prior, 0.3f, 0.3f);
        var halfway = FunnelLifecycleVisual.FailedTouchdown(prior, 0.15f, 0.3f);
        Assert.That(start.Length, Is.EqualTo(prior).Within(0.001f));
        Assert.That(halfway.Length, Is.EqualTo(prior * 0.5f).Within(0.001f));
        Assert.That(halfway.Tilt, Is.Zero);
        Assert.That(halfway.GroundContact, Is.False);
    }
}
