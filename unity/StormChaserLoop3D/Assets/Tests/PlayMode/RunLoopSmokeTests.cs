using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Drives the real gameplay scene end to end: title → run → photo → three hits → wreck → results.</summary>
public class RunLoopSmokeTests
{
    private const string SceneName = "VerificationScene";

    [TearDown]
    public void TearDown() => Time.timeScale = 1f;

    [UnityTest]
    public IEnumerator FullRun_PhotoThenWreck_ReachesResults()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;

        var run = Object.FindAnyObjectByType<RunManager>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var health = truck.GetComponent<VehicleHealth>();
        var photo = Object.FindAnyObjectByType<PhotoTrigger>();
        var score = Object.FindAnyObjectByType<ScoreAccumulator>();
        Assert.AreEqual(RunManager.State.Title, run.Current, "Scene should open on the title screen");

        int photosSeen = 0;
        ShotTier lastTier = ShotTier.Glancing;
        RunSummary? summary = null;
        System.Action<PhotoResult> onPhoto = r => { photosSeen++; lastTier = r.Tier; };
        System.Action<RunSummary> onEnd = s => summary = s;
        GameEvents.PhotoTaken += onPhoto;
        GameEvents.RunEnded += onEnd;

        try
        {
            run.StartRun();
            Assert.AreEqual(RunManager.State.Running, run.Current);

            // First spawn is 1.5s in; give it time to finish forming (3s) so it can do damage.
            yield return new WaitForSeconds(5f);
            Assert.Greater(DisasterEntity.Active.Count, 0, "A tornado should have spawned");
            var tornado = (TornadoController)DisasterEntity.Active[0];
            Debug.Log($"[Smoke] {tornado.EFRating} intensity={tornado.Intensity:F2} radius={tornado.DamageRadius:F2}");

            // Park 20 units away, facing it: textbook PERFECT shot.
            Vector3 t = tornado.transform.position;
            Place(truck, t + new Vector3(0f, 0f, -20f), Quaternion.LookRotation(Vector3.forward));
            yield return new WaitForFixedUpdate();
            photo.Shoot();
            Assert.AreEqual(1, photosSeen, "Photo event should fire");
            Assert.AreEqual(1, score.PhotoCount);
            Assert.AreEqual(photo.FilmCapacity - 1, photo.FilmRemaining);
            Assert.Greater(score.TotalScore, 0f);
            Assert.AreEqual(ShotTier.Perfect, lastTier, "Centered at optimal distance should be PERFECT");

            // Immediate repeat of the same tornado is discounted.
            float firstShot = score.TotalScore;
            yield return new WaitForSeconds(0.4f);
            photo.Shoot();
            float secondShot = score.TotalScore - firstShot;
            Debug.Log($"[Smoke] first={firstShot:F0} repeat={secondShot:F0} film={photo.FilmRemaining}");
            Assert.Less(secondShot, firstShot * 0.75f, "Repeat shot should be discounted");
            Assert.AreEqual(photo.FilmCapacity - 2, photo.FilmRemaining, "Each shot uses a frame");

            for (int hit = 0; hit < health.MaxHealth; hit++)
            {
                Place(truck, tornado.transform.position, truck.transform.rotation);
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.AreEqual(health.MaxHealth - hit - 1, health.CurrentHealth, $"Hit {hit + 1} should cost 1 HP");
                yield return new WaitForSecondsRealtime(1.6f); // clear i-frames
                if (run.Current != RunManager.State.Running) break;
            }

            Assert.IsTrue(health.IsWrecked);
            Assert.IsTrue(summary.HasValue, "RunEnded should fire on wreck");
            Assert.IsTrue(summary.Value.Wrecked);
            Assert.AreEqual(2, summary.Value.PhotosTaken);

            yield return new WaitForSecondsRealtime(1.5f); // slow-mo, then results
            Assert.AreEqual(RunManager.State.Results, run.Current);
            Assert.AreEqual(0f, Time.timeScale);
        }
        finally
        {
            GameEvents.PhotoTaken -= onPhoto;
            GameEvents.RunEnded -= onEnd;
        }
    }

    /// <summary>vehicle-feel.md AC: parked (no input) 15 m from an EF3 → > 0.5 m horizontal in 1 s; never Tossed.</summary>
    [UnityTest]
    public IEnumerator TornadoWind_ParkedNearEF3_IsDraggedNotTossed()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;

        var run = Object.FindAnyObjectByType<RunManager>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        run.StartRun();
        spawner.Stop(); // this test spawns its own EF3; random spawns would vary the wind

        // Spawn an EF3 from the spawner's own prefab and roster, away from the truck, and let it mature.
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        var prefab = (TornadoController)typeof(DisasterSpawner).GetField("_tornadoPrefab", Private).GetValue(spawner);
        var roster = (List<DisasterSpawner.RosterEntry>)typeof(DisasterSpawner).GetField("_roster", Private).GetValue(spawner);
        TornadoData ef3 = roster.Select(e => e.Data).First(d => d != null && d.EFRating == "EF3");
        Vector3 origin = truck.transform.position + new Vector3(0f, 0f, 60f);
        TornadoController tornado = Object.Instantiate(prefab, new Vector3(origin.x, 0f, origin.z), Quaternion.identity);
        tornado.Initialize(ef3, truck.transform);
        // Pin it Mature and stationary: a wandering funnel (random heading, player pull) moved several m
        // during the 1 s measurement, so the 15 m distance and the wind reading varied run to run (flaky).
        tornado.HoldMature = true;
        yield return null;
        yield return new WaitForFixedUpdate();

        // Park 15 m off the funnel's axis, no input.
        Vector3 start = tornado.transform.position + new Vector3(15f, 0f, 0f);
        Place(truck, start, Quaternion.identity);
        yield return new WaitForFixedUpdate();
        Vector3 placed = truck.transform.position;

        bool tossed = false;
        System.Action onTossed = () => tossed = true;
        GameEvents.Tossed += onTossed;
        try
        {
            float t = 0f;
            while (t < 1f)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }
        finally
        {
            GameEvents.Tossed -= onTossed;
        }

        float distance = Vector2.Distance(new Vector2(tornado.transform.position.x, tornado.transform.position.z),
                                          new Vector2(start.x, start.z));
        Assert.AreEqual(15f, distance, 0.01f, "the funnel must not move during the measurement");
        Vector3 delta = truck.transform.position - placed;
        float moved = new Vector2(delta.x, delta.z).magnitude;
        Debug.Log($"[Smoke] {tornado.EFRating} phase={tornado.Phase} windR={tornado.WindRadius:F1} " +
                  $"wind={truck.CurrentWind.magnitude:F2} moved={moved:F2} tossed={tossed}");
        Assert.Greater(moved, 0.5f, "EF3 wind at 15 m should drag a parked truck");
        Assert.IsFalse(tossed, "EF3 at 15 m must never toss");
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
