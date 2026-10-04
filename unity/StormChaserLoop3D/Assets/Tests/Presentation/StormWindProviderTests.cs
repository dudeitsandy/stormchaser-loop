using NUnit.Framework;
using UnityEngine;

public sealed class StormWindProviderProbe : DisasterEntity
{
    public Vector3 Wind;
    public override Vector3 GetWindAt(Vector3 position) => Wind;
}

/// <summary>Native provider checks, compiled but excluded from the managed math runner.</summary>
public class StormWindProviderTests
{
    [Test]
    public void Provider_SumsOpposingWindsInsteadOfTheirResultant()
    {
        var first = new GameObject("FirstStormProviderTest");
        var second = new GameObject("SecondStormProviderTest");
        try
        {
            var a = first.AddComponent<StormWindProviderProbe>();
            var b = second.AddComponent<StormWindProviderProbe>();
            a.Wind = Vector3.left * 8f;
            b.Wind = Vector3.right * 15f;
            StormWindProvider.Sample(new DisasterEntity[] { a, b }, Vector3.zero,
                out float exposure, out Vector3 bearing, out float ef5);
            Assert.That(exposure, Is.EqualTo(1f), "Vector summing would incorrectly produce e=0.35.");
            Assert.That(bearing, Is.EqualTo(Vector3.right));
            Assert.That(ef5, Is.Zero, "Unclassified disasters must not invent an EF5 rumble.");
        }
        finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
    }
}
