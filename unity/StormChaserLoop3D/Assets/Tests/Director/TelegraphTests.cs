using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>storm-director.md Rule 7 / AC-26 Unit companions and the KTVR News crawl (story 008).</summary>
public class TelegraphTests
{
    [Test]
    public void EnvironmentIntensity_Ef0At5mAndEf5At30m_IsOneAtEveryBearing()
    {
        // AC-26: magnitudes 2.72 + 20.24 m/s; a vector sum could drop to ≈ 17.5 and leave e < 1.
        for (float deg = 0f; deg < 360f; deg += 15f)
        {
            Vector3 player = Vector3.zero;
            Vector3 ef0Offset = Quaternion.Euler(0f, deg, 0f) * new Vector3(5f, 0f, 0f);
            Vector3 ef5Offset = new Vector3(-30f, 0f, 0f);
            float sum = StormScale.Wind(player - ef0Offset, 8f, 12f, 1f).magnitude
                        + StormScale.Wind(player - ef5Offset, 62f, 70f, 1f).magnitude;
            Assert.AreEqual(1f, StormTelegraph.EnvironmentIntensity(sum), 1e-5f, $"bearing {deg}");
        }
    }

    [Test]
    public void EnvironmentIntensity_ClampsAndScalesBy20()
    {
        Assert.AreEqual(0f, StormTelegraph.EnvironmentIntensity(0f));
        Assert.AreEqual(0.5f, StormTelegraph.EnvironmentIntensity(10f), 1e-5f);
        Assert.AreEqual(1f, StormTelegraph.EnvironmentIntensity(55f));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Crawl_ForAnyTrueEf_HasNoEfNumberOrDigit(int ef)
    {
        foreach (string text in new[] { StormTelegraph.FormingCrawl(ef, "NW"), StormTelegraph.TouchdownCrawl(ef, "NW") })
        {
            Assert.IsFalse(Regex.IsMatch(text, @"\d"), text);
            StringAssert.DoesNotContain("EF", text);
        }
    }

    [Test]
    public void Crawl_Ef5_IsTornadoEmergency_Ef3And4_AreTornadoWarning_BelowIsSilent()
    {
        StringAssert.Contains("TORNADO EMERGENCY", StormTelegraph.FormingCrawl(5, "N"));
        StringAssert.Contains("TORNADO WARNING", StormTelegraph.FormingCrawl(3, "N"));
        StringAssert.Contains("TORNADO WARNING", StormTelegraph.FormingCrawl(4, "N"));
        Assert.IsNull(StormTelegraph.FormingCrawl(2, "N"));
        Assert.IsTrue(StormTelegraph.IsEmergency(5));
        Assert.IsFalse(StormTelegraph.IsEmergency(4));
    }

    [TestCase(0f, 10f, "N")]
    [TestCase(10f, 0f, "E")]
    [TestCase(0f, -10f, "S")]
    [TestCase(-10f, 0f, "W")]
    [TestCase(-10f, 10f, "NW")]
    [TestCase(10f, -10f, "SE")]
    public void Bearing_EightPointCompass(float x, float z, string expected)
    {
        Assert.AreEqual(expected, StormTelegraph.Bearing(Vector3.zero, new Vector3(x, 0f, z)));
    }
}
