using UnityEngine;
using UnityEngine.InputSystem;

public class PhotoTrigger : MonoBehaviour
{
    private const float OptimalDistance = 20f;
    private const float DistanceSigma = 10f;

    [SerializeField] private ScoreAccumulator _scoreAccumulator;

    private StormChaserControls _controls;

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

    private void OnPhotograph(InputAction.CallbackContext ctx)
    {
        DisasterEntity nearest = FindNearest();
        if (nearest == null) return;

        float aimScore = CalcAimScore(nearest.transform);
        float distanceScore = CalcDistanceScore(nearest.transform);
        float score = ScoringSystem.CalculatePhotoScore(aimScore, distanceScore, nearest.ThreatMultiplier);

        Debug.Log($"[Photo] Aim={aimScore:F2} Dist={distanceScore:F2} Score={score:F0}");
        _scoreAccumulator.AddScore(score);
    }

    private DisasterEntity FindNearest()
    {
        DisasterEntity[] all = Object.FindObjectsByType<DisasterEntity>(FindObjectsSortMode.None);
        DisasterEntity nearest = null;
        float minDist = float.MaxValue;

        foreach (DisasterEntity e in all)
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
        Vector3 dir = (target.position - transform.position).normalized;
        return Mathf.Clamp01(Vector3.Dot(transform.forward, dir));
    }

    private float CalcDistanceScore(Transform target)
    {
        float dist = Vector3.Distance(transform.position, target.position);
        float exponent = -0.5f * Mathf.Pow((dist - OptimalDistance) / DistanceSigma, 2f);
        return Mathf.Exp(exponent);
    }
}
