using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Opt-in, runtime-only storm probes. URL ?stormVisualSpike=wedge or far; desktop -stormVisualSpike=wedge or far.</summary>
public sealed class StormVisualSpike : MonoBehaviour
{
    [SerializeField] private bool _farField;
    [SerializeField, Min(800f)] private float _distance = 800f;
    private Camera _camera;
    private TornadoCardVisual _wedge;
    private Transform _farRoot, _cloud, _funnel;
    private CardVfxAssets _assets;
    private Material _farMaterial;
    private float _age;
    private bool _searched;
    private float _oldFarClip;
    private bool _positioned;
    private Transform[] _anvils;
    private Mesh _cloudMesh;
    private float _cloudAge;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);
    private static void Install()
    {
        string mode = null;
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith("-stormVisualSpike=", StringComparison.Ordinal)) mode = arg.Substring(18);
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token.StartsWith("stormVisualSpike=", StringComparison.Ordinal)) mode = token.Substring(17);
        if (mode != "wedge" && mode != "far") return;
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<StormVisualSpike>() != null) return;
        var spike = new GameObject(nameof(StormVisualSpike)).AddComponent<StormVisualSpike>();
        spike._farField = mode == "far";
    }
    private void Start()
    {
        _camera = Camera.main;
        if (_camera == null) { enabled = false; return; }
        if (!_farField) return;
        var template = Resources.Load<Material>("Presentation/FarFieldStormMaterial");
        if (template == null)
        {
            Debug.LogWarning("[StormVisualSpike] Far-field probe requires Presentation/FarFieldStormMaterial; no fogged fallback is valid.");
            enabled = false;
            return;
        }
        _assets = new CardVfxAssets();
        _farMaterial = new Material(template);
        _farRoot = new GameObject("FarField800mProbe").transform;
        _farRoot.SetParent(transform, false);
        _funnel = _assets.CreateFunnel(_farRoot, 12, 0.85f);
        _cloudMesh = WallCloudMesh();
        _cloud = _assets.Create(_farRoot, CardVfxAssets.Shape.Dust, "FarWallCloud", _cloudMesh);
        _anvils = new Transform[3];
        for (int i = 0; i < _anvils.Length; i++) _anvils[i] = _assets.Create(_farRoot, CardVfxAssets.Shape.Dust, "FarAnvil");
        foreach (var renderer in _farRoot.GetComponentsInChildren<MeshRenderer>())
        {
            var material = new Material(_farMaterial);
            // Preserve the generated illustration while replacing the fogged shader.
            material.SetTexture("_BaseMap", renderer.sharedMaterial.GetTexture("_BaseMap"));
            material.SetColor("_BaseColor", new Color(0.12f, 0.2f, 0.18f, 0.95f));
            material.SetColor("_HorizonColor", RenderSettings.fogColor);
            material.SetFloat("_HorizonBlend", 0.25f);
            renderer.sharedMaterial = material;
        }
        _oldFarClip = _camera.farClipPlane;
        _camera.farClipPlane = Mathf.Max(_oldFarClip, _distance + 150f);
    }
    private void Update()
    {
        if (_farField || _searched || _wedge != null) return;
        // A bounded startup search, not a per-frame Find. Spawner may not create a storm until a run starts.
        _age += Time.deltaTime;
        if (_age < 5f) return;
        _age = 0;
        foreach (var disaster in DisasterEntity.Active)
        {
            if (!(disaster is TornadoController)) continue;
            _wedge = disaster.GetComponent<TornadoCardVisual>();
            if (_wedge == null) continue;
            _wedge.PreviewEf5Wedge(true);
            _searched = true;
            Debug.Log("[StormVisualSpike] Visual-only Mature EF5: D=12, width=24, height=20, 3 sub-vortices, debris radius=14.4, dust ring 12–35m, 1200 debris + 500 gusts in two fixed mesh batches. Gameplay scale unchanged. Seven renderers, up to 14 draws over main+PiP. Frame-time/overdraw capture still required for AC-29; probe uses separate generated materials rather than the proposed unified atlas.");
            break;
        }
    }
    private void LateUpdate()
    {
        if (!_farField || _farRoot == null || _camera == null) return;
        // Use a fixed world bearing/position after startup so camera motion tests angular readability.
        if (!_positioned)
        {
            Vector3 forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            _farRoot.position = _camera.transform.position + forward * _distance;
            _positioned = true;
        }
        if (_funnel == null || _cloud == null) return;
        _funnel.localScale = new Vector3(12f, 20f, 1);
        _cloud.localPosition = Vector3.up * 22f;
        _cloud.localScale = new Vector3(90f, 14f, 50f);
        _cloudAge += Time.deltaTime;
        _cloud.localRotation = Quaternion.Euler(0, _cloudAge * 3f, 0);
        CardVfxAssets.FaceCamera(_funnel, _camera);
        for (int i = 0; i < _anvils.Length; i++)
        {
            if (_anvils[i] == null) continue;
            _anvils[i].localPosition = new Vector3((i - 1) * 38f, 36f + i * 4f, 4f);
            _anvils[i].localScale = new Vector3(70f, 18f, 1);
            CardVfxAssets.FaceCamera(_anvils[i], _camera);
        }
    }
    private void OnDisable()
    {
        if (_wedge != null) _wedge.PreviewEf5Wedge(false);
        if (_farRoot != null) _farRoot.gameObject.SetActive(false);
        if (_farField && _oldFarClip > 0 && _camera != null) _camera.farClipPlane = _oldFarClip;
    }
    private void OnDestroy()
    {
        if (_farRoot != null)
            foreach (var renderer in _farRoot.GetComponentsInChildren<MeshRenderer>()) Destroy(renderer.sharedMaterial);
        if (_farMaterial != null) Destroy(_farMaterial);
        if (_cloudMesh != null) Destroy(_cloudMesh);
        _assets?.Dispose();
    }

    private static Mesh WallCloudMesh()
    {
        var vertices = new Vector3[10];
        var uv = new Vector2[10];
        var colors = new Color[10];
        var triangles = new int[48];
        vertices[8] = Vector3.up * 0.5f;
        vertices[9] = Vector3.down * 0.5f;
        uv[8] = uv[9] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < 10; i++) colors[i] = Color.white;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            vertices[i] = new Vector3(Mathf.Cos(angle) * 0.5f, 0, Mathf.Sin(angle) * 0.5f);
            uv[i] = new Vector2(0.5f + vertices[i].x * 0.7f, 0.5f + vertices[i].z * 0.7f);
            int next = (i + 1) % 8, t = i * 6;
            triangles[t] = i; triangles[t + 1] = 8; triangles[t + 2] = next;
            triangles[t + 3] = i; triangles[t + 4] = next; triangles[t + 5] = 9;
        }
        var mesh = new Mesh { name = "FarWallCloudOctagonal", vertices = vertices, uv = uv, colors = colors, triangles = triangles };
        mesh.RecalculateBounds();
        return mesh;
    }
}
