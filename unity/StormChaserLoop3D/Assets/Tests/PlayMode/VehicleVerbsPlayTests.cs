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
        // Assert: F8 apex v²/(2·1.5g) ≈ 1.5 m with arcade air gravity (suspension rebound adds a little).
        float rise = apex - restY;
        Debug.Log($"[Verbs] jump rise={rise:F2} m");
        Assert.That(rise, Is.InRange(1.2f, 1.9f));
    }

    [UnityTest]
    public IEnumerator EscapeTurn_BrakeHandbrakeSteer_TurnsAroundQuickly()
    {
        // Arrange: the "oh no, it's coming" move (Andy, 2026-10-03): at 18 m/s, slam brake + handbrake +
        // full steer, then floor it back the way you came. Proposed bar: heading reversed (≥ 150°) within 2.5 s.
        SpawnGround();
        var input = new ScriptedInput();
        PlayerVehicle truck = SpawnTruck(input);
        truck.transform.position = new Vector3(0f, 0.8f, -60f);
        for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
        input.Frame.Throttle = 1f;
        while (truck.CurrentSpeed < 18f) yield return new WaitForFixedUpdate();
        Vector3 startFwd = Flat(truck.transform.forward);
        // Act
        input.Frame = new VehicleInputFrame { Brake = 1f, Handbrake = true, Steer = 1f };
        float t = 0f, turnedAt = -1f;
        while (t < 4f && turnedAt < 0f)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            float angle = Vector3.Angle(startFwd, Flat(truck.transform.forward));
            if (angle >= 150f) turnedAt = t;
            if (t > 0.6f) input.Frame = new VehicleInputFrame { Throttle = 1f, Steer = 1f }; // power out of it
        }
        // Assert
        Debug.Log($"[Verbs] escape turn: 150 deg at {turnedAt:F2} s, speed then {truck.CurrentSpeed:F1} m/s, state {truck.State}");
        Assert.Greater(turnedAt, 0f, "the truck never turned around");
        Assert.LessOrEqual(turnedAt, 2.5f);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;

    /// <summary>
    /// Regression (playtest 2026-10-03): a steep landing left the truck wedged at an angle with two wheels
    /// down, and the player had to jump out. Holding throttle must recover. Before the fix, tail-first 60°
    /// ended at 16° tilt on 2 wheels and nose-first 75° drove only 2.3 m in 3 s.
    /// </summary>
    [UnityTest]
    public IEnumerator NoseFirstLanding_HoldingThrottle_RecoversAndDrivesAway(
        [Values(55f, 75f, -60f)] float pitch, [Values(0f, 40f)] float roll, [Values(-6f, -10f)] float fallSpeed)
    {
        // Arrange: tilted, 1.2 m up, coming in fast and steep.
        SpawnGround();
        var input = new ScriptedInput();
        PlayerVehicle truck = SpawnTruck(input);
        yield return new WaitForFixedUpdate();
        truck.Teleport(new Vector3(0f, 1.2f, -60f), Quaternion.Euler(pitch, 0f, roll));
        truck.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, fallSpeed, 6f);
        input.Frame.Throttle = 1f;

        // Act: 3 s with throttle held
        for (int i = 0; i < Mathf.RoundToInt(3f / Time.fixedDeltaTime); i++) yield return new WaitForFixedUpdate();

        // Assert
        float tilt = Vector3.Angle(truck.transform.up, Vector3.up);
        Vector3 moved = truck.transform.position - new Vector3(0f, 1.2f, -60f);
        float travelled = new Vector2(moved.x, moved.z).magnitude; // auto-right may leave it facing either way
        Debug.Log($"[Verbs] landing p{pitch} r{roll} v{fallSpeed}: tilt {tilt:F1} deg, wheels {truck.GroundedWheels}, travelled {travelled:F1} m");
        Assert.Less(tilt, 10f, "truck should be back on its wheels, not wedged at an angle");
        Assert.GreaterOrEqual(truck.GroundedWheels, 3);
        Assert.Greater(travelled, 4f, "truck should drive away under throttle");
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
