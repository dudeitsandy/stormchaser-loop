using UnityEngine;
using UnityEngine.Events;

public class SessionTimer : MonoBehaviour
{
    [SerializeField] private float _duration = 90f;

    public UnityEvent OnSessionEnd;

    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; }

    private void Start()
    {
        TimeRemaining = _duration;
        IsRunning = true;
    }

    private void Update()
    {
        if (!IsRunning) return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            Debug.Log("Session Over");
            OnSessionEnd.Invoke();
        }
    }
}
