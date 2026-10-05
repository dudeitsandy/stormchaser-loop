using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

/// <summary>
/// Ghostweave Labs boot card (Andy 2026-10-05, via stormchaser-96): after Unity's splash and before the title, the
/// studio badge fades in with a short VHS-tracking glitch and a quiet tape-static sting, holds, and fades into the
/// title. Any key, button or click skips straight to the title (that press doesn't also start a run). Shown once per
/// app session; a Retry or a return to the title never shows it again. No game audio; the title music starts with the
/// title as before. Photosensitivity: two small opacity dips in the first 0.3 s, never a full flash, ≤ 3 per second.
/// </summary>
public sealed class BootCard : MonoBehaviour
{
    /// <summary>True once the card has played (or been skipped) this app session.</summary>
    public static bool ShownThisSession { get; private set; }

    /// <summary>Batchmode test runs skip the card unless a test opts in (every scene test expects the title at once).</summary>
    public static bool AllowInBatchmode { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ShownThisSession = false;

    /// <summary>Tests: start a fresh "app session" so the card can show again.</summary>
    public static void ResetSessionForTests() => ShownThisSession = false;

    [SerializeField] private string _logoResource = "UI/GhostweaveLogo";
    [Tooltip("Badge height in panel units (the panel is 1200 wide; 675–750 tall): ≈ 48–53 % of the screen.")]
    [SerializeField] private float _logoHeight = 360f;
    [SerializeField] private Color _background = new Color32(0x23, 0x1F, 0x20, 0xFF); // Unity splash colour
    [SerializeField] private float _fadeIn = 0.4f;
    [SerializeField] private float _hold = 1.8f;
    [SerializeField] private float _fadeOut = 0.4f;
    [Tooltip("Tape-static sting volume, × the player's EFFECTS volume. Very low.")]
    [SerializeField] private float _stingVolume = 0.12f;

    private Action _done;
    private Texture2D _logo;
    private UIDocument _doc;
    private VisualElement _root, _badge, _tracking;
    private IDisposable _skip;
    private AudioSource _sting;
    private float _t;
    private bool _playing, _finishing, _stingPlayed;

    /// <summary>
    /// Plays the card, then calls <paramref name="done"/> (also on skip). If it has already shown this session, can't
    /// load its art, or runs in a batchmode test, calls <paramref name="done"/> at once. Returns true if it is playing.
    /// </summary>
    public bool Play(Action done)
    {
        if (ShownThisSession || (Application.isBatchMode && !AllowInBatchmode))
        {
            done?.Invoke();
            return false;
        }
        var logo = Resources.Load<Texture2D>(_logoResource);
        if (logo == null)
        {
            ShownThisSession = true;
            done?.Invoke();
            return false;
        }
        ShownThisSession = true;
        _done = done;
        GameAudio.AttractMute = true; // no game audio under the card; the title's attract mode keeps it muted
        _logo = logo;
        Build(logo);
        _t = 0f;
        _playing = true;
        return true;
    }

    /// <summary>True while the card is on screen.</summary>
    public bool IsPlaying => _playing;

    private void Build(Texture2D logo)
    {
        var host = new GameObject("BootCardUI");
        host.transform.SetParent(transform, false);
        host.SetActive(false);
        _doc = host.AddComponent<UIDocument>();
        _doc.panelSettings = Resources.Load<PanelSettings>("UI/PanelSettings");
        _doc.sortingOrder = 200; // above the title (RunScreens 100)
        host.SetActive(true);

        _root = new VisualElement { name = "BootCard", pickingMode = PickingMode.Ignore };
        _root.style.position = Position.Absolute;
        _root.style.left = 0;
        _root.style.right = 0;
        _root.style.top = 0;
        _root.style.bottom = 0;
        _root.style.alignItems = Align.Center;
        _root.style.justifyContent = Justify.Center;
        _root.style.backgroundColor = _background;
        _root.style.overflow = Overflow.Hidden;

        _badge = new VisualElement { name = "BootBadge", pickingMode = PickingMode.Ignore };
        _badge.style.backgroundImage = new StyleBackground(logo);
        _badge.style.height = _logoHeight;
        _badge.style.width = _logoHeight * logo.width / logo.height;
        _badge.style.opacity = 0f;
        _root.Add(_badge);

        // One VHS tracking band that rolls down the screen during the fade-in.
        _tracking = new VisualElement { name = "BootTracking", pickingMode = PickingMode.Ignore };
        _tracking.style.position = Position.Absolute;
        _tracking.style.left = 0;
        _tracking.style.right = 0;
        _tracking.style.height = 26;
        _tracking.style.backgroundColor = new Color(0.85f, 0.92f, 0.95f, 0.10f);
        _tracking.style.display = DisplayStyle.None;
        _root.Add(_tracking);

        _doc.rootVisualElement.Add(_root);
    }

    private void Update()
    {
        if (!_playing) return;
        // The first scene starts while Unity's splash is still drawn on top: hold the card until it's gone, or it
        // plays out unseen (found in the 0.8.5 WebGL capture). Then the sting and the timeline start.
        if (!UnityEngine.Rendering.SplashScreen.isFinished) return;
        if (!_stingPlayed)
        {
            _stingPlayed = true;
            PlaySting();
            // Any key, button or click skips, armed once the card is visible (a click to focus the browser canvas
            // during Unity's splash mustn't skip it unseen). The title's own "press anything" arms 0.25 s after the
            // title appears, so the skip press never also starts a run.
            _skip = InputSystem.onAnyButtonPress.CallOnce(_ => Finish());
        }
        _t += Time.unscaledDeltaTime;
        float total = _fadeIn + _hold + _fadeOut;
        if (_t >= total)
        {
            Finish();
            return;
        }
        float opacity = _t < _fadeIn ? _t / _fadeIn
                      : _t < _fadeIn + _hold ? 1f
                      : 1f - (_t - _fadeIn - _hold) / _fadeOut;
        // Fade-in glitch: two shallow dips (never to black) and a sideways tracking jitter.
        float glitch = GlitchAmount(_t);
        _badge.style.opacity = Mathf.Clamp01(opacity) * (1f - 0.3f * glitch);
        _badge.style.translate = new Translate(new Length(8f * glitch * Mathf.Sign(Mathf.Sin(_t * 90f)), LengthUnit.Pixel), 0);
        bool band = _t < _fadeIn + 0.1f;
        _tracking.style.display = band ? DisplayStyle.Flex : DisplayStyle.None;
        if (band) _tracking.style.top = new Length(Mathf.Lerp(-5f, 105f, _t / (_fadeIn + 0.1f)), LengthUnit.Percent);
        // The whole card (background too) fades out into the title.
        if (_t > _fadeIn + _hold) _root.style.opacity = Mathf.Clamp01(opacity);
    }

    /// <summary>0–1 glitch strength: two short blips at 0.12 s and 0.26 s (≤ 3 per second), zero after.</summary>
    public static float GlitchAmount(float t)
    {
        float Blip(float at) => Mathf.Clamp01(1f - Mathf.Abs(t - at) / 0.035f);
        return Mathf.Max(Blip(0.12f), Blip(0.26f));
    }

    private void Finish()
    {
        if (_finishing) return;
        _finishing = true;
        _playing = false;
        _skip?.Dispose();
        _skip = null;
        if (_sting != null) _sting.Stop();
        if (_doc != null) Destroy(_doc.gameObject);
        // 919×1024 isn't a multiple of 4, so it imports uncompressed (3.6 MB); it's only needed for 2.6 s.
        if (_logo != null) Resources.UnloadAsset(_logo);
        _logo = null;
        Action done = _done;
        _done = null;
        done?.Invoke();
    }

    private void OnDestroy()
    {
        _skip?.Dispose();
        _skip = null;
    }

    // A quarter second of filtered tape static with a click on top, synthesized (deterministic noise, no asset).
    private void PlaySting()
    {
        float effects = ProfileStore.Shared != null ? ProfileStore.Shared.Settings.EffectsVolume : 1f;
        if (_stingVolume <= 0f || effects <= 0f) return;
        const int rate = 22050;
        int n = Mathf.RoundToInt(rate * 0.28f);
        var data = new float[n];
        var rng = new System.Random(1996);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.35f;                                  // soften the hiss
            float env = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((0.28f - t) / 0.12f);
            float click = t < 0.004f ? (1f - t / 0.004f) : 0f;           // tape-head click at the start
            data[i] = (lp * 0.7f * env) + click * 0.8f;
        }
        AudioClip clip = AudioClip.Create("BootStatic", n, 1, rate, false);
        clip.SetData(data, 0);
        _sting = gameObject.AddComponent<AudioSource>();
        _sting.playOnAwake = false;
        _sting.spatialBlend = 0f;
        _sting.volume = _stingVolume * effects;
        _sting.clip = clip;
        _sting.Play();
    }
}
