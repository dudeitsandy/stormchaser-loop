using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the Title → Running (⇄ Paused) → Results → Retry loop. Gates input, damage, spawning, the timer and
/// time scale, and raises <see cref="GameEvents.RunStarted"/> / <see cref="GameEvents.RunEnded"/> /
/// <see cref="GameEvents.PauseChanged"/> / <see cref="GameEvents.RunForfeited"/>. Pause, forfeit and quit are run
/// state; <see cref="RunScreens"/> only draws them (design/ux/run-screens.md).
/// </summary>
public class RunManager : MonoBehaviour
{
    public enum State { Title, Running, Paused, Ending, Results }

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
    private GoalRunner _goals;
    private RadioLite _radio;
    private IDisposable _anyButton;
    private PauseMenu _pauseMenu;
    private SettingsMenu _settingsMenu;
    private bool _settingsFromTitle;
    private float _stickX;
    private static bool _displayApplied; // fullscreen / resolution once per launch, not on every scene reload
    private float _stickY;
    private const float StickThreshold = 0.5f;

    // Set before a retry reload so the next scene load skips the title.
    private static bool _skipTitleOnLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _skipTitleOnLoad = false;
        _displayApplied = false;
    }

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
        _goals = GetComponent<GoalRunner>();
        if (_goals == null) _goals = gameObject.AddComponent<GoalRunner>();
        _goals.Bind(_score, _spawner, _vehicle);
        _goals.HasCompletedBefore = id => ProfileStore.Shared.HasCompleted(id); // NEW tags come from the save stem
        _radio = GetComponent<RadioLite>();
        if (_radio == null) _radio = gameObject.AddComponent<RadioLite>();
    }

    private void OnEnable()
    {
        _timer.OnSessionEnd.AddListener(OnTimerEnd);
        if (_health != null) _health.OnWrecked.AddListener(OnWrecked);
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        _timer.OnSessionEnd.RemoveListener(OnTimerEnd);
        if (_health != null) _health.OnWrecked.RemoveListener(OnWrecked);
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        // Settings from the save stem's device file, applied live (run-screens story 002).
        SettingsApplier.ResetSceneCache();
        SettingsApplier.ApplyLive(ProfileStore.Shared.Settings);
        if (!_displayApplied)
        {
            _displayApplied = true;
            SettingsApplier.ApplyDisplay(ProfileStore.Shared.Settings, fromUserInput: false);
        }
        if (DevFlag("savecheck") == "1")
            Debug.Log($"[SaveCheck] launch {ProfileStore.Shared.CountSaveCheckLaunch()}, write ok {!ProfileStore.Shared.LastWriteFailed}, " +
                      $"best {ProfileStore.Shared.Data.BestScore:0}, accomplishments {ProfileStore.Shared.Data.Accomplishments.Count}");
        // The saved paint job, if earned; a dev preview flag (?livery=ktvr) overrides it.
        ProfileStore saveStem = ProfileStore.Shared;
        string livery = LiveryPreview()
                        ?? (saveStem.IsUnlocked(saveStem.Data.Livery) ? saveStem.Data.Livery : null);
        TruckLivery.Apply(livery);
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
        AudioListener.pause = false;
    }

    private void EnterTitle()
    {
        Current = State.Title;
        Time.timeScale = 1f;
        SetGameplayActive(false);
        _radio.PlayTitle();
        _screens.ShowTitle(Mathf.Max(BestScoreStore.Load(), ProfileStore.Shared.Data.BestScore));
        WaitForAnyButton(0.25f, control =>
        {
            if (IsSettingsKey(control)) OpenSettings(fromTitle: true);
            else if (control != Keyboard.current?.escapeKey) StartRun();
            else if (CanQuit) ConfirmQuitGame();
            else EnterTitle(); // WebGL: Application.Quit halts the player and freezes the canvas
        });
    }

    // Windows title: Esc asks first ("QUIT GAME? ESC AGAIN"); any other press returns to the title, never starts a run.
    private void ConfirmQuitGame()
    {
        _screens.ShowQuitGameConfirm();
        WaitForAnyButton(0.15f, control =>
        {
            if (control == Keyboard.current?.escapeKey) Quit();
            else EnterTitle();
        });
    }

    private void Update()
    {
        if (_settingsMenu != null) { HandleSettingsInput(); return; }
        if (Current == State.Running && PausePressed()) Pause(PauseReason.Manual);
        else if (Current == State.Paused) HandlePauseInput();
    }

    /// <summary>
    /// Pauses a running run: freezes time, the timer and audio, disables driving, shows the pause menu. Ignored in
    /// any other state (title, wreck beat, results). Public for PlayMode tests.
    /// </summary>
    public void Pause(PauseReason reason)
    {
        if (Current != State.Running) return;
        Current = State.Paused;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetGameplayActive(false);
        _pauseMenu = new PauseMenu(CanQuit);
        _screens.ShowPause(_pauseMenu, null, OnPauseHover, OnPauseClick);
        GameEvents.RaisePauseChanged(true, reason);
    }

    /// <summary>Resumes a paused run exactly where it stopped. Public for PlayMode tests.</summary>
    public void Resume()
    {
        if (Current != State.Paused) return;
        Current = State.Running;
        _pauseMenu = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        _screens.Hide();
        SetGameplayActive(true);
        // The press that resumed belongs to the menu: keep the shutter safe until it's released.
        if (_photo != null)
        {
            _photo.Armed = false;
            StartCoroutine(ArmShutterOnRelease());
        }
        GameEvents.RaisePauseChanged(false, PauseReason.Manual);
    }

    /// <summary>
    /// Quit Run, confirmed: the run is forfeited (no run end, nothing banked) and the scene reloads to the title.
    /// Public for PlayMode tests.
    /// </summary>
    public void ForfeitRun(bool quitToDesktop = false)
    {
        if (Current != State.Paused && Current != State.Running) return;
        _timer.Stop();
        _spawner.Stop();
        _goals.StopRun();
        GameEvents.RaiseRunForfeited();
        AudioListener.pause = false;
        if (quitToDesktop && CanQuit) Quit();
        else Reload(skipTitle: false);
    }

    /// <summary>
    /// Window or tab focus changed. Losing focus mid-run pauses; regaining it never resumes (the player presses
    /// Resume). Batchmode test runners report no real focus, so they pass <paramref name="ignore"/>.
    /// </summary>
    public void HandleFocusChange(bool focused, bool ignore)
    {
        if (!focused && !ignore && Current == State.Running) Pause(PauseReason.FocusLost);
    }

    private void OnApplicationFocus(bool focused) => HandleFocusChange(focused, Application.isBatchMode);
    private void OnApplicationPause(bool paused) => HandleFocusChange(!paused, Application.isBatchMode);

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            && Current == State.Running)
            Pause(PauseReason.ControllerLost);
    }

    private static bool PausePressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) return true;
        foreach (Gamepad pad in Gamepad.all)
            if (pad.startButton.wasPressedThisFrame) return true;
        return false;
    }

    private void HandlePauseInput()
    {
        if (_pauseMenu == null) return;
        Keyboard kb = Keyboard.current;
        int move = 0;
        bool select = false, back = false;
        if (kb != null)
        {
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) move = -1;
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) move = 1;
            select |= kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
            back |= kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame;
        }
        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.dpad.up.wasPressedThisFrame) move = -1;
            if (pad.dpad.down.wasPressedThisFrame) move = 1;
            select |= pad.buttonSouth.wasPressedThisFrame;
            back |= pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
        }
        // Left stick: one step per push past the threshold.
        float y = 0f;
        foreach (Gamepad pad in Gamepad.all) if (Mathf.Abs(pad.leftStick.y.ReadValue()) > Mathf.Abs(y)) y = pad.leftStick.y.ReadValue();
        if (move == 0)
        {
            if (y > StickThreshold && _stickY <= StickThreshold) move = -1;
            else if (y < -StickThreshold && _stickY >= -StickThreshold) move = 1;
        }
        _stickY = y;

        if (back) Apply(_pauseMenu.Back());
        else if (select)
        {
            GameEvents.RaiseMenuPicked();
            Apply(_pauseMenu.Select());
        }
        else if (move != 0)
        {
            _pauseMenu.Move(move);
            GameEvents.RaiseMenuMoved();
            _screens.ShowPause(_pauseMenu, null, OnPauseHover, OnPauseClick);
        }
    }

    private void OnPauseHover(int index)
    {
        // Rebuild only on a real change, so a rebuilt entry under a resting pointer can't re-trigger hover.
        if (_pauseMenu == null || _pauseMenu.Confirming.HasValue || index == _pauseMenu.Focus) return;
        _pauseMenu.FocusOn(index);
        GameEvents.RaiseMenuMoved();
        _screens.ShowPause(_pauseMenu, null, OnPauseHover, OnPauseClick);
    }

    private void OnPauseClick(int index)
    {
        if (_pauseMenu == null) return;
        if (_pauseMenu.Confirming.HasValue)
        {
            // Confirm row: 0 = KEEP PLAYING, 1 = QUIT.
            if ((index == 1) != _pauseMenu.ConfirmQuitFocused) _pauseMenu.Move(1);
        }
        else _pauseMenu.FocusOn(index);
        GameEvents.RaiseMenuPicked();
        Apply(_pauseMenu.Select());
    }

    private void Apply(PauseAction action)
    {
        switch (action)
        {
            case PauseAction.Resume: Resume(); break;
            case PauseAction.ForfeitToTitle: ForfeitRun(); break;
            case PauseAction.QuitToDesktop: ForfeitRun(quitToDesktop: true); break;
            case PauseAction.ShowSettings:
                OpenSettings(fromTitle: false);
                break;
            default:
                _screens.ShowPause(_pauseMenu, null, OnPauseHover, OnPauseClick);
                break;
        }
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
        _goals.BeginRun(); // after the director has its plan: bounties are drawn from it
        _radio.StartRadio();
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

        // Score tiers are judged on the final score, which already includes goal bonuses (they pay none themselves).
        RunGoalsInfo goals = _goals.EndRun(_score.TotalScore, endedOnTimer: !wrecked);
        ProfileStore profile = ProfileStore.Shared;
        float previousBest = Mathf.Max(BestScoreStore.Load(), profile.Data.BestScore);
        BestScoreStore.TrySave(_score.TotalScore);
        StormRunInfo storm = _spawner.Director != null ? _spawner.Director.RunInfo() : default;
        // Run-complete checkpoint (save stem): one write of best score, accomplishments and unlocks. A forfeit never gets here.
        // Rewards are judged against the record before this run's completions are added (story 006).
        List<string> unlocks = goals.Valid
            ? RunRewards.Earned(profile.CareerCount(GoalCatalogue.Mode, GoalCatalogue.Map), goals.Completions,
                                profile.HasCompleted, profile.IsUnlocked)
            : new List<string>();
        if (unlocks.Contains(RunRewards.KtvrLivery)) profile.Data.Livery = RunRewards.KtvrLivery; // a new paint job is worn next run
        bool saved = profile.RecordRunComplete(goals.Completions, GoalCatalogue.Mode, storm.Valid ? storm.Seed : 0L,
                                               Application.version, DateTime.UtcNow, _score.TotalScore, unlocks);
        if (goals.Valid) goals = new RunGoalsInfo(goals.Completions, goals.Bounties, goals.FailedBounties, unlocks, saved);
        var summary = new RunSummary(_score.TotalScore, _score.PhotoCount, _score.BestShot, wrecked, previousBest, storm, goals);
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
            bool toTitle = control == Keyboard.current?.escapeKey
                           || (control.device is Gamepad pad && control == pad.buttonEast);
            Reload(skipTitle: !toTitle);
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

    // ---------- Settings (run-screens story 002) ----------

    private static bool IsSettingsKey(InputControl control) =>
        control == Keyboard.current?.oKey || (control.device is Gamepad pad && control == pad.selectButton);

    /// <summary>Opens the Settings panel from the title or the pause menu. Public for PlayMode tests.</summary>
    public void OpenSettings(bool fromTitle)
    {
        _anyButton?.Dispose();
        _anyButton = null;
        _settingsFromTitle = fromTitle;
        _settingsMenu = new SettingsMenu(ProfileStore.Shared.Settings, CanQuit, DesktopResolutions());
        RefreshSettings();
    }

    /// <summary>Closes Settings: saves the device file once, then returns to the title or the pause menu.</summary>
    public void CloseSettings()
    {
        if (_settingsMenu == null) return;
        _settingsMenu = null;
        ProfileStore.Shared.SaveSettings();
        if (_settingsFromTitle) EnterTitle();
        else if (_pauseMenu != null) _screens.ShowPause(_pauseMenu, null, OnPauseHover, OnPauseClick);
    }

    /// <summary>Steps the focused setting and applies it live. Public for PlayMode tests.</summary>
    public void ChangeSetting(int dir)
    {
        SettingRow? changed = _settingsMenu?.Change(dir);
        if (changed == null) return;
        SettingsApplier.ApplyLive(_settingsMenu.Settings);
        if (changed == SettingRow.Fullscreen || changed == SettingRow.Resolution)
            SettingsApplier.ApplyDisplay(_settingsMenu.Settings, fromUserInput: true);
        GameEvents.RaiseMenuMoved();
        RefreshSettings();
    }

    private void RefreshSettings() => _screens.ShowSettings(_settingsMenu, OnSettingsHover, OnSettingsClick);

    private void HandleSettingsInput()
    {
        Keyboard kb = Keyboard.current;
        int move = 0, change = 0;
        bool select = false, back = false;
        if (kb != null)
        {
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) move = -1;
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) move = 1;
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) change = -1;
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) change = 1;
            select |= kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
            back |= kb.escapeKey.wasPressedThisFrame;
        }
        float sx = 0f, sy = 0f;
        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.dpad.up.wasPressedThisFrame) move = -1;
            if (pad.dpad.down.wasPressedThisFrame) move = 1;
            if (pad.dpad.left.wasPressedThisFrame) change = -1;
            if (pad.dpad.right.wasPressedThisFrame) change = 1;
            select |= pad.buttonSouth.wasPressedThisFrame;
            back |= pad.buttonEast.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame;
            Vector2 st = pad.leftStick.ReadValue();
            if (Mathf.Abs(st.x) > Mathf.Abs(sx)) sx = st.x;
            if (Mathf.Abs(st.y) > Mathf.Abs(sy)) sy = st.y;
        }
        if (move == 0 && sy > StickThreshold && _stickY <= StickThreshold) move = -1;
        else if (move == 0 && sy < -StickThreshold && _stickY >= -StickThreshold) move = 1;
        if (change == 0 && sx > StickThreshold && _stickX <= StickThreshold) change = 1;
        else if (change == 0 && sx < -StickThreshold && _stickX >= -StickThreshold) change = -1;
        _stickY = sy;
        _stickX = sx;

        if (back) { GameEvents.RaiseMenuPicked(); CloseSettings(); }
        else if (select) OnSettingsClick(_settingsMenu.Focus);
        else if (change != 0) ChangeSetting(change);
        else if (move != 0)
        {
            _settingsMenu.Move(move);
            GameEvents.RaiseMenuMoved();
            RefreshSettings();
        }
    }

    private void OnSettingsHover(int index)
    {
        if (_settingsMenu == null || index == _settingsMenu.Focus) return;
        _settingsMenu.FocusOn(index);
        GameEvents.RaiseMenuMoved();
        RefreshSettings();
    }

    // Enter / A / click: BACK closes; any other row steps its value forward.
    private void OnSettingsClick(int index)
    {
        if (_settingsMenu == null) return;
        _settingsMenu.FocusOn(index);
        GameEvents.RaiseMenuPicked();
        if (_settingsMenu.Focused == SettingRow.Back) CloseSettings();
        else ChangeSetting(1);
    }

    private static System.Collections.Generic.List<Vector2Int> DesktopResolutions()
    {
        var list = new System.Collections.Generic.List<Vector2Int>();
        if (!CanQuit) return list; // WebGL: the browser owns the canvas size
        foreach (Resolution r in Screen.resolutions)
        {
            var v = new Vector2Int(r.width, r.height);
            if (!list.Contains(v)) list.Add(v);
        }
        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return list;
    }

    /// <summary>A dev URL token (<c>?name=value</c>) or desktop arg (<c>-name=value</c>); null when absent.</summary>
    private static string DevFlag(string name)
    {
        string v = null;
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith("-" + name + "=", StringComparison.Ordinal)) v = arg.Substring(name.Length + 2);
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token.StartsWith(name + "=", StringComparison.Ordinal)) v = token.Substring(name.Length + 1);
        return v;
    }

    /// <summary>Dev preview of a paint job (URL <c>?livery=ktvr</c>, desktop <c>-livery=ktvr</c>); null when absent.</summary>
    private static string LiveryPreview()
    {
        string v = null;
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith("-livery=", StringComparison.Ordinal)) v = arg.Substring(8);
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token.StartsWith("livery=", StringComparison.Ordinal)) v = token.Substring(7);
        return string.IsNullOrEmpty(v) ? null : (v.Contains(".") ? v : "livery." + v);
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
