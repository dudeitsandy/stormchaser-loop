using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>ADR-0004 Validation #2 (determinism) and #3 (no tile seams).</summary>
public class WorldGenerationTests
{
    [Test]
    public void test_heightField_sameSeed_isDeterministic()
    {
        // Arrange / Act
        float a = HeightField.Height(1996, 123.4f, -567.8f);
        float b = HeightField.Height(1996, 123.4f, -567.8f);
        // Assert
        Assert.AreEqual(a, b);
    }

    [Test]
    public void test_heightField_differentSeeds_produceDifferentWorlds()
    {
        int differences = 0;
        for (int i = 0; i < 50; i++)
            if (!Mathf.Approximately(HeightField.Height(1, i * 37f, i * 11f), HeightField.Height(2, i * 37f, i * 11f)))
                differences++;
        Assert.Greater(differences, 40);
    }

    [Test]
    public void test_heightField_rangeIsDriveable()
    {
        // Broad ±7 m + detail ±1.5 m: rolling, never cliffs.
        for (int i = 0; i < 400; i++)
        {
            float h = HeightField.Height(1996, -1000f + i * 5f, 1000f - i * 5f);
            Assert.That(h, Is.InRange(-8.5f, 8.5f));
        }
    }

    [Test]
    public void test_tiles_sharedEdges_haveZeroHeightDelta_acrossWholeMap()
    {
        // Arrange
        var a = new List<Vector3>();
        var b = new List<Vector3>();
        int n = TileMeshBuilder.VertsPerSide;
        // Act / Assert: every horizontal and vertical neighbour pair in the 16×16 grid.
        for (int y = 0; y < HeightField.TilesPerSide; y++)
        for (int x = 0; x < HeightField.TilesPerSide; x++)
        {
            TileMeshBuilder.FillGrid(1996, new Vector2Int(x, y), a);
            if (x + 1 < HeightField.TilesPerSide)
            {
                TileMeshBuilder.FillGrid(1996, new Vector2Int(x + 1, y), b);
                for (int j = 0; j < n; j++)
                    Assert.AreEqual(a[j * n + (n - 1)].y, b[j * n].y, 0f, $"Seam at tiles ({x},{y})-({x + 1},{y}) row {j}");
            }
            if (y + 1 < HeightField.TilesPerSide)
            {
                TileMeshBuilder.FillGrid(1996, new Vector2Int(x, y + 1), b);
                for (int i = 0; i < n; i++)
                    Assert.AreEqual(a[(n - 1) * n + i].y, b[i].y, 0f, $"Seam at tiles ({x},{y})-({x},{y + 1}) col {i}");
            }
        }
    }

    [Test]
    public void test_flatShadedMesh_hasExpectedCountsAndUpwardNormals()
    {
        var grid = new List<Vector3>();
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        TileMeshBuilder.FillGrid(1996, new Vector2Int(7, 7), grid);
        TileMeshBuilder.FillFlatShaded(grid, verts, normals);
        Assert.AreEqual(TileMeshBuilder.RenderVertexCount, verts.Count);
        Assert.AreEqual(TileMeshBuilder.RenderVertexCount, TileMeshBuilder.FlatTriangles().Count);
        foreach (Vector3 nrm in normals) Assert.Greater(nrm.y, 0.5f, "Gentle terrain: every face should point up");
    }

    [Test]
    public void test_tileAt_clampsAndRoundTripsOrigins()
    {
        Assert.AreEqual(new Vector2Int(0, 0), HeightField.TileAt(-5000f, -5000f));
        Assert.AreEqual(new Vector2Int(15, 15), HeightField.TileAt(5000f, 5000f));
        Vector2 o = HeightField.TileOrigin(new Vector2Int(9, 4));
        Assert.AreEqual(new Vector2Int(9, 4), HeightField.TileAt(o.x + 1f, o.y + 1f));
    }
}
