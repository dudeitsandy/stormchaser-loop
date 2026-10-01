using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// S7-01: builds Scenes/StreamingSpike.unity — the ADR-0004 G1 gate test (2 km streamed tiles, autopilot,
/// debris load, metrics) in the ADR-0003 look. Never ships with the game (BuildScript ships only the
/// shipping scene; the spike has its own build target). Idempotent.
/// </summary>
public static class SpikeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/StreamingSpike.unity";
    private const string ToonProfile = "Assets/Settings/ToonVolumeProfile.asset";

    [MenuItem("StormChaser/Spike/Build Streaming Spike Scene")]
    public static void Build()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        ArtTestBuilder.Mats mats = ArtTestBuilder.CreateMaterials();
        int rendererIndex = ArtTestBuilder.EnsureToonRenderer(mats.Outline);
        Shader toon = Shader.Find("Doomsday/ToonLit");
        Material ground = ArtTestBuilder.ToonMat("Toon_Ground", toon, new Color(0.36f, 0.62f, 0.27f));
        Material props = ArtTestBuilder.ToonMat("Toon_SpikeProp", toon, new Color(0.78f, 0.55f, 0.32f));
        Material debris = ArtTestBuilder.ToonMat("Toon_SpikeDebris", toon, new Color(0.55f, 0.5f, 0.45f));
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ToonProfile);
        if (profile == null)
            throw new System.InvalidOperationException("[Spike] Run StormChaser > Art Test > Build ArtTest Scene first (creates the toon profile).");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        ArtTestBuilder.ConfigureSkyAndLight(mats.Sky);

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.farClipPlane = 700f;
        cam.fieldOfView = 60f;
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.SetRenderer(rendererIndex);
        camData.renderPostProcessing = true;

        var target = new GameObject("SpikeTarget");
        var driver = target.AddComponent<SpikeDriver>();

        var world = new GameObject("World");
        var streamer = world.AddComponent<TileStreamer>();
        var silhouetteGo = new GameObject("DistantSilhouette");
        silhouetteGo.AddComponent<MeshFilter>();
        silhouetteGo.AddComponent<MeshRenderer>().sharedMaterial = ground;
        var silhouette = silhouetteGo.AddComponent<DistantSilhouette>();

        var metrics = new GameObject("SpikeMetrics").AddComponent<SpikeMetrics>();

        Set(streamer, "_target", target.transform);
        Set(streamer, "_groundMaterial", ground);
        Set(streamer, "_propMaterial", props);
        Set(driver, "_camera", camGo.transform);
        Set(driver, "_debrisMaterial", debris);
        Set(silhouette, "_streamer", streamer);
        Set(metrics, "_streamer", streamer);

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        Debug.Log($"[Spike] Built {ScenePath}. Press Play for a 3-minute run, or StormChaser > Spike > Build Spike WebGL.");
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) throw new System.InvalidOperationException($"{target.GetType().Name} has no field '{field}'.");
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
