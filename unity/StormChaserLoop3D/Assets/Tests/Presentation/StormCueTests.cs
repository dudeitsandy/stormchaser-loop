using NUnit.Framework;

public class StormCueTests
{
    [Test]
    public void OvercastAmbient_DropsMoreThanDirectLightAndRestoresBaseline()
    {
        Assert.That(StormCueLevels.AmbientBrightness(0f), Is.EqualTo(1f));
        Assert.That(StormCueLevels.AmbientBrightness(1f), Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(StormCueLevels.AmbientBrightness(1f), Is.LessThan(StormCueLevels.Brightness(1f)));
    }
    [Test]
    public void OutdoorSirens_IgnoreWeakStormsAndDuplicateWarningsDoNotExtendCycle()
    {
        var cycle = new OutdoorSirenCycle();
        cycle.Forming(1, 2, 25f);
        Assert.That(cycle.Active, Is.False);
        cycle.Forming(2, 3, 25f);
        cycle.Tick(24f);
        Assert.That(cycle.Active, Is.True);
        cycle.Forming(2, 3, 25f);
        cycle.Tick(1f);
        Assert.That(cycle.Active, Is.False);
    }
    [Test]
    public void OutdoorSirens_EmergenciesSurviveTimerAndEndIndependently()
    {
        var cycle = new OutdoorSirenCycle();
        cycle.Forming(1, 5, 25f); cycle.Forming(2, 5, 25f);
        cycle.Tick(100f);
        Assert.That(cycle.Active, Is.True);
        cycle.End(1);
        Assert.That(cycle.Active, Is.True);
        cycle.End(2);
        Assert.That(cycle.Active, Is.False);
    }
    [Test]
    public void OutdoorSirens_EmergencyEndKeepsOtherWarningAndResetAllowsSameCellNextRun()
    {
        var cycle = new OutdoorSirenCycle();
        cycle.Forming(1, 4, 25f); cycle.Forming(2, 5, 25f);
        cycle.End(2); cycle.Tick(0f);
        Assert.That(cycle.Active, Is.True);
        cycle.Reset();
        Assert.That(cycle.Active, Is.False);
        cycle.Forming(1, 4, 25f);
        Assert.That(cycle.Active, Is.True);
    }
    [TestCase(5, 1f, 1f)]
    [TestCase(2, 1f, 0.5f)]
    [TestCase(0, 0.3f, 0.05f)]
    public void Storminess_UsesEfWeightedLifecycleWithoutPlayerDistance(int ef, float intensity, float expected)
    {
        Assert.That(StormCueLevels.StorminessContribution(ef, intensity), Is.EqualTo(expected).Within(0.0001f));
    }
    [Test]
    public void Storminess_MultipleCellsAddAndTargetCapsAtOne()
    {
        float sum = StormCueLevels.StorminessContribution(2, 1f) + StormCueLevels.StorminessContribution(3, 1f);
        Assert.That(StormCueLevels.StorminessTarget(sum), Is.EqualTo(1f));
        Assert.That(StormCueLevels.StorminessTarget(0f), Is.Zero);
    }
    [Test]
    public void Storminess_RiseAndFallUseTimeConstantsAndDoNotSnap()
    {
        Assert.That(StormCueLevels.EaseStorminess(0f, 1f, 5f), Is.EqualTo(1f - (float)System.Math.Exp(-1)).Within(0.0001f));
        Assert.That(StormCueLevels.EaseStorminess(1f, 0f, 20f), Is.EqualTo((float)System.Math.Exp(-1)).Within(0.0001f));
        Assert.That(StormCueLevels.EaseStorminess(0.6f, 0f, 0f), Is.EqualTo(0.6f).Within(0.0001f));
    }
    [TestCase(0f, 1f)]
    [TestCase(1f, 0f)]
    public void Storminess_EasingIsIndependentOfFrameSubdivision(float current, float target)
    {
        float one = StormCueLevels.EaseStorminess(current, target, 1f);
        float many = current;
        for (int i = 0; i < 60; i++) many = StormCueLevels.EaseStorminess(many, target, 1f / 60f);
        Assert.That(many, Is.EqualTo(one).Within(0.00001f));
    }
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
