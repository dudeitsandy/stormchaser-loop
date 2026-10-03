using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Opt-in performance-capture harness for storm presentation (AC-29, X7-06). URL <c>?stormHarness=hold</c>,
/// desktop <c>-stormHarness=hold</c>. Once a run has a tornado, it holds that tornado in Mature, parks it
/// <see cref="_distance"/> m ahead of the truck, and stops the spawner and the session timer, so a capture
/// can run for as long as needed. Pair it with Codex's <c>stormVisualSpike=wedge</c> for the EF5 wedge.
/// Never active in normal play.
/// </summary>
public sealed class StormCaptureHarness : MonoBehaviour
{
    private const string Arg = "stormHarness=hold";
    [SerializeField] private float _distance = 35f;
    private TornadoController _held;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);

    private static void Install()
    {
        if (!Requested()) return;
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<StormCaptureHarness>() != null) return;
        new GameObject(nameof(StormCaptureHarness)).AddComponent<StormCaptureHarness>();
    }

    private static bool Requested()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg == "-" + Arg) return true;
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token == Arg) return true;
        return false;
    }

    // Polls until the first tornado exists; one FindAnyObjectByType per frame only while nothing is held.
    private void Update()
    {
        if (_held != null) return;
        var tornado = FindAnyObjectByType<TornadoController>();
        var vehicle = FindAnyObjectByType<PlayerVehicle>();
        if (tornado == null || vehicle == null) return;

        _held = tornado;
        Vector3 forward = vehicle.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
        Vector3 p = vehicle.transform.position + forward.normalized * _distance;
        tornado.transform.position = new Vector3(p.x, tornado.transform.position.y, p.z);
        tornado.HoldMature = true;

        var spawner = FindAnyObjectByType<DisasterSpawner>();
        if (spawner != null) spawner.Stop();
        var timer = FindAnyObjectByType<SessionTimer>();
        if (timer != null) timer.Stop();
        Debug.Log($"[StormCaptureHarness] Holding {tornado.EFRating} Mature at {_distance} m; spawner and session timer stopped.");
    }
}
