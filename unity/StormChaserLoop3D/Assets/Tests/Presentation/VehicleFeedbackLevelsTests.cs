using NUnit.Framework;

public class VehicleFeedbackLevelsTests
{
    [TestCase(VehicleState.Grounded, 4)]
    [TestCase(VehicleState.Airborne, 0)]
    [TestCase(VehicleState.Tossed, 0)]
    [TestCase(VehicleState.Upended, 4)]
    [TestCase(VehicleState.Sliding, 1)]
    public void Skid_RejectsNonSlidingOrInsufficientContacts(VehicleState state, int wheels)
        => Assert.That(VehicleFeedbackLevels.Skid(state, wheels, 20f, 45f), Is.Zero);

    [Test]
    public void Skid_StationarySlideCannotMakeNoiseOrSmoke()
        => Assert.That(VehicleFeedbackLevels.Skid(VehicleState.Sliding, 4, 0f, 90f), Is.Zero);

    [Test]
    public void Skid_ReverseAndEitherSlipDirectionHaveEqualStrength()
    {
        float forward = VehicleFeedbackLevels.Skid(VehicleState.Sliding, 4, 6f, 20f);
        Assert.That(forward, Is.GreaterThan(0f));
        Assert.That(VehicleFeedbackLevels.Skid(VehicleState.Sliding, 4, -6f, -20f), Is.EqualTo(forward));
    }

    [Test]
    public void SevereFeedback_RemainsBounded()
    {
        Assert.That(VehicleFeedbackLevels.Skid(VehicleState.Sliding, 4, 100f, 180f), Is.EqualTo(1f));
        Assert.That(VehicleFeedbackLevels.Landing(-100f, 2f), Is.EqualTo(1f));
        Assert.That(VehicleFeedbackLevels.Impact(100f, 100), Is.EqualTo(1f));
    }

    [Test]
    public void MinorContact_DoesNotProduceLandingOrImpactFeedback()
    {
        Assert.That(VehicleFeedbackLevels.Landing(-1f, 2f), Is.Zero);
        Assert.That(VehicleFeedbackLevels.Impact(1f, 0), Is.Zero);
    }

    [Test]
    public void Landing_AcceptsMagnitudeOrSignedVelocity()
        => Assert.That(VehicleFeedbackLevels.Landing(-6f, 2f), Is.EqualTo(VehicleFeedbackLevels.Landing(6f, 2f)));

    [Test]
    public void Impact_IncreasesWithSpeedAndDamage()
    {
        float low = VehicleFeedbackLevels.Impact(4f, 0);
        Assert.That(VehicleFeedbackLevels.Impact(8f, 0), Is.GreaterThan(low));
        Assert.That(VehicleFeedbackLevels.Impact(4f, 1), Is.GreaterThan(low));
    }
}
