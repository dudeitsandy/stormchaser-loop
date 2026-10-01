using UnityEngine;

/// <summary>Body state handed to the model each physics step (world space).</summary>
public struct VehicleStepInput
{
    public float Dt;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    public Vector3 CenterOfMassWorld;
    /// <summary>Raw disaster wind velocity at the body (m/s), before exposure.</summary>
    public Vector3 Wind;
    public DamageStage Damage;
    public VehicleInputFrame Input;
    /// <summary>Tire grip multiplier for this step (knockback window lowers it so the truck slides, not trips). 0 = default 1.</summary>
    public float GripScale;
    /// <summary>Strongest disaster lift at the body as a fraction of weight (F12).</summary>
    public float LiftFraction;
    /// <summary>EF strength of the disaster producing LiftFraction (scales the toss).</summary>
    public float LiftEFStrength;
}

/// <summary>Forces the adapter applies after a step. Preallocated and reused (no per-step allocation).</summary>
public sealed class VehicleStepOutput
{
    /// <summary>Per-wheel world force (N) applied at WheelPoint with ForceMode.Force.</summary>
    public readonly Vector3[] WheelForce = new Vector3[VehicleModel.WheelCount];
    public readonly Vector3[] WheelPoint = new Vector3[VehicleModel.WheelCount];
    /// <summary>Acceleration (m/s²) at the center of mass: downforce.</summary>
    public Vector3 CenterAcceleration;
    /// <summary>Wind acceleration (m/s²) applied at WindPoint (lever above CoM → lean).</summary>
    public Vector3 WindAcceleration;
    public Vector3 WindPoint;
    /// <summary>Angular acceleration (rad/s²): yaw stability.</summary>
    public Vector3 AngularAcceleration;
    public bool AutoRight;
    /// <summary>F12b: apply TossVelocity once as a velocity change.</summary>
    public bool Toss;
    public Vector3 TossVelocity;
    public bool Landed;
    /// <summary>Vertical touchdown speed when Landed (m/s, positive).</summary>
    public float LandedSpeed;

    public void Clear()
    {
        for (int i = 0; i < VehicleModel.WheelCount; i++)
        {
            WheelForce[i] = Vector3.zero;
            WheelPoint[i] = Vector3.zero;
        }
        CenterAcceleration = WindAcceleration = WindPoint = AngularAcceleration = Vector3.zero;
        AutoRight = Landed = Toss = false;
        TossVelocity = Vector3.zero;
        LandedSpeed = 0f;
    }
}

/// <summary>
/// vehicle-feel.md driving model (ADR-0005): suspension (F1), drive/brake/coast/reverse (F2/F2b),
/// combined friction-circle grip (F3), steering (F4), slip angle (F5), downforce (F6), wind force (F11),
/// state machine, auto-right (E5). Pure C# — never touches Rigidbody, Time, Physics, or UnityEngine.Object.
/// Wheel order: 0 FL, 1 FR, 2 RL, 3 RR. Verbs (jump, boost, air control) and lift/toss arrive in S7-04/06.
/// </summary>
public sealed class VehicleModel
{
    public const int WheelCount = 4;
    private const float G = ArchetypeParams.Gravity;

    private readonly ArchetypeParams _p;
    private readonly VehicleFeelValues _v;
    private readonly float _wheelbase;
    private readonly float[] _prevCompression = new float[WheelCount];
    private readonly float[] _compression = new float[WheelCount];
    private readonly bool[] _grounded = new bool[WheelCount];

    private float _airTimer;
    private float _upendedTimer;
    private float _rearGripMul = 1f;
    private float _airMaxFallSpeed;
    private bool _tossLatched;
    private bool _tossLeftGround;
    private float _tossGroundTimer;

    public VehicleModel(ArchetypeParams parameters, VehicleFeelValues values, WheelLayout layout)
    {
        _p = parameters;
        _v = values;
        _wheelbase = Mathf.Max(0.1f, layout.FrontAxleZ - layout.RearAxleZ);
        State = VehicleState.Grounded;
    }

    public ArchetypeParams Params => _p;
    public VehicleState State { get; private set; }
    /// <summary>Critical damage stage: momentum-only (vehicle-damage.md).</summary>
    public bool Disabled { get; private set; }
    /// <summary>Body slip angle (F5), degrees.</summary>
    public float SlipAngleDeg { get; private set; }
    /// <summary>Signed speed along the body's forward axis (m/s).</summary>
    public float ForwardSpeed { get; private set; }
    public int GroundedWheels { get; private set; }
    /// <summary>Current rear-grip multiplier (handbrake + recovery ramp).</summary>
    public float RearGripMultiplier => _rearGripMul;
    public float SteerAngleDeg { get; private set; }
    /// <summary>Steering added by wind this step (−1..1 units of steer).</summary>
    public float WindSteerBias { get; private set; }

    /// <summary>Advances one fixed step. <paramref name="contacts"/> holds 4 wheel queries; output is overwritten.</summary>
    public void Step(in VehicleStepInput input, WheelContact[] contacts, VehicleStepOutput output)
    {
        output.Clear();
        float dt = Mathf.Max(1e-4f, input.Dt);
        Disabled = input.Damage == DamageStage.Critical;

        Vector3 up = input.Rotation * Vector3.up;
        Vector3 fwd = input.Rotation * Vector3.forward;
        Vector3 right = input.Rotation * Vector3.right;
        ForwardSpeed = Vector3.Dot(input.Velocity, fwd);
        float latSpeed = Vector3.Dot(input.Velocity, right);
        float planarSpeed = Mathf.Sqrt(ForwardSpeed * ForwardSpeed + latSpeed * latSpeed);
        SlipAngleDeg = planarSpeed > 0.5f ? Mathf.Atan2(Mathf.Abs(latSpeed), Mathf.Abs(ForwardSpeed)) * Mathf.Rad2Deg : 0f;

        // ---- Contacts & suspension (F1) ----
        float minGroundDot = Mathf.Cos(_v.MaxGroundSlopeDeg * Mathf.Deg2Rad);
        int groundedCount = 0;
        for (int i = 0; i < WheelCount; i++)
        {
            WheelContact c = contacts[i];
            bool grounded = c.Grounded && Vector3.Dot(c.Normal, Vector3.up) >= minGroundDot // E9: no wall-driving
                                       && c.GroundDistance <= _v.RestLength;
            _grounded[i] = grounded;
            _compression[i] = grounded ? Mathf.Clamp(_v.RestLength - c.GroundDistance, 0f, _v.Travel) : 0f;
            if (grounded) groundedCount++;
        }
        GroundedWheels = groundedCount;
        UpdateState(input, up, groundedCount, dt, output);

        // ---- Drive / brake / reverse (F2, F2b) ----
        VehicleInputFrame inp = input.Input;
        float driveTotal = 0f;
        bool braking = false;
        if (!Disabled)
        {
            if (inp.Throttle > 0.01f && ForwardSpeed > -1f)
                driveTotal = _p.Mass * _p.EngineAccel * inp.Throttle * TorqueCurve(ForwardSpeed, _p.TopSpeed);
            else if (inp.Throttle > 0.01f)
                braking = true; // throttle while rolling backward brakes first
            if (inp.Brake > 0.01f && ForwardSpeed >= 1f)
                braking = true;
            else if (inp.Brake > 0.01f)
                driveTotal = -_p.Mass * _p.EngineAccel * inp.Brake * TorqueCurve(-ForwardSpeed, _p.ReverseSpeed);
        }
        bool coasting = Mathf.Abs(inp.Throttle) < 0.01f && Mathf.Abs(inp.Brake) < 0.01f;

        // ---- Wind steer: the wind tugs the wheel toward its direction (playtest: small tornadoes pull) ----
        Vector3 windExposed = input.Wind * _p.Exposure;
        float steerInput = Mathf.Clamp(inp.Steer, -1f, 1f);
        Vector3 windFlat = new Vector3(windExposed.x, 0f, windExposed.z);
        if (groundedCount >= 2 && windFlat.sqrMagnitude > 0.01f && _v.WindSteer > 0f)
        {
            float toward = Vector3.SignedAngle(new Vector3(fwd.x, 0f, fwd.z), windFlat, Vector3.up);
            float bias = Mathf.Sin(Mathf.Clamp(toward, -90f, 90f) * Mathf.Deg2Rad)
                         * _v.WindSteer * windFlat.magnitude / Mathf.Max(0.01f, _v.WindGripLossAt);
            steerInput = Mathf.Clamp(steerInput + Mathf.Clamp(bias, -_v.WindSteerMax, _v.WindSteerMax), -1f, 1f);
        }
        WindSteerBias = steerInput - Mathf.Clamp(inp.Steer, -1f, 1f);

        // ---- Steering (F4) ----
        float damageSteer = input.Damage == DamageStage.Damaged ? 0.75f : 1f;
        float steerDeg = _v.MaxSteerDeg * steerInput
                         * Mathf.Lerp(1f, _v.HighSpeedSteerFactor, Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / _p.TopSpeed))
                         * damageSteer;
        SteerAngleDeg = steerDeg;
        Quaternion steerRot = Quaternion.AngleAxis(steerDeg, up);

        // ---- Handbrake rear-grip ramp (Core Rule 2) ----
        if (inp.Handbrake) _rearGripMul = _p.HandbrakeGrip;
        else _rearGripMul = Mathf.MoveTowards(_rearGripMul, 1f, (1f - _p.HandbrakeGrip) / Mathf.Max(0.01f, _v.GripRecoveryTime) * dt);

        // Wind magnitude seen by the tires (exposure-scaled), for F3's WindGripMul.
        float windGripMul = 1f / (1f + windExposed.magnitude / Mathf.Max(0.01f, _v.WindGripLossAt));

        // ---- Per-wheel forces ----
        float quarterMass = _p.Mass * 0.25f;
        for (int i = 0; i < WheelCount; i++)
        {
            if (!_grounded[i])
            {
                _prevCompression[i] = 0f;
                continue;
            }
            WheelContact c = contacts[i];
            Vector3 n = c.Normal;

            // F1 spring-damper with per-step impulse clamp (ADR-0005 guideline).
            float x = _compression[i];
            float xDot = (x - _prevCompression[i]) / dt;
            _prevCompression[i] = x;
            float susp = Mathf.Max(0f, _p.SpringK * x + _p.DamperC * xDot);
            float approach = Mathf.Max(0f, -Vector3.Dot(c.PointVelocity, n));
            float suspCap = quarterMass * (G + approach / dt) * 1.5f;
            susp = Mathf.Min(susp, suspCap);

            // Anti-roll (Core Rule 2 assists): transfer load across the axle by compression difference.
            int mate = i ^ 1;
            float roll = (_compression[i] - _compression[mate]) * _p.SpringK * _v.AntiRoll;
            float load = Mathf.Max(0f, susp + roll);

            // Wheel frame on the contact plane.
            Vector3 wheelFwd = i < 2 ? steerRot * fwd : fwd;
            Vector3 f = (wheelFwd - n * Vector3.Dot(wheelFwd, n)).normalized;
            Vector3 r = Vector3.Cross(n, f);
            float vLong = Vector3.Dot(c.PointVelocity, f);
            float vLat = Vector3.Dot(c.PointVelocity, r);
            float stopLong = quarterMass * Mathf.Abs(vLong) / dt;

            // Longitudinal (F2 / F2b) + surface drag.
            bool rear = i >= 2;
            float fLong = driveTotal * 0.25f * (rear && inp.Handbrake ? 0.5f : 1f);
            if (braking) fLong += -Mathf.Sign(vLong) * Mathf.Min(quarterMass * _v.BrakeDecel, stopLong);
            float resist = (coasting ? _v.CoastDecel : 0f) + c.SurfaceDrag;
            if (resist > 0f) fLong += -Mathf.Sign(vLong) * Mathf.Min(quarterMass * resist, stopLong);

            // Lateral (F3), clamped to the one-step velocity-cancel force (no explicit-force jitter).
            float fLat = -vLat * _v.GripStiffness * load;
            float latCap = quarterMass * Mathf.Abs(vLat) / dt;
            fLat = Mathf.Clamp(fLat, -latCap, latCap);

            // Combined friction circle.
            float gripScale = input.GripScale > 0f ? input.GripScale : 1f;
            float budget = _v.GripMu * load * Mathf.Max(0f, c.SurfaceGrip) * (rear ? _rearGripMul : 1f) * windGripMul * gripScale;
            float mag = Mathf.Sqrt(fLong * fLong + fLat * fLat);
            if (mag > budget && mag > 1e-4f)
            {
                float s = budget / mag;
                fLong *= s;
                fLat *= s;
            }

            output.WheelForce[i] = n * load + f * fLong + r * fLat;
            output.WheelPoint[i] = c.Point;
        }

        // ---- Downforce (F6) ----
        if (groundedCount >= 2)
        {
            float t = ForwardSpeed / Mathf.Max(0.1f, _p.TopSpeed);
            output.CenterAcceleration = -up * G * _v.DownforceCoeff * t * t;
        }

        // ---- Yaw stability (Core Rule 2 assists) ----
        if (groundedCount >= 2 && !inp.Handbrake && State != VehicleState.Upended)
        {
            float yawRate = Vector3.Dot(input.AngularVelocity, up);
            float intended = ForwardSpeed * Mathf.Tan(steerDeg * Mathf.Deg2Rad) / _wheelbase;
            float excess = yawRate - intended;
            bool steeringIntoIt = Mathf.Abs(inp.Steer) > 0.5f && Mathf.Sign(inp.Steer) == Mathf.Sign(yawRate);
            if (Mathf.Abs(yawRate) > Mathf.Abs(intended) && !steeringIntoIt)
                output.AngularAcceleration += -up * excess * _v.YawStability * 8f;
        }

        // ---- Roll stabilization (playtest 2026-10-01: tornado knockbacks rolled the truck) ----
        if (groundedCount >= 1 && State != VehicleState.Upended && _v.RollStabilization > 0f)
        {
            float rollDeg = Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up, fwd), up, fwd);
            float excessDeg = Mathf.Abs(rollDeg) - _v.RollStabilizeAngleDeg;
            if (excessDeg > 0f)
            {
                float rollRate = Vector3.Dot(input.AngularVelocity, fwd);
                float correction = -Mathf.Sign(rollDeg) * excessDeg * Mathf.Deg2Rad * _v.RollStabilization * 40f
                                   - rollRate * _v.RollStabilization * 4f;
                output.AngularAcceleration += fwd * correction;
            }
        }

        // ---- Lift (F12) and toss (F12b) ----
        if (input.LiftFraction > 0f && groundedCount >= 1)
        {
            // Ground effect only: unloads the suspension (grip loss). Never > 1 g, and never once airborne —
            // the throw comes solely from the one-shot toss impulse below.
            output.CenterAcceleration += Vector3.up * G * Mathf.Min(input.LiftFraction, 0.9f);
            if (input.LiftFraction >= _v.TossThreshold && !_tossLatched && State != VehicleState.Upended)
            {
                // Throw height scales with EF: EF4 ≈ 3 m, EF5 ≈ 4 m apex for the Pickup. No added spin.
                float efScale = 0.7f + 0.15f * input.LiftEFStrength;
                Vector3 swirl = windFlat.sqrMagnitude > 0.01f ? windFlat.normalized * windFlat.magnitude * _v.TossSwirlFraction : Vector3.zero;
                output.Toss = true;
                output.TossVelocity = Vector3.up * _v.TossUpSpeed * _p.Exposure * efScale + swirl;
                _tossLatched = true;
                _tossLeftGround = false;
                _tossGroundTimer = 0f;
                State = VehicleState.Tossed;
            }
        }

        // ---- Wind (F11, as implemented): push along the wind until the body matches its speed ----
        output.WindAcceleration = WindAcceleration(input.Wind, input.Velocity, _p.Exposure, _v.WindResponse, _v.WindForceCapG);
        output.WindPoint = input.CenterOfMassWorld + up * _v.WindLeverHeight;
    }

    private void UpdateState(in VehicleStepInput input, Vector3 up, int groundedCount, float dt, VehicleStepOutput output)
    {
        VehicleState previous = State;
        bool upended = Vector3.Dot(up, Vector3.up) < _v.UpendedDot && input.AngularVelocity.magnitude < _v.UpendedAngularSpeed;

        if (upended && groundedCount < 2)
        {
            State = VehicleState.Upended;
            _upendedTimer += dt;
            if (_upendedTimer >= _v.AutoRightDelay)
            {
                output.AutoRight = true;
                _upendedTimer = 0f;
            }
            return;
        }
        _upendedTimer = 0f;

        if (groundedCount == 0)
        {
            if (previous == VehicleState.Tossed) _tossLeftGround = true;
            _airTimer += dt;
            _airMaxFallSpeed = Mathf.Max(_airMaxFallSpeed, -input.Velocity.y);
            if (_airTimer > _v.AirborneGrace && previous != VehicleState.Tossed) State = VehicleState.Airborne;
            return;
        }
        if (groundedCount == 1) return; // hysteresis: keep previous state

        // ≥ 2 wheels grounded.
        _airTimer = 0f;
        if (previous == VehicleState.Tossed && !_tossLeftGround)
        {
            // Toss impulse applied but the wheels haven't left the ground yet; give up after 1 s (pinned).
            _tossGroundTimer += dt;
            if (_tossGroundTimer < 1f) return;
            _tossLatched = false;
            _tossGroundTimer = 0f;
            State = VehicleState.Grounded;
            return;
        }
        if (previous == VehicleState.Airborne || previous == VehicleState.Tossed)
        {
            _tossLatched = false;
            _tossLeftGround = false;
            _tossGroundTimer = 0f;
            output.Landed = true;
            output.LandedSpeed = Mathf.Max(_airMaxFallSpeed, -input.Velocity.y);
            _airMaxFallSpeed = 0f;
            State = VehicleState.Grounded;
        }

        float speed = new Vector2(input.Velocity.x, input.Velocity.z).magnitude;
        bool hb = input.Input.Handbrake;
        if (State == VehicleState.Sliding)
        {
            if ((SlipAngleDeg < _v.SlideExitDeg && !hb) || speed < _v.SlideExitSpeed) State = VehicleState.Grounded;
        }
        else if (speed > _v.SlideMinSpeed && (hb || SlipAngleDeg > _v.SlideEnterDeg))
        {
            State = VehicleState.Sliding;
        }
        else
        {
            State = VehicleState.Grounded;
        }
    }

    /// <summary>F2 torque curve: 1 − (v/vMax)^2.5, clamped to 0..1 (v ≤ 0 → full torque).</summary>
    public static float TorqueCurve(float speed, float maxSpeed)
    {
        if (speed <= 0f) return 1f;
        float u = Mathf.Clamp01(speed / Mathf.Max(0.01f, maxSpeed));
        return 1f - Mathf.Pow(u, 2.5f);
    }

    /// <summary>
    /// F11 as implemented: acceleration along the wind direction, proportional to how much slower the body
    /// moves along it than the air does. Zero wind → zero force (the GDD's literal (w − v) form would brake
    /// the truck in still air). Capped at <paramref name="capG"/>·g.
    /// </summary>
    public static Vector3 WindAcceleration(Vector3 wind, Vector3 bodyVelocity, float exposure, float response, float capG)
    {
        wind.y = 0f;
        float w = wind.magnitude;
        if (w < 1e-3f) return Vector3.zero;
        Vector3 dir = wind / w;
        float along = Vector3.Dot(new Vector3(bodyVelocity.x, 0f, bodyVelocity.z), dir);
        float deficit = Mathf.Max(0f, w - along);
        float accel = Mathf.Min(response * exposure * deficit, capG * G);
        return dir * accel;
    }

    /// <summary>F10: 2 HP at ≥ severe, 1 HP at ≥ light, else 0.</summary>
    public static int ImpactHpLoss(float severity, float light, float severe)
    {
        if (severity >= severe) return 2;
        if (severity >= light) return 1;
        return 0;
    }

    /// <summary>F10 landing severity: vertical speed / LandingThresholdMul.</summary>
    public static float LandingSeverity(float verticalSpeed, float landingMul) => Mathf.Abs(verticalSpeed) / Mathf.Max(0.01f, landingMul);

    /// <summary>
    /// F12 lift as a fraction of weight: Exposure · LiftScale · Intensity · (1 − d/(0.5R))², LiftScale =
    /// max(0, (EFStrength − 2)·0.8). Tossed when ≥ 0.7 (TossThreshold).
    /// </summary>
    public static float LiftFraction(float exposure, float efStrength, float intensity, float distance, float windRadius, float liftCoefficient = 0.8f)
    {
        float liftScale = Mathf.Max(0f, (efStrength - 2f) * liftCoefficient);
        float zone = 0.5f * windRadius;
        if (liftScale <= 0f || zone <= 0f || distance >= zone) return 0f;
        float t = 1f - distance / zone;
        return exposure * liftScale * Mathf.Clamp01(intensity) * t * t;
    }
}
