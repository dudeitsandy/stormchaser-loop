using System.Collections.Generic;
using UnityEngine;

/// <summary>Deterministic rural scenery that leaves the road and starting truck clear.</summary>
public sealed class EnvironmentScatter : MonoBehaviour
{
    [SerializeField] private int _seed = 504;
    [SerializeField] private int _propCount = 65;
    [SerializeField] private float _halfExtent = 85f;
    [SerializeField] private float _roadClearance = 10f;
    [SerializeField] private float _minimumSpacing = 9f;
    [SerializeField, Range(30f, 60f)] private float _fenceMass = 45f;
    private readonly List<Material> _materials = new List<Material>();
    private Material _wood, _leaves, _wall, _roof, _metal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install); // AfterSceneLoad alone skips reloads

    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() != null && Object.FindAnyObjectByType<EnvironmentScatter>() == null)
            new GameObject(nameof(EnvironmentScatter)).AddComponent<EnvironmentScatter>();
    }
    private void Start()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogWarning("EnvironmentScatter: URP Lit shader unavailable."); return; }
        _wood = Material(shader, new Color(0.28f, 0.18f, 0.1f));
        _leaves = Material(shader, new Color(0.14f, 0.29f, 0.13f));
        _wall = Material(shader, new Color(0.65f, 0.56f, 0.4f));
        _roof = Material(shader, new Color(0.32f, 0.13f, 0.09f));
        _metal = Material(shader, new Color(0.48f, 0.51f, 0.53f));
        var random = new System.Random(_seed);
        var points = new List<Vector3>();
        var vehicle = FindAnyObjectByType<PlayerVehicle>();
        Vector3 spawn = vehicle != null ? vehicle.transform.position : Vector3.zero;
        for (int attempt = 0; points.Count < Mathf.Clamp(_propCount, 0, 200) && attempt < 2000; attempt++)
        {
            Vector3 p = new Vector3(((float)random.NextDouble() * 2 - 1) * _halfExtent, 0,
                ((float)random.NextDouble() * 2 - 1) * _halfExtent);
            if (Mathf.Abs(p.z) < _roadClearance || (p - spawn).sqrMagnitude < 144) continue;
            bool occupied = false;
            foreach (var point in points)
                if ((point - p).sqrMagnitude < _minimumSpacing * _minimumSpacing) { occupied = true; break; }
            if (occupied) continue;
            points.Add(p);
            var group = new GameObject("RuralProp");
            group.transform.SetParent(transform, false);
            group.transform.position = p;
            group.transform.rotation = Quaternion.Euler(0, random.Next(4) * 90, 0);
            switch (random.Next(5))
            {
                case 0:
                    group.name = "RuralBarn";
                    Part(group.transform, PrimitiveType.Cube, new Vector3(0, 1.5f, 0), new Vector3(5, 3, 4), _wall);
                    Part(group.transform, PrimitiveType.Cube, new Vector3(0, 3.1f, 0), new Vector3(5.5f, 0.5f, 4.5f), _roof);
                    Part(group.transform, PrimitiveType.Cube, new Vector3(0, 1, -2.05f), new Vector3(0.9f, 2, 0.1f), _wood);
                    break;
                case 1:
                    group.name = "RuralSilo";
                    Part(group.transform, PrimitiveType.Cylinder, new Vector3(0, 3, 0), new Vector3(3, 3, 3), _metal);
                    Part(group.transform, PrimitiveType.Sphere, new Vector3(0, 6, 0), new Vector3(3, 1.2f, 3), _metal);
                    break;
                case 2:
                    group.name = "RuralFence";
                    for (int j = -1; j <= 1; j++) Part(group.transform, PrimitiveType.Cube, new Vector3(j * 2, 0.8f, 0), new Vector3(0.2f, 1.6f, 0.2f), _wood);
                    for (int j = 0; j < 2; j++) Part(group.transform, PrimitiveType.Cube, new Vector3(0, 0.5f + j * 0.6f, 0), new Vector3(4.5f, 0.15f, 0.15f), _wood);
                    var body = group.AddComponent<Rigidbody>();
                    body.mass = Mathf.Clamp(_fenceMass, 30f, 60f);
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                    group.AddComponent<ImpactSurface>().Kind = ImpactKind.Destructible;
                    body.Sleep();
                    break;
                case 3:
                    group.name = "RuralPole";
                    Part(group.transform, PrimitiveType.Cylinder, new Vector3(0, 3.5f, 0), new Vector3(0.25f, 3.5f, 0.25f), _wood);
                    Part(group.transform, PrimitiveType.Cube, new Vector3(0, 6.5f, 0), new Vector3(2, 0.18f, 0.18f), _wood);
                    break;
                default:
                    group.name = "RuralTree";
                    Part(group.transform, PrimitiveType.Cylinder, new Vector3(0, 1.5f, 0), new Vector3(0.5f, 1.5f, 0.5f), _wood);
                    Part(group.transform, PrimitiveType.Sphere, new Vector3(0, 3.5f, 0), new Vector3(3.2f, 4, 3.2f), _leaves, false);
                    break;
            }
        }
    }
    private Material Material(Shader shader, Color color)
    {
        var material = new Material(shader) { enableInstancing = true };
        material.SetColor("_BaseColor", color);
        _materials.Add(material);
        return material;
    }
    private static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid = true)
    {
        var part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        // Retain primitive colliders for solid parts. Canopies remain visual only.
        if (!solid)
        {
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
        }
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
    private void OnDestroy()
    {
        foreach (var material in _materials) if (material != null) Destroy(material);
    }
}
