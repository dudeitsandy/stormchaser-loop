using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// run-goals-v1 story 001 measurement probe (not a regression test): how much *counted* airtime (vehicle-feel.md
/// E2, air above MinAirtimeHeight) the compact map can produce, from a standing jump, a full-speed boosted jump and
/// EF4 / EF5 tosses. Logs "[Airtime]" lines for production/qa/evidence/rg-airtime-evidence.md. Explicit: run with
/// -testFilter AirtimeProbeTests.
/// </summary>
[Explicit, Category("Probe")]
public class AirtimeProbeTests
{
    private const string SceneName = "VerificationScene";
    private static readonly FieldInfo Counted = typeof(VehicleModel).GetField("_countedAirSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo Total = typeof(VehicleModel).GetField("_airTimer", BindingFlags.NonPublic | BindingFlags.Instance);

    private sealed class Script : IVehicleInput
    {
        public VehicleInputFrame Frame;
        public VehicleInputFrame Read()
        {
            VehicleInputFrame f = Frame;
            Frame.JumpPressed = false; // one-step press
            return f;
        }
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        Scene empty = SceneManager.CreateScene("AirtimeProbeEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    [UnityTest]
    public IEnumerator StandingJump() => Measure("standing jump", runUpSeconds: 0f, boost: false, tossEf: null);

    [UnityTest]
    public IEnumerator BoostedJumpAtTopSpeed() => Measure("boosted jump at speed", runUpSeconds: 4f, boost: true, tossEf: null);

    [UnityTest]
    public IEnumerator Ef4Toss() => Measure("EF4 toss (5 m, Mature)", 0f, false, "EF4");

    [UnityTest]
    public IEnumerator Ef5Toss() => Measure("EF5 toss (5 m, Mature)", 0f, false, "EF5");

    private static IEnumerator Measure(string label, float runUpSeconds, bool boost, string tossEf)
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
        var run = Object.FindAnyObjectByType<RunManager>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        run.StartRun();
        spawner.Stop();
        var script = new Script();
        truck.InputSource = script;

        var styleAir = new List<float>();
        System.Action<StyleKind, float> onStyle = (k, a) => { if (k == StyleKind.Airtime) styleAir.Add(a); };
        GameEvents.StyleEvent += onStyle;
        float maxCounted = 0f, maxTotal = 0f, maxHeight = 0f, groundY = truck.transform.position.y;
        try
        {
            if (tossEf != null)
            {
                const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
                var prefab = (TornadoController)typeof(DisasterSpawner).GetField("_tornadoPrefab", Private).GetValue(spawner);
                var roster = (List<DisasterSpawner.RosterEntry>)typeof(DisasterSpawner).GetField("_roster", Private).GetValue(spawner);
                TornadoData data = roster.Select(e => e.Data).First(x => x != null && x.EFRating == tossEf);
                Vector3 o = truck.transform.position + new Vector3(0f, 0f, 40f);
                var tornado = Object.Instantiate(prefab, new Vector3(o.x, 0f, o.z), Quaternion.identity);
                tornado.Initialize(data, truck.transform);
                tornado.HoldMature = true;
                yield return null;
                yield return new WaitForFixedUpdate();
                Vector3 spot = tornado.transform.position + new Vector3(5f, 0f, 0f);
                spot.y = truck.transform.position.y;
                truck.Teleport(spot, Quaternion.identity);
            }
            else
            {
                // Run up along the road (+X), then one jump press.
                script.Frame.Throttle = runUpSeconds > 0f ? 1f : 0f;
                script.Frame.Boost = boost;
                for (float t = 0f; t < runUpSeconds; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
                for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate(); // settle suspension
                script.Frame.JumpPressed = true;
            }

            for (float t = 0f; t < 6f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                maxCounted = Mathf.Max(maxCounted, (float)Counted.GetValue(truck.Model));
                maxTotal = Mathf.Max(maxTotal, (float)Total.GetValue(truck.Model));
                maxHeight = Mathf.Max(maxHeight, truck.transform.position.y - groundY);
            }
        }
        finally { GameEvents.StyleEvent -= onStyle; }

        string events = styleAir.Count == 0 ? "none" : string.Join(", ", styleAir.Select(a => a.ToString("F2")));
        Debug.Log($"[Airtime] {label}: counted (above MinAirtimeHeight) max {maxCounted:F2} s, total air max {maxTotal:F2} s, " +
                  $"peak body rise {maxHeight:F2} m, Airtime style events: {events}");
        Assert.Pass($"{label}: counted {maxCounted:F2} s, total {maxTotal:F2} s");
    }
}
