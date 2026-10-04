using System;
using UnityEngine;

/// <summary>Connected, tapered billboard geometry for a funnel without detached ribbon silhouettes.</summary>
public static class FunnelSurfaceGeometry
{
    /// <summary>Creates paired edge vertices from ground tip to full-width crown, normalized to unit height.</summary>
    public static Vector3[] Vertices(int segments, float baseRadius = 0.04f)
    {
        Validate(segments);
        if (baseRadius < 0f || baseRadius > 1f || float.IsNaN(baseRadius)) throw new ArgumentOutOfRangeException(nameof(baseRadius));
        var vertices = new Vector3[(segments + 1) * 2];
        for (int row = 0; row <= segments; row++)
        {
            float t = row / (float)segments;
            float radius = Mathf.Lerp(baseRadius, 1f, Mathf.Pow(t, 0.8f));
            float bend = Mathf.Sin(t * 7f) * t * 0.035f;
            vertices[row * 2] = new Vector3(bend - radius, t, 0);
            vertices[row * 2 + 1] = new Vector3(bend + radius, t, 0);
        }
        return vertices;
    }

    /// <summary>Creates triangles that share each row with its neighbor, forming one connected surface.</summary>
    public static int[] Triangles(int segments)
    {
        Validate(segments);
        var triangles = new int[segments * 6];
        for (int row = 0; row < segments; row++)
        {
            int vertex = row * 2, index = row * 6;
            triangles[index] = vertex;
            triangles[index + 1] = vertex + 3;
            triangles[index + 2] = vertex + 1;
            triangles[index + 3] = vertex;
            triangles[index + 4] = vertex + 2;
            triangles[index + 5] = vertex + 3;
        }
        return triangles;
    }

    /// <summary>Deforms an existing surface without allocations, keeping ground tip and cloud crown anchored.</summary>
    public static void Deform(Vector3[] vertices, int segments, float baseRadius, float bend, float depth, float phase)
    {
        Validate(segments);
        if (vertices == null || vertices.Length != (segments + 1) * 2) throw new ArgumentException(nameof(vertices));
        for (int row = 0; row <= segments; row++)
        {
            float t = row / (float)segments;
            float radius = baseRadius + (1f - baseRadius) * (float)Math.Pow(t, 0.8f);
            float envelope = row == 0 || row == segments ? 0f : (float)Math.Sin(Math.PI * t);
            float x = envelope * bend * (float)Math.Sin(phase + t * 5f);
            float z = envelope * depth * (float)Math.Cos(phase * 0.7f + t * 4f);
            vertices[row * 2] = new Vector3(x - radius, t, z);
            vertices[row * 2 + 1] = new Vector3(x + radius, t, z);
        }
    }

    private static void Validate(int segments)
    {
        if (segments < 2 || segments > 32) throw new ArgumentOutOfRangeException(nameof(segments));
    }
}
