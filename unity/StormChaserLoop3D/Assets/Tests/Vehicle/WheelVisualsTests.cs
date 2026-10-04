using NUnit.Framework;

/// <summary>Visual wheel placement (Andy 2026-10-04: truck sinking below the road).</summary>
public class WheelVisualsTests
{
    private const float Rest = 0.61f;
    private const float Radius = 0.23f;

    [Test]
    public void WheelCentreHeight_Grounded_PutsTyreBottomOnTheContact()
    {
        // Compressed to 0.30 m: the tyre bottom (centre − radius) sits exactly at the ground (−0.30).
        float centre = WheelVisuals.WheelCentreHeight(0f, true, 0.30f, Rest, Radius);
        Assert.AreEqual(-0.30f, centre - Radius, 1e-5f);
    }

    [Test]
    public void WheelCentreHeight_Airborne_HangsAtFullDroop()
    {
        float centre = WheelVisuals.WheelCentreHeight(0f, false, 99f, Rest, Radius);
        Assert.AreEqual(-Rest + Radius, centre, 1e-5f);
    }

    [Test]
    public void WheelCentreHeight_ContactBeyondRest_NeverDroopsPastRest()
    {
        float centre = WheelVisuals.WheelCentreHeight(0f, true, 0.9f, Rest, Radius);
        Assert.AreEqual(-Rest + Radius, centre, 1e-5f);
    }
}
