using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player vehicle (ADR-0005 engine adapter). Each 50 Hz step it sphere-casts the four wheels, samples
/// surfaces and wind, runs the pure <see cref="VehicleModel"/>, and applies its forces to the Rigidbody.
/// Public facade is unchanged from 0.4 (CurrentSpeed, MaxSpeed, CurrentWind, InputEnabled, Data,
/// ApplyKnockback) so RunManager, VehicleHealth, HUD, builders, and the presentation lane keep working.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerVehicle : MonoBehaviour
{
    [SerializeField] private VehicleData _data;
    [Tooltip("Shared tuning (vehicle-feel.md knobs). If empty, GDD defaults are used.")]
    [SerializeField] private VehicleFeelConfig _config;
    [Tooltip("Layers the wheels can stand on. The cast ignores the vehicle's own colliders.")]
    [SerializeField] private LayerMask _groundMask = ~0;
    [Tooltip("Half-extent of the drivable square (soft boundary, vehicle-feel.md E8).")]
    [SerializeField] private float _worldHalfExtent = 95f;
    [SerializeField] private float _boundaryPush = 25f;
    [SerializeField] private float _respawnBelowY = -10f;

    private Rigidbody _rb;
    private VehicleModel _model;
    private VehicleFeelValues _values;
    private WheelLayout _layout;
    private IVehicleInput _input;
    private PlayerInputSource _ownedInput;
    private VehicleHealth _health;
    private readonly WheelContact[] _contacts = new WheelContact[VehicleModel.WheelCount];
    private readonly Vector3[] _anchors = new Vector3[VehicleModel.WheelCount];
    private readonly Vector3[] _smoothedNormals = new Vector3[VehicleModel.WheelCount];
    private readonly VehicleStepOutput _output = new VehicleStepOutput();
    private readonly Dictionary<Collider, SurfaceProperties> _surfaceCache = new Dictionary<Collider, SurfaceProperties>();
    private Vector3 _rawWind;
    private Vector3 _pendingKnockback;
    private float _knockbackTimer;
    private float _stuckTimer;
    private Vector3 _lastGroundedPosition;
    private Quaternion _lastGroundedRotation = Quaternion.identity;

    /// <summary>Signed forward speed in units/sec.</summary>
    public float CurrentSpeed => _model != null ? _model.ForwardSpeed : 0f;
    /// <summary>Top forward speed in units/sec (F13 v_top).</summary>
    public float MaxSpeed => _model != null ? _model.Params.TopSpeed : 0f;
    /// <summary>Tuning data for this vehicle.</summary>
    public VehicleData Data => _data;
    /// <summary>Wind acting on the truck this physics step (exposure-scaled, m/s).</summary>
    public Vector3 CurrentWind => _model != null ? _rawWind * _model.Params.Exposure : Vector3.zero;
    /// <summary>When false, player input is ignored and the truck coasts.</summary>
    public bool InputEnabled { get; set; } = true;
    public VehicleState State => _model != null ? _model.State : VehicleState.Grounded;
    public float SlipAngle => _model != null ? _model.SlipAngleDeg : 0f;
    public int GroundedWheels => _model != null ? _model.GroundedWheels : 0;
    /// <summary>The underlying model (read-only use: tests, presentation).</summary>
    public VehicleModel Model => _model;

    /// <summary>Replaces the input source (tests, autopilots). Null restores player input.</summary>
    public IVehicleInput InputSource
    {
        set => _input = value ?? (IVehicleInput)EnsureOwnedInput();
    }

    /// <summary>Dependency injection for tests and runtime-spawned vehicles. Call before the first FixedUpdate.</summary>
    public void Initialize(VehicleData data, VehicleFeelConfig config, IVehicleInput input = null)
    {
        _data = data;
        _config = config;
        Build();
        if (input != null) _input = input;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _health = GetComponent<VehicleHealth>();
        Time.maximumDeltaTime = 0.1f; // ADR-0005: ≤ 5 catch-up steps
        Build();
    }

    private void Build()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (_data == null) _data = ScriptableObject.CreateInstance<VehicleData>();
        _values = _config != null ? _config.Values : VehicleFeelValues.Defaults;
        _layout = _data.Layout;
        ArchetypeParams p = ArchetypeParams.Derive(_data.Stars, _values);
        _model = new VehicleModel(p, _values, _layout);
        if (_input == null) _input = EnsureOwnedInput();

        _anchors[0] = new Vector3(-_layout.HalfTrack, _layout.AnchorY, _layout.FrontAxleZ);
        _anchors[1] = new Vector3(_layout.HalfTrack, _layout.AnchorY, _layout.FrontAxleZ);
        _anchors[2] = new Vector3(-_layout.HalfTrack, _layout.AnchorY, _layout.RearAxleZ);
        _anchors[3] = new Vector3(_layout.HalfTrack, _layout.AnchorY, _layout.RearAxleZ);
        for (int i = 0; i < _smoothedNormals.Length; i++) _smoothedNormals[i] = Vector3.up;

        ConfigureBody(p);
        _lastGroundedPosition = _rb.position;
        _lastGroundedRotation = _rb.rotation;
    }

    private PlayerInputSource EnsureOwnedInput()
    {
        if (_ownedInput == null) _ownedInput = new PlayerInputSource();
        return _ownedInput;
    }

    private void ConfigureBody(ArchetypeParams p)
    {
        _rb.mass = p.Mass;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0.05f;
        _rb.constraints = RigidbodyConstraints.None;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // E6
        _rb.sleepThreshold = 0f;
        _rb.maxAngularVelocity = _values.MaxAngularSpeed;
        _rb.maxLinearVelocity = 80f;
        _rb.automaticCenterOfMass = false;
        _rb.centerOfMass = _layout.CenterOfMass;
        _rb.automaticInertiaTensor = false;
        Vector3 s = _layout.BodySize;
        _rb.inertiaTensor = p.Mass / 12f * new Vector3(s.y * s.y + s.z * s.z, s.x * s.x + s.z * s.z, s.x * s.x + s.y * s.y);
        _rb.inertiaTensorRotation = Quaternion.identity;

        // The body collider rides above the suspension instead of resting on the ground (0.4's 1 m cube did).
        if (TryGetComponent(out BoxCollider box))
        {
            box.size = _layout.BodySize;
            box.center = _layout.BodyCenter;
        }
    }

    private void OnDestroy() => _ownedInput?.Dispose();

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        QueryWheels();
        _rawWind = DisasterEntity.TotalWindAt(_rb.position);

        var input = new VehicleStepInput
        {
            Dt = dt,
            Rotation = _rb.rotation,
            Velocity = _rb.linearVelocity,
            AngularVelocity = _rb.angularVelocity,
            CenterOfMassWorld = _rb.worldCenterOfMass,
            Wind = _rawWind,
            Damage = _health != null ? _health.Stage : DamageStage.Healthy,
            Input = InputEnabled ? _input.Read() : default,
            GripScale = _knockbackTimer > 0f ? _values.KnockbackGripScale : 1f,
        };
        input.LiftFraction = DisasterEntity.MaxLiftFractionAt(_rb.position, _model.Params.Exposure,
                                                              _values.LiftCoefficient, out input.LiftEFStrength);
        _model.Step(input, _contacts, _output);
        Apply();
        ApplyKnockbackStep(dt);
        HandleStuck(input.Input, dt);
        HandleBoundary();
    }

    // Funnel knockback is spread over a short window with reduced tire grip, so the truck is shoved and
    // slides instead of tripping over its own tires (playtest 2026-10-01: flips were jarring).
    private void ApplyKnockbackStep(float dt)
    {
        if (_knockbackTimer <= 0f) return;
        float spread = Mathf.Max(dt, _values.KnockbackSpreadSeconds);
        _rb.AddForce(_pendingKnockback * (dt / spread), ForceMode.VelocityChange);
        _knockbackTimer -= dt;
    }

    // High-centered or wedged with no wheels down while the player is trying to drive → hop free.
    private void HandleStuck(VehicleInputFrame inp, float dt)
    {
        bool trying = inp.Throttle > 0.5f || inp.Brake > 0.5f;
        bool stuck = trying && _model.GroundedWheels < 2 && _rb.linearVelocity.magnitude < 0.5f
                     && _model.State != VehicleState.Upended;
        _stuckTimer = stuck ? _stuckTimer + dt : 0f;
        if (_stuckTimer < _values.StuckSeconds) return;
        _stuckTimer = 0f;
        AutoRight();
    }

    private void QueryWheels()
    {
        Vector3 down = -transform.up;
        float maxDistance = _values.RestLength + 0.5f;
        for (int i = 0; i < VehicleModel.WheelCount; i++)
        {
            Vector3 origin = transform.TransformPoint(_anchors[i]);
            ref WheelContact c = ref _contacts[i];
            bool hit = Physics.SphereCast(origin, _layout.CastRadius, down, out RaycastHit h, maxDistance,
                                          _groundMask, QueryTriggerInteraction.Ignore);
            if (hit && h.rigidbody == _rb) hit = false; // never stand on ourselves

            if (!hit)
            {
                c.Grounded = false;
                c.GroundDistance = maxDistance;
                continue;
            }

            if (h.distance <= 1e-4f)
            {
                // Cast started overlapping (tile seams, debris): fully compressed, keep last normal (ADR-0005).
                c.GroundDistance = _values.RestLength - _values.Travel;
                c.Point = origin + down * _layout.CastRadius;
            }
            else
            {
                c.GroundDistance = h.distance + _layout.CastRadius;
                c.Point = h.point;
                // Triangle normals jump at MeshCollider edges: smooth and rate-limit per wheel.
                _smoothedNormals[i] = Vector3.RotateTowards(_smoothedNormals[i], h.normal, 0.35f, 0f);
            }
            c.Normal = _smoothedNormals[i];
            c.Grounded = true;
            c.PointVelocity = _rb.GetPointVelocity(c.Point);
            SurfaceProperties surface = SurfaceOf(h.collider);
            c.SurfaceGrip = surface.Grip;
            c.SurfaceDrag = surface.Drag;
        }
    }

    private SurfaceProperties SurfaceOf(Collider col)
    {
        if (col == null) return SurfaceTable.Get(SurfaceTable.Default);
        if (_surfaceCache.TryGetValue(col, out SurfaceProperties s)) return s;
        s = SurfaceTable.Get(col.TryGetComponent(out SurfaceTag tag) ? tag.Type : SurfaceTable.Default);
        _surfaceCache[col] = s;
        return s;
    }

    /// <summary>Drops cached surface lookups (call when pooled world colliders are reassigned).</summary>
    public void ClearSurfaceCache() => _surfaceCache.Clear();

    private void Apply()
    {
        for (int i = 0; i < VehicleModel.WheelCount; i++)
            if (_output.WheelForce[i] != Vector3.zero)
                _rb.AddForceAtPosition(_output.WheelForce[i], _output.WheelPoint[i], ForceMode.Force);

        if (_output.CenterAcceleration != Vector3.zero) _rb.AddForce(_output.CenterAcceleration, ForceMode.Acceleration);
        if (_output.WindAcceleration != Vector3.zero)
            _rb.AddForceAtPosition(_output.WindAcceleration * _rb.mass, _output.WindPoint, ForceMode.Force);
        if (_output.AngularAcceleration != Vector3.zero) _rb.AddTorque(_output.AngularAcceleration, ForceMode.Acceleration);

        if (_output.Toss)
        {
            _rb.AddForce(_output.TossVelocity, ForceMode.VelocityChange);
            GameEvents.RaiseTossed();
        }
        if (_output.Landed) GameEvents.RaiseLanded(_output.LandedSpeed);
        if (_output.AutoRight) AutoRight();

        if (_model.GroundedWheels >= 2 && _model.State != VehicleState.Upended)
        {
            _lastGroundedPosition = _rb.position;
            _lastGroundedRotation = Quaternion.Euler(0f, _rb.rotation.eulerAngles.y, 0f);
        }
    }

    /// <summary>E5: flip back onto the wheels, keeping heading. A documented teleport path (ADR-0005).</summary>
    private void AutoRight()
    {
        float yaw = _rb.rotation.eulerAngles.y;
        Teleport(_rb.position + Vector3.up * 1.2f, Quaternion.Euler(0f, yaw, 0f));
    }

    private void HandleBoundary()
    {
        Vector3 p = _rb.position;
        if (p.y < _respawnBelowY)
        {
            Teleport(_lastGroundedPosition + Vector3.up * 1f, _lastGroundedRotation); // E8 respawn, no HP cost
            return;
        }
        Vector3 push = Vector3.zero;
        if (p.x > _worldHalfExtent) push.x = -(p.x - _worldHalfExtent);
        else if (p.x < -_worldHalfExtent) push.x = -_worldHalfExtent - p.x;
        if (p.z > _worldHalfExtent) push.z = -(p.z - _worldHalfExtent);
        else if (p.z < -_worldHalfExtent) push.z = -_worldHalfExtent - p.z;
        if (push != Vector3.zero) _rb.AddForce(push * _boundaryPush, ForceMode.Acceleration);
    }

    /// <summary>Moves the vehicle and zeroes its motion (respawn, auto-right, tests). Resyncs interpolation.</summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = position;
        _rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// Shoves the truck along <paramref name="direction"/> (flattened), spread over KnockbackSpreadSeconds
    /// with reduced grip. Kept from 0.4 for funnel contact.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float speed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 1e-4f) return;
        _pendingKnockback = direction.normalized * speed * 0.7f + Vector3.up * 1.5f;
        _knockbackTimer = Mathf.Max(Time.fixedDeltaTime, _values.KnockbackSpreadSeconds);
    }
}
