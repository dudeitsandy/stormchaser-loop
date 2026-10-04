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

    /// <summary>
    /// Sets the run length in seconds (the Storm Director's plan duration, storm-director.md Rule 12) and, if the
    /// timer is running, restarts the countdown from it.
    /// </summary>
    public void SetDuration(float seconds)
    {
        _duration = Mathf.Max(1f, seconds);
        if (IsRunning) TimeRemaining = _duration;
    }

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
