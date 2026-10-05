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
    /// <summary>Height of the wheels above the ground while airborne (m); 0 when grounded or unknown (E2).</summary>
    public float GroundClearance;
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
    /// <summary>F8: apply JumpVelocity once as a velocity change.</summary>
    public bool Jumped;
    public Vector3 JumpVelocity;
    /// <summary>A style moment ended this step (drift or counted airtime), for GameEvents.StyleEvent.</summary>
    public bool HasStyle;
    public StyleKind StyleKind;
    public float StyleAmount;

    public void Clear()
    {
        for (int i = 0; i < VehicleModel.WheelCount; i++)
        {
            WheelForce[i] = Vector3.zero;
            WheelPoint[i] = Vector3.zero;
        }
        CenterAcceleration = WindAcceleration = WindPoint = AngularAcceleration = Vector3.zero;
        AutoRight = Landed = Toss = Jumped = HasStyle = false;
        TossVelocity = JumpVelocity = Vector3.zero;
        LandedSpeed = StyleAmount = 0f;
    }
}

/// <summary>
/// vehicle-feel.md driving model (ADR-0005): suspension (F1), drive/brake/coast/reverse (F2/F2b),
/// combined friction-circle grip (F3), steering (F4), slip angle (F5), downforce (F6), air control (F7),
/// wind force (F11), state machine, auto-right (E5), Pure C# — never touches Rigidbody, Time, Physics, or UnityEngine.Object.
/// jump (F8), boost and style refills (F9, E1-E2), lift/toss (F12). Wheel order: 0 FL, 1 FR, 2 RL, 3 RR.
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
    private float _flippedTimer;
    private float _rearGripMul = 1f;
    private float _airMaxFallSpeed;
    private bool _tossLatched;
    private bool _tossLeftGround;
    private float _tossGroundTimer;
    private float _jumpTimer;
    private float _slideSeconds;
    private float _countedAirSeconds;

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
    /// <summary>0–1 drift blend from the slip angle (S9-02a pass 2): drift assists fade in and out on it instead of switching on Sliding.</summary>
    public float DriftAmount { get; private set; }
    /// <summary>Signed speed along the body's forward axis (m/s).</summary>
    public float ForwardSpeed { get; private set; }
    public int GroundedWheels { get; private set; }
    /// <summary>Current rear-grip multiplier (handbrake + recovery ramp).</summary>
    public float RearGripMultiplier => _rearGripMul;
    public float SteerAngleDeg { get; private set; }
    /// <summary>Steering added by wind this step (−1..1 units of steer).</summary>
    public float WindSteerBias { get; private set; }
    /// <summary>
    /// Applied engine effort this step, 0..1: drive force over full-throttle force at zero speed. Includes
    /// pedal, torque falloff near top speed and limp-mode power; 0 while braking or coasting.
    /// </summary>
    public float EngineLoad { get; private set; }
    /// <summary>True while boost is actually applied (after meter, damage and input gating).</summary>
    public bool BoostActive { get; private set; }
    /// <summary>Boost meter 0-100 (F9). Starts full.</summary>
    public float BoostMeter { get; private set; } = 100f;
    /// <summary>Speed at which boost force reaches zero (BoostMaxSpeedRatio x v_top).</summary>
    public float BoostMaxSpeed => _v.BoostMaxSpeedRatio * _p.TopSpeed;

    /// <summary>E3 near-miss refill (+RefillNearMiss x StyleRefillScale). The adapter decides what counts.</summary>
    public void AddNearMiss() => AddBoost(_v.RefillNearMiss * _p.StyleRefillScale);

    private void AddBoost(float amount) => BoostMeter = Mathf.Clamp(BoostMeter + amount, 0f, 100f);

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
        float driftT = planarSpeed > _v.SlideMinSpeed
            ? Mathf.Clamp01((SlipAngleDeg - _v.DriftStartDeg) / Mathf.Max(0.1f, _v.DriftFullDeg - _v.DriftStartDeg))
            : 0f;
        DriftAmount = driftT * driftT * (3f - 2f * driftT);

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
        // Critical = limp mode: reduced engine power, never zero, so a stopped truck can always crawl away.
        float power = Disabled ? Mathf.Clamp01(_v.CriticalPowerScale) : 1f;
        if (inp.Throttle > 0.01f && ForwardSpeed > -1f)
            driveTotal = _p.Mass * _p.EngineAccel * power * inp.Throttle * TorqueCurve(ForwardSpeed, _p.TopSpeed);
        else if (inp.Throttle > 0.01f)
            braking = true; // throttle while rolling backward brakes first
        if (inp.Brake > 0.01f && ForwardSpeed >= 1f)
            braking = true;
        else if (inp.Brake > 0.01f)
            driveTotal = -_p.Mass * _p.EngineAccel * power * inp.Brake * TorqueCurve(-ForwardSpeed, _p.ReverseSpeed);
        bool coasting = Mathf.Abs(inp.Throttle) < 0.01f && Mathf.Abs(inp.Brake) < 0.01f;
        EngineLoad = Mathf.Clamp01(Mathf.Abs(driveTotal) / Mathf.Max(1e-3f, _p.Mass * _p.EngineAccel));

        // ---- Jump (F8, E14): >= 2 wheels, not Upended or Critical, off cooldown ----
        _jumpTimer = Mathf.Max(0f, _jumpTimer - dt);
        if (inp.JumpPressed && groundedCount >= 2 && !Disabled && State != VehicleState.Upended && _jumpTimer <= 0f)
        {
            Vector3 dir = Vector3.Lerp(up, Vector3.up, Mathf.Clamp01(_v.JumpWorldUpBlend)).normalized;
            output.Jumped = true;
            output.JumpVelocity = dir * _v.JumpSpeed;
            _jumpTimer = _v.JumpCooldown;
        }

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
        // Sliding keeps (near) full lock so counter-steer can catch the drift (S9-02a); grip driving narrows at speed.
        float speedSteerFactor = Mathf.Lerp(_v.HighSpeedSteerFactor, _v.SlideSteerFactor, DriftAmount);
        float steerDeg = _v.MaxSteerDeg * steerInput
                         * Mathf.Lerp(1f, speedSteerFactor, Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / _p.TopSpeed))
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
            float axleShare = rear ? _v.RearDriveBias : 1f - _v.RearDriveBias; // per axle, split across its 2 wheels
            float fLong = driveTotal * 0.5f * axleShare * (rear && inp.Handbrake ? 0.5f : 1f);
            if (braking) fLong += -Mathf.Sign(vLong) * Mathf.Min(quarterMass * _v.BrakeDecel, stopLong);
            float resist = (coasting ? _v.CoastDecel : 0f) + c.SurfaceDrag;
            if (resist > 0f) fLong += -Mathf.Sign(vLong) * Mathf.Min(quarterMass * resist, stopLong);

            // Lateral (F3), clamped to the one-step velocity-cancel force (no explicit-force jitter).
            float fLat = -vLat * _v.GripStiffness * load;
            float latCap = quarterMass * Mathf.Abs(vLat) / dt;
            fLat = Mathf.Clamp(fLat, -latCap, latCap);

            // Combined friction circle.
            float gripScale = input.GripScale > 0f ? input.GripScale : 1f;
            // Feathering (S9-02a pass 2): in a drift, throttle loosens the rear; lifting gives the grip back.
            float featherLimit = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_v.FeatherLimitStartDeg, _v.FeatherLimitEndDeg, SlipAngleDeg));
            float featherMul = rear ? 1f - _v.ThrottleRearGripLoss * DriftAmount * featherLimit * Mathf.Clamp01(inp.Throttle) : 1f;
            float budget = _v.GripMu * load * Mathf.Max(0f, c.SurfaceGrip) * (rear ? _rearGripMul : 1f) * featherMul * windGripMul * gripScale;
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

        // ---- Brake assist (F2b): arcade bite outside the friction circle; never pushes past a stop ----
        if (braking && groundedCount >= 2 && _v.BrakeAssistDecel > 0f && Mathf.Abs(ForwardSpeed) > 1e-3f)
        {
            float assist = Mathf.Min(_v.BrakeAssistDecel, Mathf.Abs(ForwardSpeed) / dt);
            output.CenterAcceleration += -fwd * Mathf.Sign(ForwardSpeed) * assist;
        }

        // ---- Drift drive (S9-02a): throttle in a slide pushes along the direction of travel, fading at top speed ----
        if (DriftAmount > 0f && groundedCount >= 2 && inp.Throttle > 0.01f && _v.DriftDriveAccel > 0f)
        {
            Vector3 travel = new Vector3(input.Velocity.x, 0f, input.Velocity.z);
            float travelSpeed = travel.magnitude;
            if (travelSpeed > 1f)
            {
                float fade = Mathf.Clamp01(1f - travelSpeed / _p.TopSpeed);
                output.CenterAcceleration += travel / travelSpeed * (_v.DriftDriveAccel * power * inp.Throttle * fade * DriftAmount);
            }
        }

        // ---- Boost (F9): fades to zero at BoostMaxSpeed; works airborne; never while Critical ----
        bool wantBoost = inp.Boost && !Disabled;
        BoostActive = wantBoost && (BoostActive ? BoostMeter > 0f : BoostMeter >= _v.BoostMinStart);
        if (BoostActive)
        {
            BoostMeter = Mathf.Max(0f, BoostMeter - _v.BoostDrain * dt);
            output.CenterAcceleration += fwd * BoostAcceleration(ForwardSpeed, BoostMaxSpeed, _v.BoostAccel);
        }

        // ---- Style refills (F9) with exploit gates E1 (slide speed) and E2 (airtime height) ----
        float refill = BoostActive ? 0f : _v.BoostPassiveRegen; // no trickle while boosting, or a held boost never empties
        if (State == VehicleState.Sliding && planarSpeed > _v.MinSlideRefillSpeed) refill += _v.RefillSlide * _p.StyleRefillScale;
        bool flying = State == VehicleState.Airborne || State == VehicleState.Tossed; // tosses count, bunny-hops don't
        if (flying && input.GroundClearance > _v.MinAirtimeHeight) refill += _v.RefillAir * _p.StyleRefillScale;
        AddBoost(refill * dt);

        // ---- Air control (F7): pitch + yaw, handbrake turns yaw into roll; rate-capped while steering ----
        if (groundedCount == 0 && State != VehicleState.Upended)
        {
            Vector2 air = Vector2.ClampMagnitude(inp.Air, 1f);
            Vector3 axisInput = right * air.y + (inp.Handbrake ? -fwd * air.x : up * air.x);
            if (axisInput.sqrMagnitude > 1e-4f)
            {
                Vector3 alpha = axisInput * (_v.AirAccel * _p.TrickScale);
                Vector3 next = input.AngularVelocity + alpha * dt;
                if (next.magnitude > _v.AirMaxRate) alpha = (next.normalized * _v.AirMaxRate - input.AngularVelocity) / dt;
                output.AngularAcceleration += alpha;
            }
        }

        // ---- Yaw stability (Core Rule 2 assists) ----
        if (groundedCount >= 2 && !inp.Handbrake && State != VehicleState.Upended)
        {
            float yawRate = Vector3.Dot(input.AngularVelocity, up);
            // In a drift the stabiliser aims at "stop rotating", not at the counter-steered heading: pulling toward the
            // counter-steer direction snapped the truck straight ("locks out at times", S9-02a pass 2). It also eases
            // to DriftStabilityKeep of its strength so the driver, not the assist, holds the angle.
            float intended = ForwardSpeed * Mathf.Tan(steerDeg * Mathf.Deg2Rad) / _wheelbase * (1f - DriftAmount);
            float excess = yawRate - intended;
            bool steeringIntoIt = Mathf.Abs(inp.Steer) > 0.5f && Mathf.Sign(inp.Steer) == Mathf.Sign(yawRate);
            if (Mathf.Abs(yawRate) > Mathf.Abs(intended) && !steeringIntoIt)
                output.AngularAcceleration += -up * excess * _v.YawStability * 8f
                                              * Mathf.Lerp(1f, _v.DriftStabilityKeep, DriftAmount);
        }

        // ---- Counter-steer assist (S9-02a): steering against a slide's rotation catches it ----
        if (DriftAmount > 0f && groundedCount >= 2 && !inp.Handbrake && _v.CounterSteerAssist > 0f)
        {
            float yawRate = Vector3.Dot(input.AngularVelocity, up);
            if (Mathf.Abs(inp.Steer) > 0.05f && Mathf.Sign(inp.Steer) != Mathf.Sign(yawRate))
                output.AngularAcceleration += -up * yawRate * _v.CounterSteerAssist * Mathf.Abs(inp.Steer) * DriftAmount;
        }

        // ---- Roll stabilization (playtest 2026-10-01: tornado knockbacks rolled the truck) ----
        // ---- and pitch (playtest 2026-10-03: nose-first landings wedged the truck on its bumper) ----
        if (groundedCount >= 1 && State != VehicleState.Upended && _v.RollStabilization > 0f)
        {
            output.AngularAcceleration += fwd * TiltCorrection(up, fwd, input.AngularVelocity, _v.RollStabilizeAngleDeg);
            output.AngularAcceleration += right * TiltCorrection(up, right, input.AngularVelocity, _v.PitchStabilizeAngleDeg);
        }

        // ---- Arcade air gravity (playtest 2026-10-03: "jump is a little floaty"). Tosses keep F12b's arc ----
        if (groundedCount == 0 && State != VehicleState.Tossed && State != VehicleState.Upended)
        {
            float mul = input.Velocity.y > 0f ? _v.AirGravityMul : _v.FallGravityMul;
            if (mul > 1f) output.CenterAcceleration += Vector3.down * G * (mul - 1f);
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

    /// <summary>
    /// Angular acceleration about <paramref name="axis"/> that pulls the tilt around that axis back inside
    /// <paramref name="limitDeg"/>, with rate damping. Zero while within the limit.
    /// </summary>
    private float TiltCorrection(Vector3 up, Vector3 axis, Vector3 angularVelocity, float limitDeg)
    {
        float tiltDeg = Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up, axis), up, axis);
        float excessDeg = Mathf.Abs(tiltDeg) - limitDeg;
        if (excessDeg <= 0f) return 0f;
        float rate = Vector3.Dot(angularVelocity, axis);
        return -Mathf.Sign(tiltDeg) * excessDeg * Mathf.Deg2Rad * _v.RollStabilization * 40f
               - rate * _v.RollStabilization * 4f;
    }

    private void UpdateState(in VehicleStepInput input, Vector3 up, int groundedCount, float dt, VehicleStepOutput output)
    {
        VehicleState previous = State;
        // Never stuck upside down (Andy 2026-10-05): jump rights the truck at once, and after UpendedFailsafeSeconds
        // on its side or back it rights itself whatever the spin (the E5 rule below waits for it to settle).
        bool upsideDown = Vector3.Dot(up, Vector3.up) < _v.UpendedDot;
        _flippedTimer = upsideDown ? _flippedTimer + dt : 0f;
        if (upsideDown && (input.Input.JumpPressed || _flippedTimer >= _v.UpendedFailsafeSeconds))
        {
            output.AutoRight = true;
            _flippedTimer = 0f;
            _upendedTimer = 0f;
            State = VehicleState.Upended;
            return;
        }
        bool upended = upsideDown && input.AngularVelocity.magnitude < _v.UpendedAngularSpeed;

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
            EndSlide(output);
            if (input.GroundClearance > _v.MinAirtimeHeight) _countedAirSeconds += dt; // E2: bunny-hops never count
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
            if (_countedAirSeconds >= _v.MinStyleSeconds) SetStyle(output, StyleKind.Airtime, _countedAirSeconds);
            _countedAirSeconds = 0f;
            State = VehicleState.Grounded;
        }

        float speed = new Vector2(input.Velocity.x, input.Velocity.z).magnitude;
        bool hb = input.Input.Handbrake;
        if (State == VehicleState.Sliding)
        {
            if (speed > _v.MinSlideRefillSpeed) _slideSeconds += dt; // E1: donuts never count
            if ((SlipAngleDeg < _v.SlideExitDeg && !hb) || speed < _v.SlideExitSpeed)
            {
                EndSlide(output);
                State = VehicleState.Grounded;
            }
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

    // A slide that ends (exit, or leaving the ground) raises a Drift style moment if long enough.
    private void EndSlide(VehicleStepOutput output)
    {
        if (_slideSeconds >= _v.MinStyleSeconds) SetStyle(output, StyleKind.Drift, _slideSeconds);
        _slideSeconds = 0f;
    }

    private static void SetStyle(VehicleStepOutput output, StyleKind kind, float amount)
    {
        output.HasStyle = true;
        output.StyleKind = kind;
        output.StyleAmount = amount;
    }

    /// <summary>F9 boost acceleration: A x (1 - (v / vMax)^2), never negative (no boost braking above vMax).</summary>
    public static float BoostAcceleration(float forwardSpeed, float boostMaxSpeed, float boostAccel)
    {
        float u = Mathf.Max(0f, forwardSpeed) / Mathf.Max(0.1f, boostMaxSpeed);
        return boostAccel * Mathf.Max(0f, 1f - u * u);
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
