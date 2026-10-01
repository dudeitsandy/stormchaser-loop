using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ADR-0003 visual test. Rebuilds Scenes/ArtTest.unity from the current gameplay scene, then restyles
/// it: toon materials, outline renderer (separate from the 0.4 renderer), stylized sky/grade, and a
/// kitbashed placeholder truck. Idempotent — re-run any time to pick up gameplay-scene changes.
/// Gameplay scene and PC_Renderer are not modified, so 0.4 stays the A in the A/B.
/// </summary>
public static class ArtTestBuilder
{
    private const string SourceScene = "Assets/Scenes/VerificationScene.unity";
    private const string TestScene = "Assets/Scenes/ArtTest.unity";
    private const string MatDir = "Assets/Materials/Toon";
    private const string PcAsset = "Assets/Settings/PC_RPAsset.asset";
    private const string PcRenderer = "Assets/Settings/PC_Renderer.asset";
    private const string ToonRenderer = "Assets/Settings/PC_Renderer_Toon.asset";
    private const string ToonProfile = "Assets/Settings/ToonVolumeProfile.asset";
    private const string OutlineFeatureName = "ToonOutline";
    private const string TornadoPrefab = "Assets/Prefabs/Tornado_EF3.prefab";
    private const string ToonTornadoPrefab = "Assets/Prefabs/Tornado_Toon.prefab";
    private const string PresentationResources = "Assets/Resources/Presentation";

    [MenuItem("StormChaser/Art Test/Build ArtTest Scene")]
    public static void Build()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        var mats = CreateMaterials();
        EnsurePresentationMaterials();
        GameObject toonTornado = EnsureToonTornadoPrefab();
        int rendererIndex = EnsureToonRenderer(mats.Outline);
        VolumeProfile profile = EnsureVolumeProfile();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) != null) AssetDatabase.DeleteAsset(TestScene);
        AssetDatabase.CopyAsset(SourceScene, TestScene);
        var scene = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);

        ConfigureCamera(rendererIndex);
        ConfigureVolume(profile);
        ConfigureSkyAndLight(mats.Sky);
        BuildPlaceholderTruck(mats);
        AddStyleApplier(mats.Template);
        UseToonTornado(toonTornado);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EnsureInBuildSettings(TestScene);
        Debug.Log($"[ArtTest] Built {TestScene} (toon renderer index {rendererIndex}). Press Play; F9 captures to production/marketing/art-test/.");
    }

    // ---------- Materials ----------

    internal struct Mats
    {
        public Material Template, TruckBody, TruckTrim, Glass, Tire, LightBar, Outline, Sky;
    }

    internal static Mats CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials", "Toon");

        Shader toon = Require(Shader.Find("Doomsday/ToonLit"), "Doomsday/ToonLit shader");
        return new Mats
        {
            Template = ToonMat("Toon_Template", toon, new Color(0.8f, 0.8f, 0.8f)),
            TruckBody = ToonMat("Toon_TruckBody", toon, new Color(0.9f, 0.22f, 0.14f)),
            TruckTrim = ToonMat("Toon_TruckTrim", toon, new Color(0.95f, 0.93f, 0.88f)),
            Glass = ToonMat("Toon_Glass", toon, new Color(0.2f, 0.32f, 0.45f)),
            Tire = ToonMat("Toon_Tire", toon, new Color(0.12f, 0.12f, 0.14f)),
            LightBar = ToonMat("Toon_LightBar", toon, new Color(1f, 0.75f, 0.1f)),
            Outline = OutlineMat(),
            Sky = Mat("GradientSky", Require(Shader.Find("Doomsday/GradientSky"), "GradientSky shader")),
        };
    }

    private static Material OutlineMat()
    {
        Material m = Mat("ToonOutline", Require(Shader.Find("Hidden/Doomsday/ToonOutline"), "ToonOutline shader"));
        // Fade out well before the ~100 m world edge so the ground/sky seam doesn't draw a horizon line.
        m.SetFloat("_FadeStart", 30f);
        m.SetFloat("_FadeEnd", 75f);
        EditorUtility.SetDirty(m);
        return m;
    }

    internal static Material ToonMat(string name, Shader shader, Color color)
    {
        Material m = Mat(name, shader);
        m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material Mat(string name, Shader shader)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader != shader)
        {
            m.shader = shader;
        }
        return m;
    }

    // ---------- Materials Codex's presentation lane loads from Resources (AGENTS.md requests) ----------

    [MenuItem("StormChaser/Art Test/Create Presentation Materials")]
    public static void EnsurePresentationMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PresentationResources)) AssetDatabase.CreateFolder("Assets/Resources", "Presentation");

        ResourceMat("CamcorderLens", Require(Shader.Find("Hidden/Doomsday/CamcorderLens"), "CamcorderLens shader"));
        Material card = ResourceMat("VfxCardMaterial", Require(Shader.Find("Doomsday/VfxCard"), "VfxCard shader"));
        card.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(card);
        AssetDatabase.SaveAssets();
    }

    private static Material ResourceMat(string name, Shader shader)
    {
        string path = $"{PresentationResources}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader != shader)
        {
            m.shader = shader;
        }
        return m;
    }

    // ---------- Toon tornado: prefab copy with Codex's card visual; 0.4 prefab untouched ----------

    private static GameObject EnsureToonTornadoPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ToonTornadoPrefab) != null) AssetDatabase.DeleteAsset(ToonTornadoPrefab);
        AssetDatabase.CopyAsset(TornadoPrefab, ToonTornadoPrefab);

        GameObject root = PrefabUtility.LoadPrefabContents(ToonTornadoPrefab);
        try
        {
            if (root.GetComponent<TornadoCardVisual>() == null) root.AddComponent<TornadoCardVisual>();
            // TornadoVisual still builds the cone mesh (RequireComponent keeps the renderer); just hide it.
            if (root.TryGetComponent(out MeshRenderer cone)) cone.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(root, ToonTornadoPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(ToonTornadoPrefab);
    }

    private static void UseToonTornado(GameObject prefab)
    {
        DisasterSpawner spawner = Require(Object.FindAnyObjectByType<DisasterSpawner>(), "DisasterSpawner");
        var so = new SerializedObject(spawner);
        so.FindProperty("_tornadoPrefab").objectReferenceValue = prefab.GetComponent<TornadoController>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Renderer: copy of PC_Renderer, CRT + SSAO off, outline on ----------

    internal static int EnsureToonRenderer(Material outlineMat)
    {
        if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ToonRenderer) == null)
            AssetDatabase.CopyAsset(PcRenderer, ToonRenderer);
        var data = Require(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ToonRenderer), ToonRenderer);

        FullScreenPassRendererFeature outline = null;
        foreach (ScriptableRendererFeature f in data.rendererFeatures.Where(f => f != null))
        {
            if (f.name == OutlineFeatureName && f is FullScreenPassRendererFeature fs) outline = fs;
            else f.SetActive(false); // CRT full-screen pass + SSAO: the retro lens moves to the viewfinder (ADR-0003)
        }

        if (outline == null)
        {
            outline = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            outline.name = OutlineFeatureName;
            AssetDatabase.AddObjectToAsset(outline, data);
            data.rendererFeatures.Add(outline);
        }
        outline.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
        outline.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
        outline.fetchColorBuffer = true;
        outline.passMaterial = outlineMat;
        outline.passIndex = 0;
        outline.SetActive(true);

        // Rebuild URP's feature-id map (internal API; it also self-heals on next validation).
        typeof(ScriptableRendererData).GetMethod("ValidateRendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(data, null);
        data.SetDirty();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        // Register in the PC pipeline asset's renderer list (WebGL + Windows both use PC).
        var pipeline = Require(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PcAsset), PcAsset);
        var so = new SerializedObject(pipeline);
        SerializedProperty list = so.FindProperty("m_RendererDataList");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == data) return i;

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = data;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return list.arraySize - 1;
    }

    // ---------- Volume: saturated, clean grade; no grain / chroma / LUT ----------

    private static VolumeProfile EnsureVolumeProfile()
    {
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ToonProfile) != null) AssetDatabase.DeleteAsset(ToonProfile);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ToonProfile);

        var tone = Add<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.Neutral);

        var color = Add<ColorAdjustments>(profile);
        color.postExposure.Override(0.15f);
        color.contrast.Override(14f);
        color.saturation.Override(10f);

        var bloom = Add<Bloom>(profile);
        bloom.threshold.Override(1.05f);
        bloom.intensity.Override(0.35f);
        bloom.scatter.Override(0.6f);

        var vignette = Add<Vignette>(profile);
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.45f);

        // URP's project-wide DefaultVolumeProfile still carries the 0.4 retro stack and applies under
        // every scene volume. Explicitly zero it here: the retro look lives only in the viewfinder (ADR-0003).
        Add<ChromaticAberration>(profile).intensity.Override(0f);
        Add<FilmGrain>(profile).intensity.Override(0f);
        Add<ColorLookup>(profile).contribution.Override(0f);
        Add<LensDistortion>(profile).intensity.Override(0f);
        Add<MotionBlur>(profile).intensity.Override(0f);
        Add<DepthOfField>(profile).mode.Override(DepthOfFieldMode.Off);

        AssetDatabase.SaveAssets();
        return profile;
    }

    private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T c = profile.Add<T>(true);
        c.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(c, profile);
        return c;
    }

    // ---------- Scene configuration ----------

    private static void ConfigureCamera(int rendererIndex)
    {
        Camera cam = Require(Camera.main, "Main Camera");
        var data = cam.GetUniversalAdditionalCameraData();
        data.SetRenderer(rendererIndex);
        data.renderPostProcessing = true;
        EditorUtility.SetDirty(data);
    }

    private static void ConfigureVolume(VolumeProfile profile)
    {
        Volume volume = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(v => v.isGlobal);
        if (volume == null) volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(volume);
    }

    internal static void ConfigureSkyAndLight(Material skyMat)
    {
        Color top = new Color(0.22f, 0.45f, 0.82f);
        Color horizon = new Color(0.98f, 0.8f, 0.62f);
        Color ground = new Color(0.34f, 0.4f, 0.3f);
        skyMat.SetColor("_TopColor", top);
        skyMat.SetColor("_HorizonColor", horizon);
        skyMat.SetColor("_BottomColor", ground);
        EditorUtility.SetDirty(skyMat);

        RenderSettings.skybox = skyMat;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = top * 0.9f;
        RenderSettings.ambientEquatorColor = horizon * 0.7f;
        RenderSettings.ambientGroundColor = ground * 0.6f;
        // Linear fog toward the horizon color gives stylized depth and hides the world edge.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = horizon;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 170f;

        Light sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sun != null)
        {
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
            EditorUtility.SetDirty(sun);
            EditorUtility.SetDirty(sun.transform);
        }
    }

    private static void BuildPlaceholderTruck(Mats m)
    {
        PlayerVehicle truck = Require(Object.FindAnyObjectByType<PlayerVehicle>(), "PlayerVehicle");
        if (truck.TryGetComponent(out MeshRenderer cube)) cube.enabled = false;

        Transform old = truck.transform.Find("TruckVisual");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("TruckVisual").transform;
        root.SetParent(truck.transform, false);

        // Local space: origin at cube center (0.5 above ground), +Z forward.
        Part(root, "Chassis", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(1.05f, 0.36f, 1.9f), m.TruckBody);
        Part(root, "Cab", PrimitiveType.Cube, new Vector3(0f, 0.26f, 0.4f), new Vector3(0.96f, 0.44f, 0.8f), m.TruckBody);
        Part(root, "Roof", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0.38f), new Vector3(0.9f, 0.05f, 0.72f), m.TruckTrim);
        Part(root, "Windshield", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.8f), new Vector3(0.84f, 0.3f, 0.04f), m.Glass);
        Part(root, "SideWindowL", PrimitiveType.Cube, new Vector3(-0.485f, 0.3f, 0.4f), new Vector3(0.02f, 0.26f, 0.6f), m.Glass);
        Part(root, "SideWindowR", PrimitiveType.Cube, new Vector3(0.485f, 0.3f, 0.4f), new Vector3(0.02f, 0.26f, 0.6f), m.Glass);
        Part(root, "LightBar", PrimitiveType.Cube, new Vector3(0f, 0.56f, 0.42f), new Vector3(0.7f, 0.07f, 0.12f), m.LightBar);
        Part(root, "BedL", PrimitiveType.Cube, new Vector3(-0.49f, 0.16f, -0.48f), new Vector3(0.07f, 0.2f, 0.9f), m.TruckBody);
        Part(root, "BedR", PrimitiveType.Cube, new Vector3(0.49f, 0.16f, -0.48f), new Vector3(0.07f, 0.2f, 0.9f), m.TruckBody);
        Part(root, "Tailgate", PrimitiveType.Cube, new Vector3(0f, 0.16f, -0.92f), new Vector3(1.05f, 0.2f, 0.07f), m.TruckTrim);
        Part(root, "Bumper", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0.97f), new Vector3(1.1f, 0.12f, 0.08f), m.TruckTrim);
        Part(root, "Instruments", PrimitiveType.Cylinder, new Vector3(0f, 0.32f, -0.5f), new Vector3(0.18f, 0.12f, 0.18f), m.TruckTrim);

        foreach (float x in new[] { -0.52f, 0.52f })
        foreach (float z in new[] { -0.6f, 0.62f })
        {
            Transform w = Part(root, "Wheel", PrimitiveType.Cylinder, new Vector3(x, -0.29f, z), new Vector3(0.44f, 0.09f, 0.44f), m.Tire);
            w.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        EditorUtility.SetDirty(truck.gameObject);
    }

    private static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    private static void AddStyleApplier(Material template)
    {
        var applier = Object.FindAnyObjectByType<ToonStyleApplier>();
        if (applier == null) applier = new GameObject("ArtStyle").AddComponent<ToonStyleApplier>();
        var so = new SerializedObject(applier);
        so.FindProperty("_template").objectReferenceValue = template;
        // Volume grade already adds saturation; boosting again here made the grass lime.
        so.FindProperty("_saturationBoost").floatValue = 1.0f;
        so.FindProperty("_valueBoost").floatValue = 1.05f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureInBuildSettings(string path)
    {
        // RunManager reloads by build index, so the test scene must be listed. BuildScript skips it.
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static T Require<T>(T obj, string what) where T : Object
    {
        if (obj == null) throw new System.InvalidOperationException($"[ArtTest] Missing {what}.");
        return obj;
    }
}
