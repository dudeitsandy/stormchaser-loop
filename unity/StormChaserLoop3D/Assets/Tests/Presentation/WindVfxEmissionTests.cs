using NUnit.Framework;

public class WindVfxEmissionTests
{
    [Test]
    public void Advance_EqualDurationAtDifferentFrameRates_EmitsSameBudget()
    {
        int low = Simulate(30), high = Simulate(120);
        Assert.That(low, Is.EqualTo(high).Within(1));
        Assert.That(low, Is.EqualTo(300).Within(1));
    }

    [Test]
    public void Advance_NoWind_DoesNotEmitOrBuildBacklog()
    {
        float remainder = 0.25f;
        Assert.That(WindVfxEmission.Advance(0, 90, 15, 30, ref remainder), Is.Zero);
        Assert.That(remainder, Is.EqualTo(0.25f));
    }

    [Test]
    public void Advance_ExtremeWind_StaysAtConfiguredMaximumRate()
    {
        float remainder = 0;
        Assert.That(WindVfxEmission.Advance(1000, 1, 15, 30, ref remainder), Is.EqualTo(30));
    }

    [Test]
    public void Advance_FrozenTime_DoesNotEmit()
    {
        float remainder = 0.5f;
        Assert.That(WindVfxEmission.Advance(15, 0, 15, 30, ref remainder), Is.Zero);
        Assert.That(remainder, Is.EqualTo(0.5f));
    }

    private static int Simulate(int framesPerSecond)
    {
        float remainder = 0;
        int count = 0;
        for (int frame = 0; frame < framesPerSecond * 10; frame++)
            count += WindVfxEmission.Advance(15, 1f / framesPerSecond, 15, 30, ref remainder);
        return count;
    }
}
