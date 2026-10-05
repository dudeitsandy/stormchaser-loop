using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Title attract mode in the real scene: on the title a showcase storm runs, the truck is frozen and effects are
/// muted; starting a run hands back a normal run (truck free, audio back, the demo storm replaced).
/// </summary>
public class TitleAttractTests
{
    private const string SceneName = "VerificationScene";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        TitleAttract.AllowInBatchmode = true;
        ProfileStore.Shared = new ProfileStore(new MemoryProfileStorage());
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        TitleAttract.AllowInBatchmode = false;
        ProfileStore.Shared = null;
        GameAudio.AttractMute = false;
        Time.timeScale = 1f;
        Scene empty = SceneManager.CreateScene("AttractEmpty");
        SceneManager.SetActiveScene(empty);
        Scene loaded = SceneManager.GetSceneByName(SceneName);
        if (loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
    }

    [UnityTest]
    public IEnumerator Title_RunsAShowcaseStorm_TruckFrozen_EffectsMuted_ThenARunIsNormal()
    {
        var run = Object.FindAnyObjectByType<RunManager>();
        var spawner = Object.FindAnyObjectByType<DisasterSpawner>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var rb = truck.GetComponent<Rigidbody>();
        Assert.AreEqual(RunManager.State.Title, run.Current);
        Assert.IsTrue(TitleAttract.Active);
        Assert.AreEqual(RigidbodyConstraints.FreezeAll, rb.constraints, "the truck sits out the demo");
        Assert.AreEqual(0f, GameAudio.EffectsVolume, "only the title music plays");
        long demoSeed = (long)spawner.Director.Plan.Seed;
        CollectionAssert.Contains(new long[] { 6, 29, 37 }, demoSeed);

        // The demo fast-forwards to 6 s before the anchor's touchdown: it is on the ground well inside 10 s.
        float waited = 0f;
        while (waited < 10f && !spawner.Director.LiveCells.Any(c => c.Cell.Role == StormCellRole.Anchor
                                                                    && StormDirector.TouchedDown(c.Cell, c.Age)))
        {
            waited += Time.deltaTime;
            yield return null;
        }
        Assert.Less(waited, 10f, "the showcase funnel touches down within 10 s of the title");

        run.StartRun();
        yield return null;
        yield return null;
        Assert.IsFalse(TitleAttract.Active);
        Assert.AreNotEqual(RigidbodyConstraints.FreezeAll, rb.constraints, "truck free");
        Assert.AreEqual(ProfileStore.Shared.Settings.EffectsVolume, GameAudio.EffectsVolume, 1e-4f, "audio back");
        Assert.AreEqual(RunManager.State.Running, run.Current);
        Assert.AreEqual(0, spawner.Director.LiveCells.Count(c => c.Age > 1f), "the demo storm was replaced by the run's plan");
        Assert.AreEqual(0, Object.FindObjectsByType<TornadoController>(FindObjectsSortMode.None)
                              .Count(t => t.DirectorDriven && spawner.Director.LiveCells.All(c => c.Tornado != t)),
                        "no demo tornado left behind");
    }

    private sealed class ScriptedInput : IVehicleInput
    {
        public VehicleInputFrame Frame;
        public VehicleInputFrame Read() => Frame;
    }

    /// <summary>
    /// Regression (Andy, 0.8.2 Windows, 2026-10-05): after sitting on the title while the demo storm ran, the run's
    /// truck steered its wheels but the body would not turn.
    /// </summary>
    [UnityTest]
    public IEnumerator AfterALongTitle_TheRunsTruckStillDrivesAndTurns()
    {
        var run = Object.FindAnyObjectByType<RunManager>();
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var rb = truck.GetComponent<Rigidbody>();
        yield return new WaitForSeconds(35f); // the demo funnel reaches the frozen truck around 30 s

        run.StartRun();
        yield return null;
        var input = new ScriptedInput { Frame = new VehicleInputFrame { Throttle = 1f, Steer = 1f } };
        truck.InputSource = input;
        Vector3 start = rb.position;
        float yaw0 = rb.rotation.eulerAngles.y;
        yield return new WaitForSeconds(3f);

        string diag = $"constraints {rb.constraints}, state {truck.State}, kinematic {rb.isKinematic}, " +
                      $"inertia {rb.inertiaTensor}, angVel {rb.angularVelocity}";
        Assert.AreEqual(RigidbodyConstraints.None, rb.constraints, diag);
        Assert.Greater(rb.inertiaTensor.y, 100f, "the hand-set inertia survived the title freeze: " + diag);
        Assert.Greater(Vector3.Distance(start, rb.position), 3f, "the truck drives: " + diag);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(yaw0, rb.rotation.eulerAngles.y)), 20f, "the truck turns: " + diag);
    }

    [UnityTest]
    public IEnumerator Overlays_PauseTheFlyover_AndReturnToTheTitleKeepsAttractRunning()
    {
        var run = Object.FindAnyObjectByType<RunManager>();
        var attract = Object.FindAnyObjectByType<TitleAttract>();
        run.OpenSettings(fromTitle: true);
        Assert.IsTrue(attract.Paused);
        run.CloseSettings();
        yield return null;
        Assert.IsFalse(attract.Paused);
        Assert.IsTrue(TitleAttract.Active, "closing an overlay doesn't restart or stop the attract mode");
    }
}
