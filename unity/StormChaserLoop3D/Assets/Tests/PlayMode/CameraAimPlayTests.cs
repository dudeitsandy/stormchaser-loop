using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// vehicle-feel.md Camera & aim criteria (S7-05) in the real gameplay scene: photo aim follows the camera,
/// not the truck, and Storm Cam keeps a circled funnel inside its F14 framing budget.
/// </summary>
public class CameraAimPlayTests
{
    private const string SceneName = "VerificationScene";
    private const float OptimalDistance = 20f;
    // F14 allows up to StormCamMaxOffset (8°) of deliberate drift; 2° more covers damping while circling.
    private const float StormCamAngleBudget = 10f;

    [UnityTest]
    public IEnumerator CameraForwardAim_TruckFacingNinetyDegreesAway_CameraOnFunnel_IsPerfect()
    {
        // Arrange
        yield return StartRunWithHeldEF3();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var photo = Object.FindAnyObjectByType<PhotoTrigger>();
        TornadoController tornado = _tornado;
        Vector3 funnel = tornado.transform.position;
        // Truck 20 m south of the funnel, facing east (90° away from it).
        Place(truck, funnel + new Vector3(0f, 0f, -OptimalDistance), Quaternion.LookRotation(Vector3.right));
        Transform cam = TakeOverMainCamera();
        cam.position = truck.transform.position + new Vector3(0f, 3f, -2f);
        cam.rotation = Quaternion.LookRotation(funnel - cam.position);
        yield return new WaitForFixedUpdate();

        ShotTier tier = ShotTier.Glancing;
        float aim = -1f;
        System.Action<PhotoResult> onPhoto = r => { tier = r.Tier; aim = r.AimScore; };
        GameEvents.PhotoTaken += onPhoto;
        try
        {
            // Act
            photo.Shoot();

            // Assert
            Debug.Log($"[CameraAim] aim={aim:F3} tier={tier}");
            Assert.AreEqual(ShotTier.Perfect, tier, "Aim follows the camera: centered on the funnel is PERFECT");
            Assert.Greater(aim, 0.9f, "the roof camcorder aims along the camera's yaw");
        }
        finally
        {
            GameEvents.PhotoTaken -= onPhoto;
        }
    }

    [UnityTest]
    public IEnumerator CameraForwardAim_TruckFacingFunnel_CameraLookingAway_IsNotAimed()
    {
        // Arrange: the old truck-forward aim would score this shot 1.0.
        yield return StartRunWithHeldEF3();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var photo = Object.FindAnyObjectByType<PhotoTrigger>();
        Vector3 funnel = _tornado.transform.position;
        Place(truck, funnel + new Vector3(0f, 0f, -OptimalDistance), Quaternion.LookRotation(Vector3.forward));
        Transform cam = TakeOverMainCamera();
        cam.position = truck.transform.position + new Vector3(0f, 3f, -8f);
        cam.rotation = Quaternion.LookRotation(Vector3.right);
        yield return new WaitForFixedUpdate();

        float aim = -1f;
        System.Action<PhotoResult> onPhoto = r => aim = r.AimScore;
        GameEvents.PhotoTaken += onPhoto;
        try
        {
            // Act
            photo.Shoot();

            // Assert
            Assert.AreEqual(0f, aim, 1e-4f, "A camera looking 90° away scores no aim");
        }
        finally
        {
            GameEvents.PhotoTaken -= onPhoto;
        }
    }

    [UnityTest]
    public IEnumerator StormCam_CirclingHeldTornado_KeepsFunnelWithinFramingBudget()
    {
        // Arrange
        yield return StartRunWithHeldEF3();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        truck.InputEnabled = false;
        var rig = Object.FindAnyObjectByType<ChaseCameraRig>();
        var body = truck.GetComponent<Rigidbody>();
        Vector3 funnel = _tornado.transform.position;
        const float radius = 30f;
        const float speed = 8f;
        float omega = speed / radius;
        rig.SetStormCam(true);

        // Act: drive a full circle around the funnel, with real velocity for F14's lag term.
        float maxAngle = 0f;
        float elapsed = 0f;
        float duration = 2f * Mathf.PI / omega;
        while (elapsed < duration)
        {
            float a = elapsed * omega;
            Vector3 offset = new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a)) * radius;
            Vector3 tangent = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Place(truck, funnel + offset, Quaternion.LookRotation(tangent));
            body.linearVelocity = tangent * speed;
            yield return null;
            elapsed += Time.deltaTime;

            Transform cam = Camera.main.transform;
            if (elapsed > 1f)
                maxAngle = Mathf.Max(maxAngle, ChaseCameraMath.FlatAngle(cam.forward, funnel - cam.position));
        }

        // Assert
        Debug.Log($"[StormCam] max camera-to-funnel angle {maxAngle:F2}° over a full circle");
        Assert.AreSame(_tornado, rig.StormCamTarget, "Storm Cam should lock the tornado in range");
        Assert.LessOrEqual(maxAngle, StormCamAngleBudget);
        rig.SetStormCam(false);
    }

    [UnityTest]
    public IEnumerator StormCam_NoDisasterInRange_ShowsNoTarget()
    {
        // Arrange
        yield return StartRunWithHeldEF3();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var rig = Object.FindAnyObjectByType<ChaseCameraRig>();
        Place(truck, _tornado.transform.position + new Vector3(0f, 0f, -80f), Quaternion.identity);

        // Act
        rig.SetStormCam(true);
        yield return null;
        yield return null;

        // Assert
        Assert.IsTrue(rig.StormCamEnabled);
        Assert.IsNull(rig.StormCamTarget, "Nothing within 60 m: Storm Cam stays toggled with no target (E11)");
        rig.SetStormCam(false);
    }

    private TornadoController _tornado;

    /// <summary>Loads the scene, starts a run with random spawns off, and pins an EF3 Mature and stationary.</summary>
    private IEnumerator StartRunWithHeldEF3()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;

        var run = Object.FindAnyObjectByType<RunManager>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        run.StartRun();
        spawner.Stop();

        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        var prefab = (TornadoController)typeof(DisasterSpawner).GetField("_tornadoPrefab", Private).GetValue(spawner);
        var roster = (List<DisasterSpawner.RosterEntry>)typeof(DisasterSpawner).GetField("_roster", Private).GetValue(spawner);
        TornadoData ef3 = roster.Select(e => e.Data).First(d => d != null && d.EFRating == "EF3");
        Vector3 origin = truck.transform.position + new Vector3(0f, 0f, 40f);
        _tornado = Object.Instantiate(prefab, new Vector3(origin.x, 0f, origin.z), Quaternion.identity);
        _tornado.Initialize(ef3, truck.transform);
        _tornado.HoldMature = true;
        yield return null;
        yield return new WaitForFixedUpdate();
    }

    /// <summary>Stops Cinemachine driving the main camera so a test can pose it directly.</summary>
    private static Transform TakeOverMainCamera()
    {
        Camera main = Camera.main;
        var brain = main.GetComponent<CinemachineBrain>();
        if (brain != null) brain.enabled = false;
        return main.transform;
    }

    private static void Place(PlayerVehicle truck, Vector3 position, Quaternion rotation)
    {
        position.y = truck.transform.position.y;
        var rb = truck.GetComponent<Rigidbody>();
        rb.position = position;
        rb.rotation = rotation;
        truck.transform.SetPositionAndRotation(position, rotation);
    }
}
