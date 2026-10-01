using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ADR-0003 art test tool. Converts opaque URP Lit/Simple Lit materials — including ones created at
/// runtime by terrain, scatter, and spawned tornadoes — to the toon shader, keeping each base color.
/// Production assets will author toon materials directly; this exists so the test scene restyles
/// everything without touching other systems' code.
/// </summary>
public class ToonStyleApplier : MonoBehaviour
{
    [Tooltip("Toon material whose settings (bands, rim, shadow tint) every converted material copies.")]
    [SerializeField] private Material _template;
    [Tooltip("Saturation multiplier applied to converted base colors (1 = unchanged).")]
    [SerializeField] private float _saturationBoost = 1.25f;
    [Tooltip("Brightness multiplier applied to converted base colors.")]
    [SerializeField] private float _valueBoost = 1.1f;
    [SerializeField] private float _rescanDelay = 0.3f;

    private readonly Dictionary<Material, Material> _converted = new Dictionary<Material, Material>();
    private int _lastDisasterCount = -1;

    private IEnumerator Start()
    {
        if (_template == null)
        {
            Debug.LogWarning("[ToonStyleApplier] No template material assigned; nothing converted.");
            enabled = false;
            yield break;
        }

        ConvertScene();
        // Scatter/terrain build in Awake/Start of their own objects; catch anything made a moment later.
        yield return new WaitForSeconds(_rescanDelay);
        ConvertScene();
    }

    private void Update()
    {
        int count = DisasterEntity.Active.Count;
        if (count == _lastDisasterCount) return;
        _lastDisasterCount = count;
        foreach (DisasterEntity e in DisasterEntity.Active)
            ConvertUnder(e.transform);
    }

    private void ConvertScene()
    {
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            Convert(r);
    }

    private void ConvertUnder(Transform root)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            Convert(r);
    }

    private void Convert(Renderer r)
    {
        if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) return;

        Material[] mats = r.sharedMaterials;
        bool changed = false;
        for (int i = 0; i < mats.Length; i++)
        {
            Material src = mats[i];
            if (src == null || src.shader == _template.shader || !IsConvertible(src)) continue;

            if (!_converted.TryGetValue(src, out Material toon))
            {
                toon = new Material(_template) { name = src.name + "_Toon" };
                toon.SetColor("_BaseColor", Stylize(BaseColorOf(src)));
                if (src.HasProperty("_BaseMap") && src.GetTexture("_BaseMap") != null)
                    toon.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
                _converted[src] = toon;
            }
            mats[i] = toon;
            changed = true;
        }
        if (changed) r.sharedMaterials = mats;
    }

    private static bool IsConvertible(Material m)
    {
        string shader = m.shader.name;
        bool lit = shader == "Universal Render Pipeline/Lit" || shader == "Universal Render Pipeline/Simple Lit"
                   || shader == "Standard";
        bool opaque = !m.HasProperty("_Surface") || m.GetFloat("_Surface") < 0.5f;
        return lit && opaque;
    }

    private static Color BaseColorOf(Material m) =>
        m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.color : Color.white;

    private Color Stylize(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s * _saturationBoost), Mathf.Clamp01(v * _valueBoost));
    }
}
