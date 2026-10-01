using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Owned, lazily generated illustrated card textures and transparent URP materials.</summary>
internal sealed class CardVfxAssets : IDisposable
{
    internal enum Shape { Band, Dust, Streak, Debris, Funnel }
    private readonly Dictionary<Shape, Material> _materials = new Dictionary<Shape, Material>();
    private readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
    private Mesh _quad;

    internal Transform Create(Transform parent, Shape shape, string name)
    {
        Material material = GetMaterial(shape);
        if (material == null) return null;
        if (_quad == null)
        {
            _quad = new Mesh { name = "VfxCardQuad" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            _quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            _quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _quad.RecalculateNormals();
            _quad.RecalculateBounds();
            _owned.Add(_quad);
        }
        var host = new GameObject(name);
        host.transform.SetParent(parent, false);
        host.AddComponent<MeshFilter>().sharedMesh = _quad;
        var renderer = host.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return host.transform;
    }

    internal void SetColor(Shape shape, Color color)
    {
        Material material = GetMaterial(shape);
        if (material != null) material.SetColor("_BaseColor", color);
    }

    private Material GetMaterial(Shape shape)
    {
        if (_materials.TryGetValue(shape, out var cached)) return cached;
        var template = Resources.Load<Material>("Presentation/VfxCardMaterial");
        var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("VFX cards need Resources/Presentation/VfxCardMaterial (transparent URP unlit).");
            _materials.Add(shape, null);
            return null;
        }
        var material = template != null ? new Material(template) : new Material(shader);
        material.name = "Illustrated" + shape;
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 0);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetTexture("_BaseMap", BuildTexture(shape));
        material.SetColor("_BaseColor", Color.white);
        _materials.Add(shape, material);
        _owned.Add(material);
        return material;
    }

    private Texture2D BuildTexture(Shape shape)
    {
        const int width = 128, height = 64;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "HandDrawn" + shape, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        if (shape == Shape.Band) texture.wrapModeU = TextureWrapMode.Repeat;
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float u = (x + 0.5f) / width * 2 - 1;
            float v = (y + 0.5f) / height * 2 - 1;
            float grain = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            grain -= Mathf.Floor(grain);
            float alpha, tone;
            switch (shape)
            {
                case Shape.Band:
                    float stroke = v - 0.16f * Mathf.Sin(u * 5f) - 0.06f * Mathf.Sin(u * 17f);
                    float edge = 1f - u * u - stroke * stroke * 4f;
                    alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge * 3f));
                    // Broad, soft shading instead of a hard highlight/black edge per ribbon.
                    tone = Mathf.Lerp(0.55f, 0.82f, Mathf.Clamp01(0.5f - stroke * 0.7f));
                    tone += 0.035f * Mathf.Sin(u * 8f + stroke * 5f);
                    break;
                case Shape.Funnel:
                    float t = (v + 1f) * 0.5f;
                    float halfWidth = Mathf.Lerp(0.04f, 1f, Mathf.Pow(t, 0.8f));
                    float bend = 0.035f * Mathf.Sin(t * 7f) * t;
                    alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((halfWidth - Mathf.Abs(u - bend)) / 0.08f));
                    alpha *= Mathf.Clamp01(t * 40f) * Mathf.Clamp01((1f - t) * 25f);
                    tone = 0.67f + t * 0.1f + 0.045f * Mathf.Sin(t * 20f + u * 5f);
                    break;
                case Shape.Dust:
                    float radius = Mathf.Sqrt(u * u + v * v);
                    alpha = Mathf.Clamp01((0.9f + 0.07f * Mathf.Sin(Mathf.Atan2(v, u) * 7f) - radius) * 7f);
                    tone = radius < 0.5f ? 0.84f : 0.6f;
                    break;
                case Shape.Streak:
                    alpha = Mathf.Clamp01((1f - Mathf.Abs(u)) * 5f) * Mathf.Clamp01((0.22f - Mathf.Abs(v - 0.12f * u * u)) * 20f);
                    tone = 1f;
                    break;
                default:
                    alpha = Mathf.Abs(u + v * 0.3f) < 0.55f && Mathf.Abs(v) < 0.65f ? 1f : 0f;
                    tone = v > 0 ? 0.9f : 0.48f;
                    break;
            }
            pixels[y * width + x] = new Color(tone, tone, tone, alpha * (0.9f + grain * 0.1f));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        _owned.Add(texture);
        return texture;
    }

    internal static void FaceCamera(Transform card, Camera camera, float roll = 0)
    {
        card.rotation = camera.transform.rotation * Quaternion.Euler(0, 0, roll);
    }

    public void Dispose()
    {
        foreach (var item in _owned)
        {
            if (item == null) continue;
            if (Application.isPlaying) UnityEngine.Object.Destroy(item);
            else UnityEngine.Object.DestroyImmediate(item);
        }
        _owned.Clear();
        _materials.Clear();
    }
}
