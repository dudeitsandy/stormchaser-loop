using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Illustrated funnel bands, dust skirt and orbiting debris; attach beside TornadoController.</summary>
[RequireComponent(typeof(TornadoController))]
public sealed class TornadoCardVisual : MonoBehaviour
{
    [SerializeField, Range(6, 20)] private int _bandCount = 12;
    [Tooltip("Card height in band spacings; larger values merge the feathered bands into a continuous funnel.")]
    [SerializeField, Range(2.5f, 5f)] private float _bandOverlap = 3.6f;
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
    private float _massHeight, _massDepth;
    private Transform[] _bands, _dust, _debris;
    private CardVfxAssets _assets;
    private MeshRenderer[] _bandRenderers;
    private MaterialPropertyBlock _bandProperties;
    private float _age;
    private string _rating;

    private void Awake()
    {
        _controller = GetComponent<TornadoController>();
        _assets = new CardVfxAssets();
        _root = new GameObject("IllustratedFunnel").transform;
        _root.SetParent(transform, false);
        _mass = _assets.Create(_root, CardVfxAssets.Shape.Funnel, "ContinuousFunnelMass");
        _bands = Build(Mathf.Clamp(_bandCount, 6, 20), CardVfxAssets.Shape.Band, "FunnelBand");
        _bandProperties = new MaterialPropertyBlock();
        _bandRenderers = new MeshRenderer[_bands.Length];
        for (int i = 0; i < _bands.Length; i++)
            if (_bands[i] != null) _bandRenderers[i] = _bands[i].GetComponent<MeshRenderer>();
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
        _age += Time.deltaTime;
        float intensity = Mathf.Clamp01(_controller.Intensity);
        _root.gameObject.SetActive(intensity > 0.01f);
        // Controller changes its parent's scale every frame. Author dimensions in world units,
        // so intensity and EF size are applied once rather than multiplied by that scale again.
        Vector3 inherited = transform.lossyScale;
        _root.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(inherited.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(inherited.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(inherited.z)));
        _root.rotation = Quaternion.identity;
        float size = Mathf.Max(0.1f, _controller.ConeScale);
        float growth = Mathf.Lerp(0.05f, 1f, intensity);
        float height = _height * size * growth;
        float radius = _topRadius * size * growth * growth;
        Color funnel = _funnelColor;
        funnel.a *= Mathf.Clamp01(intensity * 3f);
        _assets.SetColor(CardVfxAssets.Shape.Funnel, funnel);
        // The unscrolled tapered fill maintains the silhouette while translucent bands move.
        _massHeight = height * 0.54f;
        _massDepth = radius * 0.2f;
        if (_mass != null) _mass.localScale = new Vector3(radius * 2f, height, 1);
        funnel.a *= 0.6f;
        _assets.SetColor(CardVfxAssets.Shape.Band, funnel);
        Color dust = _dustColor;
        dust.a *= intensity;
        _assets.SetColor(CardVfxAssets.Shape.Dust, dust);
        _assets.SetColor(CardVfxAssets.Shape.Debris, new Color(0.56f, 0.34f, 0.18f, intensity));
        for (int i = 0; i < _bands.Length; i++)
        {
            if (_bands[i] == null) continue;
            float t = i / (float)(_bands.Length - 1);
            float width = radius * Mathf.Lerp(0.08f, 2f, Mathf.Pow(t, 0.8f));
            float phase = _age * _swirlSpeed + t * 8f;
            _bands[i].localPosition = new Vector3(Mathf.Sin(phase) * radius * 0.12f * t,
                t * height + height * 0.04f, Mathf.Cos(phase) * radius * 0.12f * t);
            float spacing = height / (_bands.Length - 1);
            _bands[i].localScale = new Vector3(width, spacing * Mathf.Clamp(_bandOverlap, 2.5f, 5f), 1);
            _bandProperties.SetVector("_BaseMap_ST", new Vector4(1, 1,
                Mathf.Repeat(_age * 0.2f + i * 0.17f, 1), 0));
            _bandRenderers[i].SetPropertyBlock(_bandProperties);
        }
        for (int i = 0; i < _dust.Length; i++)
        {
            if (_dust[i] == null) continue;
            float angle = i * Mathf.PI * 2f / _dust.Length + _age * _swirlSpeed * 0.5f;
            float r = radius * (0.6f + 0.2f * Mathf.Sin(_age * 2f + i));
            _dust[i].localPosition = new Vector3(Mathf.Cos(angle) * r, radius * 0.12f, Mathf.Sin(angle) * r);
            _dust[i].localScale = new Vector3(radius * 1.3f, radius * 0.55f, 1);
        }
        for (int i = 0; i < _debris.Length; i++)
        {
            if (_debris[i] == null) continue;
            float t = Mathf.Repeat(i * 0.618f + _age * 0.12f, 1);
            float angle = _age * _swirlSpeed * (1f + i * 0.03f) + i * 2.4f;
            float r = radius * Mathf.Lerp(0.7f, 1.5f, t);
            _debris[i].localPosition = new Vector3(Mathf.Cos(angle) * r, t * height * 0.8f, Mathf.Sin(angle) * r);
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
        if (_root == null || !_root.gameObject.activeInHierarchy) return;
        if (_mass != null)
        {
            // Place fill behind the animated cards for each view, including the PiP camera.
            _mass.position = _root.position + Vector3.up * _massHeight + camera.transform.forward * _massDepth;
            CardVfxAssets.FaceCamera(_mass, camera);
        }
        for (int i = 0; i < _bands.Length; i++)
            if (_bands[i] != null) CardVfxAssets.FaceCamera(_bands[i], camera, Mathf.Sin(_age * _swirlSpeed + i) * 7f);
        foreach (var card in _dust) if (card != null) CardVfxAssets.FaceCamera(card, camera);
        for (int i = 0; i < _debris.Length; i++)
            if (_debris[i] != null) CardVfxAssets.FaceCamera(_debris[i], camera, _age * 150f + i * 40f);
    }
    private void OnDestroy()
    {
        if (_root != null) Destroy(_root.gameObject);
        _assets?.Dispose();
    }
}
