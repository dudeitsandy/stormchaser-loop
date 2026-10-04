using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Regression (Andy 2026-10-04: "the car sinks below the driving surface at times"): the hero pickup's
/// wheels were fixed to the body, so suspension compression on landings and braking pushed them through the
/// road. Visual wheels must follow the suspension: resting on the ground at rest and never sinking into it.
/// </summary>
public class WheelVisualsPlayTests
{
    private const string SceneName = "ArtTest";
    private const float VisualWheelRadius = 0.23f;
    private static readonly string[] WheelNames = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Scene empty = SceneManager.CreateScene("WheelVisualsEmpty");
        SceneManager.SetActiveScene(empty);
        Scene art = SceneManager.GetSceneByName(SceneName);
        if (art.isLoaded) yield return SceneManager.UnloadSceneAsync(art);
    }

    [UnityTest]
    public IEnumerator HardLanding_VisualWheelsNeverSinkIntoGround_AndRestOnIt()
    {
        // Arrange
        SceneManager.LoadScene(SceneName);
        for (int i = 0; i < 5; i++) yield return null;
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        truck.InputEnabled = false;
        Transform[] wheels = FindWheels(truck.transform);
        float groundY = GroundBelow(truck);

        // Act: drop from 1.5 m with a hard downward start (a jump or toss landing)
        truck.Teleport(truck.transform.position + Vector3.up * 1.5f, truck.transform.rotation);
        yield return new WaitForFixedUpdate();
        truck.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, -6f, 0f);
        float deepest = float.MaxValue;
        for (int i = 0; i < 150; i++) // 3 s of physics steps
        {
            yield return new WaitForFixedUpdate();
            yield return null; // let LateUpdate place the visual wheels
            foreach (Transform w in wheels) deepest = Mathf.Min(deepest, w.position.y - VisualWheelRadius - groundY);
        }
        float restWorst = 0f;
        foreach (Transform w in wheels)
        {
            float gap = w.position.y - VisualWheelRadius - groundY;
            if (Mathf.Abs(gap) > Mathf.Abs(restWorst)) restWorst = gap;
        }

        // Assert
        Debug.Log($"[WheelVisuals] deepest during landing {deepest:F3} m, worst at rest {restWorst:F3} m");
        Assert.GreaterOrEqual(deepest, -0.05f, "a visual wheel sank into the ground");
        Assert.Less(Mathf.Abs(restWorst), 0.03f, "at rest every visual wheel should sit on the ground");
    }

    // Highest non-truck collider under the truck (road plane / ground slab).
    private static float GroundBelow(PlayerVehicle truck)
    {
        RaycastHit[] hits = Physics.RaycastAll(truck.transform.position + Vector3.up * 5f, Vector3.down, 20f, ~0,
                                               QueryTriggerInteraction.Ignore);
        float best = float.MinValue;
        foreach (RaycastHit h in hits)
            if (h.rigidbody == null || h.rigidbody.gameObject != truck.gameObject) best = Mathf.Max(best, h.point.y);
        Assert.AreNotEqual(float.MinValue, best, "no ground under the truck");
        return best;
    }

    private static Transform[] FindWheels(Transform truck)
    {
        Transform visual = truck.Find("TruckVisualBlender");
        Assert.IsNotNull(visual, "the hero pickup should be installed");
        Assert.IsTrue(visual.gameObject.activeInHierarchy, "the hero pickup is the default visual (G2)");
        var wheels = new Transform[WheelNames.Length];
        for (int i = 0; i < WheelNames.Length; i++)
        {
            wheels[i] = Find(visual, WheelNames[i]);
            Assert.IsNotNull(wheels[i], WheelNames[i]);
        }
        return wheels;
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
}
