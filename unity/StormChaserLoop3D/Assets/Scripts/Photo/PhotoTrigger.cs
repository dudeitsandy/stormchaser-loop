using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Scores the nearest disaster when the shutter is pressed and broadcasts the result via <see cref="GameEvents"/>.</summary>
public class PhotoTrigger : MonoBehaviour
{
    private const float OptimalDistance = 20f;
    private const float DistanceSigma = 10f;

    [SerializeField] private ScoreAccumulator _scoreAccumulator;
    [Tooltip("Disasters farther than this are out of range; pressing the shutter is a miss.")]
    [SerializeField] private float _maxRange = 60f;
    [Tooltip("Minimum seconds between shots.")]
    [SerializeField] private float _cooldown = 0.35f;

    private StormChaserControls _controls;
    private float _nextShotTime;

    /// <summary>When false, shutter presses are ignored (title / results screens).</summary>
    public bool Armed { get; set; } = true;

    private void Awake()
    {
        _controls = new StormChaserControls();
    }

    private void OnEnable()
    {
        _controls.Driving.Enable();
        _controls.Driving.Photograph.performed += OnPhotograph;
    }

    private void OnDisable()
    {
        _controls.Driving.Photograph.performed -= OnPhotograph;
        _controls.Driving.Disable();
    }

    private void OnPhotograph(InputAction.CallbackContext ctx) => Shoot();

    /// <summary>Takes a photo of the nearest in-range disaster, honoring <see cref="Armed"/> and the cooldown.</summary>
    public void Shoot()
    {
        if (!Armed || Time.time < _nextShotTime) return;
        _nextShotTime = Time.time + _cooldown;

        DisasterEntity nearest = FindNearest();
        if (nearest == null)
        {
            GameEvents.RaisePhotoMissed();
            return;
        }

        float aimScore = CalcAimScore(nearest.transform);
        float distanceScore = CalcDistanceScore(nearest.transform);
        float quality = ScoringSystem.CalculateQuality(aimScore, distanceScore);
        float score = ScoringSystem.CalculatePhotoScore(aimScore, distanceScore, nearest.ThreatMultiplier);

        _scoreAccumulator.AddScore(score);
        GameEvents.RaisePhotoTaken(new PhotoResult(score, aimScore, distanceScore, quality,
            ScoringSystem.GetTier(quality), nearest, nearest.transform.position));
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

    private float CalcAimScore(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        return Mathf.Clamp01(Vector3.Dot(transform.forward, dir.normalized));
    }

    private float CalcDistanceScore(Transform target)
    {
        float dist = Vector3.Distance(transform.position, target.position);
        float exponent = -0.5f * Mathf.Pow((dist - OptimalDistance) / DistanceSigma, 2f);
        return Mathf.Exp(exponent);
    }
}
