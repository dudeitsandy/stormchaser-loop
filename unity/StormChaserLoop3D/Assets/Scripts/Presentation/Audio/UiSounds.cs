using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime synthesized menu and goal sounds; menu audio remains audible while the listener is paused.</summary>
public sealed class UiSounds : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _volume = 0.38f;
    private static UiSounds _current;
    private AudioSource _menu, _goals;
    private AudioClip _move, _pick, _complete, _firstEver, _miss;
    private readonly List<AudioClip> _clips = new List<AudioClip>();
    private float _lastMove = float.NegativeInfinity;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);
    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<UiSounds>() != null) return;
        new GameObject(nameof(UiSounds)).AddComponent<UiSounds>();
    }
    /// <summary>Called by gameplay UI only when menu focus actually changes, including mouse hover.</summary>
    public static void MenuMove()
    {
        if (_current == null || Time.unscaledTime - _current._lastMove < 0.045f) return;
        _current._lastMove = Time.unscaledTime;
        _current._menu.PlayOneShot(_current._move, _current._volume);
    }
    /// <summary>Called by gameplay UI when a menu item is selected; does not sample raw input.</summary>
    public static void MenuPick()
    {
        if (_current != null) _current._menu.PlayOneShot(_current._pick, _current._volume);
    }
    private void Awake()
    {
        _menu = gameObject.AddComponent<AudioSource>(); _goals = gameObject.AddComponent<AudioSource>();
        foreach (var source in new[] { _menu, _goals }) { source.playOnAwake = false; source.spatialBlend = 0f; }
        _menu.ignoreListenerPause = true;
        _move = Clip("MenuMoveTick", 0.045f, 0); _pick = Clip("MenuPickClunk", 0.12f, 1);
        _complete = Clip("GoalCompleteSting", 0.42f, 2); _firstEver = Clip("FirstEverGoalSting", 0.7f, 3);
        _miss = Clip("BountyMiss", 0.18f, 4);
    }
    private void OnEnable()
    {
        _current = this; GameEvents.GoalCompleted += Completed; GameEvents.BountyFailed += Failed;
        GameEvents.MenuMoved += MenuMove; GameEvents.MenuPicked += MenuPick;
    }
    private void Completed(GoalCompletion goal) => _goals.PlayOneShot(goal.FirstEver ? _firstEver : _complete, _volume);
    private void Failed(string id) => _goals.PlayOneShot(_miss, _volume * 0.7f);
    private AudioClip Clip(string name, float seconds, int kind)
    {
        const int rate = 22050;
        var samples = new float[Mathf.RoundToInt(seconds * rate)];
        var random = new System.Random(806 + kind);
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float value;
            if (kind <= 1)
                value = (Mathf.Sin(2f * Mathf.PI * (kind == 0 ? 1100f : 140f) * t) * 0.5f
                    + ((float)random.NextDouble() * 2f - 1f) * 0.12f) * Mathf.Exp(-t * (kind == 0 ? 80f : 25f));
            else if (kind == 4)
                value = Mathf.Sin(2f * Mathf.PI * (300f * t - 350f * t * t)) * Mathf.Exp(-t * 12f) * 0.45f;
            else
            {
                int note = Mathf.Min(kind == 3 ? 3 : 2, (int)(t / (kind == 3 ? 0.14f : 0.12f)));
                float frequency = note == 0 ? 523.25f : note == 1 ? 659.25f : note == 2 ? 783.99f : 1046.5f;
                float local = t - note * (kind == 3 ? 0.14f : 0.12f);
                value = (Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.42f
                    + Mathf.Sin(4f * Mathf.PI * frequency * t) * 0.1f) * Mathf.Exp(-local * 8f)
                    * Mathf.Clamp01(local / 0.008f);
            }
            samples[i] = value * Mathf.Clamp01(t / 0.005f) * Mathf.Clamp01((seconds - t) / 0.025f);
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); _clips.Add(clip); return clip;
    }
    private void OnDisable()
    {
        GameEvents.GoalCompleted -= Completed; GameEvents.BountyFailed -= Failed;
        GameEvents.MenuMoved -= MenuMove; GameEvents.MenuPicked -= MenuPick;
        if (_current == this) _current = null;
        if (_menu != null) _menu.Stop(); if (_goals != null) _goals.Stop();
    }
    private void OnDestroy() { foreach (var clip in _clips) if (clip != null) Destroy(clip); }
}
