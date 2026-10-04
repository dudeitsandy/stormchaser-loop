using UnityEngine;
using Unity.Profiling;

/// <summary>One cached exposure drives world lighting, wind cards and audio. Owns only runtime sky copies.</summary>
[DefaultExecutionOrder(-500)]
public sealed class StormEnvironmentCues : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _tealShift = 0.35f;
    [SerializeField] private Color _teal = new Color(0.3f, 0.55f, 0.48f, 1f);
    [SerializeField, Min(0.1f)] private float _stormRiseSeconds = 5f;
    [SerializeField, Min(0.1f)] private float _stormFallSeconds = 20f;
    [SerializeField, Min(47f)] private float _deckHeight = 48f;
    [SerializeField, Min(180f)] private float _deckWidth = 440f;
    private StormDirector _director;
    private float _storminess, _ambientIntensity;
    private Color _ambientSky, _ambientEquator, _ambientGround, _ambientLight;
    private CardVfxAssets _deckAssets;
    private Transform _deck;
    private Mesh _deckMesh;
    private static StormEnvironmentCues _current;
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.StormCues.Update");
    private PlayerVehicle _vehicle;
    private Camera _camera;
    private Light _sun;
    private Material _originalSky, _sky;
    private Color _sunColor, _background;
    private float _sunIntensity;
    private bool _captured;
    private int[] _colorProperties;
    private Color[] _skyColors;
    private float _exposure, _ef5Exposure;
    private Vector3 _bearing;
    /// <summary>Current Rule 7 exposure, zero without an active running presentation.</summary>
    public static float Exposure => _current != null ? _current._exposure : 0f;
    /// <summary>Run-wide eased storminess, independent of player distance.</summary>
    public static float Storminess => _current != null ? _current._storminess : 0f;
    /// <summary>Exposure contributed by EF5 cells only, for the low rumble.</summary>
    public static float Ef5Exposure => _current != null ? _current._ef5Exposure : 0f;
    /// <summary>Strongest wind bearing; visual gusts do not cancel in opposing cells.</summary>
    public static Vector3 WindBearing => _current != null ? _current._bearing : Vector3.zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);
    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<StormEnvironmentCues>() != null) return;
        new GameObject(nameof(StormEnvironmentCues)).AddComponent<StormEnvironmentCues>();
    }
    private void OnEnable() { _current = this; GameEvents.RunEnded += End; }
    private void Start()
    {
        _vehicle = FindAnyObjectByType<PlayerVehicle>();
        _camera = Camera.main;
        var spawner = FindAnyObjectByType<DisasterSpawner>();
        _director = spawner != null ? spawner.Director : null;
        _ambientIntensity = RenderSettings.ambientIntensity;
        _ambientSky = RenderSettings.ambientSkyColor;
        _ambientEquator = RenderSettings.ambientEquatorColor;
        _ambientGround = RenderSettings.ambientGroundColor;
        _ambientLight = RenderSettings.ambientLight;
        _sun = RenderSettings.sun;
        if (_sun == null)
            foreach (var light in FindObjectsByType<Light>())
                if (light.type == LightType.Directional) { _sun = light; break; }
        if (_sun != null) { _sunColor = _sun.color; _sunIntensity = _sun.intensity; }
        if (_camera != null) _background = _camera.backgroundColor;
        _originalSky = RenderSettings.skybox;
        if (_originalSky != null)
        {
            _sky = new Material(_originalSky) { name = "StormSkyRuntime" };
            _colorProperties = new[] { Shader.PropertyToID("_TopColor"), Shader.PropertyToID("_HorizonColor"),
                Shader.PropertyToID("_BottomColor"), Shader.PropertyToID("_SunColor"), Shader.PropertyToID("_SkyTint"),
                Shader.PropertyToID("_GroundColor"), Shader.PropertyToID("_Tint") };
            _skyColors = new Color[_colorProperties.Length];
            for (int i = 0; i < _colorProperties.Length; i++)
                if (_sky.HasProperty(_colorProperties[i])) _skyColors[i] = _sky.GetColor(_colorProperties[i]);
            RenderSettings.skybox = _sky;
        }
        _captured = true;
        BuildDeck();
    }
    private void BuildDeck()
    {
        const int side = 9;
        var vertices = new Vector3[side * side];
        var uv = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var triangles = new int[(side - 1) * (side - 1) * 6];
        int index = 0;
        for (int z = 0; z < side; z++)
            for (int x = 0; x < side; x++)
            {
                int v = z * side + x;
                float u = x / (float)(side - 1), w = z / (float)(side - 1);
                vertices[v] = new Vector3((u - 0.5f) * _deckWidth,
                    3f * Mathf.Sin(u * Mathf.PI) * Mathf.Sin(w * Mathf.PI), (w - 0.5f) * _deckWidth);
                uv[v] = new Vector2(u * 3f, w * 3f);
                float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(w, 1f - w));
                colors[v] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge * 8f));
                if (x == side - 1 || z == side - 1) continue;
                triangles[index++] = v; triangles[index++] = v + 1; triangles[index++] = v + side;
                triangles[index++] = v + 1; triangles[index++] = v + side + 1; triangles[index++] = v + side;
            }
        _deckMesh = new Mesh { name = "StormOvercastDeck", vertices = vertices, uv = uv, colors = colors, triangles = triangles };
        _deckMesh.RecalculateBounds();
        _deckAssets = new CardVfxAssets();
        _deck = _deckAssets.Create(transform, CardVfxAssets.Shape.Overcast, "RunWideCloudDeck", _deckMesh);
        if (_deck != null) { _deck.localPosition = Vector3.up * _deckHeight; _deck.gameObject.SetActive(false); }
    }
    private Color Grade(Color original, float e)
    {
        // Preserve baseline luminance while adding a teal cast, then apply the bounded darkening.
        float value = Mathf.Max(original.r, Mathf.Max(original.g, original.b));
        float tealValue = Mathf.Max(_teal.r, Mathf.Max(_teal.g, _teal.b));
        Color shifted = Color.Lerp(original, _teal * (value / Mathf.Max(0.001f, tealValue)), e * _tealShift);
        shifted *= StormCueLevels.Brightness(e);
        shifted.a = original.a;
        return shifted;
    }
    private void Update()
    {
        using var marker = UpdateMarker.Auto();
        if (!_captured) return;
        if (_vehicle == null || !_vehicle.InputEnabled) { Restore(); return; }
        StormWindProvider.Sample(_vehicle.transform.position, out _exposure, out _bearing, out _ef5Exposure);
        float summed = 0f;
        if (_director != null)
        {
            var cells = _director.LiveCells;
            for (int i = 0; i < cells.Count; i++)
                summed += StormCueLevels.StorminessContribution(cells[i].Cell.Ef, cells[i].Intensity);
        }
        _storminess = StormCueLevels.EaseStorminess(_storminess, StormCueLevels.StorminessTarget(summed),
            Time.deltaTime, _stormRiseSeconds, _stormFallSeconds);
        float skyExposure = Mathf.Max(_exposure, _storminess);
        if (_deck != null)
        {
            _deck.gameObject.SetActive(_storminess > 0.005f);
            _deck.localScale = new Vector3(1f, Mathf.Lerp(0.3f, 1f, _storminess), 1f);
            _deckAssets.SetColor(CardVfxAssets.Shape.Overcast,
                new Color(0.065f, 0.13f, 0.115f, _storminess * 0.96f));
        }
        RenderSettings.ambientIntensity = _ambientIntensity * StormCueLevels.Brightness(_storminess);
        RenderSettings.ambientSkyColor = Grade(_ambientSky, _storminess);
        RenderSettings.ambientEquatorColor = Grade(_ambientEquator, _storminess);
        RenderSettings.ambientGroundColor = Grade(_ambientGround, _storminess);
        RenderSettings.ambientLight = Grade(_ambientLight, _storminess);
        if (_sun != null)
        {
            _sun.intensity = _sunIntensity * StormCueLevels.Brightness(skyExposure);
            // Intensity supplies the darkening; the light's color supplies only the cast.
            _sun.color = Color.Lerp(_sunColor, _teal, skyExposure * _tealShift);
        }
        if (_camera != null) _camera.backgroundColor = Grade(_background, skyExposure);
        if (_sky != null)
        {
            RenderSettings.skybox = _sky;
            for (int i = 0; i < _colorProperties.Length; i++)
                if (_sky.HasProperty(_colorProperties[i])) _sky.SetColor(_colorProperties[i], Grade(_skyColors[i], skyExposure));
        }
    }
    private void End(RunSummary summary) => Restore();
    private void Restore()
    {
        _exposure = _ef5Exposure = 0f;
        _storminess = 0f;
        if (_deck != null) _deck.gameObject.SetActive(false);
        _bearing = Vector3.zero;
        if (!_captured) return;
        RenderSettings.ambientIntensity = _ambientIntensity;
        RenderSettings.ambientSkyColor = _ambientSky;
        RenderSettings.ambientEquatorColor = _ambientEquator;
        RenderSettings.ambientGroundColor = _ambientGround;
        RenderSettings.ambientLight = _ambientLight;
        if (_sun != null) { _sun.intensity = _sunIntensity; _sun.color = _sunColor; }
        if (_camera != null) _camera.backgroundColor = _background;
        if (RenderSettings.skybox == _sky) RenderSettings.skybox = _originalSky;
    }
    private void OnDisable()
    {
        GameEvents.RunEnded -= End;
        Restore();
        if (_current == this) _current = null;
    }
    private void OnDestroy()
    {
        if (_sky != null) Destroy(_sky);
        _deckAssets?.Dispose();
        if (_deckMesh != null) Destroy(_deckMesh);
    }
}
