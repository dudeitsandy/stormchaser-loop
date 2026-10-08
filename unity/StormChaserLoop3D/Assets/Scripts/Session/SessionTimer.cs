using UnityEngine;
using UnityEngine.Events;

/// <summary>Counts down the run. Idle until <see cref="Begin"/> is called.</summary>
public class SessionTimer : MonoBehaviour
{
    [SerializeField] private float _duration = 90f;
    [Tooltip("Final countdown (Andy 2026-10-08): the HUD timer turns red and GameEvents.CountdownTick fires each second from here to 0.")]
    [SerializeField] private int _countdownSeconds = 5;

    public UnityEvent OnSessionEnd;

    public float Duration => _duration;
    /// <summary>Seconds left when the final countdown starts (timer red, one tick per second).</summary>
    public int CountdownSeconds => _countdownSeconds;
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

        float before = TimeRemaining;
        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            GameEvents.RaiseCountdownTick(0);
            OnSessionEnd.Invoke();
            return;
        }
        int tick = CountdownTickCrossed(before, TimeRemaining, _countdownSeconds);
        if (tick > 0) GameEvents.RaiseCountdownTick(tick);
    }

    /// <summary>
    /// The countdown second entered this frame, or −1. A tick N fires when the remaining time drops to N seconds or
    /// below (the HUD shows N then, rounding up), for N from <paramref name="countdown"/> down to 1; 0 is raised at
    /// the end. A long frame that skips seconds reports the latest one.
    /// </summary>
    public static int CountdownTickCrossed(float before, float after, int countdown)
    {
        int now = Mathf.CeilToInt(after);
        if (now < 1 || now > countdown) return -1;
        return now < Mathf.CeilToInt(before) ? now : -1;
    }
}
