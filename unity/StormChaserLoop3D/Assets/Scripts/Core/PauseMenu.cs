using System.Collections.Generic;

/// <summary>An entry in the pause menu.</summary>
public enum PauseItem { Resume, Settings, QuitRun, QuitToDesktop }

/// <summary>What the run should do after a pause-menu input.</summary>
public enum PauseAction { None, Resume, ShowSettings, ForfeitToTitle, QuitToDesktop }

/// <summary>
/// Pause menu model (design/ux/run-screens.md): RESUME, SETTINGS, QUIT RUN and, on platforms that can quit,
/// QUIT TO DESKTOP. Both quits go through a confirm whose default focus is KEEP PLAYING, so a mashed button never
/// quits. Pure state: <see cref="RunManager"/> feeds it input and acts on the returned <see cref="PauseAction"/>.
/// </summary>
public sealed class PauseMenu
{
    private readonly List<PauseItem> _items = new List<PauseItem>(4);

    /// <summary>The menu entries, top to bottom.</summary>
    public IReadOnlyList<PauseItem> Items => _items;
    /// <summary>Index of the focused entry in <see cref="Items"/>.</summary>
    public int Focus { get; private set; }
    /// <summary>The quit being confirmed, or null when the main menu shows.</summary>
    public PauseItem? Confirming { get; private set; }
    /// <summary>In the confirm: true when QUIT is focused, false for KEEP PLAYING (the default).</summary>
    public bool ConfirmQuitFocused { get; private set; }

    /// <param name="canQuitToDesktop">False on WebGL, where a page cannot close itself.</param>
    public PauseMenu(bool canQuitToDesktop)
    {
        _items.Add(PauseItem.Resume);
        _items.Add(PauseItem.Settings);
        _items.Add(PauseItem.QuitRun);
        if (canQuitToDesktop) _items.Add(PauseItem.QuitToDesktop);
    }

    /// <summary>Moves focus by <paramref name="delta"/> (−1 up, +1 down), wrapping; in the confirm, toggles.</summary>
    public void Move(int delta)
    {
        if (delta == 0) return;
        if (Confirming.HasValue)
        {
            ConfirmQuitFocused = !ConfirmQuitFocused;
            return;
        }
        int n = _items.Count;
        Focus = ((Focus + delta) % n + n) % n;
    }

    /// <summary>Focuses an entry directly (mouse hover).</summary>
    public void FocusOn(int index)
    {
        if (!Confirming.HasValue && index >= 0 && index < _items.Count) Focus = index;
    }

    /// <summary>Activates the focused entry (Enter / A / click).</summary>
    public PauseAction Select()
    {
        if (Confirming.HasValue)
        {
            PauseItem quit = Confirming.Value;
            bool confirmed = ConfirmQuitFocused;
            Confirming = null;
            ConfirmQuitFocused = false;
            if (!confirmed) return PauseAction.None;
            return quit == PauseItem.QuitToDesktop ? PauseAction.QuitToDesktop : PauseAction.ForfeitToTitle;
        }
        switch (_items[Focus])
        {
            case PauseItem.Resume: return PauseAction.Resume;
            case PauseItem.Settings: return PauseAction.ShowSettings;
            default:
                Confirming = _items[Focus];
                ConfirmQuitFocused = false;
                return PauseAction.None;
        }
    }

    /// <summary>Back (Esc / B / Start): leaves the confirm, otherwise resumes.</summary>
    public PauseAction Back()
    {
        if (!Confirming.HasValue) return PauseAction.Resume;
        Confirming = null;
        ConfirmQuitFocused = false;
        return PauseAction.None;
    }

    /// <summary>HUD text for an entry.</summary>
    public static string Label(PauseItem item)
    {
        switch (item)
        {
            case PauseItem.Resume: return "RESUME";
            case PauseItem.Settings: return "SETTINGS";
            case PauseItem.QuitRun: return "QUIT RUN";
            default: return "QUIT TO DESKTOP";
        }
    }
}
