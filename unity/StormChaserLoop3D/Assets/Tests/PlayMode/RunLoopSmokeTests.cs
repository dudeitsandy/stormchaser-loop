using System.Collections;
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

    [UnityTest]
    public IEnumerator TornadoWind_DragsAParkedTruck()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;

        var run = Object.FindAnyObjectByType<RunManager>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        run.StartRun();
        yield return new WaitForSeconds(5f);
        Assert.Greater(DisasterEntity.Active.Count, 0, "A tornado should have spawned");
        var tornado = (TornadoController)DisasterEntity.Active[0];

        // Park inside the wind field but outside the damage radius, no throttle.
        float standoff = (tornado.DamageRadius + tornado.WindRadius) * 0.5f;
        Vector3 start = tornado.transform.position + new Vector3(standoff, 0f, 0f);
        Place(truck, start, Quaternion.identity);
        yield return new WaitForFixedUpdate();
        Vector3 placed = truck.transform.position;

        yield return new WaitForSeconds(0.75f);
        float moved = Vector3.Distance(placed, truck.transform.position);
        Debug.Log($"[Smoke] {tornado.EFRating} windR={tornado.WindRadius:F1} standoff={standoff:F1} " +
                  $"wind={truck.CurrentWind.magnitude:F2} moved={moved:F2}");
        Assert.Greater(truck.CurrentWind.magnitude, 0.5f, "Truck should feel wind inside the field");
        Assert.Greater(moved, 0.5f, "Wind should move a truck with no input");
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
