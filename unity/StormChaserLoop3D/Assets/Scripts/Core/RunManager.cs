using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the Title → Running → Results → Retry loop. Gates input, damage, spawning, and the timer,
/// and raises <see cref="GameEvents.RunStarted"/> / <see cref="GameEvents.RunEnded"/>.
/// </summary>
public class RunManager : MonoBehaviour
{
    public enum State { Title, Running, Ending, Results }

    [SerializeField] private SessionTimer _timer;
    [SerializeField] private ScoreAccumulator _score;
    [SerializeField] private DisasterSpawner _spawner;
    [SerializeField] private PlayerVehicle _vehicle;
    [SerializeField] private VehicleHealth _health;
    [SerializeField] private PhotoTrigger _photo;

    [Tooltip("Real seconds before the results screen accepts input, so a mashed shutter doesn't skip it.")]
    [SerializeField] private float _resultsInputDelay = 1.0f;
    [Tooltip("Real seconds of slow motion after a wreck before results appear.")]
    [SerializeField] private float _wreckSlowMoSeconds = 1.2f;
    [SerializeField] private float _wreckTimeScale = 0.25f;

    private RunScreens _screens;
    private IDisposable _anyButton;

    // Set before a retry reload so the next scene load skips the title.
    private static bool _skipTitleOnLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _skipTitleOnLoad = false;

    public State Current { get; private set; }

    private void Awake()
    {
        if (_timer == null) _timer = FindAnyObjectByType<SessionTimer>();
        if (_score == null) _score = FindAnyObjectByType<ScoreAccumulator>();
        if (_spawner == null) _spawner = FindAnyObjectByType<DisasterSpawner>();
        if (_vehicle == null) _vehicle = FindAnyObjectByType<PlayerVehicle>();
        if (_health == null && _vehicle != null) _health = _vehicle.GetComponent<VehicleHealth>();
        if (_photo == null) _photo = FindAnyObjectByType<PhotoTrigger>();

        _screens = GetComponent<RunScreens>();
        if (_screens == null) _screens = gameObject.AddComponent<RunScreens>();
    }

    private void OnEnable()
    {
        _timer.OnSessionEnd.AddListener(OnTimerEnd);
        if (_health != null) _health.OnWrecked.AddListener(OnWrecked);
    }

    private void OnDisable()
    {
        _timer.OnSessionEnd.RemoveListener(OnTimerEnd);
        if (_health != null) _health.OnWrecked.RemoveListener(OnWrecked);
    }

    private void Start()
    {
        if (_skipTitleOnLoad)
        {
            _skipTitleOnLoad = false;
            StartRun();
        }
        else
        {
            EnterTitle();
        }
    }

    private void OnDestroy()
    {
        _anyButton?.Dispose();
        Time.timeScale = 1f;
    }

    private void EnterTitle()
    {
        Current = State.Title;
        Time.timeScale = 1f;
        SetGameplayActive(false);
        _screens.ShowTitle(BestScoreStore.Load());
        WaitForAnyButton(0.25f, control =>
        {
            if (control != Keyboard.current?.escapeKey) StartRun();
            else if (CanQuit) Quit();
            else EnterTitle(); // WebGL: Application.Quit halts the player and freezes the canvas
        });
    }

    /// <summary>Starts a run immediately (title dismissed). Public for PlayMode tests.</summary>
    public void StartRun()
    {
        Current = State.Running;
        Time.timeScale = 1f;
        _screens.Hide();
        SetGameplayActive(true);
        // The press that dismissed the title/results belongs to the menu: keep the shutter safe until it's released.
        if (_photo != null)
        {
            _photo.Armed = false;
            StartCoroutine(ArmShutterOnRelease());
        }
        _timer.Begin();
        _spawner.Begin();
        GameEvents.RaiseRunStarted();
    }

    private void OnTimerEnd() => EndRun(wrecked: false);
    private void OnWrecked() => EndRun(wrecked: true);

    private void EndRun(bool wrecked)
    {
        if (Current != State.Running) return;
        Current = State.Ending;

        _timer.Stop();
        _spawner.Stop();
        SetGameplayActive(false);

        float previousBest = BestScoreStore.Load();
        BestScoreStore.TrySave(_score.TotalScore);
        var summary = new RunSummary(_score.TotalScore, _score.PhotoCount, _score.BestShot, wrecked, previousBest);
        GameEvents.RaiseRunEnded(summary);

        StartCoroutine(ShowResults(summary));
    }

    private IEnumerator ShowResults(RunSummary summary)
    {
        if (summary.Wrecked)
        {
            Time.timeScale = _wreckTimeScale;
            yield return new WaitForSecondsRealtime(_wreckSlowMoSeconds);
        }

        Time.timeScale = 0f;
        Current = State.Results;
        _screens.ShowResults(summary);
        WaitForAnyButton(_resultsInputDelay, control =>
        {
            Reload(skipTitle: control != Keyboard.current?.escapeKey);
        });
    }

    /// <summary>Reloads the scene for a clean world. Retry skips the title; Esc returns to it.</summary>
    private static void Reload(bool skipTitle)
    {
        Time.timeScale = 1f;
        _skipTitleOnLoad = skipTitle;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator ArmShutterOnRelease()
    {
        while (AnyButtonHeld()) yield return null;
        if (Current == State.Running && _photo != null) _photo.Armed = true;
    }

    private static bool AnyButtonHeld()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed) return true;
        foreach (Gamepad pad in Gamepad.all)
            foreach (InputControl control in pad.allControls)
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed && !button.synthetic)
                    return true;
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
        return false;
    }

    private void SetGameplayActive(bool active)
    {
        if (_vehicle != null) _vehicle.InputEnabled = active;
        if (_photo != null) _photo.Armed = active;
        if (_health != null) _health.Vulnerable = active;
    }

    private void WaitForAnyButton(float delaySeconds, Action<InputControl> onPress)
    {
        _anyButton?.Dispose();
        _anyButton = null;
        StartCoroutine(ArmAnyButton(delaySeconds, onPress));
    }

    private IEnumerator ArmAnyButton(float delaySeconds, Action<InputControl> onPress)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        _anyButton = InputSystem.onAnyButtonPress.CallOnce(control =>
        {
            _anyButton = null;
            onPress(control);
        });
    }

    private static bool CanQuit => Application.platform != RuntimePlatform.WebGLPlayer;

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
