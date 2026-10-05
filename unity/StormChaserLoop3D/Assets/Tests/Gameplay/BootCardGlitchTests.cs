using NUnit.Framework;

/// <summary>Boot card glitch stays inside the accessibility Basic photosensitivity rule (no more than 3 flashes per second).</summary>
public class BootCardGlitchTests
{
    [Test]
    public void Glitch_HasAtMostThreeBlipsPerSecond_AndStopsAfterTheFadeIn()
    {
        // Arrange / Act: count rising edges over the first second, sampled at 1 kHz
        int blips = 0;
        bool on = false;
        for (int i = 0; i <= 1000; i++)
        {
            bool now = BootCard.GlitchAmount(i / 1000f) > 0.01f;
            if (now && !on) blips++;
            on = now;
        }
        // Assert
        Assert.LessOrEqual(blips, 3);
        Assert.Greater(blips, 0, "the glitch is there");
        Assert.AreEqual(0f, BootCard.GlitchAmount(0.4f));
        Assert.AreEqual(0f, BootCard.GlitchAmount(2f));
    }
}
