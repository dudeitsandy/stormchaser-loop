using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

/// <summary>Fixed mesh atlas batch: camera streaks and two cosmetic rain curtains per live cell.</summary>
public sealed class RainCardVfx : MonoBehaviour
{
    [SerializeField, Range(0, 300)] private int _streakCapacity = 240;
    [SerializeField, Min(1f)] private float _fallSpeed = 16f;
    [SerializeField, Min(1f)] private float _cameraRadius = 12f;
    private const int CurtainCapacity = 16;
    private static readonly ProfilerMarker CameraMarker = new ProfilerMarker("Presentation.Rain.Camera");
    private StormDirector _director;
    private PlayerVehicle _vehicle;
    private Camera _mainCamera;
    private CardVfxAssets _assets;
    private Transform _batch;
    private Mesh _mesh;
    private Vector3[] _vertices, _seeds;
    private Vector2[] _uv;
    private Color[] _colors;
    private int _capacity;
    private readonly Dictionary<int, TornadoCardVisual> _visuals = new Dictionary<int, TornadoCardVisual>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);
    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<RainCardVfx>() != null) return;
        new GameObject(nameof(RainCardVfx)).AddComponent<RainCardVfx>();
    }
    private void Awake()
    {
        _capacity = Mathf.Clamp(_streakCapacity, 0, 300);
        int quads = _capacity + CurtainCapacity;
        _vertices = new Vector3[quads * 4]; _uv = new Vector2[_vertices.Length]; _colors = new Color[_vertices.Length];
        _seeds = new Vector3[_capacity];
        var random = new System.Random(803);
        for (int i = 0; i < _capacity; i++)
            _seeds[i] = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
        var triangles = new int[quads * 6];
        for (int i = 0; i < quads; i++)
        {
            int v = i * 4, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }
        _mesh = new Mesh { name = "RainAtlasBatch", vertices = _vertices, uv = _uv, colors = _colors, triangles = triangles };
        _mesh.MarkDynamic();
        _assets = new CardVfxAssets();
        _batch = _assets.Create(transform, CardVfxAssets.Shape.Rain, "RainCurtainsAndStreaks", _mesh);
        if (_batch != null) _batch.gameObject.SetActive(false);
    }
    private void Start()
    {
        _vehicle = FindAnyObjectByType<PlayerVehicle>(); _mainCamera = Camera.main;
        var spawner = FindAnyObjectByType<DisasterSpawner>(); _director = spawner != null ? spawner.Director : null;
    }
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnCamera;
        GameEvents.RunStarted += Clear;
        GameEvents.RunEnded += EndRun;
    }
    private void Clear() { _visuals.Clear(); if (_batch != null) _batch.gameObject.SetActive(false); }
    private void EndRun(RunSummary summary) => Clear();
    private void OnCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != _mainCamera && camera.name != "PhotoPreviewCamera") return;
        using var marker = CameraMarker.Auto();
        if (_batch == null) return;
        if (_vehicle == null || !_vehicle.InputEnabled || _mainCamera == null) { _batch.gameObject.SetActive(false); return; }
        int count = RainLevels.StreakCount(StormEnvironmentCues.Storminess, _capacity);
        Vector3 wind = StormEnvironmentCues.WindBearing * StormEnvironmentCues.Exposure * 20f;
        Vector3 fall = (Vector3.down + wind * 0.035f).normalized;
        int used = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 seed = _seeds[i];
            Vector3 offset = new Vector3((seed.x - 0.5f) * _cameraRadius * 2f,
                Mathf.Repeat(seed.y * 16f - Time.time * _fallSpeed, 16f) - 6f, (seed.z - 0.5f) * _cameraRadius * 2f);
            Vector3 center = _mainCamera.transform.position + offset;
            // Avoid drawing ground-penetrating streaks across the truck's immediate foreground.
            if (center.y < 0.2f) continue;
            Quad(used++, center - fall * 0.45f, center + fall * 0.45f, camera.transform.right * 0.017f, 0.42f, false, 0f);
        }
        if (_director != null)
        {
            var cells = _director.LiveCells;
            int curtains = 0;
            for (int i = 0; i < cells.Count && curtains < CurtainCapacity; i++)
            {
                var cell = cells[i];
                if (cell.Tornado == null || cell.Ended) continue;
                if (!_visuals.TryGetValue(cell.Cell.Id, out var visual))
                {
                    visual = cell.Tornado.GetComponent<TornadoCardVisual>(); _visuals[cell.Cell.Id] = visual;
                }
                float height = visual != null ? visual.CloudBaseHeight : 26.2f;
                float opacity = RainLevels.CurtainOpacity(cell.Intensity);
                if (opacity <= 0f) continue;
                float halfWidth = (visual != null ? visual.CloudBaseWidth : 8f) * 0.25f;
                float heading = cell.Track.HeadingDeg * Mathf.Deg2Rad;
                Vector3 trail = new Vector3(-Mathf.Sin(heading), 0f, -Mathf.Cos(heading)) * Mathf.Min(7f, height * 0.15f);
                Vector3 right = camera.transform.right; right.y = 0f; right.Normalize();
                for (int side = -1; side <= 1 && curtains < CurtainCapacity; side += 2)
                {
                    Vector3 top = cell.Position + Vector3.up * height + right * (side * halfWidth);
                    Quad(used++, top, top + Vector3.down * (height - 0.3f) + trail, right * halfWidth,
                        opacity, true, Mathf.Repeat(Time.time * 0.6f, 1f));
                    curtains++;
                }
            }
        }
        for (int v = used * 4; v < _vertices.Length; v++) { _vertices[v] = Vector3.zero; _colors[v] = Color.clear; }
        _batch.gameObject.SetActive(used > 0);
        _mesh.vertices = _vertices; _mesh.uv = _uv; _mesh.colors = _colors; _mesh.RecalculateBounds();
    }
    private void Quad(int index, Vector3 top, Vector3 bottom, Vector3 halfWidth, float opacity, bool curtain, float scroll)
    {
        int v = index * 4;
        _vertices[v] = bottom - halfWidth; _vertices[v + 1] = bottom + halfWidth;
        _vertices[v + 2] = top + halfWidth; _vertices[v + 3] = top - halfWidth;
        float left = curtain ? 0.51f : 0.01f, right = curtain ? 0.99f : 0.49f;
        _uv[v] = new Vector2(left, scroll); _uv[v + 1] = new Vector2(right, scroll);
        _uv[v + 2] = new Vector2(right, scroll + 1f); _uv[v + 3] = new Vector2(left, scroll + 1f);
        for (int j = 0; j < 4; j++) _colors[v + j] = new Color(0.72f, 0.86f, 0.85f, opacity);
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnCamera;
        GameEvents.RunStarted -= Clear; GameEvents.RunEnded -= EndRun; Clear();
    }
    private void OnDestroy() { _assets?.Dispose(); if (_mesh != null) Destroy(_mesh); }
}
