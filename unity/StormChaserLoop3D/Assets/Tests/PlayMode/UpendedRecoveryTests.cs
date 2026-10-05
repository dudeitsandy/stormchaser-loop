using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Regression (Andy 2026-10-05, 0.8.6 goal pass): flipped by a storm, the truck lay on its back and never righted.
/// vehicle-feel.md E5: upended 1.2 s with angular speed under 1.5 rad/s auto-rights. Real scene, real truck.
/// </summary>
public class UpendedRecoveryTests
{
    private const string SceneName = "ArtTest";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        ProfileStore.Shared = new ProfileStore(new MemoryProfileStorage());
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        ProfileStore.Shared = null;
        Time.timeScale = 1f;
        Scene empty = SceneManager.CreateScene("UpendedEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    [UnityTest]
    public IEnumerator FlippedTruck_RightsItself_WithinThreeSeconds([Values(180f, 95f, 150f)] float roll)
    {
        // Arrange: a run in progress, truck dropped onto its roof / side in open field
        var run = Object.FindAnyObjectByType<RunManager>();
        run.StartRun();
        yield return new WaitForSeconds(0.5f);
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        Vector3 at = truck.transform.position + new Vector3(0f, 1.5f, 0f);
        truck.Teleport(at, Quaternion.Euler(0f, 30f, roll));
        // Act
        float t = 0f, minDot = 1f;
        string trace = "";
        while (t < 4f)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            float dot = Vector3.Dot(truck.transform.up, Vector3.up);
            minDot = Mathf.Min(minDot, dot);
            if (Mathf.Repeat(t, 0.5f) < Time.fixedDeltaTime)
                trace += $" | {t:F1}s up·Y {dot:F2} state {truck.State} wheels {truck.GroundedWheels} " +
                         $"ang {truck.GetComponent<Rigidbody>().angularVelocity.magnitude:F2}";
        }
        float upDot = Vector3.Dot(truck.transform.up, Vector3.up);
        Debug.Log($"[Upended] roll {roll}: end up·Y {upDot:F2}{trace}");
        // Assert
        Assert.Greater(upDot, 0.8f, "back on its wheels" + trace);
    }

    private sealed class ScriptedInput : IVehicleInput
    {
        public VehicleInputFrame Frame;
        public VehicleInputFrame Read()
        {
            VehicleInputFrame f = Frame;
            Frame.JumpPressed = false;
            return f;
        }
    }

    [UnityTest]
    public IEnumerator FlippedTruck_JumpRightsItAtOnce()
    {
        // Arrange: on its back and rocking (the case that used to stick)
        var run = Object.FindAnyObjectByType<RunManager>();
        run.StartRun();
        yield return new WaitForSeconds(0.5f);
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var input = new ScriptedInput();
        truck.InputSource = input;
        truck.Teleport(truck.transform.position + new Vector3(0f, 1.5f, 0f), Quaternion.Euler(0f, 30f, 150f));
        yield return new WaitForSeconds(0.6f);
        Assert.Less(Vector3.Dot(truck.transform.up, Vector3.up), 0f, "precondition: upside down");
        // Act
        input.Frame.JumpPressed = true;
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        // Assert: well before the 3 s failsafe
        Assert.Greater(Vector3.Dot(truck.transform.up, Vector3.up), 0.8f, "jump flipped it back onto its wheels");
    }
}
