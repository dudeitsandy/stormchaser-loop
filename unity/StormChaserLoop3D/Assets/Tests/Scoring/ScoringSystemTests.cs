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

    [Test]
    public void PerfectShot_EF5_Returns400()
    {
        Assert.AreEqual(400f, ScoringSystem.CalculatePhotoScore(1f, 1f, 4f), 0.01f);
    }

    [Test]
    public void Quality_ClampsOutOfRangeInputs()
    {
        Assert.AreEqual(1f, ScoringSystem.CalculateQuality(2f, 5f), 0.0001f);
        Assert.AreEqual(0f, ScoringSystem.CalculateQuality(-1f, -1f), 0.0001f);
    }

    [TestCase(1.0f, ShotTier.Perfect)]
    [TestCase(0.85f, ShotTier.Perfect)]
    [TestCase(0.84f, ShotTier.Good)]
    [TestCase(0.5f, ShotTier.Good)]
    [TestCase(0.49f, ShotTier.Glancing)]
    [TestCase(0f, ShotTier.Glancing)]
    public void GetTier_UsesQualityThresholds(float quality, ShotTier expected)
    {
        Assert.AreEqual(expected, ScoringSystem.GetTier(quality));
    }

    [Test]
    public void PerfectlyFramedEF0_IsPerfectTier()
    {
        float quality = ScoringSystem.CalculateQuality(1f, 1f);
        Assert.AreEqual(ShotTier.Perfect, ScoringSystem.GetTier(quality));
    }
}
