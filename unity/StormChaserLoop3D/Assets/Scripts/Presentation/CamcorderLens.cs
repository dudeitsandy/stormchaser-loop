using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Unity.Profiling;

/// <summary>Viewfinder-only camcorder processing, recording overlays and brief photo review.</summary>
public sealed class CamcorderLens : MonoBehaviour
{
    private static readonly ProfilerMarker RenderMarker = new ProfilerMarker("Presentation.Lens.Blit");
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.Lens.UI");
    [SerializeField, Range(0, 0.4f)] private float _scanlineStrength = 0.12f;
    [SerializeField, Range(0, 0.2f)] private float _grainStrength = 0.045f;
    [SerializeField, Range(0, 3f)] private float _chromaPixels = 0.8f;
    [SerializeField, Range(0, 0.1f)] private float _barrelStrength = 0.025f;
    [SerializeField, Min(0)] private float _photoReviewSeconds = 0.25f;
    [SerializeField] private string _dateStamp = "OCT 01 1996";
    private Camera _camera;
    private RenderTexture _source, _processed, _photo;
    private Material _material;
    private Image _preview, _fallback;
    private Texture2D _fallbackTexture;
    private Texture2D _rainTexture;
    private Image _rainDrops;
    private Label _recording, _stamp;
    private VisualElement _overlay;
    private float _elapsed, _reviewUntil;
    private int _photoCount, _lastSecond = -1;
    private bool _hasFootage;
    private bool _wasReviewing;

    /// <summary>Attaches camcorder presentation to a PiP texture and its image container.</summary>
    public void Initialize(Camera camera, RenderTexture source, VisualElement container, Image preview)
    {
        if (_preview != null) throw new System.InvalidOperationException("CamcorderLens is already initialized.");
        _camera = camera;
        _source = source;
        _preview = preview;
        var template = Resources.Load<Material>("Presentation/CamcorderLens");
        if (template != null)
        {
            _material = new Material(template);
            _processed = Target("CamcorderFootage", source);
            _material.SetVector("_SourceSize", new Vector4(source.width, source.height, 1f / source.width, 1f / source.height));
        }
        else Debug.LogWarning("CamcorderLens: using scanline/grain UI fallback; Resources/Presentation/CamcorderLens is needed for chroma/barrel processing.");
        _photo = Target("CamcorderPhotoReview", source);
        _overlay = new VisualElement { pickingMode = PickingMode.Ignore };
        Fill(_overlay);
        container.Add(_overlay);
        _rainTexture = BuildRainDrops();
        _rainDrops = new Image { image = _rainTexture, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
        Fill(_rainDrops);
        _rainDrops.style.opacity = 0f;
        _overlay.Add(_rainDrops);
        if (_material == null)
        {
            _fallbackTexture = BuildFallback(source.width, source.height);
            _fallback = new Image { image = _fallbackTexture, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            Fill(_fallback);
            _overlay.Add(_fallback);
        }
        _recording = PresentationOverlay.Label("● REC", 12, new Color(1f, 0.27f, 0.2f));
        _recording.style.position = Position.Absolute;
        _recording.style.top = 7;
        _recording.style.left = 9;
        _overlay.Add(_recording);
        _stamp = PresentationOverlay.Label("", 10, new Color(1f, 0.89f, 0.62f));
        _stamp.style.position = Position.Absolute;
        _stamp.style.bottom = 5;
        _stamp.style.left = 9;
        _overlay.Add(_stamp);
        for (int i = 0; i < 4; i++)
        {
            var bracket = PresentationOverlay.Label(i < 2 ? (i == 0 ? "┌" : "┐") : (i == 2 ? "└" : "┘"), 18, Color.white);
            bracket.style.position = Position.Absolute;
            if (i % 2 == 0) bracket.style.left = 4; else bracket.style.right = 4;
            if (i < 2) bracket.style.top = 0; else bracket.style.bottom = 0;
            _overlay.Add(bracket);
        }
    }
    private static void Fill(VisualElement element)
    {
        element.style.position = Position.Absolute;
        element.style.left = element.style.right = element.style.top = element.style.bottom = 0;
    }
    private static RenderTexture Target(string name, RenderTexture source)
    {
        var target = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32)
            { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        target.Create();
        return target;
    }
    private Texture2D BuildFallback(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "CamcorderScanlineGrain", wrapMode = TextureWrapMode.Clamp };
        var random = new System.Random(1996);
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            pixels[y * width + x] = new Color(0, 0, 0,
                (y % 3 == 0 ? _scanlineStrength : 0) + (float)random.NextDouble() * _grainStrength);
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }
    private static Texture2D BuildRainDrops()
    {
        const int width = 160, height = 120;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "CamcorderWaterDrops", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color[width * height];
        var random = new System.Random(804);
        for (int drop = 0; drop < 14; drop++)
        {
            int cx = random.Next(6, width - 6), cy = random.Next(6, height - 6);
            // Keep the center aim mark unobscured.
            if (Mathf.Abs(cx - width / 2) < 16 && Mathf.Abs(cy - height / 2) < 16) continue;
            float radius = 2f + (float)random.NextDouble() * 3f;
            for (int y = Mathf.Max(0, cy - 8); y <= Mathf.Min(height - 1, cy + 8); y++)
                for (int x = Mathf.Max(0, cx - 6); x <= Mathf.Min(width - 1, cx + 6); x++)
                {
                    float dx = (x - cx) / radius, dy = (y - cy) / (radius * 1.45f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.8f) / 0.22f);
                    float fill = Mathf.Clamp01(1f - distance) * 0.14f;
                    float highlight = dy > 0f ? 0.9f : 0.18f;
                    pixels[y * width + x] = new Color(highlight, highlight + 0.04f, highlight + 0.06f, Mathf.Max(fill, ring * 0.7f));
                }
        }
        texture.SetPixels(pixels); texture.Apply(false, true); return texture;
    }
    private void OnEnable()
    {
        if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
        RenderPipelineManager.endCameraRendering += OnCameraRendered;
        GameEvents.PhotoTaken += OnPhoto;
        GameEvents.RunStarted += ResetRecording;
        GameEvents.RunEnded += OnRunEnded;
    }
    private void OnDisable()
    {
        if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        if (_preview != null) _preview.image = _source;
        RenderPipelineManager.endCameraRendering -= OnCameraRendered;
        GameEvents.PhotoTaken -= OnPhoto;
        GameEvents.RunStarted -= ResetRecording;
        GameEvents.RunEnded -= OnRunEnded;
    }
    private void ResetRecording()
    {
        _elapsed = 0;
        _photoCount = 0;
        _reviewUntil = 0;
        _lastSecond = -1;
        _wasReviewing = false;
        if (_recording != null) _recording.text = "● REC";
    }
    private void OnRunEnded(RunSummary summary) => _reviewUntil = 0;
    private void OnPhoto(PhotoResult result)
    {
        if (_preview == null || _source == null) return;
        _photoCount++;
        if (!_hasFootage) return;
        Blit(_material != null ? _processed : _source, _photo, null);
        _recording.text = $"PHOTO {_photoCount:00}";
        _reviewUntil = Time.unscaledTime + _photoReviewSeconds;
    }
    private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
    {
        if (camera != _camera) return;
        using var sample = RenderMarker.Auto();
        _hasFootage = true;
        if (_material == null) return;
        _material.SetFloat("_TimeSeconds", Time.unscaledTime);
        _material.SetFloat("_ScanlineStrength", _scanlineStrength);
        _material.SetFloat("_GrainStrength", _grainStrength);
        _material.SetFloat("_ChromaPixels", _chromaPixels);
        _material.SetFloat("_BarrelStrength", _barrelStrength);
        Blit(_source, _processed, _material);
    }
    private static void Blit(RenderTexture source, RenderTexture destination, Material material)
    {
        var previous = RenderTexture.active;
        try
        {
            if (material != null) Graphics.Blit(source, destination, material, 0);
            else Graphics.Blit(source, destination);
        }
        finally { RenderTexture.active = previous; }
    }
    private void LateUpdate()
    {
        using var sample = UpdateMarker.Auto();
        if (_preview == null) return;
        if (_rainDrops != null) _rainDrops.style.opacity = RainLevels.LensOpacity(StormEnvironmentCues.Storminess);
        if (_camera != null && _camera.enabled) _elapsed += Time.unscaledDeltaTime;
        bool reviewing = Time.unscaledTime < _reviewUntil;
        _preview.image = reviewing ? _photo : _material != null ? _processed : _source;
        if (!reviewing && _wasReviewing) _recording.text = "● REC";
        _wasReviewing = reviewing;
        _recording.style.opacity = reviewing || Mathf.Repeat(_elapsed, 1.2f) < 0.8f ? 1 : 0.3f;
        int second = Mathf.FloorToInt(_elapsed);
        if (second != _lastSecond)
        {
            _lastSecond = second;
            _stamp.text = $"{_dateStamp}  {second / 3600:00}:{second / 60 % 60:00}:{second % 60:00}";
        }
        if (_fallback != null)
            _fallback.style.opacity = 0.85f + 0.15f * Mathf.Sin(_elapsed * 37f);
    }
    private void OnDestroy()
    {
        _overlay?.RemoveFromHierarchy();
        Release(_processed);
        Release(_photo);
        if (_material != null) Destroy(_material);
        if (_fallbackTexture != null) Destroy(_fallbackTexture);
        if (_rainTexture != null) Destroy(_rainTexture);
    }
    private static void Release(RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        Destroy(texture);
    }
}
