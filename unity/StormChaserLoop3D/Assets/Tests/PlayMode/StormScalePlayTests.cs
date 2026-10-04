using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// storm-director.md F3 in play (story 004): EF4 / EF5 toss inside their cores and not beyond, and a Forming
/// cell never hurts. AC-11 (EF3 drag at 15 m) lives in RunLoopSmokeTests.TornadoWind_ParkedNearEF3_IsDraggedNotTossed.
/// </summary>
public class StormScalePlayTests
{
    private const string SceneName = "VerificationScene";

    // Unload the gameplay scene so later tests that build their own empty world (VehicleImpactTests drops a
    // bare truck at the origin) don't land on this scene's ground.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        Scene empty = SceneManager.CreateScene("StormScaleEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    [UnityTest]
    public IEnumerator Ef5Mature_ParkedAt5m_IsTossedWithinOneSecond() // AC-12
    {
        bool tossed = false;
        yield return ParkNear("EF5", 5f, mature: true, seconds: 1f, t => tossed = t);
        Assert.IsTrue(tossed, "an EF5 core tosses at 5 m");
    }

    [UnityTest]
    public IEnumerator Ef4Mature_ParkedAt5m_IsTossedWithinOneSecond() // AC-13 (lift 0.75)
    {
        bool tossed = false;
        yield return ParkNear("EF4", 5f, mature: true, seconds: 1f, t => tossed = t);
        Assert.IsTrue(tossed);
    }

    [UnityTest]
    public IEnumerator Ef4Mature_ParkedAt8m_IsNotTossedAfterTwoSeconds() // AC-13 (lift 0.55)
    {
        bool tossed = true;
        yield return ParkNear("EF4", 8f, mature: true, seconds: 2f, t => tossed = t);
        Assert.IsFalse(tossed);
    }

    [UnityTest]
    public IEnumerator Ef5Mature_LiftAt17m_IsBelowTossThreshold() // AC-13 (lift 0.61)
    {
        // A parked truck does not stay at 17 m: the EF5 inflow (≈ 19 m/s) drags it into the 15.7 m toss zone
        // within 2 s, which is intended. AC-13's 17 m case checks the live lift field, not a parked truck.
        float lift = -1f;
        yield return ParkNear("EF5", 17f, mature: true, seconds: 0f, t => { }, (tornado, truck) =>
            lift = tornado.GetLiftFractionAt(tornado.transform.position + new Vector3(17f, 0f, 0f), 0.85f, 1.35f));
        Debug.Log($"[StormScale] EF5 lift at 17 m = {lift:F3}");
        Assert.AreEqual(0.61f, lift, 0.01f);
        Assert.Less(lift, 0.7f);
    }

    [UnityTest]
    public IEnumerator Ef5Forming_TruckInsideCore_NoDamageAndNotTossed() // AC-19
    {
        // Arrange
        int damaged = 0;
        System.Action<int, int> onDamage = (_, __) => damaged++;
        GameEvents.PlayerDamaged += onDamage;
        bool tossed = true;
        try
        {
            // Act: park on the axis of an EF5 held at Forming I = 0.9 for 2 s
            yield return ParkNear("EF5", 0.5f, mature: false, seconds: 2f, t => tossed = t);
        }
        finally
        {
            GameEvents.PlayerDamaged -= onDamage;
        }

        // Assert
        Assert.IsFalse(tossed, "no lift while Forming");
        Assert.AreEqual(0, damaged, "no damage while Forming");
    }

    // Loads the scene, stops random spawns, spawns one tornado of the given EF held Mature (or Forming at
    // I = 0.9), parks the truck d metres off its axis with no input and reports whether Tossed fired.
    private static IEnumerator ParkNear(string ef, float d, bool mature, float seconds, System.Action<bool> report,
                                        System.Action<TornadoController, PlayerVehicle> probe = null)
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;

        var run = Object.FindAnyObjectByType<RunManager>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        run.StartRun();
        spawner.Stop();
        truck.InputEnabled = false;

        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        var prefab = (TornadoController)typeof(DisasterSpawner).GetField("_tornadoPrefab", Private).GetValue(spawner);
        var roster = (List<DisasterSpawner.RosterEntry>)typeof(DisasterSpawner).GetField("_roster", Private).GetValue(spawner);
        TornadoData data = roster.Select(e => e.Data).First(x => x != null && x.EFRating == ef);
        Vector3 origin = truck.transform.position + new Vector3(0f, 0f, 40f);
        var tornado = Object.Instantiate(prefab, new Vector3(origin.x, 0f, origin.z), Quaternion.identity);
        tornado.Initialize(data, truck.transform);
        if (mature) tornado.HoldMature = true;
        else tornado.HoldFormingIntensity = 0.9f;
        yield return null;
        yield return new WaitForFixedUpdate();

        probe?.Invoke(tornado, truck);

        // Listen before placing the truck: a core toss fires on the very first physics step.
        bool tossed = false;
        System.Action onTossed = () => tossed = true;
        GameEvents.Tossed += onTossed;
        try
        {
            Vector3 spot = tornado.transform.position + new Vector3(d, 0f, 0f);
            spot.y = truck.transform.position.y;
            truck.Teleport(spot, Quaternion.identity);
            float t = 0f;
            while (t < seconds)
            {
                yield return new WaitForFixedUpdate();
                t += Time.fixedDeltaTime;
            }
        }
        finally
        {
            GameEvents.Tossed -= onTossed;
        }
        Debug.Log($"[StormScale] {ef} {(mature ? "Mature" : "Forming")} d={d} tossed={tossed}");
        report(tossed);
    }
}
