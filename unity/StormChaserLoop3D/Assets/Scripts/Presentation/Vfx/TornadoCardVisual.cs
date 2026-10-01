using UnityEngine;
using UnityEngine.Rendering;

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
        if (_mass != null)
        {
            _mass.localScale = new Vector3(radius, height, 1);
            _massProperties.SetVector("_BaseMap_ST", new Vector4(1, 1, 0,
                Mathf.Repeat(_age * _swirlSpeed * 0.08f, 1)));
            _massRenderer.SetPropertyBlock(_massProperties);
        }
        Color dust = _dustColor;
        dust.a *= intensity;
        _assets.SetColor(CardVfxAssets.Shape.Dust, dust);
        _assets.SetColor(CardVfxAssets.Shape.Debris, new Color(0.56f, 0.34f, 0.18f, intensity));
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
        if (_mass != null) CardVfxAssets.FaceCamera(_mass, camera);
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
