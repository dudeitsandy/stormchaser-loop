using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// vehicle-feel.md verb criteria with the real PlayerVehicle on flat ground (S7-04): holding boost never
/// exceeds BoostMaxSpeed + 0.5 m/s and beats plain throttle; a jump reaches ≈ the 1.5 m F8 apex.
/// </summary>
public class VehicleVerbsPlayTests
{
    private sealed class ScriptedInput : IVehicleInput
    {
        public VehicleInputFrame Frame;
        public VehicleInputFrame Read()
        {
            VehicleInputFrame f = Frame;
            Frame.JumpPressed = false; // a press is consumed once
            return f;
        }
    }

    private readonly List<GameObject> _spawned = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _spawned) Object.Destroy(go);
        _spawned.Clear();
    }

    [UnityTest]
    public IEnumerator HoldingBoost_NeverExceedsBoostMaxSpeed_AndBeatsThrottle()
    {
        // Arrange
        float boosted = 0f, plain = 0f, boostMax = 0f;
        // Act: 4 s from rest, full throttle, with and without boost.
        yield return Drive(boost: true, seconds: 4f, (top, max) => { boosted = top; boostMax = max; });
        yield return Drive(boost: false, seconds: 4f, (top, _) => plain = top);
        // Assert
        Debug.Log($"[Verbs] boosted={boosted:F2} plain={plain:F2} boostMax={boostMax:F2}");
        Assert.LessOrEqual(boosted, boostMax + 0.5f);
        Assert.Greater(boosted, plain + 1f, "boost should add real speed");
    }

    [UnityTest]
    public IEnumerator Jump_FromRest_ReachesAboutOnePointFiveMeters()
    {
        // Arrange
        SpawnGround();
        var input = new ScriptedInput();
        PlayerVehicle truck = SpawnTruck(input);
        for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate(); // settle on the springs
        float restY = truck.transform.position.y;
        // Act
        input.Frame.JumpPressed = true;
        float apex = restY;
        for (int i = 0; i < 75; i++)
        {
            yield return new WaitForFixedUpdate();
            apex = Mathf.Max(apex, truck.transform.position.y);
        }
        // Assert: F8 apex v²/2g ≈ 1.54 m (suspension rebound adds a little).
        float rise = apex - restY;
        Debug.Log($"[Verbs] jump rise={rise:F2} m");
        Assert.That(rise, Is.InRange(1.2f, 1.9f));
    }

    private IEnumerator Drive(bool boost, float seconds, System.Action<float, float> report)
    {
        SpawnGround();
        var input = new ScriptedInput();
        PlayerVehicle truck = SpawnTruck(input);
        for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
        input.Frame.Throttle = 1f;
        input.Frame.Boost = boost;
        float top = 0f;
        int steps = Mathf.RoundToInt(seconds / Time.fixedDeltaTime);
        for (int i = 0; i < steps; i++)
        {
            yield return new WaitForFixedUpdate();
            top = Mathf.Max(top, truck.CurrentSpeed);
        }
        report(top, truck.Model.BoostMaxSpeed);
        TearDown();
    }

    private void SpawnGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _spawned.Add(ground);
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(60f, 1f, 400f);
    }

    private PlayerVehicle SpawnTruck(IVehicleInput input)
    {
        var go = new GameObject("TestTruck");
        _spawned.Add(go);
        go.transform.position = new Vector3(0f, 0.8f, -90f); // drives +Z toward the soft boundary at 95 m
        go.AddComponent<Rigidbody>();
        go.AddComponent<BoxCollider>();
        var truck = go.AddComponent<PlayerVehicle>();
        truck.InputSource = input;
        return truck;
    }
}
