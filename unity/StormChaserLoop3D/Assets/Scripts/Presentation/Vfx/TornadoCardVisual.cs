using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

/// <summary>One continuous illustrated funnel surface with orbiting dust and debris cards.</summary>
[RequireComponent(typeof(TornadoController))]
public sealed class TornadoCardVisual : MonoBehaviour
{
    // Preserve the prefab field; its value now controls continuous mesh tessellation.
    [InspectorName("Funnel Segments")]
    [SerializeField, Range(6, 20)] private int _bandCount = 12;
    [SerializeField, Range(4, 20)] private int _dustCount = 8;
    [SerializeField, Range(0, 24)] private int _debrisCount = 12;
    [SerializeField, Min(0.1f)] private float _height = 6f;
    [SerializeField, Min(0.1f)] private float _topRadius = 2f;
    [SerializeField] private float _swirlSpeed = 2.5f;
    [SerializeField] private Color _funnelColor = new Color(0.62f, 0.72f, 0.77f, 0.95f);
    [SerializeField] private Color _dustColor = new Color(0.72f, 0.57f, 0.35f, 0.5f);
    private TornadoController _controller;
    private Transform _root;
    private Transform _mass;
    private Transform[] _dust, _debris;
    private CardVfxAssets _assets;
    private MeshRenderer _massRenderer;
    private MaterialPropertyBlock _massProperties;
    private float _age;
    private string _rating;
    private bool _previewWedge;
    private Transform _previewMass;
    private Transform[] _subVortices;
    private Transform _previewDustRing;
    private Mesh _previewRingMesh;
    private StormParticleBatch _previewDebris, _previewGust;
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.Tornado.Update");
    private static readonly ProfilerMarker CameraMarker = new ProfilerMarker("Presentation.Tornado.Camera");

    /// <summary>Opt-in visual-only EF5 spike: D=12, 24 m crown, 20 m height; never changes gameplay.</summary>
    public void PreviewEf5Wedge(bool enabled)
    {
        _previewWedge = enabled;
        if (enabled && _previewMass == null)
        {
            _previewMass = _assets.CreateFunnel(_root, Mathf.Clamp(_bandCount, 6, 20), 0.85f);
            _subVortices = Build(3, CardVfxAssets.Shape.Funnel, "SpikeSubVortex");
            _previewRingMesh = BuildSpikeDustRing();
            _previewDustRing = _assets.Create(_root, CardVfxAssets.Shape.Dust, "SpikeDustRing", _previewRingMesh);
            _previewDebris = new StormParticleBatch(_assets, _root, 1200, false);
            _previewGust = new StormParticleBatch(_assets, _root, 500, true);
            _assets.SetColor(CardVfxAssets.Shape.Streak, new Color(0.7f, 0.8f, 0.75f, 0.12f));
        }
        if (_mass != null) _mass.gameObject.SetActive(!enabled);
        if (_previewMass != null) _previewMass.gameObject.SetActive(enabled);
        if (_subVortices != null) foreach (var card in _subVortices) if (card != null) card.gameObject.SetActive(enabled);
        if (_previewDustRing != null) _previewDustRing.gameObject.SetActive(enabled);
        if (_previewDebris?.Transform != null) _previewDebris.Transform.gameObject.SetActive(enabled);
        if (_previewGust?.Transform != null) _previewGust.Transform.gameObject.SetActive(enabled);
        foreach (var card in _dust) if (card != null) card.gameObject.SetActive(!enabled);
        foreach (var card in _debris) if (card != null) card.gameObject.SetActive(!enabled);
    }

    private static Mesh BuildSpikeDustRing()
    {
        const int segments = 32;
        var vertices = new Vector3[(segments + 1) * 2];
        var uv = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 bearing = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices[i * 2] = bearing * 12f + Vector3.up * 0.1f;
            vertices[i * 2 + 1] = bearing * 35f + Vector3.up * 1.2f;
            uv[i * 2] = new Vector2(i / (float)segments, 0);
            uv[i * 2 + 1] = new Vector2(i / (float)segments, 1);
            colors[i * 2] = colors[i * 2 + 1] = Color.white;
            if (i == segments) continue;
            int t = i * 6, v = i * 2;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        var mesh = new Mesh { name = "SpikeDustRing12to35m", vertices = vertices, uv = uv, colors = colors, triangles = triangles };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void Awake()
    {
        _controller = GetComponent<TornadoController>();
        _assets = new CardVfxAssets();
        _root = new GameObject("IllustratedFunnel").transform;
        _root.SetParent(transform, false);
        _mass = _assets.CreateFunnel(_root, Mathf.Clamp(_bandCount, 6, 20));
        if (_mass != null) _massRenderer = _mass.GetComponent<MeshRenderer>();
        _massProperties = new MaterialPropertyBlock();
        _dust = Build(Mathf.Clamp(_dustCount, 4, 20), CardVfxAssets.Shape.Dust, "DustSkirt");
        _debris = Build(Mathf.Clamp(_debrisCount, 0, 24), CardVfxAssets.Shape.Debris, "FlyingDebris");
    }
    private Transform[] Build(int count, CardVfxAssets.Shape shape, string name)
    {
        var cards = new Transform[count];
        for (int i = 0; i < count; i++) cards[i] = _assets.Create(_root, shape, name);
        return cards;
    }
    private void OnEnable() => RenderPipelineManager.beginCameraRendering += OnCamera;
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnCamera;
        if (_root != null) _root.gameObject.SetActive(false);
    }
    private void LateUpdate()
    {
        using var sample = UpdateMarker.Auto();
        _age += Time.deltaTime;
        float intensity = _previewWedge ? 1f : Mathf.Clamp01(_controller.Intensity);
        _root.gameObject.SetActive(intensity > 0.01f);
        // Controller changes its parent's scale every frame. Author dimensions in world units,
        // so intensity and EF size are applied once rather than multiplied by that scale again.
        Vector3 inherited = transform.lossyScale;
        _root.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(inherited.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(inherited.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(inherited.z)));
        _root.rotation = Quaternion.identity;
        float size = Mathf.Max(0.1f, _controller.ConeScale);
        float growth = Mathf.Lerp(0.05f, 1f, intensity);
        float height = _previewWedge ? 20f : _height * size * growth;
        float radius = _previewWedge ? 12f : _topRadius * size * growth * growth;
        Color funnel = _previewWedge ? new Color(0.12f, 0.2f, 0.18f, 0.96f) : _funnelColor;
        funnel.a *= Mathf.Clamp01(intensity * 3f);
        _assets.SetColor(CardVfxAssets.Shape.Funnel, funnel);
        if (_mass != null)
        {
            _mass.localScale = new Vector3(radius, height, 1);
            _massProperties.SetVector("_BaseMap_ST", new Vector4(1, 1, 0,
                Mathf.Repeat(_age * _swirlSpeed * 0.08f, 1)));
            _massRenderer.SetPropertyBlock(_massProperties);
        }
        if (_previewWedge && _previewMass != null)
        {
            _previewMass.localScale = new Vector3(radius, height, 1);
            for (int i = 0; i < _subVortices.Length; i++)
            {
                if (_subVortices[i] == null) continue;
                float angle = _age * _swirlSpeed * 0.4f + i * Mathf.PI * 2f / 3f;
                _subVortices[i].localPosition = new Vector3(Mathf.Cos(angle) * radius * 0.45f, 0, Mathf.Sin(angle) * radius * 0.45f);
                _subVortices[i].localScale = new Vector3(1.5f, height * 0.85f, 1);
            }
        }
        Color dust = _dustColor;
        dust.a *= intensity;
        _assets.SetColor(CardVfxAssets.Shape.Dust, dust);
        _assets.SetColor(CardVfxAssets.Shape.Debris, new Color(0.56f, 0.34f, 0.18f, intensity));
        for (int i = 0; i < _dust.Length; i++)
        {
            if (_dust[i] == null) continue;
            float angle = i * Mathf.PI * 2f / _dust.Length + _age * _swirlSpeed * 0.5f;
            float r = _previewWedge ? 28f : radius * (0.6f + 0.2f * Mathf.Sin(_age * 2f + i));
            _dust[i].localPosition = new Vector3(Mathf.Cos(angle) * r, radius * 0.12f, Mathf.Sin(angle) * r);
            _dust[i].localScale = _previewWedge ? new Vector3(14f, 4f, 1) : new Vector3(radius * 1.3f, radius * 0.55f, 1);
        }
        for (int i = 0; i < _debris.Length; i++)
        {
            if (_debris[i] == null) continue;
            float t = Mathf.Repeat(i * 0.618f + _age * 0.12f, 1);
            float angle = _age * _swirlSpeed * (1f + i * 0.03f) + i * 2.4f;
            float r = _previewWedge ? 14.4f : radius * Mathf.Lerp(0.7f, 1.5f, t);
            _debris[i].localPosition = new Vector3(Mathf.Cos(angle) * r, t * (_previewWedge ? 18f : height * 0.8f), Mathf.Sin(angle) * r);
            _debris[i].localScale = Vector3.one * Mathf.Clamp(size * growth * (0.12f + i % 3 * 0.04f), 0.04f, 0.5f);
        }
        // EF is read from the same controller, with no independent rating or gameplay state.
        string rating = _controller.EFRating;
        if (rating != _rating)
        {
            _rating = rating;
            _root.gameObject.name = "IllustratedFunnel " + rating;
        }
    }
    private void OnCamera(ScriptableRenderContext context, Camera camera)
    {
        using var sample = CameraMarker.Auto();
        if (_root == null || !_root.gameObject.activeInHierarchy) return;
        if (_mass != null) CardVfxAssets.FaceCamera(_mass, camera);
        if (_previewWedge && _previewMass != null) CardVfxAssets.FaceCamera(_previewMass, camera);
        if (_previewWedge && _subVortices != null)
            foreach (var card in _subVortices) if (card != null) CardVfxAssets.FaceCamera(card, camera);
        if (_previewWedge)
        {
            _previewDebris?.FaceCamera(camera, _age);
            _previewGust?.FaceCamera(camera, _age);
        }
        foreach (var card in _dust) if (card != null) CardVfxAssets.FaceCamera(card, camera);
        for (int i = 0; i < _debris.Length; i++)
            if (_debris[i] != null) CardVfxAssets.FaceCamera(_debris[i], camera, _age * 150f + i * 40f);
    }
    private void OnDestroy()
    {
        _previewDebris?.Dispose();
        _previewGust?.Dispose();
        if (_previewRingMesh != null) Destroy(_previewRingMesh);
        if (_root != null) Destroy(_root.gameObject);
        _assets?.Dispose();
    }
}
