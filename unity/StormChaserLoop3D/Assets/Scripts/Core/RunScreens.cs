using System.Collections.Generic;
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
    [SerializeField] private float _bannerWidth = 520f; // 640 → 520 (run-screens 006): the title must fit 1080p's ≈ 675-unit height
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
        _overlay.Add(MakeLabel($"{_seasonName}  ·  PROTOTYPE {Application.version}", 20, Bone, letterSpacing: 6));
        _overlay.Add(Spacer(8));
        AddRadio();
        _overlay.Add(Spacer(8));
        // vehicle-feel.md Core Rule 10 bindings (keyboard / gamepad).
        _overlay.Add(MakeLabel("DRIVE  WASD  /  RT LT + LEFT STICK   |   SHOOT  LEFT MOUSE  /  RB", 16, Bone, letterSpacing: 2));
        _overlay.Add(MakeLabel("SLIDE  CTRL / X   |   JUMP  SPACE / A   |   BOOST  SHIFT / B", 16, Bone, letterSpacing: 2));
        _overlay.Add(MakeLabel("CAMERA  MOUSE / RIGHT STICK   |   STORM CAM  TAB / Y", 16, Bone, letterSpacing: 2));
        if (bestScore > 0f)
        {
            _overlay.Add(Spacer(10));
            _overlay.Add(MakeLabel($"BEST  {bestScore:N0}", 20, Amber, letterSpacing: 4));
        }
        _overlay.Add(Spacer(6));
        AddCareerStrip();
        _overlay.Add(MakeLabel(Application.platform == RuntimePlatform.WebGLPlayer
            ? "C / Y  CAREER      O / SELECT  SETTINGS" : "C / Y  CAREER      O / SELECT  SETTINGS      ESC  QUIT",
            16, Dim, letterSpacing: 3));
        _overlay.Add(Spacer(8));
        AddPrompt("PRESS ANYTHING. GO GO GO.");
        _overlay.style.display = DisplayStyle.Flex;
    }

    // Title career strip (run-screens story 005): progress and the next reward, or the paint toggle once it's owned.
    private void AddCareerStrip()
    {
        ProfileStore p = ProfileStore.Shared;
        _overlay.Add(Spacer(10));
        _overlay.Add(MakeLabel(CareerStripText(p.CareerCount(GoalCatalogue.Mode, GoalCatalogue.Map), GoalCatalogue.Career.Count,
                                               p.IsUnlocked(RunRewards.KtvrLivery), p.Data.Livery == RunRewards.KtvrLivery),
                               20, Amber, bold: true, letterSpacing: 4));
        _overlay.Add(Spacer(6));
    }

    /// <summary>Title career strip text (first launch, progress, or the owned paint toggle).</summary>
    public static string CareerStripText(int done, int total, bool ktvrOwned, bool ktvrWorn)
    {
        if (ktvrOwned) return $"CAREER {done}/{total}  ·  PAINT: {(ktvrWorn ? "KTVR" : "STOCK")}  (L / X)";
        int more = Mathf.Max(0, RunRewards.RewardThreshold - done);
        return done == 0
            ? $"CAREER 0/{total}  ·  {RunRewards.RewardThreshold} GOALS UNLOCK KTVR PAINT"
            : $"CAREER {done}/{total}  ·  {more} MORE: KTVR PAINT";
    }

    /// <summary>Career page over the title: the 10 career goals done or not, and the reward.</summary>
    public void ShowCareer()
    {
        ClearOverlay();
        ProfileStore p = ProfileStore.Shared;
        int done = p.CareerCount(GoalCatalogue.Mode, GoalCatalogue.Map);
        int total = GoalCatalogue.Career.Count;
        _overlay.Add(MakeLabel(done == total ? "CAREER COMPLETE" : $"HEARTLAND CAREER   {done} / {total}", 44, Amber, bold: true, letterSpacing: 8));
        _overlay.Add(Spacer(16));
        // Two explicit columns of five (run-screens 006: wrapping collapsed to one column).
        var grid = new VisualElement { pickingMode = PickingMode.Ignore };
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.justifyContent = Justify.Center;
        VisualElement colA = Column(480), colB = Column(480);
        grid.Add(colA);
        grid.Add(colB);
        for (int i = 0; i < GoalCatalogue.Career.Count; i++)
        {
            GoalDef g = GoalCatalogue.Career[i];
            bool has = p.HasCompleted(g.Id);
            Label line = LeftLabel($"{(has ? "DONE" : "  -  ")}   {CareerGoalText(g)}", 20, has ? Amber : Bone, bold: has, letterSpacing: 2);
            line.style.paddingTop = 4;
            line.style.paddingBottom = 4;
            (i < (GoalCatalogue.Career.Count + 1) / 2 ? colA : colB).Add(line);
        }
        _overlay.Add(grid);
        _overlay.Add(Spacer(16));
        bool owned = p.IsUnlocked(RunRewards.KtvrLivery);
        _overlay.Add(MakeLabel(owned ? "REWARD  ·  KTVR PAINT JOB  ·  EARNED"
                                     : $"REWARD  ·  {RunRewards.RewardThreshold} GOALS: KTVR PAINT JOB  ({Mathf.Min(done, RunRewards.RewardThreshold)}/{RunRewards.RewardThreshold})",
                               22, owned ? Amber : Bone, bold: true, letterSpacing: 4));
        _overlay.Add(Spacer(20));
        AddPrompt("ANY BUTTON  BACK");
        _overlay.style.display = DisplayStyle.Flex;
    }

    /// <summary>Career-page text: score goals show their threshold ("PRO SCORE 1,500").</summary>
    public static string CareerGoalText(GoalDef g)
    {
        GoalTuning t = GoalTuning.Defaults;
        switch (g.Key)
        {
            case "score_rookie": return $"{g.Text} {t.ScoreRookie:N0}";
            case "score_pro": return $"{g.Text} {t.ScorePro:N0}";
            case "score_sick": return $"{g.Text} {t.ScoreSick:N0}";
            default: return g.Text;
        }
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

    /// <summary>
    /// Results (design/ux/run-screens.md, run-screens story 004): header, then two columns (score, best, stats and
    /// weather left; this run's goals right), then a footer (unlock banner, save toast, seed line, prompt). The panel
    /// scales to a 1200-unit width, so two columns always fit; the layout stays within ≈ 480 units of height for the
    /// shortest screens (1080p is ≈ 675 units tall, the 960×600 itch embed 750).
    /// </summary>
    public void ShowResults(RunSummary summary)
    {
        ClearOverlay();
        _overlay.Add(MakeLabel(summary.Wrecked ? "WRECKED" : "SESSION OVER", 60,
            summary.Wrecked ? Alarm : Amber, bold: true, letterSpacing: 12));
        _overlay.Add(Spacer(10));

        var columns = new VisualElement { pickingMode = PickingMode.Ignore };
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.alignItems = Align.FlexStart;
        columns.style.justifyContent = Justify.Center;

        var left = Column(380);
        left.Add(MakeLabel($"{summary.Score:N0}", 72, Bone, bold: true));
        if (summary.IsNewBest && summary.Score > 0f)
            left.Add(MakeLabel("NEW BEST", 22, Amber, bold: true, letterSpacing: 8));
        else
            left.Add(MakeLabel($"BEST  {Mathf.Max(summary.PreviousBest, summary.Score):N0}", 20, Dim, letterSpacing: 4));
        left.Add(Spacer(12));
        left.Add(Row("PHOTOS", summary.PhotosTaken.ToString()));
        left.Add(Row("BEST SHOT", $"{summary.BestShot:N0}"));
        if (summary.Storm.Valid)
        {
            left.Add(Row("WEATHER", summary.Storm.Regime));
            if (summary.Storm.BigOneGotAwayEf >= 0)
            {
                left.Add(Spacer(6));
                left.Add(MakeLabel($"THE BIG ONE GOT AWAY (EF{summary.Storm.BigOneGotAwayEf})", 20, Amber, bold: true, letterSpacing: 3));
            }
        }
        columns.Add(left);

        if (summary.Goals.Valid)
        {
            var gap = new VisualElement();
            gap.style.width = 48;
            columns.Add(gap);
            columns.Add(GoalsColumn(summary.Goals));
        }
        _overlay.Add(columns);
        _overlay.Add(Spacer(14));
        AddResultsFooter(summary);
        AddPrompt("ANY BUTTON  RETRY      ESC / B  TITLE");
        _overlay.style.display = DisplayStyle.Flex;
    }

    // Right column: this run's completions (short names, bonus, NEW), the drawn bounties' outcomes, the career count.
    private static VisualElement GoalsColumn(RunGoalsInfo goals)
    {
        var col = Column(420);
        col.Add(LeftLabel("GOALS", 22, Amber, bold: true, letterSpacing: 6));
        var done = new HashSet<string>();
        if (goals.Completions.Count == 0)
            col.Add(LeftLabel("NO GOALS THIS RUN", 18, Dim, letterSpacing: 3));
        foreach (GoalCompletion c in goals.Completions)
        {
            done.Add(c.Id);
            GoalDef def = GoalCatalogue.Find(c.Id);
            if (def == null || c.Kind != GoalKind.Career) continue;
            col.Add(GoalLine(ResultsGoalText(def, c), c.FirstEver));
        }
        if (goals.Bounties.Count > 0)
        {
            col.Add(Spacer(6));
            col.Add(LeftLabel("KTVR BOUNTIES", 16, Dim, letterSpacing: 4));
            foreach (string id in goals.Bounties)
            {
                GoalDef def = GoalCatalogue.Find(id);
                if (def == null) continue;
                GoalCompletion completion = default;
                foreach (GoalCompletion c in goals.Completions) if (c.Id == id) completion = c;
                bool missed = false;
                foreach (string f in goals.FailedBounties) if (f == id) missed = true;
                string text = done.Contains(id) ? ResultsGoalText(def, completion)
                            : missed ? $"MISS  {def.ShortName}" : $"-  {def.ShortName}";
                col.Add(GoalLine(text, done.Contains(id) && completion.FirstEver, missed ? Dim : (Color?)null));
            }
        }
        col.Add(Spacer(6));
        ProfileStore profile = ProfileStore.Shared;
        col.Add(LeftLabel($"CAREER  {profile.CareerCount(GoalCatalogue.Mode, GoalCatalogue.Map)} / {GoalCatalogue.Career.Count}",
                          18, Bone, bold: true, letterSpacing: 4));
        return col;
    }

    /// <summary>Results line for a completed goal: "DONE  BIG AIR  +150" (score goals show no bonus).</summary>
    public static string ResultsGoalText(GoalDef def, GoalCompletion c) =>
        c.Bonus > 0 ? $"DONE  {def.ShortName}  +{c.Bonus}" : $"DONE  {def.ShortName}";

    private static VisualElement GoalLine(string text, bool isNew, Color? color = null)
    {
        var line = new VisualElement { pickingMode = PickingMode.Ignore };
        line.style.flexDirection = FlexDirection.Row;
        line.style.justifyContent = Justify.SpaceBetween;
        line.style.width = 400;
        line.Add(LeftLabel(text, 18, color ?? Bone, letterSpacing: 2));
        if (isNew)
        {
            Label tag = LeftLabel("NEW", 16, Amber, bold: true, letterSpacing: 3);
            // NEW pulses amber once (spec: Transitions).
            tag.style.opacity = 0.2f;
            tag.schedule.Execute(() => tag.style.opacity = 1f).StartingIn(250);
            line.Add(tag);
        }
        return line;
    }

    // Footer: unlock banner, save toast, seed / version line.
    private void AddResultsFooter(RunSummary summary)
    {
        foreach (string unlock in summary.Goals.Valid ? summary.Goals.NewUnlocks : System.Array.Empty<string>())
            if (unlock == RunRewards.KtvrLivery)
                _overlay.Add(MakeLabel("UNLOCKED: KTVR PAINT JOB  ·  WORN NEXT RUN", 24, Amber, bold: true, letterSpacing: 4));
        if (summary.Goals.Valid && !summary.Goals.Saved)
            _overlay.Add(MakeLabel("COULDN'T SAVE — PROGRESS KEPT, WILL RETRY", 18, Alarm, bold: true, letterSpacing: 2));
        if (summary.Storm.Valid)
        {
            _overlay.Add(MakeLabel($"SEED {summary.Storm.Seed}  ·  v{summary.Storm.BuildVersion}  ·  REPLAY WITH ?seed={summary.Storm.Seed}",
                                   14, Dim, letterSpacing: 2));
            if (summary.Storm.VersionMismatch)
                _overlay.Add(MakeLabel("Different version: storms may differ.", 16, Alarm));
        }
        _overlay.Add(Spacer(10));
    }

    /// <summary>The WRECKED slam during the slow-mo beat (run-screens story 003).</summary>
    public void ShowWrecked()
    {
        ClearOverlay();
        _overlay.style.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 0.25f);
        Label slam = MakeLabel("W R E C K E D", 96, Alarm, bold: true, letterSpacing: 10);
        slam.style.scale = new Scale(Vector3.one * 1.4f);
        slam.schedule.Execute(() => slam.style.scale = new Scale(Vector3.one)).StartingIn(200);
        _overlay.Add(slam);
        _overlay.style.display = DisplayStyle.Flex;
    }

    private static VisualElement Column(float width)
    {
        var col = new VisualElement { pickingMode = PickingMode.Ignore };
        col.style.width = width;
        col.style.alignItems = Align.Center;
        return col;
    }

    private static Label LeftLabel(string text, int size, Color color, bool bold = false, float letterSpacing = 0f)
    {
        Label l = MakeLabel(text, size, color, bold, letterSpacing);
        l.style.unityTextAlign = TextAnchor.MiddleLeft;
        l.style.alignSelf = Align.FlexStart;
        return l;
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
        _overlay.style.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 0.78f);
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
