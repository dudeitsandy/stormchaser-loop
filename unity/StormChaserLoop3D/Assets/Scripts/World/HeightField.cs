using UnityEngine;

/// <summary>
/// Seeded world heightfield (ADR-0004). Heights are sampled in world space, so neighbouring tiles agree
/// exactly on shared edges. Pure function of (seed, x, z): same seed → same world.
/// </summary>
public static class HeightField
{
    /// <summary>World is centered on the origin and spans ±HalfExtent.</summary>
    public const float WorldSize = 2048f;
    public const float HalfExtent = WorldSize * 0.5f;
    public const float TileSize = 128f;
    public const int TilesPerSide = 16;
    /// <summary>Cells per tile edge (4 m spacing → 33×33 vertices).</summary>
    public const int CellsPerTile = 32;
    public const float CellSize = TileSize / CellsPerTile;

    private const float BroadScale = 300f, BroadAmplitude = 7f;
    private const float DetailScale = 70f, DetailAmplitude = 1.5f;

    /// <summary>Terrain height (m) at world position (x, z).</summary>
    public static float Height(int seed, float x, float z)
    {
        Offsets(seed, out float ox, out float oz, out float dx, out float dz);
        float broad = (Mathf.PerlinNoise(ox + x / BroadScale, oz + z / BroadScale) - 0.5f) * 2f * BroadAmplitude;
        float detail = (Mathf.PerlinNoise(dx + x / DetailScale, dz + z / DetailScale) - 0.5f) * 2f * DetailAmplitude;
        return broad + detail;
    }

    /// <summary>World-space minimum corner (x, z) of a tile.</summary>
    public static Vector2 TileOrigin(Vector2Int tile) =>
        new Vector2(-HalfExtent + tile.x * TileSize, -HalfExtent + tile.y * TileSize);

    /// <summary>Tile containing world position (clamped to the map).</summary>
    public static Vector2Int TileAt(float x, float z) => new Vector2Int(
        Mathf.Clamp(Mathf.FloorToInt((x + HalfExtent) / TileSize), 0, TilesPerSide - 1),
        Mathf.Clamp(Mathf.FloorToInt((z + HalfExtent) / TileSize), 0, TilesPerSide - 1));

    public static bool IsInMap(Vector2Int tile) =>
        tile.x >= 0 && tile.y >= 0 && tile.x < TilesPerSide && tile.y < TilesPerSide;

    // Large seed-derived offsets keep Perlin away from its symmetric origin and decorrelate the octaves.
    private static void Offsets(int seed, out float ox, out float oz, out float dx, out float dz)
    {
        uint h = (uint)seed * 2654435761u;
        ox = 1000f + (h & 0xFFFF) * 0.37f;
        oz = 1000f + ((h >> 16) & 0xFFFF) * 0.41f;
        dx = ox + 517.3f;
        dz = oz + 911.7f;
    }
}
