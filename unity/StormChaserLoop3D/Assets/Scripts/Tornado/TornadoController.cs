using UnityEngine;

/// <summary>
/// Tornado disaster: wanders on a slow Perlin-driven heading, leans toward the player so encounters
/// happen, and runs a forming → mature → dissipating lifecycle that scales size, speed, and danger.
/// </summary>
public class TornadoController : DisasterEntity
{
    [Header("Lifecycle")]
    [SerializeField] private float _formSeconds = 3f;
    [SerializeField] private Vector2 _matureSecondsRange = new Vector2(18f, 30f);
    [SerializeField] private float _dissipateSeconds = 4f;

    [Header("Movement")]
    [Tooltip("0 = pure wander, 1 = beeline at the player.")]
    [Range(0f, 1f)] [SerializeField] private float _playerPull = 0.3f;
    [Tooltip("Max heading drift in degrees/sec.")]
    [SerializeField] private float _wanderTurnRate = 35f;
    [SerializeField] private float _worldHalfExtent = 90f;

    [Header("Danger")]
    [Tooltip("Intensity below which the funnel cannot hurt the player.")]
    [SerializeField] private float _harmlessBelowIntensity = 0.4f;
    [SerializeField] private float _baseDamageRadius = 1.2f;
    [SerializeField] private float _damageRadiusPerConeScale = 1.3f;

    [Header("Wind (see WindField.Vortex)")]
    [SerializeField] private float _windRadiusBase = 14f;
    [SerializeField] private float _windRadiusPerConeScale = 10f;
    [Tooltip("Peak inward pull at the funnel, units/sec, before EF scaling.")]
    [SerializeField] private float _windInflow = 7f;
    [Tooltip("Peak sideways swirl at the funnel, units/sec, before EF scaling.")]
    [SerializeField] private float _windSwirl = 11f;
    [Tooltip("EF wind scale = Base + PerSqrtEF × sqrt(EF strength). Defaults: EF0 1.25×, EF3 1.69×, EF5 2.0×.")]
    [SerializeField] private float _windScaleBase = 0.5f;
    [SerializeField] private float _windScalePerSqrtEF = 0.75f;

    [Header("Visual")]
    [SerializeField] private float _spinDegreesPerSecond = 240f;

    private TornadoLifecycle _lifecycle;
    private Transform _target;
    private float _age;
    private float _heading;
    private float _noiseSeed;
    private Vector3 _baseScale = Vector3.one;

    private TornadoData TornadoData => _data as TornadoData;
    public float ConeScale => TornadoData != null ? TornadoData.ConeScale : 1f;
    public string EFRating => TornadoData != null ? TornadoData.EFRating : "EF?";
    /// <summary>0–1 lifecycle strength.</summary>
    public float Intensity => _lifecycle != null ? _lifecycle.GetIntensity(_age) : 1f;
    public TornadoLifecycle.Phase Phase => _lifecycle != null ? _lifecycle.GetPhase(_age) : TornadoLifecycle.Phase.Mature;

    public override float DamageRadius =>
        Intensity < _harmlessBelowIntensity ? 0f : (_baseDamageRadius + _damageRadiusPerConeScale * ConeScale) * Intensity;

    /// <summary>Radius of the wind field at current intensity.</summary>
    public float WindRadius => (_windRadiusBase + _windRadiusPerConeScale * ConeScale) * Intensity;

    public override float GetLiftFractionAt(Vector3 position, float exposure, float liftCoefficient)
    {
        Vector3 d = position - transform.position;
        d.y = 0f;
        return VehicleModel.LiftFraction(exposure, ThreatMultiplier, Intensity, d.magnitude, WindRadius, liftCoefficient);
    }

    public override Vector3 GetWindAt(Vector3 position)
    {
        float strength = (_windScaleBase + _windScalePerSqrtEF * Mathf.Sqrt(ThreatMultiplier)) * Intensity;
        return WindField.Vortex(position - transform.position, WindRadius,
            _windInflow * strength, _windSwirl * strength);
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
        _age += Time.deltaTime;
        if (_lifecycle.GetPhase(_age) == TornadoLifecycle.Phase.Done)
        {
            Destroy(gameObject);
            return;
        }

        Move(Time.deltaTime);
        ApplyScale();
        transform.Rotate(0f, _spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }

    private void Move(float dt)
    {
        float noise = Mathf.PerlinNoise(_noiseSeed, _age * 0.25f) * 2f - 1f;
        _heading += noise * _wanderTurnRate * dt;

        Vector3 wander = Quaternion.Euler(0f, _heading, 0f) * Vector3.forward;
        Vector3 dir = wander;

        if (_target != null)
        {
            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 1f)
                dir = Vector3.Slerp(wander, toTarget.normalized, _playerPull);
        }

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
