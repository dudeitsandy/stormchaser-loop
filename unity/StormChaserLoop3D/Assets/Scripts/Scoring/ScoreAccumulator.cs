using UnityEngine;

public class ScoreAccumulator : MonoBehaviour
{
    [SerializeField] private SessionTimer _sessionTimer;

    public float TotalScore { get; private set; }

    private void Start()
    {
        _sessionTimer.OnSessionEnd.AddListener(OnSessionEnd);
    }

    private void OnDestroy()
    {
        _sessionTimer.OnSessionEnd.RemoveListener(OnSessionEnd);
    }

    public void AddScore(float amount)
    {
        TotalScore += amount;
        Debug.Log($"[Score] +{amount:F0} | Total: {TotalScore:F0}");
    }

    private void OnSessionEnd()
    {
        Debug.Log($"[Session Over] Final Score: {TotalScore:F0}");
    }
}
