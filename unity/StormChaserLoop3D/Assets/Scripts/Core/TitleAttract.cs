using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Title attract mode (Andy 2026-10-05, via stormchaser-38): while <see cref="RunManager"/> is on the title, a slow
/// drone camera orbits the arena and the Storm Director runs a showcase seed so a funnel drops, the sky darkens and
/// rain moves. All game audio is muted (title music only). Fully isolated: the truck is frozen, the HUD is hidden
/// and storm events are ignored by the HUD crawl, goals and the radio duck. On run start the camera eases into the
/// chase camera (Cinemachine blend) and <see cref="DisasterSpawner.Begin"/> replaces the demo with the run's real seed.
/// </summary>
public sealed class TitleAttract : MonoBehaviour
{
    /// <summary>True while the attract mode runs. Presentation reads it to keep storm visuals alive on the title.</summary>
    public static bool Active { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active = false;

    [Header("Demo storm (data, not code)")]
    [Tooltip("Showcase seeds: anchor EF4+ 60–80 m from the truck (AttractSeedProbe, 2026-10-05). Rotated in order.")]
    [SerializeField] private long[] _showcaseSeeds = { 6, 29, 37 };
    [Tooltip("The demo fast-forwards to this many seconds before the anchor touches down, so a funnel is on the ground within seconds.")]
    [SerializeField] private float _touchdownLeadSeconds = 6f;
    [Tooltip("Seconds per showcase seed before rotating to the next.")]
    [SerializeField] private float _demoSeconds = 75f;

    [Header("Flyover camera")]
    [SerializeField] private float _orbitRadius = 34f;
    [SerializeField] private float _orbitHeight = 14f;
    [SerializeField] private float _orbitDegreesPerSecond = 5f;
    [SerializeField] private float _fieldOfView = 50f;
    [Tooltip("How far the look point leans from the truck toward the anchor storm (0 = truck, 1 = storm).")]
    [Range(0f, 1f)] [SerializeField] private float _stormFraming = 0.85f;
    [Tooltip("How far the orbit centre moves from the truck toward the anchor storm.")]
    [Range(0f, 1f)] [SerializeField] private float _orbitTowardStorm = 0.5f;
    [Tooltip("Turns the camera off the storm so the funnel sits in the open right margin, clear of the centred title column.")]
    [SerializeField] private float _screenOffsetDegrees = 30f;
    [SerializeField] private int _priority = 100;

    private DisasterSpawner _spawner;
    private PlayerVehicle _truck;
    private Rigidbody _rb;
    private RigidbodyConstraints _savedConstraints;
    private Vector3 _savedInertia;
    private Quaternion _savedInertiaRotation;
    private Vector3 _savedCenterOfMass;
    private CinemachineCamera _cam;
    private float _yaw;
    private float _clock;
    private float _demoStarted;
    private int _seedIndex;
    private Vector3 _lookPoint;
    private Vector3 _centre;

    /// <summary>Holds the flyover still (Settings and Career overlays on the title).</summary>
    public bool Paused { get; set; }

    /// <summary>Batchmode test runs skip the demo storm unless a test opts in (it would leave tornadoes in their scene).</summary>
    public static bool AllowInBatchmode { get; set; }

    /// <summary>Starts the attract mode (idempotent: the title re-enters after overlays close).</summary>
    public void Begin(DisasterSpawner spawner, PlayerVehicle truck)
    {
        if (Active) return;
        if (Application.isBatchMode && !AllowInBatchmode) return;
        Active = true;
        Paused = false;
        _spawner = spawner;
        _truck = truck;
        GameAudio.AttractMute = true;

        // The truck sits out the demo: no wind, lift or toss can move it before the run starts.
        _rb = truck != null ? truck.GetComponent<Rigidbody>() : null;
        if (_rb != null)
        {
            // FreezeAll zeroes the hand-set inertia tensor and restoring the constraints doesn't bring it back: the
            // run's truck then drove but could not turn (0.8.2, Windows). Keep the mass properties and restore them.
            _savedConstraints = _rb.constraints;
            _savedInertia = _rb.inertiaTensor;
            _savedInertiaRotation = _rb.inertiaTensorRotation;
            _savedCenterOfMass = _rb.centerOfMass;
            _rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (_cam == null)
        {
            var go = new GameObject("TitleFlyoverCam");
            _cam = go.AddComponent<CinemachineCamera>();
        }
        _cam.Lens.FieldOfView = _fieldOfView;
        _cam.Priority = _priority;
        _lookPoint = TruckPosition() + Vector3.up * 2f;
        _centre = TruckPosition();
        PlaceCamera(0f);
        StartDemoStorm();
    }

    /// <summary>Run start: hand the camera back (Cinemachine blends to the chase camera), unfreeze the truck, unmute.</summary>
    public void End()
    {
        if (!Active) return;
        Active = false;
        GameAudio.AttractMute = false;
        if (_rb != null)
        {
            _rb.constraints = _savedConstraints;
            _rb.centerOfMass = _savedCenterOfMass;
            _rb.inertiaTensor = _savedInertia;
            _rb.inertiaTensorRotation = _savedInertiaRotation;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        if (_cam != null)
        {
            _cam.Priority = -1;
            Destroy(_cam.gameObject, 3f); // after the brain's 2 s blend
            _cam = null;
        }
        // The demo storm is cleared by DisasterSpawner.Begin (the director's restart removes it).
    }

    private void OnDestroy()
    {
        if (Active) GameAudio.AttractMute = false;
        Active = false;
    }

    private void Update()
    {
        if (!Active) return;
        float dt = Time.unscaledDeltaTime;
        _clock += dt;
        if (!Paused) PlaceCamera(dt);
        if (_showcaseSeeds.Length > 1 && _clock - _demoStarted > _demoSeconds) StartDemoStorm();
    }

    private void StartDemoStorm()
    {
        _demoStarted = _clock;
        if (_spawner == null || _showcaseSeeds.Length == 0) return;
        long seed = _showcaseSeeds[_seedIndex % _showcaseSeeds.Length];
        _seedIndex++;
        StormDirector director = _spawner.BeginDemo(seed);
        if (director == null || director.Plan == null) return;
        float touchdown = 0f;
        foreach (PlannedCell c in director.Plan.Cells)
            if (c.Role == StormCellRole.Anchor && !c.Dropped) { touchdown = c.SpawnTime + c.Form; break; }
        float skip = touchdown - _touchdownLeadSeconds;
        if (skip > 0f) director.Tick(skip);
    }

    private void PlaceCamera(float dt)
    {
        if (_cam == null) return;
        _yaw += _orbitDegreesPerSecond * dt;
        TornadoController anchor = AnchorTornado();
        Vector3 truck = TruckPosition();
        // Orbit between the truck and the storm, and look mostly at the funnel's mid-height; both eased so a new
        // storm (or the next showcase seed) glides into frame rather than jerking it.
        Vector3 centreTarget = anchor != null ? Vector3.Lerp(truck, anchor.transform.position, _orbitTowardStorm) : truck;
        Vector3 lookTarget = truck + Vector3.up * 2f;
        if (anchor != null) lookTarget = Vector3.Lerp(lookTarget, anchor.transform.position + Vector3.up * 12f, _stormFraming);
        float ease = dt > 0f ? 1f - Mathf.Exp(-dt * 0.8f) : 1f;
        _centre = Vector3.Lerp(_centre, new Vector3(centreTarget.x, truck.y, centreTarget.z), ease);
        _lookPoint = Vector3.Lerp(_lookPoint, lookTarget, ease);
        float height = _orbitHeight + 2.5f * Mathf.Sin(_clock * 0.21f);
        Vector3 offset = Quaternion.Euler(0f, _yaw, 0f) * new Vector3(0f, 0f, -_orbitRadius);
        _cam.transform.position = _centre + offset + Vector3.up * height;
        _cam.transform.rotation = Quaternion.AngleAxis(-_screenOffsetDegrees, Vector3.up)
                                  * Quaternion.LookRotation(_lookPoint - _cam.transform.position, Vector3.up);
        _cam.Lens.FieldOfView = _fieldOfView + 4f * Mathf.Sin(_clock * 0.13f);
    }

    private Vector3 TruckPosition() => _truck != null ? _truck.transform.position : Vector3.zero;

    private TornadoController AnchorTornado()
    {
        StormDirector d = _spawner != null ? _spawner.Director : null;
        if (d == null) return null;
        foreach (LiveStormCell c in d.LiveCells)
            if (c.Cell.Role == StormCellRole.Anchor && c.Tornado != null) return c.Tornado;
        return null;
    }
}
