using UnityEngine;

/// <summary>Diagnostic-only fixed mesh batch for the EF5 particle-capacity probe; no per-particle GameObjects.</summary>
internal sealed class StormParticleBatch
{
    internal readonly Transform Transform;
    private readonly Mesh _mesh;
    private readonly Vector3[] _vertices;
    private readonly int _count;
    private readonly bool _gust;

    internal StormParticleBatch(CardVfxAssets assets, Transform parent, int count, bool gust)
    {
        _count = Mathf.Clamp(count, 1, 2000);
        _gust = gust;
        _vertices = new Vector3[_count * 4];
        var uv = new Vector2[_vertices.Length];
        var colors = new Color[_vertices.Length];
        var triangles = new int[_count * 6];
        for (int i = 0; i < _count; i++)
        {
            int v = i * 4, t = i * 6;
            uv[v] = Vector2.zero; uv[v + 1] = Vector2.right; uv[v + 2] = Vector2.one; uv[v + 3] = Vector2.up;
            for (int j = 0; j < 4; j++) colors[v + j] = Color.white;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }
        _mesh = new Mesh { name = gust ? "EF5GustBatch500" : "EF5DebrisBatch1200" };
        _mesh.MarkDynamic();
        _mesh.vertices = _vertices;
        _mesh.uv = uv;
        _mesh.colors = colors;
        _mesh.triangles = triangles;
        _mesh.bounds = new Bounds(new Vector3(0, 9, 0), new Vector3(80, 40, 80));
        Transform = assets.Create(parent, gust ? CardVfxAssets.Shape.Streak : CardVfxAssets.Shape.Debris, _mesh.name, _mesh);
    }

    internal void FaceCamera(Camera camera, float age)
    {
        if (Transform == null) return;
        Vector3 right = Transform.InverseTransformDirection(camera.transform.right);
        Vector3 up = Transform.InverseTransformDirection(camera.transform.up);
        for (int i = 0; i < _count; i++)
        {
            float phase = Mathf.Repeat(i * 0.6180339f + age * (_gust ? 0.35f : 0.08f), 1f);
            float angle = i * 2.399963f + age * (0.7f + i % 5 * 0.08f);
            float radius = _gust ? Mathf.Lerp(16f, 35f, phase) : 14.4f + Mathf.Sin(i * 7f) * 1.8f;
            Vector3 center = new Vector3(Mathf.Cos(angle) * radius, phase * (_gust ? 8f : 18f), Mathf.Sin(angle) * radius);
            float size = _gust ? 1.2f : i % 3 == 0 ? 0.55f : i % 3 == 1 ? 0.3f : 0.15f;
            Vector3 horizontal = right * size * (_gust ? 2.5f : 1f);
            Vector3 vertical = up * size * (_gust ? 0.35f : 0.65f);
            int v = i * 4;
            _vertices[v] = center - horizontal - vertical;
            _vertices[v + 1] = center + horizontal - vertical;
            _vertices[v + 2] = center + horizontal + vertical;
            _vertices[v + 3] = center - horizontal + vertical;
        }
        _mesh.vertices = _vertices;
    }

    internal void Dispose() => Object.Destroy(_mesh);
}
