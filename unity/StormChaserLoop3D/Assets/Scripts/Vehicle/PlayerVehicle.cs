using UnityEngine;

/// <summary>
/// Arcade truck. Throttle sets a target speed along the heading; actual planar velocity converges on
/// heading × speed + wind at the Grip rate, so hard turns and strong wind produce slide.
/// Still a placeholder for the full Vehicle Feel pass (vision-1.0, Kinetic Chaos).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerVehicle : MonoBehaviour
{
    [SerializeField] private VehicleData _data;
    [Tooltip("Half-extent of the drivable square, centered on world origin.")]
    [SerializeField] private float _worldHalfExtent = 95f;
    [Tooltip("How fast knockback velocity decays (units/sec²).")]
    [SerializeField] private float _knockbackDecay = 30f;

    private Rigidbody _rb;
    private StormChaserControls _controls;
    private UnityEngine.InputSystem.InputAction _moveAction;
    private float _currentSpeed;
    private Vector3 _velocity;
    private Vector3 _knockback;
    private float _wobbleSeed;

    /// <summary>Signed forward speed in units/sec.</summary>
    public float CurrentSpeed => _currentSpeed;
    /// <summary>Top forward speed in units/sec.</summary>
    public float MaxSpeed => _data.MoveSpeed;
    /// <summary>Tuning data for this vehicle.</summary>
    public VehicleData Data => _data;
    /// <summary>Wind velocity acting on the truck this physics step (already scaled by exposure).</summary>
    public Vector3 CurrentWind { get; private set; }
    /// <summary>When false, throttle/steer input is ignored and the truck coasts to a stop.</summary>
    public bool InputEnabled { get; set; } = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ
                        | RigidbodyConstraints.FreezePositionY;

        _controls = new StormChaserControls();
        _moveAction = _controls.Driving.Move;
        _wobbleSeed = Random.value * 100f;
    }

    private void OnEnable() => _controls.Driving.Enable();
    private void OnDisable() => _controls.Driving.Disable();
    private void OnDestroy() => _controls.Dispose();

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 input = InputEnabled ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        CurrentWind = DisasterEntity.TotalWindAt(_rb.position) * _data.WindExposure;

        UpdateSpeed(input.y, dt);
        UpdateSteering(input.x, dt);
        UpdateVelocity(dt);
        ClampToWorld();
    }

    /// <summary>Flings the truck along <paramref name="direction"/> (flattened) and kills forward speed.</summary>
    public void ApplyKnockback(Vector3 direction, float speed)
    {
        direction.y = 0f;
        _knockback = direction.normalized * speed;
        _currentSpeed *= 0.25f;
    }

    private void UpdateSpeed(float throttle, float dt)
    {
        float targetSpeed = throttle > 0f ? throttle * _data.MoveSpeed : throttle * _data.ReverseSpeed;

        float rate;
        bool braking = Mathf.Abs(throttle) > 0.01f && Mathf.Abs(_currentSpeed) > 0.5f
                       && Mathf.Sign(throttle) != Mathf.Sign(_currentSpeed);
        if (braking) rate = _data.BrakeDeceleration;
        else if (Mathf.Abs(throttle) > 0.01f) rate = _data.Acceleration;
        else rate = _data.Deceleration;

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * dt);
    }

    private void UpdateSteering(float steer, float dt)
    {
        float speedAbs = Mathf.Abs(_currentSpeed);
        float lowSpeedRamp = Mathf.Clamp01(speedAbs / Mathf.Max(0.01f, _data.FullTurnSpeed));
        float highSpeedDamp = Mathf.Lerp(1f, _data.HighSpeedTurnFactor, Mathf.Clamp01(speedAbs / _data.MoveSpeed));
        float turnRate = _data.TurnSpeed * lowSpeedRamp * highSpeedDamp;

        float turn = steer * turnRate * Mathf.Sign(_currentSpeed == 0f ? 1f : _currentSpeed);

        // Strong wind jerks the wheel around.
        float windSpeed = CurrentWind.magnitude;
        if (windSpeed > 0.1f)
        {
            float gust = Mathf.PerlinNoise(_wobbleSeed, Time.time * 2.5f) * 2f - 1f;
            turn += gust * _data.WindWobble * Mathf.Clamp01(windSpeed / _data.WindGripLossAt);
        }

        if (Mathf.Abs(turn) > 0.001f)
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turn * dt, 0f));
    }

    private void UpdateVelocity(float dt)
    {
        // Grip halves at WindGripLossAt, so the truck floats and slides in a tornado's wind.
        float windSpeed = CurrentWind.magnitude;
        float grip = _data.Grip / (1f + windSpeed / Mathf.Max(0.01f, _data.WindGripLossAt));

        Vector3 target = transform.forward * _currentSpeed + CurrentWind;
        _velocity = Vector3.MoveTowards(_velocity, target, grip * dt);
        _velocity.y = 0f;

        _knockback = Vector3.MoveTowards(_knockback, Vector3.zero, _knockbackDecay * dt);
        _rb.linearVelocity = _velocity + _knockback;
    }

    private void ClampToWorld()
    {
        Vector3 p = _rb.position;
        float x = Mathf.Clamp(p.x, -_worldHalfExtent, _worldHalfExtent);
        float z = Mathf.Clamp(p.z, -_worldHalfExtent, _worldHalfExtent);
        if (x == p.x && z == p.z) return;

        _rb.position = new Vector3(x, p.y, z);
        _currentSpeed *= 0.5f;
        _velocity *= 0.5f;
        _knockback = Vector3.zero;
    }
}
