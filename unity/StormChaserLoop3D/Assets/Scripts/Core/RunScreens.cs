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
    [Tooltip("Title key art (Resources path). Falls back to the text title when missing.")]
    [SerializeField] private string _bannerResource = "UI/TitleBanner";
    [Tooltip("Banner width in panel units (reference 1200 wide); height follows the image's aspect.")]
    [SerializeField] private float _bannerWidth = 640f;
    [Tooltip("Shown before the build version, e.g. STORM SEASON · PROTOTYPE 0.5.0.")]
    [SerializeField] private string _seasonName = "STORM SEASON";
    [Tooltip("Title attract-mode DJ (Andy 2026-10-03: Crazy Taxi energy). Rotates one line at a time.")]
    [SerializeField] private string[] _radioLines =
    {
        "IT'S A BIG ONE, PEOPLE!",
        "GET IN THE TRUCK!",
        "THAT FUNNEL WON'T FILM ITSELF!",
        "WHO NEEDS A ROOF ANYWAY?!",
        "24 FRAMES! MAKE 'EM COUNT!",
        "GET IN THE WIND! DOUBLE THE MONEY!",
    };
    [Tooltip("Milliseconds each DJ line stays up.")]
    [SerializeField] private long _radioLineMs = 1700;

    private VisualElement _overlay;
    private IVisualElementScheduledItem _blink;
    private IVisualElementScheduledItem _ticker;

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
        AddTitleArt();
        // Version comes from PlayerSettings.bundleVersion (set by BuildScript), never a scene-saved string.
        _overlay.Add(MakeLabel($"{_seasonName}  ·  PROTOTYPE {Application.version}", 22, Bone, letterSpacing: 6));
        _overlay.Add(Spacer(16));
        AddRadio();
        _overlay.Add(Spacer(20));
        // vehicle-feel.md Core Rule 10 bindings (keyboard / gamepad).
        _overlay.Add(MakeLabel("DRIVE  WASD  /  RT LT + LEFT STICK", 18, Bone, letterSpacing: 2));
        _overlay.Add(MakeLabel("SHOOT  LEFT MOUSE  /  RB", 18, Bone, letterSpacing: 2));
        _overlay.Add(MakeLabel("SLIDE  CTRL / X   |   JUMP  SPACE / A   |   BOOST  SHIFT / B", 18, Bone, letterSpacing: 2));
        _overlay.Add(MakeLabel("CAMERA  MOUSE / RIGHT STICK   |   STORM CAM  TAB / Y", 18, Bone, letterSpacing: 2));
        if (bestScore > 0f)
        {
            _overlay.Add(Spacer(24));
            _overlay.Add(MakeLabel($"BEST  {bestScore:N0}", 22, Amber, letterSpacing: 4));
        }
        _overlay.Add(Spacer(28));
        _overlay.Add(MakeLabel(Application.platform == RuntimePlatform.WebGLPlayer
            ? "O / SELECT  SETTINGS" : "O / SELECT  SETTINGS      ESC  QUIT", 16, Dim, letterSpacing: 3));
        _overlay.Add(Spacer(8));
        AddPrompt("PRESS ANYTHING. GO GO GO.");
        _overlay.style.display = DisplayStyle.Flex;
    }

    /// <summary>Key art banner (docs/visual-targets "Doomsday Tornado Highway"), or the text title without it.</summary>
    private void AddTitleArt()
    {
        var art = Resources.Load<Texture2D>(_bannerResource);
        if (art == null)
        {
            _overlay.Add(MakeLabel(_title, 112, Amber, bold: true, letterSpacing: 18));
            return;
        }
        var banner = new VisualElement { name = "TitleBanner", pickingMode = PickingMode.Ignore };
        banner.style.backgroundImage = new StyleBackground(art);
        banner.style.width = _bannerWidth;
        banner.style.height = _bannerWidth * art.height / art.width;
        banner.style.marginBottom = 6;
        _overlay.Add(banner);
    }

    /// <summary>
    /// "KTVR STORM RADIO — LIVE" plus one shouted line at a time. Each new line slams in oversized and
    /// tilted, alternating sides, then settles (UI Toolkit scheduler: real time, runs while paused).
    /// </summary>
    private void AddRadio()
    {
        Label station = MakeLabel(">> KTVR STORM RADIO  ·  LIVE <<", 18, Alarm, bold: true, letterSpacing: 4);
        _overlay.Add(station);
        _overlay.Add(Spacer(10));
        if (_radioLines == null || _radioLines.Length == 0) return;

        Label line = MakeLabel(_radioLines[0], 40, Amber, bold: true, letterSpacing: 2);
        line.style.height = 56;
        _overlay.Add(line);
        int index = 0;
        Slam(line, index);
        _ticker = line.schedule.Execute(() =>
        {
            index = (index + 1) % _radioLines.Length;
            line.text = _radioLines[index];
            Slam(line, index);
        }).Every(_radioLineMs);
    }

    private static void Slam(Label line, int index)
    {
        float tilt = index % 2 == 0 ? -4f : 3f;
        line.style.scale = new Scale(Vector3.one * 1.35f);
        line.style.rotate = new Rotate(new Angle(tilt * 2f));
        line.schedule.Execute(() =>
        {
            line.style.scale = new Scale(Vector3.one);
            line.style.rotate = new Rotate(new Angle(tilt));
        }).StartingIn(90);
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
        AddStormBlock(summary.Storm);
        _overlay.Add(Spacer(40));
        AddPrompt("ANY BUTTON  RETRY      ESC  TITLE");
        _overlay.style.display = DisplayStyle.Flex;
    }

    // storm-director.md UI Requirements (story 009): regime, the anchor that got away, seed + build for replay.
    private void AddStormBlock(StormRunInfo storm)
    {
        if (!storm.Valid) return;
        _overlay.Add(Row("WEATHER", storm.Regime));
        if (storm.BigOneGotAwayEf >= 0)
        {
            _overlay.Add(Spacer(10));
            _overlay.Add(MakeLabel($"THE BIG ONE GOT AWAY (EF{storm.BigOneGotAwayEf})", 24, Amber, bold: true, letterSpacing: 4));
        }
        _overlay.Add(Spacer(12));
        _overlay.Add(MakeLabel($"SEED {storm.Seed}  ·  v{storm.BuildVersion}  ·  REPLAY WITH ?seed={storm.Seed}", 14, Dim, letterSpacing: 2));
        if (storm.VersionMismatch)
            _overlay.Add(MakeLabel("Different version: storms may differ.", 16, Alarm));
    }

    /// <summary>
    /// Pause menu (design/ux/run-screens.md): PAUSED over the frozen world, the entries with the focused one
    /// marked, or the quit confirm. <paramref name="message"/> is an optional line under the menu. Entries are
    /// mouse-hoverable and clickable; callbacks get the entry index (confirm: 0 KEEP PLAYING, 1 QUIT).
    /// </summary>
    public void ShowPause(PauseMenu menu, string message, System.Action<int> onHover, System.Action<int> onClick)
    {
        ClearOverlay();
        _overlay.Add(MakeLabel("PAUSED", 72, Amber, bold: true, letterSpacing: 12));
        _overlay.Add(Spacer(28));
        if (menu.Confirming.HasValue)
        {
            string what = menu.Confirming.Value == PauseItem.QuitToDesktop ? "QUIT TO DESKTOP?" : "QUIT RUN?";
            _overlay.Add(MakeLabel($"{what} NOTHING FROM THIS RUN IS BANKED.", 24, Alarm, bold: true, letterSpacing: 2));
            _overlay.Add(Spacer(16));
            _overlay.Add(MenuEntry("KEEP PLAYING", !menu.ConfirmQuitFocused, 0, null, onClick));
            _overlay.Add(MenuEntry("QUIT", menu.ConfirmQuitFocused, 1, null, onClick));
        }
        else
        {
            for (int i = 0; i < menu.Items.Count; i++)
                _overlay.Add(MenuEntry(PauseMenu.Label(menu.Items[i]), i == menu.Focus, i, onHover, onClick));
        }
        if (!string.IsNullOrEmpty(message))
        {
            _overlay.Add(Spacer(16));
            _overlay.Add(MakeLabel(message, 18, Dim, letterSpacing: 4));
        }
        _overlay.Add(Spacer(28));
        _overlay.Add(MakeLabel("W S / D-PAD  MOVE      ENTER / A  PICK      ESC / B  BACK", 16, Dim, letterSpacing: 2));
        _overlay.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Settings panel (design/ux/run-screens.md): label column left, value column right, focused row marked; values
    /// change with ◂ ▸ (shown as "&lt; &gt;", the font has no arrow-glyph guarantee). Callbacks get the row index.
    /// </summary>
    public void ShowSettings(SettingsMenu menu, System.Action<int> onHover, System.Action<int> onClick)
    {
        ClearOverlay();
        _overlay.Add(MakeLabel("SETTINGS", 60, Amber, bold: true, letterSpacing: 12));
        _overlay.Add(Spacer(20));
        for (int i = 0; i < menu.Rows.Count; i++)
        {
            SettingRow row = menu.Rows[i];
            bool focused = i == menu.Focus;
            var line = new VisualElement { pickingMode = PickingMode.Position };
            line.style.flexDirection = FlexDirection.Row;
            line.style.justifyContent = Justify.SpaceBetween;
            line.style.width = 560;
            line.style.paddingTop = 3;
            line.style.paddingBottom = 3;
            Color c = focused ? Amber : Bone;
            Label name = MakeLabel((focused ? ">  " : "   ") + SettingsMenu.Label(row), 24, c, bold: focused, letterSpacing: 4);
            name.style.unityTextAlign = TextAnchor.MiddleLeft;
            line.Add(name);
            string value = menu.Value(row);
            if (value.Length > 0)
            {
                Label v = MakeLabel(focused ? $"<  {value}  >" : value, 24, c, bold: focused, letterSpacing: 2);
                v.style.unityTextAlign = TextAnchor.MiddleRight;
                line.Add(v);
            }
            int index = i;
            if (onHover != null) line.RegisterCallback<PointerEnterEvent>(_ => onHover(index));
            if (onClick != null) line.RegisterCallback<ClickEvent>(_ => onClick(index));
            _overlay.Add(line);
        }
        _overlay.Add(Spacer(20));
        _overlay.Add(MakeLabel("W S  MOVE     A D / < >  CHANGE     ESC / B  BACK  (SAVED ON CLOSE)", 16, Dim, letterSpacing: 2));
        _overlay.style.display = DisplayStyle.Flex;
    }

    /// <summary>Windows title: the second Esc quits; anything else returns to the title.</summary>
    public void ShowQuitGameConfirm()
    {
        ClearOverlay();
        _overlay.Add(MakeLabel("QUIT GAME?", 72, Amber, bold: true, letterSpacing: 12));
        _overlay.Add(Spacer(24));
        AddPrompt("ESC AGAIN TO QUIT      ANY OTHER KEY  BACK");
        _overlay.style.display = DisplayStyle.Flex;
    }

    // A focusable menu row: "> ENTRY <" in amber when focused (the font has no ▸ glyph guarantee).
    private static Label MenuEntry(string text, bool focused, int index, System.Action<int> onHover, System.Action<int> onClick)
    {
        Label entry = MakeLabel(focused ? $">  {text}  <" : text, 30, focused ? Amber : Bone, bold: focused, letterSpacing: 6);
        entry.pickingMode = PickingMode.Position;
        entry.style.minWidth = 420;
        entry.style.paddingTop = 6;
        entry.style.paddingBottom = 6;
        if (onHover != null) entry.RegisterCallback<PointerEnterEvent>(_ => onHover(index));
        if (onClick != null) entry.RegisterCallback<ClickEvent>(_ => onClick(index));
        return entry;
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
        _ticker?.Pause();
        _ticker = null;
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
