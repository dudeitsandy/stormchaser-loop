using NUnit.Framework;
using UnityEngine;

/// <summary>Radio now-playing card envelope (HudController.NowPlayingEnvelope): slide in, hold, fade out.</summary>
public class NowPlayingCardTests
{
    private const float Hold = 2f;

    [Test]
    public void Envelope_AtSkip_StartsInvisibleAndLeftOfItsSpot()
    {
        Vector2 e = HudController.NowPlayingEnvelope(0f, Hold);
        Assert.AreEqual(0f, e.x, 1e-5f);
        Assert.AreEqual(-HudController.NowPlayingSlidePx, e.y, 1e-4f);
    }

    [Test]
    public void Envelope_SlideIn_OpacityRisesAndOffsetShrinks()
    {
        Vector2 early = HudController.NowPlayingEnvelope(HudController.NowPlayingInSeconds * 0.25f, Hold);
        Vector2 late = HudController.NowPlayingEnvelope(HudController.NowPlayingInSeconds * 0.75f, Hold);
        Assert.Greater(late.x, early.x);
        Assert.Greater(late.y, early.y, "moves right toward its spot");
        Assert.Less(late.y, 0f);
    }

    [TestCase(0.25f)]
    [TestCase(1f)]
    [TestCase(1.99f)]
    public void Envelope_DuringHold_FullyShownInPlace(float age)
    {
        Assert.AreEqual(new Vector2(1f, 0f), HudController.NowPlayingEnvelope(age, Hold));
    }

    [Test]
    public void Envelope_FadeOut_DriftsLeftAndEndsInvisible()
    {
        Vector2 mid = HudController.NowPlayingEnvelope(Hold + HudController.NowPlayingOutSeconds * 0.5f, Hold);
        Assert.AreEqual(0.5f, mid.x, 1e-4f);
        Assert.Less(mid.y, 0f);
        Vector2 done = HudController.NowPlayingEnvelope(Hold + HudController.NowPlayingOutSeconds, Hold);
        Assert.AreEqual(0f, done.x, 1e-5f);
    }

    [Test]
    public void Envelope_HoldShorterThanSlideIn_StillSlidesInFully()
    {
        Assert.AreEqual(new Vector2(1f, 0f), HudController.NowPlayingEnvelope(HudController.NowPlayingInSeconds, 0.1f));
    }
}
