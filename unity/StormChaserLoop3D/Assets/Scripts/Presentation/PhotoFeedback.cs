using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Shutter flash and animated shot feedback, driven only by gameplay events.</summary>
public sealed class PhotoFeedback : MonoBehaviour
{
    [SerializeField] private float _flashSeconds = 0.12f;
    [SerializeField] private float _popupSeconds = 1.1f;
    private VisualElement _flash;
    private VisualElement _popup;
    private Label _score;
    private Label _tier;
    private Label _modifiers;
    private float _flashStrength = 0.65f;
    private float _age = float.PositiveInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() != null && Object.FindAnyObjectByType<PhotoFeedback>() == null)
            new GameObject(nameof(PhotoFeedback)).AddComponent<PhotoFeedback>();
    }

    private void Awake()
    {
        var root = PresentationOverlay.Create(transform, "PhotoFeedbackUI", 40);
        if (root == null) return;
        _flash = new VisualElement { pickingMode = PickingMode.Ignore };
        _flash.style.position = Position.Absolute;
        _flash.style.left = _flash.style.right = _flash.style.top = _flash.style.bottom = 0;
        _flash.style.backgroundColor = Color.white;
        _flash.style.opacity = 0;
        root.Add(_flash);
        _popup = new VisualElement { pickingMode = PickingMode.Ignore };
        _popup.style.position = Position.Absolute;
        _popup.style.top = Length.Percent(30);
        _popup.style.left = 0;
        _popup.style.right = 0;
        _popup.style.opacity = 0;
        _score = PresentationOverlay.Label("", 40, Color.white);
        _tier = PresentationOverlay.Label("", 22, Color.white);
        _modifiers = PresentationOverlay.Label("", 18, new Color(1f, 0.75f, 0.15f));
        _popup.Add(_score);
        _popup.Add(_tier);
        _popup.Add(_modifiers);
        root.Add(_popup);
    }

    private void OnEnable()
    {
        GameEvents.PhotoTaken += OnPhoto;
        GameEvents.PhotoMissed += OnMiss;
        GameEvents.OutOfFilm += OnOutOfFilm;
        GameEvents.RunStarted += Clear;
        GameEvents.RunEnded += OnEnd;
    }
    private void OnDisable()
    {
        GameEvents.PhotoTaken -= OnPhoto;
        GameEvents.PhotoMissed -= OnMiss;
        GameEvents.OutOfFilm -= OnOutOfFilm;
        GameEvents.RunStarted -= Clear;
        GameEvents.RunEnded -= OnEnd;
    }
    private void OnPhoto(PhotoResult result)
    {
        string modifiers = result.InTheWind
            ? "IN THE WIND ×" + result.WindMultiplier.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
            : "";
        if (result.IsStaleRepeat) modifiers += (modifiers.Length > 0 ? "  ·  " : "") + "SAME SHOT";
        Show($"+{result.Score:N0}", result.Tier == ShotTier.Perfect ? "PERFECT" :
            result.Tier == ShotTier.Good ? "GOOD SHOT" : "GLANCING",
            result.Tier == ShotTier.Perfect ? new Color(1f, 0.75f, 0.15f) : Color.white, modifiers);
    }
    private void OnMiss() => Show("NO SUBJECT", "Get within range of a tornado", new Color(1f, 0.55f, 0.35f));
    private void OnOutOfFilm() => Show("NO FILM", "Film exhausted for this run", new Color(1f, 0.55f, 0.35f), dry: true);
    private void Show(string score, string tier, Color color, string modifiers = "", bool dry = false)
    {
        if (_popup == null) return;
        _score.text = score;
        _tier.text = tier;
        _modifiers.text = modifiers;
        _flash.style.backgroundColor = dry ? color : Color.white;
        _flashStrength = dry ? 0.25f : 0.65f;
        _score.style.color = _tier.style.color = color;
        _age = 0;
    }
    private void OnEnd(RunSummary summary) => Clear();
    private void Clear() => _age = float.PositiveInfinity;
    private void Update()
    {
        if (_popup == null) return;
        _age += Time.unscaledDeltaTime;
        _flash.style.opacity = _flashStrength * Mathf.Clamp01(1f - _age / Mathf.Max(0.01f, _flashSeconds));
        _popup.style.opacity = Mathf.Clamp01((_popupSeconds - _age) / 0.3f);
        _popup.style.top = Length.Percent(30f - Mathf.Min(_age, _popupSeconds) * 3f);
    }
}
