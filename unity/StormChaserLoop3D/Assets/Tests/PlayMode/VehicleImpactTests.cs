using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// vehicle-feel.md F10 PlayMode criteria with the real PlayerVehicle: static wall at 14 m/s → 1 HP, at 24 m/s
/// → 2 HP, light fence → 0 HP; E12 one impact per step. HP cost is off until S8-C2, so these check the
/// HP value VehicleImpact reports and that health is untouched. S7-06.
/// </summary>
public class VehicleImpactTests
{
    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly List<ImpactInfo> _impacts = new List<ImpactInfo>();
    private int _damageEvents;

    private void OnImpact(ImpactInfo info) => _impacts.Add(info);
    private void OnDamaged(int current, int max) => _damageEvents++;

    [SetUp]
    public void SetUp()
    {
        _impacts.Clear();
        _damageEvents = 0;
        GameEvents.VehicleImpact += OnImpact;
        GameEvents.PlayerDamaged += OnDamaged;
    }

    [TearDown]
    public void TearDown()
    {
        GameEvents.VehicleImpact -= OnImpact;
        GameEvents.PlayerDamaged -= OnDamaged;
        foreach (GameObject go in _spawned) Object.Destroy(go);
        _spawned.Clear();
    }

    [UnityTest]
    public IEnumerator StaticWallAt14_LightBump_CostsNothing()
    {
        // Arrange
        PlayerVehicle truck = SpawnTruck(out VehicleHealth health);
        SpawnWall();
        // Act
        yield return Launch(truck, 14f);
        // Assert: S8-C1, below the 15.5 m/s Light threshold a bump is free
        Assert.AreEqual(1, _impacts.Count, "E12: exactly one impact for one wall hit");
        Assert.AreEqual(0, _impacts[0].HpLoss);
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
        Assert.AreEqual(0, _damageEvents);
    }

    [UnityTest]
    public IEnumerator StaticWallAt18_CostsOneHp_FromSixHp()
    {
        // Arrange
        PlayerVehicle truck = SpawnTruck(out VehicleHealth health);
        SpawnWall();
        // Act
        yield return Launch(truck, 18f);
        // Assert
        Assert.AreEqual(1, _impacts.Count, "E12: exactly one impact for one wall hit");
        Assert.AreEqual(1, _impacts[0].HpLoss);
        Assert.AreEqual(ImpactKind.World, _impacts[0].Kind);
        Assert.AreEqual(6, health.MaxHealth, "S8-C1 Pickup HP");
        Assert.AreEqual(5, health.CurrentHealth, "ImpactsCostHp is on");
        Assert.AreEqual(1, _damageEvents);
    }

    [UnityTest]
    public IEnumerator StaticWallAt24_ReportsTwoHp()
    {
        // Arrange
        PlayerVehicle truck = SpawnTruck(out _);
        SpawnWall();
        // Act
        yield return Launch(truck, 24f);
        // Assert
        Assert.AreEqual(1, _impacts.Count);
        Assert.AreEqual(2, _impacts[0].HpLoss);
    }

    [UnityTest]
    public IEnumerator LightFenceAtFullSpeed_ReportsNoHp()
    {
        // Arrange: 50 kg tagged destructible in the truck's path (E13).
        PlayerVehicle truck = SpawnTruck(out _);
        GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _spawned.Add(fence);
        fence.transform.position = new Vector3(0f, 2f, 5f);
        fence.transform.localScale = new Vector3(4f, 1.5f, 0.2f);
        var body = fence.AddComponent<Rigidbody>();
        body.mass = 50f;
        body.useGravity = false;
        fence.AddComponent<ImpactSurface>().Kind = ImpactKind.Destructible;
        // Act
        yield return Launch(truck, 21.5f);
        // Assert: the truck really hit it, and any reported impact is worth 0 HP.
        Assert.Greater(body.linearVelocity.z, 1f, "the truck should have shoved the fence");
        foreach (ImpactInfo impact in _impacts) Assert.AreEqual(0, impact.HpLoss, $"fence impact at {impact.Speed:F1} m/s");
    }

    // Truck in mid-air 2 m up (no ground contacts), no input, so only the wall can produce an impact.
    private PlayerVehicle SpawnTruck(out VehicleHealth health)
    {
        var go = new GameObject("TestTruck");
        _spawned.Add(go);
        go.SetActive(false); // add every component before Awake so PlayerVehicle finds VehicleHealth
        go.AddComponent<Rigidbody>();
        go.AddComponent<BoxCollider>();
        var truck = go.AddComponent<PlayerVehicle>();
        health = go.AddComponent<VehicleHealth>();
        go.transform.position = new Vector3(0f, 2f, 0f);
        go.SetActive(true);
        truck.InputEnabled = false;
        return truck;
    }

    private void SpawnWall()
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _spawned.Add(wall);
        wall.transform.position = new Vector3(0f, 2f, 6f);
        wall.transform.localScale = new Vector3(12f, 10f, 1f);
    }

    private static IEnumerator Launch(PlayerVehicle truck, float speed)
    {
        var rb = truck.GetComponent<Rigidbody>();
        rb.useGravity = false; // keep the approach purely horizontal
        rb.linearVelocity = new Vector3(0f, 0f, speed);
        for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate(); // 0.6 s: hit, then resolve
    }
}
