using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Title and results overlays, built in code on their own UIDocument (sortOrder 100).</summary>
public class RunScreens : MonoBehaviour
{
    private static readonly Color Amber = new Color(1f, 0.69f, 0f);
    private static readonly Color Bone = new Color(0.93f, 0.91f, 0.86f);
    private static readonly Color Dim = new Color(0.62f, 0.62f, 0.6f);
    private static readonly Color Alarm = new Color(0.95f, 0.22f, 0.18f);

    [SerializeField] private string _title = "DOOMSDAY";
    [SerializeField] private string _subtitle = "STORM SEASON  ·  PROTOTYPE 0.4";
    [SerializeField] private string _tagline = "Chase the storm. Get the shot. Don't get caught.";

    private VisualElement _overlay;
    private IVisualElementScheduledItem _blink;

    private void Awake()
    {
        // Assign the panel before the UIDocument enables, so it attaches once to the shared panel.
        var host = new GameObject("RunScreensUI");
        host.transform.SetParent(transform, false);
        host.SetActive(false);
        var doc = host.AddComponent<UIDocument>();
        doc.panelSettings = Resources.Load<PanelSettings>("UI/PanelSettings");
        doc.sortingOrder = 100;
        host.SetActive(true);

        _overlay = new VisualElement { name = "RunScreenOverlay", pickingMode = PickingMode.Ignore };
        _overlay.style.position = Position.Absolute;
        _overlay.style.left = 0;
        _overlay.style.right = 0;
        _overlay.style.top = 0;
        _overlay.style.bottom = 0;
        _overlay.style.alignItems = Align.Center;
        _overlay.style.justifyContent = Justify.Center;
        _overlay.style.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 0.78f);
        _overlay.style.display = DisplayStyle.None;
        doc.rootVisualElement.Add(_overlay);
    }

    /// <summary>Shows the title card with the stored best score.</summary>
    public void ShowTitle(float bestScore)
    {
        ClearOverlay();
        _overlay.Add(MakeLabel(_title, 112, Amber, bold: true, letterSpacing: 18));
        _overlay.Add(MakeLabel(_subtitle, 22, Bone, letterSpacing: 6));
        _overlay.Add(Spacer(28));
        _overlay.Add(MakeLabel(_tagline, 20, Dim));
        _overlay.Add(Spacer(36));
        _overlay.Add(MakeLabel("DRIVE   WASD / LEFT STICK", 18, Bone, letterSpacing: 2));
        _overlay.Add(MakeLabel("SHOOT   SPACE / L2", 18, Bone, letterSpacing: 2));
        _overlay.Add(Spacer(12));
        _overlay.Add(MakeLabel("24 frames of film. Centered at ~20m scores best. Shoot from inside the wind for a bonus.", 16, Dim));
        _overlay.Add(MakeLabel("Same storm twice in a row is worth less. Touch the funnel and it scores you.", 16, Dim));
        if (bestScore > 0f)
        {
            _overlay.Add(Spacer(24));
            _overlay.Add(MakeLabel($"BEST  {bestScore:N0}", 22, Amber, letterSpacing: 4));
        }
        _overlay.Add(Spacer(48));
        AddPrompt("PRESS ANY BUTTON");
        _overlay.style.display = DisplayStyle.Flex;
    }

    /// <summary>Shows the end-of-run tally.</summary>
    public void ShowResults(RunSummary summary)
    {
        ClearOverlay();
        _overlay.Add(MakeLabel(summary.Wrecked ? "WRECKED" : "SESSION OVER", 84,
            summary.Wrecked ? Alarm : Amber, bold: true, letterSpacing: 12));
        _overlay.Add(Spacer(24));
        _overlay.Add(MakeLabel($"{summary.Score:N0}", 96, Bone, bold: true));
        if (summary.IsNewBest && summary.Score > 0f)
            _overlay.Add(MakeLabel("NEW BEST", 26, Amber, bold: true, letterSpacing: 8));
        else
            _overlay.Add(MakeLabel($"BEST  {Mathf.Max(summary.PreviousBest, summary.Score):N0}", 22, Dim, letterSpacing: 4));
        _overlay.Add(Spacer(28));
        _overlay.Add(Row("PHOTOS", summary.PhotosTaken.ToString()));
        _overlay.Add(Row("BEST SHOT", $"{summary.BestShot:N0}"));
        _overlay.Add(Spacer(48));
        AddPrompt("ANY BUTTON  RETRY      ESC  TITLE");
        _overlay.style.display = DisplayStyle.Flex;
    }

    /// <summary>Hides whichever screen is showing.</summary>
    public void Hide()
    {
        ClearOverlay();
        _overlay.style.display = DisplayStyle.None;
    }

    private void ClearOverlay()
    {
        _blink?.Pause();
        _blink = null;
        _overlay.Clear();
    }

    private void AddPrompt(string text)
    {
        Label prompt = MakeLabel(text, 22, Bone, bold: true, letterSpacing: 6);
        _overlay.Add(prompt);
        // UI Toolkit's scheduler runs on real time, so this keeps blinking while Time.timeScale = 0.
        _blink = prompt.schedule.Execute(() =>
            prompt.style.opacity = prompt.resolvedStyle.opacity > 0.5f ? 0.15f : 1f).Every(550);
    }

    private static VisualElement Row(string label, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.width = 360;
        row.style.justifyContent = Justify.SpaceBetween;
        row.Add(MakeLabel(label, 22, Dim, letterSpacing: 4));
        row.Add(MakeLabel(value, 22, Bone, bold: true));
        return row;
    }

    private static VisualElement Spacer(float height)
    {
        var v = new VisualElement();
        v.style.height = height;
        return v;
    }

    private static Label MakeLabel(string text, int size, Color color, bool bold = false, float letterSpacing = 0f)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.style.fontSize = size;
        label.style.color = color;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.letterSpacing = letterSpacing;
        label.style.marginTop = 0;
        label.style.marginBottom = 2;
        if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
        return label;
    }
}
