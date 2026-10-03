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
    [SerializeField, Range(0, 40)] private int _loosePropCount = 32;
    [SerializeField, Range(100f, 250f)] private float _baleMass = 250f;
    [SerializeField, Range(10f, 30f)] private float _mailboxMass = 20f;
    [SerializeField, Range(10f, 25f)] private float _signMass = 15f;
    [SerializeField, Range(15f, 50f)] private float _crateMass = 30f;
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
        var barns = new List<Vector3>();
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
                    barns.Add(p);
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
                    MakeLoose(group, Mathf.Clamp(_fenceMass, 30f, 60f));
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
        ScatterLooseProps(points, barns, spawn);
    }
    private void ScatterLooseProps(List<Vector3> solidPoints, List<Vector3> barns, Vector3 spawn)
    {
        // Separate stream preserves the already-shipped solid scenery for the same seed.
        var random = new System.Random(_seed ^ 707);
        var placed = new List<Vector3>();
        var clusters = new List<Vector3>();
        float extent = Mathf.Max(0f, _halfExtent);
        for (int i = 0; i < 4; i++)
            clusters.Add(new Vector3((float)(random.NextDouble() * 2 - 1) * extent * 0.8f, 0,
                (i % 2 == 0 ? -1f : 1f) * Mathf.Lerp(_roadClearance + 12f, extent * 0.8f, (float)random.NextDouble())));
        for (int index = 0; index < Mathf.Clamp(_loosePropCount, 0, 40); index++)
        {
            int kind = index % 4;
            for (int attempt = 0; attempt < 160; attempt++)
            {
                Vector3 position;
                if (kind == 1 || kind == 2)
                    position = new Vector3((float)(random.NextDouble() * 2 - 1) * extent, 0,
                        (index % 8 < 4 ? -1f : 1f) * (_roadClearance + 0.9f + (float)random.NextDouble() * 1.5f));
                else
                {
                    if (kind == 3 && barns.Count == 0) break;
                    Vector3 anchor = kind == 0 ? clusters[(index / 4) % clusters.Count] : barns[(index / 4) % barns.Count];
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float distance = kind == 0 ? 2f + (float)random.NextDouble() * 5f : 5.5f + (float)random.NextDouble() * 2f;
                    position = anchor + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                }
                // Entire footprint stays off the road and at least 12 m from the spawn.
                if (Mathf.Abs(position.x) + 1f > extent || Mathf.Abs(position.z) + 1f > extent ||
                    Mathf.Abs(position.z) < _roadClearance + 0.8f || (position - spawn).sqrMagnitude < 169f) continue;
                bool occupied = false;
                foreach (var point in solidPoints)
                    if ((position - point).sqrMagnitude < 25f) { occupied = true; break; }
                foreach (var point in placed)
                    if ((position - point).sqrMagnitude < 6.25f) { occupied = true; break; }
                if (occupied) continue;
                var group = new GameObject(kind == 0 ? "RuralHayBale" : kind == 1 ? "RuralMailbox" : kind == 2 ? "RuralRoadSign" : "RuralCrates");
                group.transform.SetParent(transform, false);
                group.transform.position = position;
                group.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
                float mass;
                switch (kind)
                {
                    case 0:
                        var bale = Part(group.transform, PrimitiveType.Cylinder, new Vector3(0, 0.8f, 0), new Vector3(1.6f, 0.9f, 1.6f), _wall);
                        bale.localRotation = Quaternion.Euler(0, 0, 90);
                        mass = Mathf.Clamp(_baleMass, 100f, 250f);
                        break;
                    case 1:
                        Part(group.transform, PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(0.12f, 1.2f, 0.12f), _wood);
                        Part(group.transform, PrimitiveType.Cube, new Vector3(0, 1.3f, 0), new Vector3(0.5f, 0.35f, 0.65f), _metal);
                        mass = Mathf.Clamp(_mailboxMass, 10f, 30f);
                        break;
                    case 2:
                        Part(group.transform, PrimitiveType.Cube, new Vector3(0, 0.8f, 0), new Vector3(0.1f, 1.6f, 0.1f), _metal);
                        Part(group.transform, PrimitiveType.Cube, new Vector3(0, 1.65f, 0), new Vector3(0.8f, 0.65f, 0.08f), _wall);
                        mass = Mathf.Clamp(_signMass, 10f, 25f);
                        break;
                    default:
                        Part(group.transform, PrimitiveType.Cube, new Vector3(0, 0.12f, 0), new Vector3(1.2f, 0.24f, 1f), _wood);
                        Part(group.transform, PrimitiveType.Cube, new Vector3(0, 0.65f, 0), new Vector3(0.85f, 0.8f, 0.85f), _wall);
                        mass = Mathf.Clamp(_crateMass, 15f, 50f);
                        break;
                }
                MakeLoose(group, mass);
                placed.Add(position);
                break;
            }
        }
    }
    private static void MakeLoose(GameObject group, float mass)
    {
        var body = group.AddComponent<Rigidbody>();
        body.mass = mass;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        group.AddComponent<ImpactSurface>().Kind = ImpactKind.Destructible;
        body.Sleep();
    }
    private Material Material(Shader shader, Color color)
    {
        var material = new Material(shader) { enableInstancing = true };
        material.SetColor("_BaseColor", color);
        _materials.Add(material);
        return material;
    }
    private static Transform Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid = true)
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
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part.transform;
    }
    private void OnDestroy()
    {
        foreach (var material in _materials)
        {
            if (material == null) continue;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
