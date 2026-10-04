using NUnit.Framework;

public class FunnelVisualDimensionsTests
{
    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void CloudLowestEdge_StaysAboveClearanceAtAllHeights(float sample)
    {
        float centre = FunnelVisualDimensions.CloudHeight(25f, 2.4f, 18.8f, sample);
        Assert.That(centre - 1.2f, Is.GreaterThanOrEqualTo(24.999f));
        Assert.That(centre, Is.EqualTo(26.2f + 18.8f * sample).Within(0.001f));
    }
    [Test]
    public void WeakFunnel_RemainsTallAndNarrowInsteadOfShrinkingItsCloudHeight()
    {
        float height = FunnelVisualDimensions.CloudHeight(25f, 2.4f, 18.8f, 0.5f);
        float weakRadius = FunnelVisualDimensions.CrownRadius(0.35f, 2f, 0.5f);
        float strongRadius = FunnelVisualDimensions.CrownRadius(1.5f, 2f, 0.5f);
        Assert.That(height, Is.GreaterThan(25f));
        Assert.That(weakRadius * 2f, Is.EqualTo(3.01f).Within(0.01f));
        Assert.That(strongRadius, Is.GreaterThan(weakRadius));
    }
    [TestCase(0f)]
    [TestCase(1f)]
    public void EfFive_WidthFollowsDamageScaleRegardlessOfCloudHeight(float heightSample)
    {
        float height = FunnelVisualDimensions.CloudHeight(25f, 2.4f, 18.8f, heightSample);
        float radius = FunnelVisualDimensions.CrownRadius(12f / 4.3f, 2f, 0.5f);
        Assert.That(radius * 2f, Is.EqualTo(24f).Within(0.001f));
        Assert.That(radius * 2f, Is.LessThan(height), "A high cloud must not automatically expand the dangerous-looking silhouette.");
    }
    [Test]
    public void SpawnVariation_IsStableAndCloudWidthUseSeparateStreams()
    {
        float height = FunnelVisualDimensions.Variation(12f, -31f, 71);
        Assert.That(FunnelVisualDimensions.Variation(12f, -31f, 71), Is.EqualTo(height));
        Assert.That(height, Is.InRange(0f, 1f));
        Assert.That(FunnelVisualDimensions.Variation(12f, -31f, 73), Is.Not.EqualTo(height));
        Assert.That(FunnelVisualDimensions.Variation(52f, 4f, 71), Is.Not.EqualTo(height));
    }
    [TestCase(0f)]
    [TestCase(1.5f)]
    [TestCase(3f)]
    public void ShapeMotion_KeepsGroundAndCloudEndpointsFixed(float phase)
    {
        const int segments = 12;
        var vertices = new UnityEngine.Vector3[(segments + 1) * 2];
        FunnelSurfaceGeometry.Deform(vertices, segments, 0.04f, 0.65f, 0.39f, phase, 1f);
        Assert.That((vertices[0].x + vertices[1].x) * 0.5f, Is.Zero);
        Assert.That(vertices[0].y, Is.Zero);
        Assert.That(vertices[0].z, Is.Zero);
        Assert.That((vertices[24].x + vertices[25].x) * 0.5f, Is.Zero);
        Assert.That(vertices[24].y, Is.EqualTo(1f));
        Assert.That(vertices[24].z, Is.Zero);
    }
    [Test]
    public void SlenderMotion_CurvesTheTaperAndMovesTheMiddleWithoutCollapsingWidth()
    {
        var first = new UnityEngine.Vector3[26];
        var later = new UnityEngine.Vector3[26];
        FunnelSurfaceGeometry.Deform(first, 12, 0.04f, 0.65f, 0.39f, 0f, 1f);
        FunnelSurfaceGeometry.Deform(later, 12, 0.04f, 0.65f, 0.39f, 1.5f, 1f);
        Assert.That(first[13].x - first[12].x, Is.LessThan(0.8f), "The middle should stay narrow before flaring into the cloud.");
        Assert.That(first[12].x + first[13].x, Is.Not.EqualTo(later[12].x + later[13].x));
        for (int row = 0; row <= 12; row++)
            Assert.That(later[row * 2 + 1].x, Is.GreaterThan(later[row * 2].x));
    }
    [Test]
    public void EllipticalDepth_VariesProfileWithoutChangingHeightOrEndpoints()
    {
        float front = FunnelVisualDimensions.ProjectedRadius(2f, 0.7f, 0f);
        float side = FunnelVisualDimensions.ProjectedRadius(2f, 0.7f, (float)System.Math.PI * 0.5f);
        Assert.That(front, Is.EqualTo(2f).Within(0.001f));
        Assert.That(side, Is.EqualTo(1.4f).Within(0.001f));
    }
}
