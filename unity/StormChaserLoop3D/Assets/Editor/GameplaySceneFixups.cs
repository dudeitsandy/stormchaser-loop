using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Scene-level fixes shared by SceneWiring and ArtTestBuilder (playtest 2026-10-01, ADR-0005 vehicle):
/// a solid ground slab under the zero-thickness Plane ground, and a heading-only follow camera so
/// suspension pitch/roll and tornado flips don't swing the view.
/// </summary>
public static class GameplaySceneFixups
{
    private const string SlabName = "GroundSlab";
    private const int LockToTargetWithWorldUp = 1; // Cinemachine BindingMode: tilt and roll zeroed

    public static void Apply()
    {
        EnsureGroundSlab();
        ConfigureFollowCamera();
    }

    /// <summary>2 m-thick box under y = 0 so the truck can't end up beneath the single-sided Plane collider.</summary>
    private static void EnsureGroundSlab()
    {
        GameObject slab = GameObject.Find(SlabName);
        if (slab == null) slab = new GameObject(SlabName);
        slab.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        slab.transform.localScale = Vector3.one;
        BoxCollider box = slab.GetComponent<BoxCollider>();
        if (box == null) box = slab.AddComponent<BoxCollider>();
        box.size = new Vector3(240f, 2f, 240f);
        box.center = new Vector3(0f, -1f, 0f);
        EditorUtility.SetDirty(slab);
    }

    private static void ConfigureFollowCamera()
    {
        var follow = Object.FindAnyObjectByType<CinemachineFollow>();
        if (follow == null)
        {
            Debug.LogWarning("[SceneFixups] No CinemachineFollow found; camera left unchanged.");
            return;
        }
        var so = new SerializedObject(follow);
        SetInt(so, "TrackerSettings.BindingMode", LockToTargetWithWorldUp);
        // Slightly more yaw smoothing: "a little less extreme camera turns".
        SetVector(so, "TrackerSettings.RotationDamping", new Vector3(1f, 1.6f, 1f));
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(follow);
    }

    private static void SetInt(SerializedObject so, string path, int value)
    {
        SerializedProperty p = so.FindProperty(path);
        if (p == null) { Debug.LogWarning($"[SceneFixups] Missing property {path}"); return; }
        if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = value;
        else p.intValue = value;
    }

    private static void SetVector(SerializedObject so, string path, Vector3 value)
    {
        SerializedProperty p = so.FindProperty(path);
        if (p == null) { Debug.LogWarning($"[SceneFixups] Missing property {path}"); return; }
        p.vector3Value = value;
    }
}
