using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Idempotently wires the gameplay scene (components + references) from code, so scene changes are
/// reproducible and nobody hand-merges scene YAML. Run from the menu or headless:
/// Unity -batchmode -quit -projectPath ... -executeMethod SceneWiring.Apply
/// </summary>
public static class SceneWiring
{
    private const string ScenePath = "Assets/Scenes/VerificationScene.unity";
    private const string TornadoPrefabPath = "Assets/Prefabs/Tornado_EF3.prefab";

    // EF0..EF5: (early weight, late weight). Weak tornadoes dominate early; EF4/EF5 show up late.
    private static readonly (string Rating, float Early, float Late)[] Roster =
    {
        ("EF0", 30f, 5f),
        ("EF1", 30f, 10f),
        ("EF2", 25f, 20f),
        ("EF3", 12f, 25f),
        ("EF4", 3f, 25f),
        ("EF5", 0f, 15f),
    };

    [MenuItem("StormChaser/Wire Gameplay Scene")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var truck = Require(Object.FindAnyObjectByType<PlayerVehicle>(), "PlayerVehicle");
        var timer = Require(Object.FindAnyObjectByType<SessionTimer>(), "SessionTimer");
        var score = Require(Object.FindAnyObjectByType<ScoreAccumulator>(), "ScoreAccumulator");
        var spawner = Require(Object.FindAnyObjectByType<DisasterSpawner>(), "DisasterSpawner");
        var hud = Require(Object.FindAnyObjectByType<HudController>(), "HudController");

        var health = GetOrAdd<VehicleHealth>(truck.gameObject);
        var photo = Object.FindAnyObjectByType<PhotoTrigger>();
        if (photo == null) photo = GetOrAdd<PhotoTrigger>(truck.gameObject);
        Set(photo, "_scoreAccumulator", score);

        var run = GetOrAdd<RunManager>(timer.gameObject);
        GetOrAdd<RunScreens>(timer.gameObject);
        Set(run, "_timer", timer);
        Set(run, "_score", score);
        Set(run, "_spawner", spawner);
        Set(run, "_vehicle", truck);
        Set(run, "_health", health);
        Set(run, "_photo", photo);

        Set(hud, "_sessionTimer", timer);
        Set(hud, "_scoreAccumulator", score);
        Set(hud, "_vehicleHealth", health);

        WireSpawner(spawner, timer, truck.transform);
        GameplaySceneFixups.Apply();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneWiring] Gameplay scene wired and saved.");
    }

    private static void WireSpawner(DisasterSpawner spawner, SessionTimer timer, Transform player)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TornadoPrefabPath);
        Require(prefab, TornadoPrefabPath);

        var so = new SerializedObject(spawner);
        so.FindProperty("_tornadoPrefab").objectReferenceValue = prefab.GetComponent<TornadoController>();
        so.FindProperty("_sessionTimer").objectReferenceValue = timer;
        so.FindProperty("_player").objectReferenceValue = player;

        SerializedProperty roster = so.FindProperty("_roster");
        roster.arraySize = Roster.Length;
        for (int i = 0; i < Roster.Length; i++)
        {
            string path = $"Assets/Prefabs/TornadoData_{Roster[i].Rating}.asset";
            var data = Require(AssetDatabase.LoadAssetAtPath<TornadoData>(path), path);
            SerializedProperty entry = roster.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("Data").objectReferenceValue = data;
            entry.FindPropertyRelative("EarlyWeight").floatValue = Roster[i].Early;
            entry.FindPropertyRelative("LateWeight").floatValue = Roster[i].Late;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
            throw new System.InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T Require<T>(T obj, string what) where T : Object
    {
        if (obj == null) throw new System.InvalidOperationException($"[SceneWiring] Missing {what}.");
        return obj;
    }
}
