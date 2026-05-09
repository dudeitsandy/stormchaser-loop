using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    [SerializeField] private SessionTimer _sessionTimer;
    [SerializeField] private ScoreAccumulator _scoreAccumulator;

    private Label _timeLabel;
    private Label _scoreLabel;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var container = new VisualElement();
        container.style.position = Position.Absolute;
        container.style.top = 16;
        container.style.right = 16;
        container.style.alignItems = Align.FlexEnd;

        _timeLabel = new Label("1:30");
        _timeLabel.style.fontSize = 24;
        _timeLabel.style.color = Color.white;
        _timeLabel.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        _timeLabel.style.paddingLeft = 8;
        _timeLabel.style.paddingRight = 8;
        _timeLabel.style.paddingTop = 4;
        _timeLabel.style.paddingBottom = 4;
        _timeLabel.style.marginBottom = 4;

        _scoreLabel = new Label("0");
        _scoreLabel.style.fontSize = 32;
        _scoreLabel.style.color = Color.white;
        _scoreLabel.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        _scoreLabel.style.paddingLeft = 8;
        _scoreLabel.style.paddingRight = 8;
        _scoreLabel.style.paddingTop = 4;
        _scoreLabel.style.paddingBottom = 4;

        container.Add(_timeLabel);
        container.Add(_scoreLabel);
        root.Add(container);
    }

    private void Update()
    {
        if (_timeLabel == null || _scoreLabel == null) return;
        int seconds = Mathf.CeilToInt(_sessionTimer.TimeRemaining);
        _timeLabel.text = $"{seconds / 60}:{seconds % 60:D2}";
        _scoreLabel.text = $"{_scoreAccumulator.TotalScore:F0}";
    }
}
