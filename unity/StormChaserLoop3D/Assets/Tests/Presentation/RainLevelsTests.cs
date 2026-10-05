using NUnit.Framework;

public class RainLevelsTests
{
    [TestCase(0f, 0)]
    [TestCase(0.5f, 150)]
    [TestCase(1f, 300)]
    [TestCase(2f, 300)]
    [TestCase(-1f, 0)]
    public void StreakCount_IsDryAtZeroAndBoundedAtThreeHundred(float s, int expected)
    {
        Assert.That(RainLevels.StreakCount(s, 500), Is.EqualTo(expected));
    }
    [Test]
    public void StreakCount_RespectsConfiguredCapacityAndRejectsInvalidDensity()
    {
        Assert.That(RainLevels.StreakCount(1f, 240), Is.EqualTo(240));
        Assert.That(RainLevels.StreakCount(1f, -1), Is.Zero);
        Assert.That(RainLevels.StreakCount(float.NaN, 300), Is.Zero);
    }
    [Test]
    public void CurtainsAndLens_HaveIndependentBoundedOpacityAndAreDryAtZero()
    {
        Assert.That(RainLevels.CurtainOpacity(0f), Is.Zero);
        Assert.That(RainLevels.LensOpacity(0f), Is.Zero);
        Assert.That(RainLevels.CurtainOpacity(1f), Is.EqualTo(0.24f));
        Assert.That(RainLevels.LensOpacity(1f), Is.EqualTo(0.38f));
        Assert.That(RainLevels.CurtainOpacity(2f), Is.EqualTo(RainLevels.CurtainOpacity(1f)));
        Assert.That(RainLevels.LensOpacity(float.NaN), Is.Zero);
    }
}
