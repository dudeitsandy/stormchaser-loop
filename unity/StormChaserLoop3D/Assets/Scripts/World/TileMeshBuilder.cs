using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fills pooled lists with a tile's geometry (ADR-0004 §3). Allocation-free after warm-up: callers own
/// and reuse the lists, which matters for WebGL GC hitches.
/// </summary>
public static class TileMeshBuilder
{
    public const int VertsPerSide = HeightField.CellsPerTile + 1;
    /// <summary>Flat-shaded render mesh: 2 triangles × 3 unshared verts per cell.</summary>
    public const int RenderVertexCount = HeightField.CellsPerTile * HeightField.CellsPerTile * 6;

    private static List<int> _flatTriangles;
    private static List<int> _gridTriangles;

    /// <summary>Shared-vertex grid heights in tile-local space (used for the collider and as the source for the render mesh).</summary>
    public static void FillGrid(int seed, Vector2Int tile, List<Vector3> grid)
    {
        grid.Clear();
        Vector2 o = HeightField.TileOrigin(tile);
        for (int j = 0; j < VertsPerSide; j++)
        for (int i = 0; i < VertsPerSide; i++)
        {
            float lx = i * HeightField.CellSize, lz = j * HeightField.CellSize;
            grid.Add(new Vector3(lx, HeightField.Height(seed, o.x + lx, o.y + lz), lz));
        }
    }

    /// <summary>Unshared vertices + per-face normals from a filled grid, for the low-poly look.</summary>
    public static void FillFlatShaded(List<Vector3> grid, List<Vector3> vertices, List<Vector3> normals)
    {
        vertices.Clear();
        normals.Clear();
        int n = VertsPerSide;
        for (int j = 0; j < HeightField.CellsPerTile; j++)
        for (int i = 0; i < HeightField.CellsPerTile; i++)
        {
            Vector3 a = grid[j * n + i], b = grid[j * n + i + 1], c = grid[(j + 1) * n + i], d = grid[(j + 1) * n + i + 1];
            AddTri(a, c, b, vertices, normals);
            AddTri(b, c, d, vertices, normals);
        }
    }

    /// <summary>Index list 0..N-1 for the flat mesh (shared by every tile).</summary>
    public static List<int> FlatTriangles()
    {
        if (_flatTriangles != null) return _flatTriangles;
        _flatTriangles = new List<int>(RenderVertexCount);
        for (int i = 0; i < RenderVertexCount; i++) _flatTriangles.Add(i);
        return _flatTriangles;
    }

    /// <summary>Index list for the shared-vertex grid (collider mesh; shared by every tile).</summary>
    public static List<int> GridTriangles()
    {
        if (_gridTriangles != null) return _gridTriangles;
        int n = VertsPerSide;
        _gridTriangles = new List<int>(HeightField.CellsPerTile * HeightField.CellsPerTile * 6);
        for (int j = 0; j < HeightField.CellsPerTile; j++)
        for (int i = 0; i < HeightField.CellsPerTile; i++)
        {
            int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
            _gridTriangles.Add(a); _gridTriangles.Add(c); _gridTriangles.Add(b);
            _gridTriangles.Add(b); _gridTriangles.Add(c); _gridTriangles.Add(d);
        }
        return _gridTriangles;
    }

    private static void AddTri(Vector3 a, Vector3 b, Vector3 c, List<Vector3> vertices, List<Vector3> normals)
    {
        Vector3 nrm = Vector3.Cross(b - a, c - a).normalized;
        vertices.Add(a); vertices.Add(b); vertices.Add(c);
        normals.Add(nrm); normals.Add(nrm); normals.Add(nrm);
    }
}
