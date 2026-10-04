using UnityEngine;

/// <summary>
/// Tornado disaster: wanders on a slow heading (never toward the player — storm-director.md Rule 5) and
/// runs a forming → mature → dissipating lifecycle. Wind, lift and damage come from the per-EF storm scale
/// (<see cref="StormScale"/>, F3) on its <see cref="TornadoData"/>.
/// </summary>
public class TornadoController : DisasterEntity
{
    [Header("Lifecycle")]
    [SerializeField] private float _formSeconds = 3f;
    [SerializeField] private Vector2 _matureSecondsRange = new Vector2(18f, 30f);
    [SerializeField] private float _dissipateSeconds = 4f;

    [Header("Movement")]
    [SerializeField] private float _worldHalfExtent = 90f;

    [Header("Visual")]
    [SerializeField] private float _spinDegreesPerSecond = 240f;

    private TornadoLifecycle _lifecycle;
    private Transform _target;
    private float _age;
    private float _heading;
    private float _noiseSeed;
    private Vector3 _baseScale = Vector3.one;

    private TornadoData TornadoData => _data as TornadoData;

    /// <summary>
    /// Capture harness only (<see cref="StormCaptureHarness"/>): pins the tornado at the start of Mature and
    /// stops it moving, so a 60 s performance capture is never cut short by the lifecycle.
    /// </summary>
    public bool HoldMature { get; set; }

    /// <summary>
    /// Test/harness seam: when ≥ 0, pins the tornado in Forming at this intensity (0–1) and stops it moving
    /// (storm-director.md AC-19). Negative = off.
    /// </summary>
    public float HoldFormingIntensity { get; set; } = -1f;
    public float ConeScale => TornadoData != null ? TornadoData.ConeScale : 1f;
    public string EFRating => TornadoData != null ? TornadoData.EFRating : "EF?";
    /// <summary>0–1 lifecycle strength.</summary>
    public float Intensity => _lifecycle != null ? _lifecycle.GetIntensity(_age) : 1f;
    public TornadoLifecycle.Phase Phase => _lifecycle != null ? _lifecycle.GetPhase(_age) : TornadoLifecycle.Phase.Mature;

    /// <summary>F3 phase: Forming deals no damage or lift; Roping Out scales with intensity.</summary>
    public StormScale.Phase ScalePhase
    {
        get
        {
            switch (Phase)
            {
                case TornadoLifecycle.Phase.Forming: return StormScale.Phase.Forming;
                case TornadoLifecycle.Phase.Mature: return StormScale.Phase.Mature;
                default: return StormScale.Phase.RopingOut;
            }
        }
    }

    /// <summary>F3 damage radius: 0 while Forming, D when Mature, D · I while roping out.</summary>
    public override float DamageRadius =>
        TornadoData != null ? StormScale.DamageRadius(ScalePhase, TornadoData.DamageRadius, Intensity) : 0f;

    /// <summary>Radius of the wind field at current intensity (R · I).</summary>
    public float WindRadius => TornadoData != null ? TornadoData.WindRadius * Intensity : 0f;

    public override float GetLiftFractionAt(Vector3 position, float exposure, float liftCoefficient)
    {
        if (TornadoData == null) return 0f;
        Vector3 d = position - transform.position;
        d.y = 0f;
        return StormScale.Lift(ScalePhase, exposure, ThreatMultiplier, Intensity, d.magnitude, TornadoData.WindRadius,
                               liftCoefficient);
    }

    public override Vector3 GetWindAt(Vector3 position)
    {
        if (TornadoData == null) return Vector3.zero;
        return StormScale.Wind(position - transform.position, TornadoData.PeakWind, TornadoData.WindRadius, Intensity);
    }

    /// <summary>Call right after Instantiate, before the first Update.</summary>
    public void Initialize(TornadoData data, Transform target)
    {
        _data = data;
        _target = target;
    }

    private void Start()
    {
        _baseScale = transform.localScale;
        _lifecycle = new TornadoLifecycle(_formSeconds,
            Random.Range(_matureSecondsRange.x, _matureSecondsRange.y), _dissipateSeconds);
        _noiseSeed = Random.value * 1000f;

        Vector3 toTarget = _target != null ? _target.position - transform.position : -transform.position;
        _heading = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg + Random.Range(-50f, 50f);
        ApplyScale();
    }

    private void Update()
    {
        bool holding = HoldMature || HoldFormingIntensity >= 0f;
        if (HoldFormingIntensity >= 0f) _age = _lifecycle.FormSeconds * Mathf.Min(HoldFormingIntensity, 0.999f);
        else _age = HoldMature ? _lifecycle.FormSeconds : _age + Time.deltaTime;
        if (_lifecycle.GetPhase(_age) == TornadoLifecycle.Phase.Done)
        {
            Destroy(gameObject);
            return;
        }

        if (!holding) Move(Time.deltaTime);
        ApplyScale();
        transform.Rotate(0f, _spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }

    private void Move(float dt)
    {
        float noise = Mathf.PerlinNoise(_noiseSeed, _age * 0.25f) * 2f - 1f;
        float turnRate = TornadoData != null ? TornadoData.TurnRateDeg : 20f;
        _heading += noise * turnRate * dt;

        // No homing (Rule 5): the track never reads the player. Story 005 replaces this wander with the
        // director's deterministic track model.
        Vector3 dir = Quaternion.Euler(0f, _heading, 0f) * Vector3.forward;

        // Steer back inside the world when near the edge.
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x) > _worldHalfExtent || Mathf.Abs(pos.z) > _worldHalfExtent)
        {
            dir = new Vector3(-pos.x, 0f, -pos.z).normalized;
            _heading = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        }

        float speed = MoveSpeed * Mathf.Lerp(0.4f, 1f, Intensity);
        transform.position += dir.normalized * speed * dt;
    }

    private void ApplyScale()
    {
        float s = Mathf.Lerp(0.05f, 1f, Intensity);
        // Funnel grows taller faster than it widens, so a forming tornado reads as a thin rope.
        transform.localScale = new Vector3(_baseScale.x * s * s, _baseScale.y * s, _baseScale.z * s * s);
    }
}
