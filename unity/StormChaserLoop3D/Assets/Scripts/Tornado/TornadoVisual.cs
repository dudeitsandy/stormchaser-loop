using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a procedural funnel cone mesh (tip at ground, wide at top).
/// Add to the Tornado prefab alongside TornadoController.
/// Remove any existing cylinder MeshFilter/MeshRenderer before adding this.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TornadoVisual : MonoBehaviour
{
    [SerializeField] private int _segments = 16;
    [SerializeField] private float _baseTopRadius = 2f;
    [SerializeField] private float _baseHeight = 6f;

    private void Awake()
    {
        var controller = GetComponent<TornadoController>();
        float scale = controller != null ? controller.ConeScale : 1f;
        GetComponent<MeshFilter>().mesh = BuildFunnelMesh(_baseTopRadius * scale, _baseHeight * scale, _segments);
    }

    private static Mesh BuildFunnelMesh(float topRadius, float height, int segments)
    {
        var mesh = new Mesh { name = "TornadoFunnel" };

        // Layout: [0..segments-1] top ring, [segments] top center,
        //         [segments+1..segments*2] bottom ring, [segments*2+1] bottom apex
        int topRingStart = 0;
        int topCenter = segments;
        int botRingStart = segments + 1;
        int botApex = segments * 2 + 1;
        int vertCount = segments * 2 + 2;

        var verts = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        float tipRadius = topRadius * 0.04f;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float t = (float)i / segments;

            verts[topRingStart + i] = new Vector3(cos * topRadius, height, sin * topRadius);
            uvs[topRingStart + i] = new Vector2(t, 1f);

            verts[botRingStart + i] = new Vector3(cos * tipRadius, 0f, sin * tipRadius);
            uvs[botRingStart + i] = new Vector2(t, 0f);
        }

        verts[topCenter] = new Vector3(0f, height, 0f);
        uvs[topCenter] = new Vector2(0.5f, 1f);

        verts[botApex] = new Vector3(0f, 0f, 0f);
        uvs[botApex] = new Vector2(0.5f, 0f);

        var tris = new List<int>();

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            // Side quads: top ring → bottom ring
            tris.Add(topRingStart + i);
            tris.Add(botRingStart + i);
            tris.Add(topRingStart + next);

            tris.Add(topRingStart + next);
            tris.Add(botRingStart + i);
            tris.Add(botRingStart + next);

            // Top cap
            tris.Add(topCenter);
            tris.Add(topRingStart + next);
            tris.Add(topRingStart + i);

            // Bottom apex
            tris.Add(botApex);
            tris.Add(botRingStart + i);
            tris.Add(botRingStart + next);
        }

        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
