using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Re-runs self-installing systems on every single-mode scene load. <c>RuntimeInitializeLoadType.AfterSceneLoad</c>
/// fires only for the first scene of an app launch, so installers that relied on it vanished after a retry
/// reload (0.6.0 itch bug). Installers register once from AfterSceneLoad and are idempotent.
/// </summary>
public static class SceneInstaller
{
    private static readonly List<Action> _installers = new List<Action>();

    // Statics survive play sessions when domain reload is disabled; reset and re-hook on entry.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _installers.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Runs <paramref name="install"/> now and after every later single-mode scene load.</summary>
    public static void EveryScene(Action install)
    {
        if (!_installers.Contains(install)) _installers.Add(install);
        install();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        foreach (Action install in _installers) install();
    }
}
