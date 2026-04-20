using NUnit.Framework;

public class ScoringSystemTests
{
    [Test]
    public void PerfectShot_EF3_ReturnsExpectedScore()
    {
        // Perfect aim, perfect distance, EF3 strength = 2.5
        float score = ScoringSystem.CalculatePhotoScore(1f, 1f, 2.5f);
        Assert.AreEqual(250f, score, 0.01f);
    }

    [Test]
    public void ZeroAim_FullDistance_EF1_ReturnsExpectedScore()
    {
        // No aim quality, perfect distance, EF1 strength = 1.5
        // (0 * 0.6 + 1 * 0.4) * 1.5 * 100 = 60
        float score = ScoringSystem.CalculatePhotoScore(0f, 1f, 1.5f);
        Assert.AreEqual(60f, score, 0.01f);
    }

    [Test]
    public void PartialShot_EF5_ReturnsExpectedScore()
    {
        // 0.7 aim, 0.5 distance, EF5 strength = 4.0
        // (0.7 * 0.6 + 0.5 * 0.4) * 4.0 * 100 = (0.42 + 0.20) * 400 = 248
        float score = ScoringSystem.CalculatePhotoScore(0.7f, 0.5f, 4f);
        Assert.AreEqual(248f, score, 0.01f);
    }
}
