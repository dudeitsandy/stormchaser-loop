using UnityEngine;
using Unity.Profiling;

/// <summary>One cached exposure drives world lighting, wind cards and audio. Owns only runtime sky copies.</summary>
[DefaultExecutionOrder(-500)]
public sealed class StormEnvironmentCues : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _tealShift = 0.35f;
    [SerializeField] private Color _teal = new Color(0.3f, 0.55f, 0.48f, 1f);
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
        if (_sun != null)
        {
            _sun.intensity = _sunIntensity * StormCueLevels.Brightness(_exposure);
            // Intensity supplies the darkening; the light's color supplies only the cast.
            _sun.color = Color.Lerp(_sunColor, _teal, _exposure * _tealShift);
        }
        if (_camera != null) _camera.backgroundColor = Grade(_background, _exposure);
        if (_sky != null)
        {
            RenderSettings.skybox = _sky;
            for (int i = 0; i < _colorProperties.Length; i++)
                if (_sky.HasProperty(_colorProperties[i])) _sky.SetColor(_colorProperties[i], Grade(_skyColors[i], _exposure));
        }
    }
    private void End(RunSummary summary) => Restore();
    private void Restore()
    {
        _exposure = _ef5Exposure = 0f;
        _bearing = Vector3.zero;
        if (!_captured) return;
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
    private void OnDestroy() { if (_sky != null) Destroy(_sky); }
}
