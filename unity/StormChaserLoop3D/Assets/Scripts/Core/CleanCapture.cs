using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
/// Clean capture for marketing shots (Andy 2026-10-05, via stormchaser-96): URL <c>?clean=1</c>, desktop
/// <c>-clean=1</c>. Hides every UI Toolkit overlay (title, HUD, crawl, style pops, viewfinder) while the world, storms
/// and the title flyover keep running, and skips the boot card. <b>V</b> toggles the viewfinder back on for PiP shots.
/// Gameplay, scoring, goals and saves are untouched. Never active without the flag.
/// </summary>
public sealed class CleanCapture : MonoBehaviour
{
    private const string Arg = "clean=1";
    private const string ViewfinderHost = "ViewfinderUI"; // PipViewfinder's overlay (Presentation lane)
    private bool _showViewfinder;

    /// <summary>True when the flag is on (checked once per app session).</summary>
    public static bool Active => _active ??= Requested();
    private static bool? _active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _active = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);

    private static void Install()
    {
        if (!Active || Object.FindAnyObjectByType<CleanCapture>() != null) return;
        new GameObject(nameof(CleanCapture)).AddComponent<CleanCapture>();
    }

    private static bool Requested()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg == "-" + Arg) return true;
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token == Arg) return true;
        return false;
    }

    // Overlays are built at runtime by several systems (some after a delay), so re-apply twice a second rather than
    // scanning every frame.
    private void OnEnable() => InvokeRepeating(nameof(Apply), 0f, 0.5f);
    private void OnDisable() => CancelInvoke(nameof(Apply));

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.vKey.wasPressedThisFrame) return;
        _showViewfinder = !_showViewfinder;
        Apply();
    }

    private void Apply()
    {
        foreach (UIDocument doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            if (doc.rootVisualElement == null) continue;
            bool show = _showViewfinder && doc.gameObject.name == ViewfinderHost;
            doc.rootVisualElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
