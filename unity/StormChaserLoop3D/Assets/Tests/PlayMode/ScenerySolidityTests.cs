using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// 0.7.1: Codex's solid scenery (EnvironmentScatter) against the real truck in the shipping scene. Trunks,
/// barns and silos block and raise VehicleImpact without HP loss (HP cost off until S8-C2); canopies are
/// not solid; fences are light destructibles that go flying (vehicle-feel.md E13).
/// </summary>
public class ScenerySolidityTests
{
    private const string SceneName = "ArtTest";
    private readonly List<ImpactInfo> _impacts = new List<ImpactInfo>();
    private void OnImpact(ImpactInfo info) => _impacts.Add(info);

    [SetUp]
    public void SetUp()
    {
        _impacts.Clear();
        GameEvents.VehicleImpact += OnImpact;
    }

    // Unload the shipping scene so later tests on their own flat ground don't share space with scenery.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        GameEvents.VehicleImpact -= OnImpact;
        Scene empty = SceneManager.CreateScene("ScenerySolidityEmpty");
        SceneManager.SetActiveScene(empty);
        Scene art = SceneManager.GetSceneByName(SceneName);
        if (art.isLoaded) yield return SceneManager.UnloadSceneAsync(art);
    }

    [UnityTest]
    public IEnumerator TreeTrunk_BlocksTruck_CanopyIsNotSolid()
    {
        // Arrange
        yield return LoadScene();
        GameObject tree = GameObject.Find("RuralTree");
        Assert.IsNotNull(tree, "scatter should place trees");
        int solidParts = tree.GetComponentsInChildren<Collider>().Length;
        // Act
        yield return RamInto(tree.transform.position, 15f, out PlayerVehicle truck, out VehicleHealth health, out Vector3 dir);
        // Assert
        Assert.AreEqual(1, solidParts, "trunk only; canopy collider removed");
        AssertBlocked(truck, tree.transform.position, dir, health);
    }

    [UnityTest]
    public IEnumerator Barn_BlocksTruck_WithImpactButNoHpLoss()
    {
        yield return LoadScene();
        GameObject barn = GameObject.Find("RuralBarn");
        Assert.IsNotNull(barn, "scatter should place barns");
        yield return RamInto(barn.transform.position, 15f, out PlayerVehicle truck, out VehicleHealth health, out Vector3 dir);
        AssertBlocked(truck, barn.transform.position, dir, health);
    }

    [UnityTest]
    public IEnumerator Fence_AtFullSpeed_GoesFlying_TruckKeepsGoing()
    {
        // Arrange
        yield return LoadScene();
        GameObject fence = GameObject.Find("RuralFence");
        Assert.IsNotNull(fence, "scatter should place fences");
        var body = fence.GetComponent<Rigidbody>();
        Assert.IsNotNull(body, "fence is one light rigidbody");
        Assert.AreEqual(ImpactKind.Destructible, fence.GetComponent<ImpactSurface>().Kind);
        // Act: hit it square-on (along its local forward, the rails run along local X).
        yield return RamInto(fence.transform.position, 21.5f, out PlayerVehicle truck, out VehicleHealth health, out _,
                             fence.transform.forward);
        // Assert
        Debug.Log($"[Scenery] fence speed={body.linearVelocity.magnitude:F1} truck={truck.CurrentSpeed:F1} impacts={_impacts.Count}");
        Assert.Greater(body.linearVelocity.magnitude + body.angularVelocity.magnitude, 1f, "fence should be knocked loose");
        Assert.Greater(truck.CurrentSpeed, 12f, "a fence barely slows the truck");
        foreach (ImpactInfo i in _impacts) Assert.AreEqual(0, i.HpLoss, $"fence impact at {i.Speed:F1} m/s");
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
    }

    private static IEnumerator LoadScene()
    {
        SceneManager.LoadScene(SceneName);
        for (int i = 0; i < 5; i++) yield return null; // scatter installs after scene load
    }

    // Places the truck 7 m short of the prop's centre, facing it, and launches it at speed for 1.5 s.
    private IEnumerator RamInto(Vector3 target, float speed, out PlayerVehicle truck, out VehicleHealth health,
                                out Vector3 dir, Vector3? approachAxis = null)
    {
        truck = Object.FindAnyObjectByType<PlayerVehicle>();
        health = truck.GetComponent<VehicleHealth>();
        truck.InputEnabled = false;
        Vector3 axis = approachAxis ?? (target - truck.transform.position);
        axis.y = 0f;
        dir = axis.normalized;
        float y = truck.transform.position.y;
        Vector3 start = new Vector3(target.x, y, target.z) - dir * 7f;
        truck.Teleport(start, Quaternion.LookRotation(dir));
        var rb = truck.GetComponent<Rigidbody>();
        _impacts.Clear();
        return Launch(rb, dir * speed);
    }

    private static IEnumerator Launch(Rigidbody rb, Vector3 velocity)
    {
        yield return new WaitForFixedUpdate();
        rb.linearVelocity = velocity;
        for (int i = 0; i < 75; i++) yield return new WaitForFixedUpdate();
    }

    private void AssertBlocked(PlayerVehicle truck, Vector3 target, Vector3 dir, VehicleHealth health)
    {
        Vector3 rel = truck.transform.position - target;
        float past = Vector3.Dot(new Vector3(rel.x, 0f, rel.z), dir);
        float best = 0f;
        foreach (ImpactInfo i in _impacts) best = Mathf.Max(best, i.Speed);
        Debug.Log($"[Scenery] past={past:F2} m impacts={_impacts.Count} maxSpeed={best:F1} hp={health.CurrentHealth}/{health.MaxHealth}");
        Assert.Less(past, 0f, "the truck must not pass through the prop's centre");
        Assert.Greater(_impacts.Count, 0, "hitting solid scenery raises VehicleImpact");
        Assert.Greater(best, 8f, "a 15 m/s head-on hit reports real severity");
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth, "HP cost stays off until S8-C2");
    }
}
