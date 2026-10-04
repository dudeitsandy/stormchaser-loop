using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// S7-07: imports the Blender hero pickup (tools/blender/build_pickup.py), maps its five material slots
/// onto the existing toon materials, saves a prefab, and installs it on the truck in both gameplay scenes
/// as <c>TruckVisualBlender</c>, inactive. <see cref="TruckVisualSelector"/> chooses cube or Blender at
/// runtime, so the cube truck stays the default until the G2 gate passes.
/// </summary>
public static class PickupVisualInstaller
{
    private const string FbxPath = "Assets/Art/Vehicles/Pickup/Pickup.fbx";
    private const string PrefabPath = "Assets/Prefabs/PickupVisual.prefab";
    private const string ChildName = "TruckVisualBlender";
    private static readonly string[] Scenes = { "Assets/Scenes/VerificationScene.unity", "Assets/Scenes/ArtTest.unity" };

    private static readonly Dictionary<string, string> MaterialMap = new Dictionary<string, string>
    {
        { "Body", "Assets/Materials/Toon/Toon_TruckBody.mat" },
        { "Trim", "Assets/Materials/Toon/Toon_TruckTrim.mat" },
        { "Dark", "Assets/Materials/Toon/Toon_Tire.mat" },
        { "Glass", "Assets/Materials/Toon/Toon_Glass.mat" },
        { "Light", "Assets/Materials/Toon/Toon_LightBar.mat" },
    };

    [MenuItem("StormChaser/Art/Install Blender Pickup")]
    public static void Install()
    {
        ConfigureImporter();
        GameObject prefab = BuildPrefab();
        foreach (string path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            InstallIn(prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Pickup] installed in {path}");
        }
    }

    private static void ConfigureImporter()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.bakeAxisConversion = true;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        foreach (KeyValuePair<string, string> pair in MaterialMap)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(pair.Value);
            if (mat == null) throw new System.Exception($"[Pickup] missing toon material {pair.Value}");
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), mat);
        }
        importer.SaveAndReimport();
    }

    private static GameObject BuildPrefab()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        Transform fl = Find(instance.transform, "Wheel_FL");
        Bounds bounds = WorldBounds(instance);
        // Axis check: front-left wheel must be at -X (left), +Z (forward); the body ~1.9 m long along Z.
        Debug.Log($"[Pickup] Wheel_FL local={fl.localPosition} bounds center={bounds.center} size={bounds.size}");
        if (fl.position.x > 0f || fl.position.z < 0f)
            throw new System.Exception($"[Pickup] axis conversion wrong: Wheel_FL at {fl.position}");
        foreach (Collider c in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void InstallIn(GameObject prefab)
    {
        PlayerVehicle truck = Object.FindAnyObjectByType<PlayerVehicle>();
        if (truck == null) throw new System.Exception("[Pickup] no PlayerVehicle in scene");
        Transform old = truck.transform.Find(ChildName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, truck.transform);
        visual.name = ChildName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        visual.SetActive(false);
        if (truck.GetComponent<TruckVisualSelector>() == null) truck.gameObject.AddComponent<TruckVisualSelector>();
        EditorUtility.SetDirty(truck.gameObject);
    }

    private static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform hit = Find(child, name);
            if (hit != null) return hit;
        }
        return null;
    }

    private static Bounds WorldBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
