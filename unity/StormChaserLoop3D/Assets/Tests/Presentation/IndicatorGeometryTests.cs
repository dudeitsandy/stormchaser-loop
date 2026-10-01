using NUnit.Framework;
using UnityEngine;

public class IndicatorGeometryTests
{
    [TestCase(10f, 0f, 10f, 925f, 300f)]
    [TestCase(-10f, 0f, 10f, 75f, 300f)]
    [TestCase(0f, 10f, 10f, 500f, 75f)]
    [TestCase(0f, 0f, -10f, 500f, 525f)]
    public void EdgePosition_CardinalBearing_ReachesInsetEdge(float x, float y, float z, float expectedX, float expectedY)
    {
        Vector2 result = IndicatorGeometry.EdgePosition(new Vector3(x, y, z), new Vector2(1000, 600), 75);
        Assert.That(result.x, Is.EqualTo(expectedX).Within(0.01f));
        Assert.That(result.y, Is.EqualTo(expectedY).Within(0.01f));
    }

    [Test]
    public void EdgePosition_DiagonalBearing_RemainsInsideBothMargins()
    {
        Vector2 result = IndicatorGeometry.EdgePosition(new Vector3(10, -10, 10), new Vector2(1000, 600), 75);
        Assert.That(result.x, Is.InRange(75f, 925f));
        Assert.That(result.y, Is.EqualTo(525f).Within(0.01f));
    }

    [Test]
    public void EdgePosition_ZeroBearing_ReturnsFinitePosition()
    {
        Vector2 result = IndicatorGeometry.EdgePosition(Vector3.zero, new Vector2(1000, 600), 75);
        Assert.That(float.IsNaN(result.x) || float.IsNaN(result.y), Is.False);
        Assert.That(result.y, Is.EqualTo(75f).Within(0.01f));
    }
}
