using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Applies <see cref="DeviceSettings"/> to the game live (run-screens story 002): camera preset / sensitivity / invert
/// on the chase rig, BRIGHTNESS as a post-exposure offset on the global URP Volume (main view and viewfinder; never
/// photo scoring), MASTER as the listener volume, EFFECTS via <see cref="GameAudio"/>, MUSIC via <see cref="RadioLite"/>
/// (it reads the settings itself), and the display mode on desktop.
/// </summary>
public static class SettingsApplier
{
    private static ColorAdjustments _adjust;

    /// <summary>Brightness −0.5…+0.5 as post-exposure EV: log2(1 + b), so −50 % halves and +50 % is ×1.5 light.</summary>
    public static float BrightnessEv(float brightness) => Mathf.Log(1f + Mathf.Clamp(brightness, -0.5f, 0.5f), 2f);

    /// <summary>Applies everything except the display mode (use at scene start and on every change).</summary>
    public static void ApplyLive(DeviceSettings s)
    {
        ChaseCameraRig rig = Object.FindAnyObjectByType<ChaseCameraRig>();
        if (rig != null)
        {
            rig.ApplyPreset(s.CameraPreset);
            rig.SensitivityScale = s.Sensitivity;
            rig.InvertY = s.InvertY;
        }
        ApplyBrightness(s.Brightness);
        AudioListener.volume = Mathf.Clamp01(s.MasterVolume);
        GameAudio.EffectsVolume = s.EffectsVolume;
    }

    /// <summary>Applies fullscreen / resolution. Desktop always; WebGL only from a key or click (browsers require it).</summary>
    public static void ApplyDisplay(DeviceSettings s, bool fromUserInput)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (fromUserInput) Screen.fullScreen = s.Fullscreen;
            return;
        }
        if (s.ResolutionWidth > 0 && s.ResolutionHeight > 0)
            Screen.SetResolution(s.ResolutionWidth, s.ResolutionHeight, s.Fullscreen);
        else Screen.fullScreen = s.Fullscreen;
    }

    private static void ApplyBrightness(float brightness)
    {
        if (_adjust == null)
        {
            Volume global = null;
            foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (v.isGlobal) { global = v; break; }
            if (global == null) return;
            VolumeProfile profile = global.profile; // runtime instance: the shared asset is never modified
            if (!profile.TryGet(out _adjust)) _adjust = profile.Add<ColorAdjustments>(true);
        }
        _adjust.postExposure.overrideState = true;
        _adjust.postExposure.value = BrightnessEv(brightness);
    }

    /// <summary>Scene changed: forget the cached Volume override.</summary>
    public static void ResetSceneCache() => _adjust = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _adjust = null;
}
