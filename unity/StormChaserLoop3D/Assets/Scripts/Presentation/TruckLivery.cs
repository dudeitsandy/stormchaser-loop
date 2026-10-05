using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Runtime-only KTVR station paint and decals, with exact restoration of the pickup's stock materials.</summary>
[DefaultExecutionOrder(200)]
public sealed class TruckLivery : MonoBehaviour
{
    /// <summary>Gameplay unlock ID for the KTVR News paint job.</summary>
    public const string KtvrId = "livery.ktvr";
    private static TruckLivery _current;
    private static string _requestedId;
    private readonly List<Renderer> _renderers = new List<Renderer>();
    private readonly List<Material[]> _stock = new List<Material[]>();
    private readonly List<Material> _owned = new List<Material>();
    private GameObject _decals;
    private Texture2D _logo;
    private Mesh _quad;
    private Transform _pickup;
    private bool _built;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _current = null; _requestedId = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);
    private static void Install()
    {
        var vehicle = UnityEngine.Object.FindAnyObjectByType<PlayerVehicle>();
        if (vehicle == null || vehicle.GetComponent<TruckLivery>() != null) return;
        vehicle.gameObject.AddComponent<TruckLivery>();
    }
    /// <summary>Applies livery.ktvr; null, stock or unknown IDs restore stock. Unlock validation/persistence belongs to gameplay.</summary>
    public static void Apply(string id)
    {
        _requestedId = id;
        if (_current != null) _current.SetPaint(id);
    }
    private void Awake()
    {
        _current = this; _pickup = transform.Find("TruckVisualBlender");
        if (_pickup == null) return;
        foreach (var renderer in _pickup.GetComponentsInChildren<MeshRenderer>(true))
        {
            string name = renderer.name;
            if (name != "LowerBody" && name != "Hood" && name != "Cab" && !name.StartsWith("BedSide", StringComparison.Ordinal)) continue;
            _renderers.Add(renderer); _stock.Add(renderer.sharedMaterials);
        }
        SetPaint(_requestedId);
    }
    private void SetPaint(string id)
    {
        if (_pickup == null) return;
        bool ktvr = string.Equals(id, KtvrId, StringComparison.Ordinal);
        if (ktvr && !_built) Build();
        for (int i = 0; i < _renderers.Count; i++)
        {
            if (!ktvr) _renderers[i].sharedMaterials = _stock[i];
            else
            {
                var painted = new Material[_stock[i].Length];
                for (int j = 0; j < painted.Length; j++) painted[j] = _stock[i][j] != null ? _paint[_stock[i][j]] : null;
                _renderers[i].sharedMaterials = painted;
            }
        }
        if (_decals != null) _decals.SetActive(ktvr);
    }
    private readonly Dictionary<Material, Material> _paint = new Dictionary<Material, Material>();
    private void Build()
    {
        for (int i = 0; i < _stock.Count; i++)
            foreach (var original in _stock[i])
            {
                if (original == null || _paint.ContainsKey(original)) continue;
                var material = new Material(original) { name = "KTVRStationBlue" };
                material.SetColor("_BaseColor", new Color(0.035f, 0.14f, 0.32f, 1f));
                _paint.Add(original, material); _owned.Add(material);
            }
        _decals = new GameObject("KTVRRuntimeMarkings"); _decals.transform.SetParent(_pickup, false);
        _logo = BuildLogo();
        var template = Resources.Load<Material>("Presentation/VfxCardMaterial");
        if (template != null)
        {
            var logoMaterial = new Material(template) { name = "KTVRDoorAndHood" };
            logoMaterial.SetTexture("_BaseMap", _logo); logoMaterial.SetColor("_BaseColor", Color.white);
            _owned.Add(logoMaterial);
            _quad = new Mesh { name = "KTVRDecalQuad",
                vertices = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(0.5f,0.5f,0), new Vector3(-0.5f,0.5f,0) },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                colors = new[] { Color.white, Color.white, Color.white, Color.white }, triangles = new[] { 0,1,2,0,2,3 } };
            _quad.RecalculateBounds();
            // Blender pickup is imported in truck axes: +Z forward, +Y up, doors on +/-X.
            Decal("KTVRLeftDoor", new Vector3(-0.496f,0.025f,0.25f), Quaternion.Euler(0,90,0), new Vector3(0.49f,0.20f,1), logoMaterial);
            Decal("KTVRRightDoor", new Vector3(0.496f,0.025f,0.25f), Quaternion.Euler(0,-90,0), new Vector3(0.49f,0.20f,1), logoMaterial);
            Decal("KTVRHood", new Vector3(0,0.09f,0.76f), Quaternion.Euler(90,0,0), new Vector3(0.66f,0.27f,1), logoMaterial);
        }
        else Debug.LogWarning("KTVR markings require Presentation/VfxCardMaterial.");
        _built = true;
    }
    private void Decal(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        var go = new GameObject(name); go.transform.SetParent(_decals.transform, false);
        go.transform.localPosition = position; go.transform.localRotation = rotation; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = _quad;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
    }
    private static Texture2D BuildLogo()
    {
        const int width = 256, height = 96;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "KTVRNewsLettering", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[width * height];
        var navy = new Color(0.02f,0.08f,0.18f,1f); var amber = new Color(1f,0.66f,0.12f,1f);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) pixels[y * width + x] = y < 12 || y > 83 ? amber : navy;
        string[] glyphs = { "10001100101010011000101001001010001", "11111001000010000100001000010000100",
            "10001100011000110001100010101000100", "11110100011000111110101001001010001" };
        for (int letter = 0; letter < glyphs.Length; letter++)
            for (int row = 0; row < 7; row++) for (int col = 0; col < 5; col++)
                if (glyphs[letter][row * 5 + col] == '1')
                    for (int dy = 0; dy < 8; dy++) for (int dx = 0; dx < 8; dx++)
                        pixels[(20 + (6-row)*8 + dy) * width + 28 + letter*52 + col*8 + dx] = Color.white;
        texture.SetPixels(pixels); texture.Apply(false,true); return texture;
    }
    private void OnDestroy()
    {
        for (int i = 0; i < _renderers.Count; i++) if (_renderers[i] != null) _renderers[i].sharedMaterials = _stock[i];
        foreach (var material in _owned) Release(material);
        Release(_logo); Release(_quad); Release(_decals);
        if (_current == this) _current = null;
    }
    private static void Release(UnityEngine.Object owned)
    {
        if (owned == null) return;
        if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
    }
}
