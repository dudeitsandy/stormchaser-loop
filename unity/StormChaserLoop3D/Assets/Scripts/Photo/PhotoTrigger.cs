using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shutter. Each press uses one frame of film and scores the nearest in-range disaster:
/// base score × repeat-shot decay × "IN THE WIND" bonus. Broadcasts results via <see cref="GameEvents"/>.
/// See design/gdd/photo-scoring.md.
/// </summary>
public class PhotoTrigger : MonoBehaviour
{
    private const float OptimalDistance = 20f;
    private const float DistanceSigma = 10f;

    [SerializeField] private ScoreAccumulator _scoreAccumulator;
    [Tooltip("Disasters farther than this are out of range; pressing the shutter is a miss.")]
    [SerializeField] private float _maxRange = 60f;
    [Tooltip("Minimum seconds between shots.")]
    [SerializeField] private float _cooldown = 0.35f;

    [Header("Film")]
    [Tooltip("Frames per run. Misses use a frame too.")]
    [SerializeField] private int _filmPerRun = 24;

    [Header("Repeat Shots")]
    [Tooltip("Each rapid repeat of the same subject is worth this fraction of the previous one.")]
    [Range(0f, 1f)] [SerializeField] private float _repeatDecay = 0.5f;
    [Tooltip("Seconds for one repeat's penalty to wear off.")]
    [SerializeField] private float _repeatRecoverySeconds = 4f;

    [Header("In The Wind")]
    [Tooltip("Wind speed at the camera (units/sec) that earns the full bonus.")]
    [SerializeField] private float _windBonusFullAt = 15f;
    [Tooltip("Extra multiplier at full wind (1 = up to 2× score).")]
    [SerializeField] private float _windBonusMax = 1f;

    private StormChaserControls _controls;
    private CamcorderMount _mount;
    private RepeatPenalty _repeatPenalty;
    private float _nextShotTime;

    /// <summary>When false, shutter presses are ignored (title / results screens).</summary>
    public bool Armed { get; set; } = true;
    public int FilmRemaining { get; private set; }
    public int FilmCapacity => _filmPerRun;

    /// <summary>"IN THE WIND" multiplier a shot taken right now would get. The HUD meter reads this.</summary>
    public float CurrentWindMultiplier =>
        ScoringSystem.WindMultiplier(DisasterEntity.TotalWindAt(transform.position).magnitude,
            _windBonusFullAt, _windBonusMax);

    private void Awake()
    {
        _controls = new StormChaserControls();
        // Self-installs on the truck so scenes need no wiring; the viewfinder renders from the same mount.
        _mount = GetComponent<CamcorderMount>();
        if (_mount == null) _mount = gameObject.AddComponent<CamcorderMount>();
        _repeatPenalty = new RepeatPenalty(_repeatDecay, _repeatRecoverySeconds);
        FilmRemaining = _filmPerRun;
    }

    private void OnEnable()
    {
        _controls.Driving.Enable();
        _controls.Driving.Photograph.performed += OnPhotograph;
        GameEvents.RunStarted += OnRunStarted;
    }

    private void OnDisable()
    {
        _controls.Driving.Photograph.performed -= OnPhotograph;
        _controls.Driving.Disable();
        GameEvents.RunStarted -= OnRunStarted;
    }

    private void OnDestroy() => _controls.Dispose();

    private void Start() => GameEvents.RaiseFilmChanged(FilmRemaining, _filmPerRun);

    private void OnRunStarted()
    {
        FilmRemaining = _filmPerRun;
        _repeatPenalty = new RepeatPenalty(_repeatDecay, _repeatRecoverySeconds);
        GameEvents.RaiseFilmChanged(FilmRemaining, _filmPerRun);
    }

    private void OnPhotograph(InputAction.CallbackContext ctx) => Shoot();

    /// <summary>Takes a photo of the nearest in-range disaster, honoring <see cref="Armed"/>, cooldown, and film.</summary>
    public void Shoot()
    {
        if (!Armed || Time.time < _nextShotTime) return;
        _nextShotTime = Time.time + _cooldown;

        if (FilmRemaining <= 0)
        {
            GameEvents.RaiseOutOfFilm();
            return;
        }
        FilmRemaining--;
        GameEvents.RaiseFilmChanged(FilmRemaining, _filmPerRun);

        DisasterEntity nearest = FindNearest();
        if (nearest == null)
        {
            GameEvents.RaisePhotoMissed();
            return;
        }

        float aimScore = CalcAimScore(nearest.transform);
        float distanceScore = CalcDistanceScore(nearest.transform);
        float quality = ScoringSystem.CalculateQuality(aimScore, distanceScore);
        float baseScore = ScoringSystem.CalculatePhotoScore(aimScore, distanceScore, nearest.ThreatMultiplier);

        object subjectId = nearest;
        float repeat = _repeatPenalty.GetMultiplier(subjectId, Time.time);
        _repeatPenalty.Record(subjectId, Time.time);

        float wind = CurrentWindMultiplier;

        float score = baseScore * repeat * wind;
        _scoreAccumulator.AddScore(score);
        GameEvents.RaisePhotoTaken(new PhotoResult(score, aimScore, distanceScore, quality,
            ScoringSystem.GetTier(quality), nearest, nearest.transform.position, repeat, wind));
    }

    private DisasterEntity FindNearest()
    {
        DisasterEntity nearest = null;
        float minDist = _maxRange;

        foreach (DisasterEntity e in DisasterEntity.Active)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = e;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Framing curve from the roof camcorder: angle between its flattened forward and the flattened
    /// camcorder→subject direction. The camcorder aims along the main camera's yaw (vehicle-feel.md Rule 8)
    /// and the viewfinder renders from the same pose, so what the viewfinder shows is what is scored.
    /// </summary>
    private float CalcAimScore(Transform target)
    {
        _mount.GetPose(out Vector3 position, out Quaternion rotation);
        float angle = ChaseCameraMath.FlatAngle(rotation * Vector3.forward, target.position - position);
        return ScoringSystem.AimScore(angle);
    }

    private float CalcDistanceScore(Transform target)
    {
        float dist = Vector3.Distance(transform.position, target.position);
        float exponent = -0.5f * Mathf.Pow((dist - OptimalDistance) / DistanceSigma, 2f);
        return Mathf.Exp(exponent);
    }
}
