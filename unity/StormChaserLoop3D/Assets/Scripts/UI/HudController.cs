using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>In-run HUD: time and score top-right, truck HP top-left.</summary>
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    private static readonly Color HpFull = new Color(1f, 0.69f, 0f);
    private static readonly Color HpEmpty = new Color(1f, 1f, 1f, 0.15f);
    private static readonly Color HpHit = new Color(0.95f, 0.22f, 0.18f);

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
    private PhotoTrigger _photo;
    private float _lastWindShown = -1f;
    private readonly List<VisualElement> _hpPips = new List<VisualElement>();
    private int _lastSeconds = -1;
    private float _lastScore = -1f;

    private void Awake()
    {
        if (_vehicleHealth == null) _vehicleHealth = FindAnyObjectByType<VehicleHealth>();
    }

    private void OnEnable()
    {
        GameEvents.PlayerDamaged += OnPlayerDamaged;
        GameEvents.FilmChanged += OnFilmChanged;
        GameEvents.OutOfFilm += OnOutOfFilm;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDamaged -= OnPlayerDamaged;
        GameEvents.FilmChanged -= OnFilmChanged;
        GameEvents.OutOfFilm -= OnOutOfFilm;
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

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
        root.Add(right);

        var left = new VisualElement();
        left.style.position = Position.Absolute;
        left.style.top = 16;
        left.style.left = 16;
        left.style.flexDirection = FlexDirection.Row;
        left.style.alignItems = Align.Center;
        left.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        left.style.paddingLeft = 8;
        left.style.paddingRight = 8;
        left.style.paddingTop = 6;
        left.style.paddingBottom = 6;

        var hpTitle = new Label("TRUCK");
        hpTitle.style.color = Color.white;
        hpTitle.style.fontSize = 16;
        hpTitle.style.marginRight = 8;
        left.Add(hpTitle);

        int max = _vehicleHealth != null ? _vehicleHealth.MaxHealth : 3;
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
        root.Add(left);

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
        root.Add(center);
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

        if (!Mathf.Approximately(_scoreAccumulator.TotalScore, _lastScore))
        {
            _lastScore = _scoreAccumulator.TotalScore;
            _scoreLabel.text = $"{_lastScore:N0}";
        }
    }

    private void OnPlayerDamaged(int current, int max)
    {
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
