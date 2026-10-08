using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// In-run HUD: time and score top-right, truck HP and boost meter top-left, IN THE WIND and the Storm Cam
/// indicator top-centre.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    private static readonly Color HpFull = new Color(1f, 0.69f, 0f);
    private static readonly Color HpEmpty = new Color(1f, 1f, 1f, 0.15f);
    private static readonly Color HpHit = new Color(0.95f, 0.22f, 0.18f);
    private static readonly Color BoostOff = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color BoostFlash = new Color(1f, 0.95f, 0.7f);
    private const long BoostFlashMs = 250;
    private const float BoostBarWidth = 96f;

    [SerializeField] private SessionTimer _sessionTimer;
    [SerializeField] private ScoreAccumulator _scoreAccumulator;
    [SerializeField] private VehicleHealth _vehicleHealth;
    [Tooltip("Seconds remaining at which the timer turns red.")]
    [SerializeField] private float _lowTimeWarning = 10f;
    [Tooltip("Wind multiplier above which the IN THE WIND meter appears.")]
    [SerializeField] private float _windMeterThreshold = 1.05f;

    private Label _timeLabel;
    private Label _scoreLabel;
    private Label _filmLabel;
    private Label _windLabel;
    private Label _stormCamLabel;

    // KTVR News crawl (storm-director.md Rule 7 / UI Requirements, story 008).
    private static readonly Color NewsBlue = new Color(0.08f, 0.16f, 0.42f, 0.92f);
    private static readonly Color NewsRed = new Color(0.62f, 0.05f, 0.05f, 0.94f);
    [Tooltip("Seconds a news crawl stays on screen.")]
    [SerializeField] private float _crawlSeconds = 9f;
    [Tooltip("Crawl scroll speed in panel units per second.")]
    [SerializeField] private float _crawlSpeed = 140f;
    private VisualElement _crawlBar;
    private Label _crawlTag;
    private Label _crawlText;
    private float _crawlUntil;
    private float _crawlX;
    private readonly Queue<(string text, bool emergency)> _crawlQueue = new Queue<(string, bool)>();

    // Forecast panel (storm-director.md UI Requirements, story 007).
    private const int ForecastNearest = 3;
    private const float ForecastRefresh = 0.2f;
    private StormDirector _director;
    private VisualElement _forecastPanel;
    private readonly List<Label> _forecastRows = new List<Label>();
    private Label _forecastMore;
    private float _nextForecast;
    private readonly List<ForecastRow> _forecastScratch = new List<ForecastRow>(8);
    private ChaseCameraRig _cameraRig;
    private string _stormCamShown;
    private Label _hpTitle;
    private VisualElement _boostFill;
    private PlayerVehicle _vehicle;
    private float _lastBoostShown = -1f;
    private bool _boostDisabledShown;
    private PhotoTrigger _photo;
    private float _lastWindShown = -1f;
    private readonly List<VisualElement> _hpPips = new List<VisualElement>();
    private int _lastSeconds = -1;
    private float _lastScore = -1f;

    // Run Goals HUD (event-system.md Run Goals Rule 8; design/ux/run-screens.md; run-goals-v1 story 007).
    private static readonly Color GoalOpen = new Color(1f, 1f, 1f, 0.72f);
    private static readonly Color GoalMiss = new Color(0.95f, 0.45f, 0.4f, 0.8f);
    [Tooltip("Seconds the career-goal pop-up holds.")]
    [SerializeField] private float _goalPopupSeconds = 2f;
    private GoalRunner _goals;
    private VisualElement _bountyBlock;
    private readonly Dictionary<string, Label> _bountyRows = new Dictionary<string, Label>();
    private Label _goalPopup;
    [SerializeField] private float _nowPlayingSeconds = 2f;
    private RadioLite _radio;
    private Label _nowPlaying;
    private IVisualElementScheduledItem _nowPlayingHide;
    private IVisualElementScheduledItem _goalPopupHide;

    private void Awake()
    {
        if (_vehicleHealth == null) _vehicleHealth = FindAnyObjectByType<VehicleHealth>();
        if (_vehicleHealth != null) _vehicle = _vehicleHealth.GetComponent<PlayerVehicle>();
        _cameraRig = FindAnyObjectByType<ChaseCameraRig>();
    }

    private void OnEnable()
    {
        GameEvents.PlayerDamaged += OnPlayerDamaged;
        GameEvents.StyleEvent += OnStyleEvent;
        GameEvents.FilmChanged += OnFilmChanged;
        GameEvents.OutOfFilm += OnOutOfFilm;
        GameEvents.StormCellForming += OnStormCellForming;
        GameEvents.StormCellPeak += OnStormCellPeak;
        GameEvents.RunStarted += OnRunStarted;
        GameEvents.RunEnded += OnRunEndedHideHud;
        GameEvents.GoalCompleted += OnGoalCompleted;
        GameEvents.BountyFailed += OnBountyFailed;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDamaged -= OnPlayerDamaged;
        GameEvents.StyleEvent -= OnStyleEvent;
        GameEvents.FilmChanged -= OnFilmChanged;
        GameEvents.OutOfFilm -= OnOutOfFilm;
        GameEvents.StormCellForming -= OnStormCellForming;
        GameEvents.StormCellPeak -= OnStormCellPeak;
        GameEvents.RunStarted -= OnRunStarted;
        GameEvents.RunEnded -= OnRunEndedHideHud;
        GameEvents.GoalCompleted -= OnGoalCompleted;
        GameEvents.BountyFailed -= OnBountyFailed;
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _hudRoot = root;
        // The HUD shows only during a run (run-screens 006): hidden behind the title, the WRECKED beat and results.
        root.style.display = _sessionTimer != null && _sessionTimer.IsRunning ? DisplayStyle.Flex : DisplayStyle.None;

        var right = new VisualElement();
        right.style.position = Position.Absolute;
        right.style.top = 16;
        right.style.right = 16;
        right.style.alignItems = Align.FlexEnd;

        _timeLabel = MakePanelLabel("1:30", 24);
        _timeLabel.style.marginBottom = 4;
        _scoreLabel = MakePanelLabel("0", 32);
        _scoreLabel.style.marginBottom = 4;
        _photo = FindAnyObjectByType<PhotoTrigger>();
        _filmLabel = MakePanelLabel(_photo != null ? FilmText(_photo.FilmRemaining) : "", 20);
        right.Add(_timeLabel);
        right.Add(_scoreLabel);
        right.Add(_filmLabel);
        BuildForecastPanel(right);
        root.Add(right);

        // Top-left column: truck HP, then the boost meter, stacked by layout so they never overlap.
        var leftColumn = new VisualElement { pickingMode = PickingMode.Ignore };
        leftColumn.style.position = Position.Absolute;
        leftColumn.style.top = 16;
        leftColumn.style.left = 16;
        leftColumn.style.alignItems = Align.FlexStart;

        var left = new VisualElement();
        left.style.flexDirection = FlexDirection.Row;
        left.style.alignItems = Align.Center;
        left.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        left.style.paddingLeft = 8;
        left.style.paddingRight = 8;
        left.style.paddingTop = 6;
        left.style.paddingBottom = 6;

        var hpTitle = _hpTitle = new Label("TRUCK");
        hpTitle.style.color = Color.white;
        hpTitle.style.fontSize = 16;
        hpTitle.style.marginRight = 8;
        left.Add(hpTitle);

        int max = _vehicleHealth != null ? _vehicleHealth.MaxHealth : 6;
        for (int i = 0; i < max; i++)
        {
            var pip = new VisualElement();
            pip.style.width = 22;
            pip.style.height = 14;
            pip.style.marginRight = 4;
            pip.style.backgroundColor = HpFull;
            _hpPips.Add(pip);
            left.Add(pip);
        }
        leftColumn.Add(left);

        // vehicle-feel.md UI: boost meter under truck HP; amber fill, grey while Critical disables boost.
        var boostRow = new VisualElement { pickingMode = PickingMode.Ignore };
        boostRow.style.marginTop = 4;
        boostRow.style.flexDirection = FlexDirection.Row;
        boostRow.style.alignItems = Align.Center;
        boostRow.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        boostRow.style.paddingLeft = 8;
        boostRow.style.paddingRight = 8;
        boostRow.style.paddingTop = 4;
        boostRow.style.paddingBottom = 4;
        var boostTitle = new Label("BOOST");
        boostTitle.style.color = Color.white;
        boostTitle.style.fontSize = 12;
        boostTitle.style.marginRight = 8;
        boostRow.Add(boostTitle);
        var track = new VisualElement();
        track.style.width = BoostBarWidth;
        track.style.height = 8;
        track.style.backgroundColor = HpEmpty;
        _boostFill = new VisualElement();
        _boostFill.style.height = 8;
        _boostFill.style.width = BoostBarWidth;
        _boostFill.style.backgroundColor = HpFull;
        track.Add(_boostFill);
        boostRow.Add(track);
        leftColumn.Add(boostRow);
        BuildBountyBlock(leftColumn);
        root.Add(leftColumn);

        // Top-center risk/reward meter: shows the live bonus a shot would get from here.
        var center = new VisualElement { pickingMode = PickingMode.Ignore };
        center.style.position = Position.Absolute;
        center.style.top = 16;
        center.style.left = 0;
        center.style.right = 0;
        center.style.alignItems = Align.Center;
        _windLabel = MakePanelLabel("", 26);
        _windLabel.style.color = HpFull;
        _windLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _windLabel.style.letterSpacing = 4;
        _windLabel.style.display = DisplayStyle.None;
        center.Add(_windLabel);
        // vehicle-feel.md UI: Storm Cam lock + target EF, or NO TARGET while toggled with nothing in range.
        _stormCamLabel = MakePanelLabel("", 16);
        _stormCamLabel.style.letterSpacing = 3;
        _stormCamLabel.style.marginTop = 6;
        _stormCamLabel.style.display = DisplayStyle.None;
        center.Add(_stormCamLabel);
        root.Add(center);

        BuildNewsCrawl(root);
        BuildGoalPopup(root);
        BuildNowPlaying(root);
        // In Start, not Awake: RunManager may add the RadioLite in its own Awake.
        _radio = FindAnyObjectByType<RadioLite>();
        if (_radio != null) _radio.SongSkipped += ShowNowPlaying;
        _goals = FindAnyObjectByType<GoalRunner>();
        if (_goals != null && _sessionTimer != null && _sessionTimer.IsRunning) OnRunStarted(); // retry skips the title
    }

    // ---------- Run Goals ----------

    private void BuildBountyBlock(VisualElement parent)
    {
        _bountyBlock = new VisualElement { pickingMode = PickingMode.Ignore };
        _bountyBlock.style.marginTop = 4;
        _bountyBlock.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        _bountyBlock.style.paddingLeft = 8;
        _bountyBlock.style.paddingRight = 8;
        _bountyBlock.style.paddingTop = 4;
        _bountyBlock.style.paddingBottom = 4;
        _bountyBlock.style.display = DisplayStyle.None;
        parent.Add(_bountyBlock);
    }

    private void BuildGoalPopup(VisualElement root)
    {
        // Upper centre, above Codex's style pops (44 % height) and below IN THE WIND.
        var holder = new VisualElement { pickingMode = PickingMode.Ignore };
        holder.style.position = Position.Absolute;
        holder.style.top = Length.Percent(24);
        holder.style.left = 0;
        holder.style.right = 0;
        holder.style.alignItems = Align.Center;
        _goalPopup = MakePanelLabel("", 30);
        _goalPopup.style.color = HpFull;
        _goalPopup.style.unityFontStyleAndWeight = FontStyle.Bold;
        _goalPopup.style.letterSpacing = 4;
        _goalPopup.style.display = DisplayStyle.None;
        holder.Add(_goalPopup);
        root.Add(holder);
    }

    // Radio skip toast (Andy 2026-10-08): bottom-left, just above the news crawl, for 2 s.
    private void BuildNowPlaying(VisualElement root)
    {
        _nowPlaying = MakePanelLabel("", 16);
        _nowPlaying.pickingMode = PickingMode.Ignore;
        _nowPlaying.style.position = Position.Absolute;
        _nowPlaying.style.left = 16;
        _nowPlaying.style.bottom = 44;
        _nowPlaying.style.letterSpacing = 3;
        _nowPlaying.style.display = DisplayStyle.None;
        root.Add(_nowPlaying);
    }

    private void OnDestroy()
    {
        if (_radio != null) _radio.SongSkipped -= ShowNowPlaying;
    }

    private void ShowNowPlaying(string title)
    {
        if (_nowPlaying == null) return;
        _nowPlayingHide?.Pause();
        _nowPlaying.text = NowPlayingText(title);
        _nowPlaying.style.display = DisplayStyle.Flex;
        _nowPlayingHide = _nowPlaying.schedule.Execute(() => _nowPlaying.style.display = DisplayStyle.None)
                                              .StartingIn((long)(_nowPlayingSeconds * 1000f));
    }

    /// <summary>Radio skip toast text: "KTVR RADIO  ·  GEESE".</summary>
    public static string NowPlayingText(string title) => $"KTVR RADIO  ·  {title}";

    private VisualElement _hudRoot;

    private void OnRunEndedHideHud(RunSummary _)
    {
        if (_hudRoot != null) _hudRoot.style.display = DisplayStyle.None;
    }

    private void OnRunStarted()
    {
        if (_hudRoot != null) _hudRoot.style.display = DisplayStyle.Flex;
        if (_bountyBlock == null) return;
        if (_goals == null) _goals = FindAnyObjectByType<GoalRunner>();
        _bountyBlock.Clear();
        _bountyRows.Clear();
        IReadOnlyList<string> bounties = _goals != null ? _goals.Bounties : null;
        if (bounties == null || bounties.Count == 0)
        {
            _bountyBlock.style.display = DisplayStyle.None; // legacy spawner: no plan, no bounties
            return;
        }
        Label title = MakeGoalLabel("KTVR WANTS", 12, Color.white);
        title.style.letterSpacing = 3;
        _bountyBlock.Add(title);
        foreach (string id in bounties)
        {
            GoalDef def = GoalCatalogue.Find(id);
            if (def == null) continue;
            Label row = MakeGoalLabel(BountyText(def, BountyState.Open, 0), 14, GoalOpen);
            _bountyRows[id] = row;
            _bountyBlock.Add(row);
        }
        _bountyBlock.style.display = DisplayStyle.Flex;
    }

    private void OnGoalCompleted(GoalCompletion c)
    {
        GoalDef def = GoalCatalogue.Find(c.Id);
        if (def == null) return;
        if (c.Kind == GoalKind.Bounty && _bountyRows.TryGetValue(c.Id, out Label row))
        {
            row.text = BountyText(def, BountyState.Done, c.Bonus);
            row.style.color = HpFull;
            Brighten(row);
        }
        if (c.Kind == GoalKind.Career) ShowGoalPopup(CareerPopupText(def, c));
    }

    private void OnBountyFailed(string id)
    {
        GoalDef def = GoalCatalogue.Find(id);
        if (def == null || !_bountyRows.TryGetValue(id, out Label row)) return;
        row.text = BountyText(def, BountyState.Missed, 0);
        row.style.color = GoalMiss;
        Brighten(row);
    }

    private void ShowGoalPopup(string text)
    {
        if (_goalPopup == null) return;
        _goalPopupHide?.Pause();
        _goalPopup.text = text;
        _goalPopup.style.opacity = 1f;
        _goalPopup.style.display = DisplayStyle.Flex;
        _goalPopupHide = _goalPopup.schedule.Execute(() => _goalPopup.style.display = DisplayStyle.None)
                                            .StartingIn((long)(_goalPopupSeconds * 1000f));
    }

    private static void Brighten(Label row)
    {
        row.style.opacity = 1f;
        row.style.unityFontStyleAndWeight = FontStyle.Bold;
        row.schedule.Execute(() => row.style.unityFontStyleAndWeight = FontStyle.Normal).StartingIn(2000);
    }

    /// <summary>Bounty row states shown in the HUD list.</summary>
    public enum BountyState { Open, Done, Missed }

    /// <summary>
    /// HUD text for a bounty row. Every state carries a text marker (never colour alone): open "·", done "DONE … +bonus",
    /// missed "MISS" with strike-through.
    /// </summary>
    public static string BountyText(GoalDef def, BountyState state, int bonus)
    {
        switch (state)
        {
            case BountyState.Done: return $"DONE  {def.Text}  +{bonus}";
            case BountyState.Missed: return $"MISS  <s>{def.Text}</s>";
            default: return $"·  {def.Text}";
        }
    }

    /// <summary>Career pop-up text: "GOAL! BIG AIR +150", "NEW" on a first-ever completion.</summary>
    public static string CareerPopupText(GoalDef def, GoalCompletion c)
    {
        string bonus = c.Bonus > 0 ? $"  +{c.Bonus}" : "";
        return $"GOAL! {def.ShortName}{bonus}{(c.FirstEver ? "  NEW" : "")}";
    }

    private static Label MakeGoalLabel(string text, int size, Color color)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.style.fontSize = size;
        label.style.color = color;
        label.style.marginTop = 1;
        label.style.marginBottom = 1;
        return label;
    }

    private void Update()
    {
        if (_timeLabel == null) return;

        int seconds = Mathf.CeilToInt(_sessionTimer.TimeRemaining);
        if (seconds != _lastSeconds)
        {
            _lastSeconds = seconds;
            _timeLabel.text = $"{seconds / 60}:{seconds % 60:D2}";
            _timeLabel.style.color = _sessionTimer.IsRunning && seconds <= _lowTimeWarning ? HpHit : Color.white;
        }

        UpdateWindMeter();
        UpdateStormCam();
        UpdateNewsCrawl();
        UpdateForecast();
        UpdateBoostMeter();

        if (!Mathf.Approximately(_scoreAccumulator.TotalScore, _lastScore))
        {
            _lastScore = _scoreAccumulator.TotalScore;
            _scoreLabel.text = $"{_lastScore:N0}";
        }
    }

    private void OnPlayerDamaged(int current, int max)
    {
        if (_hpTitle != null)
        {
            bool critical = current > 0 && VehicleHealth.StageFor(current, max) == DamageStage.Critical;
            _hpTitle.text = critical ? "CRITICAL" : "TRUCK";
            _hpTitle.style.color = critical ? HpHit : Color.white;
        }
        for (int i = 0; i < _hpPips.Count; i++)
            _hpPips[i].style.backgroundColor = i < current ? HpFull : HpEmpty;
        // The pip just lost flashes red briefly.
        if (current >= 0 && current < _hpPips.Count)
        {
            VisualElement lost = _hpPips[current];
            lost.style.backgroundColor = HpHit;
            lost.schedule.Execute(() => lost.style.backgroundColor = HpEmpty).StartingIn(350);
        }
    }

    // vehicle-feel.md UI: the boost meter flashes when a style refill lands (drift, airtime, near-miss).
    private void OnStyleEvent(StyleKind kind, float amount)
    {
        if (_boostFill == null || _boostDisabledShown) return;
        _boostFill.style.backgroundColor = BoostFlash;
        _boostFill.schedule.Execute(() =>
            _boostFill.style.backgroundColor = _boostDisabledShown ? BoostOff : HpFull).StartingIn(BoostFlashMs);
    }

    private void UpdateBoostMeter()
    {
        if (_vehicle == null || _boostFill == null) return;
        float shown = Mathf.Round(_vehicle.BoostMeter);
        bool disabled = _vehicleHealth != null && _vehicleHealth.Stage == DamageStage.Critical;
        if (shown != _lastBoostShown)
        {
            _lastBoostShown = shown;
            _boostFill.style.width = BoostBarWidth * shown / 100f;
        }
        // Colour only on a state change, so a refill flash isn't overwritten by the next width update.
        if (disabled != _boostDisabledShown)
        {
            _boostDisabledShown = disabled;
            _boostFill.style.backgroundColor = disabled ? BoostOff : HpFull;
        }
    }

    private void UpdateWindMeter()
    {
        float mult = _photo != null && _photo.Armed ? _photo.CurrentWindMultiplier : 1f;
        if (mult < _windMeterThreshold)
        {
            if (_lastWindShown != 0f)
            {
                _windLabel.style.display = DisplayStyle.None;
                _lastWindShown = 0f;
            }
            return;
        }

        float shown = Mathf.Round(mult * 10f) / 10f;
        if (shown != _lastWindShown)
        {
            _lastWindShown = shown;
            _windLabel.text = $"IN THE WIND ×{shown:0.0}";
            _windLabel.style.display = DisplayStyle.Flex;
        }

        // Stronger wind = bigger, redder, faster pulse.
        float t = Mathf.InverseLerp(_windMeterThreshold, 2f, mult);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(4f, 12f, t));
        _windLabel.style.color = Color.Lerp(HpFull, HpHit, t);
        _windLabel.style.opacity = Mathf.Lerp(0.75f, 1f, pulse);
        _windLabel.style.scale = new Scale(Vector3.one * Mathf.Lerp(1f, 1.25f, t));
    }

    private void BuildForecastPanel(VisualElement parent)
    {
        var spawner = FindAnyObjectByType<DisasterSpawner>();
        _director = spawner != null ? spawner.Director : null;
        _forecastPanel = new VisualElement { pickingMode = PickingMode.Ignore };
        _forecastPanel.style.marginTop = 8;
        _forecastPanel.style.alignItems = Align.FlexEnd;
        _forecastPanel.style.display = DisplayStyle.None;
        for (int i = 0; i < ForecastNearest + 2; i++)
        {
            Label row = MakePanelLabel("", 15);
            row.style.marginBottom = 2;
            row.style.display = DisplayStyle.None;
            _forecastRows.Add(row);
            _forecastPanel.Add(row);
        }
        _forecastMore = MakePanelLabel("", 13);
        _forecastMore.style.color = BoostOff;
        _forecastMore.style.display = DisplayStyle.None;
        _forecastPanel.Add(_forecastMore);
        parent.Add(_forecastPanel);
    }

    // Nearest 3 cells plus the anchor and co-anchor (always shown, highlighted), then "+N more".
    private void UpdateForecast()
    {
        if (_forecastPanel == null || _director == null || Time.unscaledTime < _nextForecast) return;
        _nextForecast = Time.unscaledTime + ForecastRefresh;
        var live = _director.LiveCells;
        if (_director.State != StormDirector.DirectorState.Running || live.Count == 0 || _director.Forecast == null)
        {
            _forecastPanel.style.display = DisplayStyle.None;
            return;
        }
        Vector3 player = _vehicle != null ? _vehicle.transform.position : Vector3.zero;
        _forecastScratch.Clear();
        for (int i = 0; i < live.Count; i++) _forecastScratch.Add(_director.Forecast.Evaluate(live[i], player));
        _forecastScratch.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        int shown = 0;
        for (int i = 0; i < _forecastScratch.Count && shown < _forecastRows.Count; i++)
        {
            ForecastRow r = _forecastScratch[i];
            bool keyCell = r.Role != StormCellRole.Satellite;
            if (i >= ForecastNearest && !keyCell) continue;
            Label label = _forecastRows[shown++];
            label.text = ForecastText(r);
            label.style.color = keyCell ? HpFull : Color.white;
            label.style.unityFontStyleAndWeight = keyCell ? FontStyle.Bold : FontStyle.Normal;
            label.style.display = DisplayStyle.Flex;
        }
        for (int i = shown; i < _forecastRows.Count; i++) _forecastRows[i].style.display = DisplayStyle.None;
        int more = _forecastScratch.Count - shown;
        _forecastMore.text = more > 0 ? $"+{more} more" : "";
        _forecastMore.style.display = more > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        _forecastPanel.style.display = DisplayStyle.Flex;
    }

    /// <summary>"NW  ~EF3  85 m  PEAK ~45s" (no "~" inside 150 m, where the estimate is exact).</summary>
    public static string ForecastText(ForecastRow r)
    {
        string ef = (r.Uncertain ? "~EF" : "EF") + r.ShownEf;
        string status;
        switch (r.Status)
        {
            case ForecastStatus.Forming: status = $"PEAK ~{r.EtaSeconds:0}s"; break;
            case ForecastStatus.OnGround: status = $"ON GROUND ~{r.EtaSeconds:0}s"; break;
            default: status = "ROPING OUT"; break;
        }
        return $"{r.Bearing,-2}  {ef}  {r.Distance:0} m  {status}";
    }

    private void BuildNewsCrawl(VisualElement root)
    {
        _crawlBar = new VisualElement { pickingMode = PickingMode.Ignore };
        _crawlBar.style.position = Position.Absolute;
        _crawlBar.style.left = 0;
        _crawlBar.style.right = 0;
        _crawlBar.style.bottom = 0;
        _crawlBar.style.height = 34;
        _crawlBar.style.flexDirection = FlexDirection.Row;
        _crawlBar.style.alignItems = Align.Center;
        _crawlBar.style.overflow = Overflow.Hidden;
        _crawlBar.style.backgroundColor = NewsBlue;
        _crawlBar.style.display = DisplayStyle.None;

        _crawlTag = new Label("KTVR NEWS");
        _crawlTag.style.unityFontStyleAndWeight = FontStyle.Bold;
        _crawlTag.style.fontSize = 16;
        _crawlTag.style.color = Color.white;
        _crawlTag.style.backgroundColor = new Color(0.85f, 0.1f, 0.1f);
        _crawlTag.style.paddingLeft = 10;
        _crawlTag.style.paddingRight = 10;
        _crawlTag.style.height = 34;
        _crawlTag.style.unityTextAlign = TextAnchor.MiddleCenter;

        var lane = new VisualElement { pickingMode = PickingMode.Ignore };
        lane.style.flexGrow = 1;
        lane.style.height = 34;
        lane.style.overflow = Overflow.Hidden;
        _crawlText = new Label { pickingMode = PickingMode.Ignore };
        _crawlText.style.position = Position.Absolute;
        _crawlText.style.top = 0;
        _crawlText.style.height = 34;
        _crawlText.style.unityTextAlign = TextAnchor.MiddleLeft;
        _crawlText.style.paddingTop = 0;
        _crawlText.style.paddingBottom = 0;
        _crawlText.style.fontSize = 18;
        _crawlText.style.color = Color.white;
        _crawlText.style.unityFontStyleAndWeight = FontStyle.Bold;
        _crawlText.style.whiteSpace = WhiteSpace.NoWrap;
        lane.Add(_crawlText);

        _crawlBar.Add(_crawlTag);
        _crawlBar.Add(lane);
        root.Add(_crawlBar);
    }

    private void OnStormCellForming(StormCellInfo cell)
    {
        if (TitleAttract.Active) return; // the title's demo storm never reaches the crawl
        string text = StormTelegraph.FormingCrawl(cell.EF, BearingTo(cell.Position));
        if (text != null) QueueCrawl(text, StormTelegraph.IsEmergency(cell.EF));
    }

    private void OnStormCellPeak(StormCellInfo cell)
    {
        if (TitleAttract.Active) return; // the title's demo storm never reaches the crawl
        if (cell.Role != StormCellRole.Anchor || cell.EF < StormTelegraph.WarningMinEf) return;
        QueueCrawl(StormTelegraph.TouchdownCrawl(cell.EF, BearingTo(cell.Position)), StormTelegraph.IsEmergency(cell.EF));
    }

    private string BearingTo(Vector3 cell) =>
        StormTelegraph.Bearing(_vehicle != null ? _vehicle.transform.position : Vector3.zero, cell);

    private void QueueCrawl(string text, bool emergency)
    {
        // An emergency jumps the queue and replaces whatever is crawling.
        if (emergency) { _crawlQueue.Clear(); _crawlUntil = 0f; }
        _crawlQueue.Enqueue((text, emergency));
    }

    private void UpdateNewsCrawl()
    {
        if (_crawlBar == null) return;
        float now = Time.unscaledTime;
        if (now >= _crawlUntil)
        {
            if (_crawlQueue.Count == 0)
            {
                if (_crawlBar.style.display != DisplayStyle.None) _crawlBar.style.display = DisplayStyle.None;
                return;
            }
            (string text, bool emergency) = _crawlQueue.Dequeue();
            _crawlText.text = text;
            _crawlTag.text = emergency ? "KTVR EMERGENCY" : "KTVR NEWS";
            _crawlBar.style.backgroundColor = emergency ? NewsRed : NewsBlue;
            _crawlBar.style.display = DisplayStyle.Flex;
            _crawlUntil = now + _crawlSeconds;
            _crawlX = _crawlBar.resolvedStyle.width > 0f ? _crawlBar.resolvedStyle.width : 1200f;
        }
        _crawlX -= _crawlSpeed * Time.unscaledDeltaTime;
        float textWidth = _crawlText.resolvedStyle.width;
        if (textWidth > 0f && _crawlX < -textWidth) _crawlX = _crawlBar.resolvedStyle.width; // loop until time is up
        _crawlText.style.left = _crawlX;
    }

    private void UpdateStormCam()
    {
        string text = null;
        if (_cameraRig != null && _cameraRig.StormCamEnabled)
        {
            DisasterEntity target = _cameraRig.StormCamTarget;
            text = target == null ? "STORM CAM  ·  NO TARGET"
                : target is TornadoController tornado ? $"STORM CAM  ·  {tornado.EFRating}" : "STORM CAM  ·  LOCKED";
        }
        if (text == _stormCamShown) return;
        _stormCamShown = text;
        if (text == null)
        {
            _stormCamLabel.style.display = DisplayStyle.None;
            return;
        }
        _stormCamLabel.text = text;
        _stormCamLabel.style.color = _cameraRig.StormCamTarget != null ? HpFull : BoostOff;
        _stormCamLabel.style.display = DisplayStyle.Flex;
    }

    // Start() builds the labels, but FilmChanged can fire before that (PhotoTrigger.Start); the label reads the count itself then.
    private void OnFilmChanged(int remaining, int capacity)
    {
        if (_filmLabel == null) return;
        _filmLabel.text = FilmText(remaining);
        _filmLabel.style.color = remaining <= 3 ? HpHit : Color.white;
    }

    private void OnOutOfFilm()
    {
        if (_filmLabel == null) return;
        _filmLabel.text = "NO FILM";
        _filmLabel.style.color = HpHit;
        _filmLabel.schedule.Execute(() => _filmLabel.text = FilmText(0)).StartingIn(600);
    }

    private static string FilmText(int remaining) => $"FILM {remaining:D2}";

    private static Label MakePanelLabel(string text, int size)
    {
        var label = new Label(text);
        label.style.fontSize = size;
        label.style.color = Color.white;
        label.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        label.style.paddingLeft = 8;
        label.style.paddingRight = 8;
        label.style.paddingTop = 4;
        label.style.paddingBottom = 4;
        return label;
    }
}
