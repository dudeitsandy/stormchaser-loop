#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Dev-only: press F9 to save a screenshot (with UI) to &lt;repo&gt;/production/marketing/art-test/,
/// named by scene and time, for ADR-0003 side-by-side reviews. Installs itself in every scene.
/// </summary>
public class CaptureHotkey : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var go = new GameObject(nameof(CaptureHotkey));
        DontDestroyOnLoad(go);
        go.AddComponent<CaptureHotkey>();
    }

    private static string OutputDir =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "production", "marketing", "art-test"));

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame) return;

        Directory.CreateDirectory(OutputDir);
        string file = Path.Combine(OutputDir,
            $"{SceneManager.GetActiveScene().name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshot(file);
        Debug.Log($"[Capture] {file}");
    }
}
#endif
