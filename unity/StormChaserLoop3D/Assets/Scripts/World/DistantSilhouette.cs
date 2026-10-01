using UnityEngine;

/// <summary>
/// ADR-0004 horizon: one low-resolution mesh of the whole 2 km map (32 m grid, no collider), sunk slightly
/// below the live tiles so it only shows beyond the streamed ring. Built once at start.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DistantSilhouette : MonoBehaviour
{
    [SerializeField] private TileStreamer _streamer;
    [SerializeField] private float _spacing = 32f;
    [Tooltip("How far below the true surface the silhouette sits, so live tiles always win.")]
    [SerializeField] private float _sink = 1.5f;

    private void Start()
    {
        int seed = _streamer != null ? _streamer.Seed : 1996;
        int n = Mathf.RoundToInt(HeightField.WorldSize / _spacing) + 1;
        var verts = new Vector3[n * n];
        for (int j = 0; j < n; j++)
        for (int i = 0; i < n; i++)
        {
            float x = -HeightField.HalfExtent + i * _spacing, z = -HeightField.HalfExtent + j * _spacing;
            verts[j * n + i] = new Vector3(x, HeightField.Height(seed, x, z) - _sink, z);
        }

        var tris = new int[(n - 1) * (n - 1) * 6];
        int k = 0;
        for (int j = 0; j < n - 1; j++)
        for (int i = 0; i < n - 1; i++)
        {
            int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
            tris[k++] = a; tris[k++] = c; tris[k++] = b;
            tris[k++] = b; tris[k++] = c; tris[k++] = d;
        }

        var mesh = new Mesh { name = "DistantSilhouette", vertices = verts, triangles = tris };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        transform.position = Vector3.zero;
    }
}
