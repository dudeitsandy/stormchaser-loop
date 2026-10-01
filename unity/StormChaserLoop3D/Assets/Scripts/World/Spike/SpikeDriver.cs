using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S7-01 spike autopilot: flies the streaming target across the 2 km map at boost speed, crossing tile
/// boundaries constantly, with a chase camera. Periodically drops a burst of dynamic debris (the
/// ADR-0004 WebGL cap) near the target so the physics step is measured under realistic load.
/// </summary>
public class SpikeDriver : MonoBehaviour
{
    [SerializeField] private int _seed = 1996;
    [Tooltip("m/s — Pickup BoostMaxSpeed per vehicle-feel.md F9.")]
    [SerializeField] private float _speed = 29f;
    [SerializeField] private float _turnRateDeg = 25f;
    [SerializeField] private float _edgeMargin = 850f;
    [SerializeField] private float _rideHeight = 1f;
    [SerializeField] private Transform _camera;
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 4f, -9f);

    [Header("Debris load")]
    [SerializeField] private int _debrisCount = 60;
    [SerializeField] private float _debrisInterval = 20f;
    [SerializeField] private Material _debrisMaterial;

    private float _heading = 35f;
    private float _targetHeading = 35f;
    private float _nextTurnTime;
    private float _nextDebrisTime = 5f;
    private readonly List<Rigidbody> _debris = new List<Rigidbody>();
    private System.Random _rng;

    private void Awake()
    {
        _rng = new System.Random(_seed);
        for (int i = 0; i < _debrisCount; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SpikeDebris";
            if (_debrisMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = _debrisMaterial;
            go.transform.localScale = Vector3.one * (0.4f + (float)_rng.NextDouble() * 0.8f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 40f + (float)_rng.NextDouble() * 400f;
            go.SetActive(false);
            _debris.Add(rb);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 p = transform.position;

        // Wander, but turn back toward the center near the map edge.
        if (Mathf.Abs(p.x) > _edgeMargin || Mathf.Abs(p.z) > _edgeMargin)
            _targetHeading = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg + (float)(_rng.NextDouble() * 60 - 30);
        else if (Time.time > _nextTurnTime)
        {
            _targetHeading = _heading + (float)(_rng.NextDouble() * 140 - 70);
            _nextTurnTime = Time.time + 4f + (float)_rng.NextDouble() * 6f;
        }
        _heading = Mathf.MoveTowardsAngle(_heading, _targetHeading, _turnRateDeg * dt);

        Vector3 fwd = Quaternion.Euler(0f, _heading, 0f) * Vector3.forward;
        p += fwd * _speed * dt;
        p.y = HeightField.Height(_seed, p.x, p.z) + _rideHeight;
        transform.SetPositionAndRotation(p, Quaternion.LookRotation(fwd));

        if (_camera != null)
        {
            Vector3 camPos = transform.TransformPoint(_cameraOffset);
            camPos.y = Mathf.Max(camPos.y, HeightField.Height(_seed, camPos.x, camPos.z) + 1.5f);
            _camera.SetPositionAndRotation(camPos, Quaternion.LookRotation(transform.position + fwd * 12f - camPos));
        }

        if (Time.time >= _nextDebrisTime)
        {
            _nextDebrisTime = Time.time + _debrisInterval;
            DropDebris(p, fwd);
        }
    }

    private void DropDebris(Vector3 p, Vector3 fwd)
    {
        // Ahead of the target, inside the collider ring, so pieces land on cooked terrain.
        Vector3 center = p + fwd * 40f;
        foreach (Rigidbody rb in _debris)
        {
            Vector3 offset = new Vector3((float)_rng.NextDouble() * 30f - 15f, 8f + (float)_rng.NextDouble() * 10f,
                (float)_rng.NextDouble() * 30f - 15f);
            Vector3 pos = center + offset;
            pos.y = HeightField.Height(_seed, pos.x, pos.z) + offset.y;
            rb.gameObject.SetActive(true);
            rb.position = pos;
            rb.rotation = Random.rotation;
            rb.linearVelocity = new Vector3((float)_rng.NextDouble() * 8f - 4f, 0f, (float)_rng.NextDouble() * 8f - 4f);
            rb.angularVelocity = Random.insideUnitSphere * 4f;
        }
    }
}
