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
    [Tooltip("Legacy prefab height floor in metres; no longer multiplied by EF scale. Cloud clearance below controls normal height.")]
    [SerializeField, Min(0.1f)] private float _height = 6f;
    [Tooltip("Lowest cloud edge above the funnel's ground base, independent of EF width.")]
    [SerializeField, Min(25f)] private float _minimumCloudClearance = 25f;
    [SerializeField, Min(0f)] private float _cloudHeightVariation = 18.8f;
    [SerializeField, Min(0.5f)] private float _cloudThickness = 2.4f;
    [SerializeField, Range(0f, 0.3f)] private float _funnelBend = 0.1f;
    [Tooltip("Width tuning: 2 preserves the damage-radius scale; cloud height never widens the funnel.")]
    [SerializeField, Min(0.1f)] private float _topRadius = 2f;
    [SerializeField] private float _swirlSpeed = 2.5f;
    [SerializeField] private Color _funnelColor = new Color(0.62f, 0.72f, 0.77f, 0.95f);
    [SerializeField] private Color _dustColor = new Color(0.72f, 0.57f, 0.35f, 0.5f);
    [SerializeField] private Color _cloudColor = new Color(0.22f, 0.28f, 0.29f, 0.85f);
    private TornadoController _controller;
    private Transform _root;
    private Transform _mass;
    private Transform _cloud;
    private bool _touchedDown;
    private int _cellId = -1;
    private bool _cellPeak, _cellRope, _cellEnded, _failedTouchdown;
    private float _failedLength, _failedIntensity;
    private TornadoLifecycle.Phase _lastPhase;
    private float _ropeAge;
    private Vector3[] _debrisDropOrigins;
    private float _visualHeight, _visualRadius, _cloudHeight, _ropeTilt;
    private float _heightSample, _widthSample, _depthRatio, _shapePhase;
    private Mesh _funnelMesh;
    private Vector3[] _funnelVertices;
    private int _segments;
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
        if (_cloud != null) _cloud.gameObject.SetActive(!enabled);
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
        _segments = Mathf.Clamp(_bandCount, 6, 20);
        _mass = _assets.CreateFunnel(_root, _segments);
        Vector3 spawn = transform.position;
        _heightSample = FunnelVisualDimensions.Variation(spawn.x, spawn.z, 71);
        _widthSample = FunnelVisualDimensions.Variation(spawn.x, spawn.z, 73);
        _depthRatio = Mathf.Lerp(0.7f, 1.15f, FunnelVisualDimensions.Variation(spawn.x, spawn.z, 79));
        _shapePhase = FunnelVisualDimensions.Variation(spawn.x, spawn.z, 83) * Mathf.PI * 2f;
        if (_mass != null)
        {
            _funnelMesh = _mass.GetComponent<MeshFilter>().sharedMesh;
            _funnelVertices = new Vector3[(_segments + 1) * 2];
            _funnelMesh.MarkDynamic();
        }
        _cloud = _assets.Create(_root, CardVfxAssets.Shape.Cloud, "FunnelCloudBase");
        if (_mass != null) _massRenderer = _mass.GetComponent<MeshRenderer>();
        _massProperties = new MaterialPropertyBlock();
        _dust = Build(Mathf.Clamp(_dustCount, 4, 20), CardVfxAssets.Shape.Dust, "DustSkirt");
        _debris = Build(Mathf.Clamp(_debrisCount, 0, 24), CardVfxAssets.Shape.Debris, "FlyingDebris");
        _debrisDropOrigins = new Vector3[_debris.Length];
    }
    private Transform[] Build(int count, CardVfxAssets.Shape shape, string name)
    {
        var cards = new Transform[count];
        for (int i = 0; i < count; i++) cards[i] = _assets.Create(_root, shape, name);
        return cards;
    }
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnCamera;
        GameEvents.StormCellForming += CellForming;
        GameEvents.StormCellPeak += CellPeak;
        GameEvents.StormCellRopeOut += CellRopeOut;
        GameEvents.StormCellEnded += CellEnded;
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnCamera;
        GameEvents.StormCellForming -= CellForming;
        GameEvents.StormCellPeak -= CellPeak;
        GameEvents.StormCellRopeOut -= CellRopeOut;
        GameEvents.StormCellEnded -= CellEnded;
        if (_root != null) _root.gameObject.SetActive(false);
    }
    private void CellForming(StormCellInfo cell)
    {
        if (_cellId >= 0 || _controller.EFRating != "EF" + cell.EF) return;
        float distance = (transform.position - cell.Position).sqrMagnitude;
        if (distance > 1f) return;
        // Temporary association at spawn only. Prefer a future controller CellId query.
        foreach (var disaster in DisasterEntity.Active)
        {
            if (disaster == _controller || !(disaster is TornadoController other) || other.EFRating != _controller.EFRating) continue;
            if ((other.transform.position - cell.Position).sqrMagnitude <= distance + 0.0001f) return;
        }
        _cellId = cell.CellId;
        _cellPeak = _cellRope = _cellEnded = _failedTouchdown = _touchedDown = false;
    }
    private void CellPeak(StormCellInfo cell) { if (cell.CellId == _cellId && !_cellRope && !_cellEnded) _cellPeak = true; }
    private void CellRopeOut(StormCellInfo cell)
    {
        if (cell.CellId != _cellId || _cellRope || _cellEnded) return;
        _cellRope = true;
        _failedTouchdown = !_cellPeak;
        _failedLength = _cloudHeight > 0f ? _visualHeight / _cloudHeight : 0f;
        _failedIntensity = _controller.Intensity;
    }
    private void CellEnded(StormCellInfo cell)
    {
        if (cell.CellId != _cellId) return;
        _cellEnded = true;
        if (_root != null) _root.gameObject.SetActive(false);
    }
    private void LateUpdate()
    {
        using var sample = UpdateMarker.Auto();
        _age += Time.deltaTime;
        float intensity = _previewWedge ? 1f : Mathf.Clamp01(_controller.Intensity);
        var phase = _controller.Phase;
        if (!_previewWedge && _cellId >= 0)
            phase = _cellEnded ? TornadoLifecycle.Phase.Done : _cellRope ? TornadoLifecycle.Phase.Dissipating :
                _cellPeak ? TornadoLifecycle.Phase.Mature : TornadoLifecycle.Phase.Forming;
        if (!_previewWedge && phase == TornadoLifecycle.Phase.Dissipating)
        {
            if (_lastPhase != phase)
            {
                _ropeAge = 0;
                for (int i = 0; i < _debris.Length; i++)
                    if (_debris[i] != null) _debrisDropOrigins[i] = _debris[i].localPosition;
            }
            else _ropeAge += Time.deltaTime;
        }
        _lastPhase = phase;
        if (!_previewWedge && phase == TornadoLifecycle.Phase.Mature) _touchedDown = true;
        var lifecycle = FunnelLifecycleVisual.Evaluate(phase, intensity, _touchedDown, _controller.DamageRadius > 0f);
        if (!_previewWedge && _failedTouchdown && !_cellEnded)
            lifecycle = FunnelLifecycleVisual.FailedTouchdown(_failedLength, intensity, _failedIntensity);
        _root.gameObject.SetActive(_previewWedge || phase != TornadoLifecycle.Phase.Done);
        // Controller changes its parent's scale every frame. Author dimensions in world units,
        // so intensity and EF size are applied once rather than multiplied by that scale again.
        Vector3 inherited = transform.lossyScale;
        _root.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(inherited.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(inherited.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(inherited.z)));
        _root.rotation = Quaternion.identity;
        float size = Mathf.Max(0.1f, _controller.ConeScale);
        float growth = _previewWedge ? 1f : lifecycle.Width;
        _cloudHeight = FunnelVisualDimensions.CloudHeight(Mathf.Max(25f, Mathf.Max(_height, _minimumCloudClearance)),
            _cloudThickness, _cloudHeightVariation, _heightSample);
        bool wedge = _controller.EFRating == "EF5";
        float crownRadius = FunnelVisualDimensions.CrownRadius(size, _topRadius, _widthSample);
        float height = _previewWedge ? 20f : _cloudHeight * lifecycle.Length;
        float radius = _previewWedge ? 12f : crownRadius * growth;
        _visualHeight = height;
        _visualRadius = radius;
        _ropeTilt = lifecycle.Tilt;
        if (_cloud != null && !_previewWedge)
        {
            _cloud.localPosition = Vector3.up * _cloudHeight;
            _cloud.localScale = new Vector3(Mathf.Max(8f, crownRadius * 3f), _cloudThickness, 1);
            Color cloud = Color.Lerp(_funnelColor, _cloudColor, intensity);
            cloud.a = _cloudColor.a * (phase == TornadoLifecycle.Phase.Dissipating ? intensity : Mathf.Lerp(0.35f, 1f, intensity));
            _assets.SetColor(CardVfxAssets.Shape.Cloud, cloud);
        }
        Color funnel = _previewWedge ? new Color(0.12f, 0.2f, 0.18f, 0.96f) : _funnelColor;
        funnel.a *= Mathf.Clamp01(intensity * 3f);
        _assets.SetColor(CardVfxAssets.Shape.Funnel, funnel);
        if (_mass != null)
        {
            if (!_previewWedge && _funnelMesh != null)
            {
                FunnelSurfaceGeometry.Deform(_funnelVertices, _segments, wedge ? 0.85f : 0.04f,
                    _funnelBend, _funnelBend * _depthRatio * 0.6f, _shapePhase + _age * _swirlSpeed * 0.12f);
                _funnelMesh.vertices = _funnelVertices;
                _funnelMesh.RecalculateBounds();
            }
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
            _dust[i].gameObject.SetActive(!_previewWedge && lifecycle.GroundContact);
            float angle = i * Mathf.PI * 2f / _dust.Length + _age * _swirlSpeed * 0.5f;
            float r = _previewWedge ? 28f : radius * (0.6f + 0.2f * Mathf.Sin(_age * 2f + i));
            _dust[i].localPosition = new Vector3(Mathf.Cos(angle) * r, radius * 0.12f, Mathf.Sin(angle) * r);
            _dust[i].localScale = _previewWedge ? new Vector3(14f, 4f, 1) : new Vector3(radius * 1.3f, radius * 0.55f, 1);
        }
        for (int i = 0; i < _debris.Length; i++)
        {
            if (_debris[i] == null) continue;
            if (!_previewWedge && phase == TornadoLifecycle.Phase.Dissipating)
            {
                Vector3 dropped = _debrisDropOrigins[i] + Vector3.down * (4.9f * _ropeAge * _ropeAge);
                _debris[i].gameObject.SetActive(_touchedDown && _ropeAge < 1f && dropped.y > 0.1f);
                _debris[i].localPosition = dropped;
                continue;
            }
            _debris[i].gameObject.SetActive(!_previewWedge && lifecycle.GroundContact);
            float t = Mathf.Repeat(i * 0.618f + _age * 0.12f, 1);
            float angle = _age * _swirlSpeed * (1f + i * 0.03f) + i * 2.4f;
            float r = _previewWedge ? 14.4f : radius * Mathf.Lerp(0.7f, 1.5f, t);
            _debris[i].localPosition = new Vector3(Mathf.Cos(angle) * r, t * (_previewWedge ? 18f : Mathf.Min(height * 0.8f, radius * 3f)), Mathf.Sin(angle) * r);
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
        if (_mass != null && !_previewWedge)
        {
            // Keep the funnel vertical in world space regardless of the camera's downward pitch.
            Vector3 forward = camera.transform.forward;
            forward.y = 0;
            Quaternion facing = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
            _mass.rotation = facing * Quaternion.Euler(0, 0, _ropeTilt);
            _mass.position = _root.position + Vector3.up * _cloudHeight - _mass.up * _visualHeight;
            float bearing = Mathf.Atan2(forward.x, forward.z) + _shapePhase;
            float depth = _controller.EFRating == "EF5" ? Mathf.Max(1f, _depthRatio) : _depthRatio;
            _mass.localScale = new Vector3(FunnelVisualDimensions.ProjectedRadius(_visualRadius, depth, bearing), _visualHeight, _visualRadius);
        }
        if (_cloud != null && !_previewWedge)
        {
            // No billboard roll/pitch: neither camera tilt nor a wide cloud can drag an edge below clearance.
            Vector3 cloudForward = camera.transform.forward;
            cloudForward.y = 0;
            _cloud.rotation = cloudForward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(cloudForward, Vector3.up) : Quaternion.identity;
        }
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
