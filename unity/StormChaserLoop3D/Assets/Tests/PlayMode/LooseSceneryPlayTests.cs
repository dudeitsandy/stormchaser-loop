using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// X7-07 live acceptance (AGENTS.md 2026-10-03): Codex's knock-loose props against the real truck in the
/// shipping scene. A boosted hit (≥ 50 MPH) sends light props flying with no impact reported (E13) while the
/// truck keeps most of its speed; a 250 kg bale flies but may report a light impact (reconciled with Codex);
/// a slow push moves a prop without launching it; Retry reinstalls every prop at its cold-boot place.
/// </summary>
public class LooseSceneryPlayTests
{
    private const string SceneName = "ArtTest";
    private const float BoostedSpeed = 22.4f; // 50 MPH
    private readonly List<ImpactInfo> _impacts = new List<ImpactInfo>();
    private void OnImpact(ImpactInfo info) => _impacts.Add(info);

    [SetUp]
    public void SetUp()
    {
        _impacts.Clear();
        GameEvents.VehicleImpact += OnImpact;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        GameEvents.VehicleImpact -= OnImpact;
        Time.timeScale = 1f;
        Scene empty = SceneManager.CreateScene("LooseSceneryEmpty");
        SceneManager.SetActiveScene(empty);
        Scene art = SceneManager.GetSceneByName(SceneName);
        if (art.isLoaded) yield return SceneManager.UnloadSceneAsync(art);
    }

    [UnityTest]
    public IEnumerator LightProp_BoostedHit_FliesWithNoImpact_TruckKeepsSpeed(
        [Values("RuralMailbox", "RuralRoadSign", "RuralCrates")] string propName)
    {
        // Arrange
        yield return LoadScene();
        GameObject prop = GameObject.Find(propName);
        Assert.IsNotNull(prop, $"scatter should place {propName}");
        var body = prop.GetComponent<Rigidbody>();
        Assert.IsNotNull(body, "loose prop is one rigidbody");
        Assert.AreEqual(ImpactKind.Destructible, prop.GetComponent<ImpactSurface>().Kind);

        // Act
        PlayerVehicle truck = null;
        float propPeak = 0f;
        yield return Ram(prop.transform.position, BoostedSpeed, t => truck = t,
                         () => propPeak = Mathf.Max(propPeak, body.linearVelocity.magnitude));

        // Assert
        Debug.Log($"[Loose] {propName} mass={body.mass:F0} peak={propPeak:F1} truck={truck.CurrentSpeed:F1} impacts={_impacts.Count}");
        Assert.Greater(propPeak, 5f, "a boosted hit should send it flying");
        Assert.Greater(truck.CurrentSpeed, 15f, "a light prop barely slows the truck");
        Assert.AreEqual(0, _impacts.Count, "E13: light props report no impact");
    }

    [UnityTest]
    public IEnumerator HayBale_BoostedHit_FliesAndCostsNoHp()
    {
        // Arrange
        yield return LoadScene();
        GameObject bale = GameObject.Find("RuralHayBale");
        Assert.IsNotNull(bale, "scatter should place bales");
        var body = bale.GetComponent<Rigidbody>();
        var health = Object.FindAnyObjectByType<VehicleHealth>();

        // Act
        PlayerVehicle truck = null;
        float peak = 0f;
        yield return Ram(bale.transform.position, BoostedSpeed, t => truck = t,
                         () => peak = Mathf.Max(peak, body.linearVelocity.magnitude));

        // Assert: a 250 kg bale thuds (impact allowed), but flies and costs no HP while HP cost is off.
        Debug.Log($"[Loose] bale mass={body.mass:F0} peak={peak:F1} truck={truck.CurrentSpeed:F1} impacts={_impacts.Count}");
        Assert.Greater(peak, 5f, "the bale should be knocked loose");
        Assert.Greater(truck.CurrentSpeed, 8f, "the truck should plough through, not stop");
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
    }

    [UnityTest]
    public IEnumerator Mailbox_SlowPush_MovesButIsNotLaunched()
    {
        // Arrange
        yield return LoadScene();
        GameObject prop = GameObject.Find("RuralMailbox");
        var body = prop.GetComponent<Rigidbody>();
        Vector3 start = body.position;

        // Act: a steady 3 m/s push (no throttle coasts to a stop before contact)
        float peakSpeed = 0f, peakRise = 0f;
        yield return Ram(prop.transform.position, 3f, _ => { },
                         () =>
                         {
                             peakSpeed = Mathf.Max(peakSpeed, body.linearVelocity.magnitude);
                             peakRise = Mathf.Max(peakRise, body.position.y - start.y);
                         }, startDistance: 3f, holdSpeed: true);

        // Assert
        Vector3 moved = body.position - start;
        Debug.Log($"[Loose] slow push moved={new Vector2(moved.x, moved.z).magnitude:F2} m peak={peakSpeed:F1} rise={peakRise:F2}");
        Assert.Greater(new Vector2(moved.x, moved.z).magnitude, 0.2f, "a push should move it");
        Assert.Less(peakSpeed, 7f, "a 3 m/s push must not launch it");
        Assert.Less(peakRise, 1f);
    }

    [UnityTest]
    public IEnumerator Retry_ReinstallsLooseProps_AtColdBootPlaces()
    {
        // Arrange: cold boot, then knock a mailbox away during a run.
        yield return LoadScene();
        string coldBoot = PropSnapshot();
        Object.FindAnyObjectByType<RunManager>().StartRun();
        GameObject prop = GameObject.Find("RuralMailbox");
        yield return Ram(prop.transform.position, BoostedSpeed, _ => { }, () => { });
        Assert.AreNotEqual(coldBoot, PropSnapshot(), "the hit should have moved something");

        // Act: the results screen's retry path
        typeof(RunManager).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { true });
        for (int i = 0; i < 5; i++) yield return null;

        // Assert
        Assert.AreEqual(coldBoot, PropSnapshot(), "retry should rebuild props exactly where a cold boot puts them");
    }

    private static IEnumerator LoadScene()
    {
        SceneManager.LoadScene(SceneName);
        for (int i = 0; i < 5; i++) yield return null; // scatter installs after scene load
    }

    // Truck startDistance short of the prop, facing it, launched at speed; sample() runs every physics step.
    // Stops once the truck is 3 m past the prop's centre (or after 3 s), so whatever scenery stands behind a
    // prop (crates sit by barns, signs by poles) never counts as this prop's impact or slowdown.
    private IEnumerator Ram(Vector3 target, float speed, System.Action<PlayerVehicle> got, System.Action sample,
                            float startDistance = 7f, bool holdSpeed = false)
    {
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        got(truck);
        truck.InputEnabled = false;
        Vector3 axis = target - truck.transform.position;
        axis.y = 0f;
        Vector3 dir = axis.normalized;
        Vector3 flatTarget = new Vector3(target.x, truck.transform.position.y, target.z);
        Vector3 start = flatTarget - dir * startDistance;
        truck.Teleport(start, Quaternion.LookRotation(dir));
        var rb = truck.GetComponent<Rigidbody>();
        yield return new WaitForFixedUpdate();
        _impacts.Clear();
        rb.linearVelocity = dir * speed;
        for (int i = 0; i < 150; i++)
        {
            if (holdSpeed) rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);
            yield return new WaitForFixedUpdate();
            sample();
            if (Vector3.Dot(truck.transform.position - flatTarget, dir) > 3f) break;
        }
    }

    private static string PropSnapshot()
    {
        string[] names = { "RuralHayBale", "RuralMailbox", "RuralRoadSign", "RuralCrates" };
        return string.Join("|", Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)
            .Where(b => names.Contains(b.gameObject.name))
            .Select(b => $"{b.gameObject.name}@{b.position.x:F1},{b.position.z:F1}")
            .OrderBy(s => s));
    }
}
