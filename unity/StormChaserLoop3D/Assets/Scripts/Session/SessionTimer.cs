using UnityEngine;
using UnityEngine.Events;

/// <summary>Counts down the run. Idle until <see cref="Begin"/> is called.</summary>
public class SessionTimer : MonoBehaviour
{
    [SerializeField] private float _duration = 90f;

    public UnityEvent OnSessionEnd;

    public float Duration => _duration;
    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; }
    /// <summary>0 at run start, 1 at run end.</summary>
    public float Progress => _duration > 0f ? 1f - TimeRemaining / _duration : 1f;

    private void Awake()
    {
        TimeRemaining = _duration;
    }

    /// <summary>Starts the countdown from full duration.</summary>
    public void Begin()
    {
        TimeRemaining = _duration;
        IsRunning = true;
    }

    /// <summary>Halts the countdown without firing <see cref="OnSessionEnd"/>.</summary>
    public void Stop() => IsRunning = false;

    private void Update()
    {
        if (!IsRunning) return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            OnSessionEnd.Invoke();
        }
    }
}
