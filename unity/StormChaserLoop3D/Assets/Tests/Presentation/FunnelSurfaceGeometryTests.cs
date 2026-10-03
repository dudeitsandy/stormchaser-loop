using System.Collections.Generic;
using NUnit.Framework;

public class FunnelSurfaceGeometryTests
{
    [Test]
    public void Wedge_HasBroadGroundContactAndWidthExceedsHeight()
    {
        var vertices = FunnelSurfaceGeometry.Vertices(12, 0.85f);
        float baseWidth = (vertices[1].x - vertices[0].x) * 12f;
        float crownWidth = (vertices[vertices.Length - 1].x - vertices[vertices.Length - 2].x) * 12f;
        Assert.That(baseWidth, Is.GreaterThanOrEqualTo(20f));
        Assert.That(crownWidth, Is.EqualTo(24f).Within(0.001f));
        Assert.That(crownWidth, Is.GreaterThanOrEqualTo(20f), "EF5 must read as a wedge, not a tall thin cone.");
    }

    [TestCase(-0.1f)]
    [TestCase(1.1f)]
    [TestCase(float.NaN)]
    public void Wedge_RejectsInvalidBaseRadius(float radius)
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => FunnelSurfaceGeometry.Vertices(12, radius));
    }
    [TestCase(6)]
    [TestCase(12)]
    [TestCase(20)]
    public void Triangles_AllFacesConnectedThroughSharedEdges(int segments)
    {
        var triangles = FunnelSurfaceGeometry.Triangles(segments);
        var reached = new HashSet<int> { 0 };
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int face = 0; face < triangles.Length / 3; face++)
            {
                if (reached.Contains(face)) continue;
                foreach (int neighbor in new List<int>(reached))
                {
                    int shared = 0;
                    for (int a = 0; a < 3; a++)
                        for (int b = 0; b < 3; b++)
                            if (triangles[face * 3 + a] == triangles[neighbor * 3 + b]) shared++;
                    if (shared < 2) continue;
                    reached.Add(face);
                    changed = true;
                    break;
                }
            }
        }
        Assert.That(reached.Count, Is.EqualTo(triangles.Length / 3), "Detached triangles would reopen ribbon gaps.");
    }

    [Test]
    public void Vertices_TaperWidensContinuouslyFromGroundToCrown()
    {
        var vertices = FunnelSurfaceGeometry.Vertices(12);
        float previousWidth = 0;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            Assert.That(vertices[i].y, Is.EqualTo(vertices[i + 1].y));
            float width = vertices[i + 1].x - vertices[i].x;
            Assert.That(width, Is.GreaterThan(previousWidth));
            previousWidth = width;
        }
        Assert.That(vertices[0].y, Is.Zero);
        Assert.That(vertices[vertices.Length - 1].y, Is.EqualTo(1));
    }
}
