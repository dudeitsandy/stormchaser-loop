using NUnit.Framework;

public class FunnelLifecycleVisualTests
{
    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void Forming_ExtendsDownFromFixedCrownWithoutGroundContact(float intensity)
    {
        var visual = FunnelLifecycleVisual.Evaluate(TornadoLifecycle.Phase.Forming, intensity, false, true);
        Assert.That(visual.Length, Is.EqualTo(intensity * 0.6f).Within(0.001f));
        Assert.That(1f - visual.Length, Is.GreaterThanOrEqualTo(0.399f));
        Assert.That(visual.GroundContact, Is.False, "Current gameplay can expose damage while forming; visuals must still stay aloft.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Mature_ReachesGroundButDustRequiresDamage(bool damaging)
    {
        var visual = FunnelLifecycleVisual.Evaluate(TornadoLifecycle.Phase.Mature, 1f, true, damaging);
        Assert.That(visual.Length, Is.EqualTo(1f));
        Assert.That(visual.Width, Is.EqualTo(1f));
        Assert.That(visual.GroundContact, Is.EqualTo(damaging));
    }

    [Test]
    public void Rope_ThinsBeforeRetractingUpward()
    {
        var early = FunnelLifecycleVisual.Evaluate(TornadoLifecycle.Phase.Dissipating, 0.65f, true, true);
        var late = FunnelLifecycleVisual.Evaluate(TornadoLifecycle.Phase.Dissipating, 0.2f, true, true);
        Assert.That(early.Width, Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(early.Length, Is.EqualTo(1f));
        Assert.That(late.Width, Is.EqualTo(early.Width));
        Assert.That(late.Length, Is.LessThan(early.Length));
        Assert.That(late.Tilt, Is.InRange(0f, 30f));
        Assert.That(late.GroundContact, Is.False);
    }

    [Test]
    public void FailedTouchdown_RetractsWithoutRopeOrGroundEffects()
    {
        var visual = FunnelLifecycleVisual.Evaluate(TornadoLifecycle.Phase.Dissipating, 0.5f, false, true);
        Assert.That(visual.Length, Is.LessThanOrEqualTo(0.6f));
        Assert.That(visual.Tilt, Is.Zero);
        Assert.That(visual.GroundContact, Is.False);
    }

    [Test]
    public void Done_HasNoFunnelOrGroundContact()
    {
        var visual = FunnelLifecycleVisual.Evaluate(TornadoLifecycle.Phase.Done, 1f, true, true);
        Assert.That(visual.Length, Is.Zero);
        Assert.That(visual.Width, Is.Zero);
        Assert.That(visual.GroundContact, Is.False);
    }
}
