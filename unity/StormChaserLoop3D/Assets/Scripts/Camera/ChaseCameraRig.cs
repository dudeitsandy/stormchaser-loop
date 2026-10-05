using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Chase camera and Storm Cam (vehicle-feel.md Core Rule 8, F14, E11). Runs as a Cinemachine extension on
/// the follow camera and owns its position and orientation at the Body stage; any Follow / HardLookAt
/// components on the same camera are disabled so they can't fight it.
/// <para>Chase: right stick / mouse orbits; on release the camera recenters behind the direction of
/// travel, or behind the truck's facing below <c>_restMinSpeed</c> (reversing never flips it).</para>
/// <para>Storm Cam (toggle): the camera sits behind the truck on the truck→funnel line and aims at the
/// nearest disaster in shutter range with F14's drifting offset; orbit input nudges the aim.</para>
/// Photo aim reads the rendered camera, not this component (photo-scoring.md AimScore).
/// </summary>
[AddComponentMenu("Cinemachine/Extensions/Chase Camera Rig")]
public class ChaseCameraRig : CinemachineExtension
{
    [Header("Framing")]
    [Tooltip("Camera distance from the pivot (same 8.94 m as the 0.7 framing).")]
    [SerializeField] private float _distance = 8.944f;
    [Tooltip("Default downward pitch in degrees. 12° (was 26.6°, Andy 2026-10-04: \"lower so it captures more of the " +
             "sky, like Rocket League\"): the sky fills the top third so forming funnels, rain and the storm sky read.")]
    [SerializeField] private float _defaultPitch = 12f;
    [Tooltip("Pivot height above the truck origin: raised so the truck sits a little below centre.")]
    [SerializeField] private float _pivotHeight = 1.2f;
    [SerializeField] private float _minPitch = 5f;
    [SerializeField] private float _maxPitch = 50f;

    [Header("Chase")]
    [Tooltip("Below this speed (m/s) the camera rests behind the truck's facing, not its velocity.")]
    [SerializeField] private float _restMinSpeed = 3f;
    [Tooltip("Seconds to settle 99 % of a heading change (Cinemachine damping; 0.7 shipped 1.6).")]
    [SerializeField] private float _yawDamping = 1.6f;
    [Tooltip("Seconds to settle 99 % of a height change (suspension bob, jumps).")]
    [SerializeField] private float _heightDamping = 0.5f;
    [Tooltip("A target jump larger than this (m) in one update is a teleport: the camera snaps.")]
    [SerializeField] private float _teleportSnapDistance = 10f;

    [Header("Orbit")]
    [SerializeField] private float _stickYawSpeed = 180f;
    [SerializeField] private float _stickPitchSpeed = 90f;
    [Tooltip("Degrees per pixel of mouse movement.")]
    [SerializeField] private float _mouseSensitivity = 0.15f;
    [SerializeField] private float _stickDeadzone = 0.15f;
    [Tooltip("OrbitRecenterTime: seconds to settle 99 % back behind the truck after orbit input stops.")]
    [SerializeField] private float _recenterTime = 0.6f;
    [Tooltip("Mouse has no 'release': recentering starts after this many seconds without mouse movement.")]
    [SerializeField] private float _mouseRecenterDelay = 1f;

    [Header("Storm Cam")]
    [Tooltip("Same as the shutter's range.")]
    [SerializeField] private float _stormCamRange = 60f;
    [Tooltip("StormCamSwitchRatio: a new target must be closer than this × the current one.")]
    [SerializeField] private float _switchRatio = 0.75f;
    [SerializeField] private float _stormCamMaxOffset = 8f;
    [SerializeField] private float _stormCamLag = 10f;
    [SerializeField] private float _stormCamSway = 3f;
    [Tooltip("Seconds to settle 99 % of a Storm Cam bearing change.")]
    [SerializeField] private float _stormCamDamping = 0.35f;
    [Tooltip("Downward pitch while framing a storm (lower than chase so the funnel fills the frame).")]
    [SerializeField] private float _stormCamLookPitch = 12f;
    [Tooltip("How far orbit input can nudge the Storm Cam aim, in degrees.")]
    [SerializeField] private float _nudgeMax = 15f;
    [Tooltip("v_top for F14 when the target has no PlayerVehicle (Pickup top speed).")]
    [SerializeField] private float _fallbackTopSpeed = 21.5f;

    private StormChaserControls _controls;
    private Transform _target;
    private Rigidbody _targetBody;
    private PlayerVehicle _vehicle;
    private bool _initialized;
    private Vector3 _lastTargetPos;
    private float _pivotY;
    private float _restYaw;
    private float _baseYaw;
    private float _orbitYaw;
    private float _orbitPitch;
    private float _stormPosYaw;
    private float _stormAimYaw;
    private float _nudge;
    private bool _wasFramingStorm;
    private Vector2 _mouseAccum;
    private float _mouseIdle = float.MaxValue;
    private bool _stormCamRequested;

    /// <summary>True while Storm Cam is toggled on (it may have no target: "NO TARGET").</summary>
    public bool StormCamEnabled { get; private set; }
    /// <summary>The disaster Storm Cam is framing, or null.</summary>
    public DisasterEntity StormCamTarget { get; private set; }
    /// <summary>Current F14 offset in degrees (0 when not framing a storm).</summary>
    public float StormCamOffset { get; private set; }
    /// <summary>World yaw the camera is aiming along, in degrees.</summary>
    public float AimYaw { get; private set; }

    /// <summary>Turns Storm Cam on or off (the Y / Tab toggle calls this).</summary>
    public void SetStormCam(bool enabled)
    {
        StormCamEnabled = enabled;
        if (!enabled) StormCamTarget = null;
    }

    /// <summary>Flips Storm Cam.</summary>
    public void ToggleStormCam() => SetStormCam(!StormCamEnabled);

    /// <summary>Drops orbit and damping so the next update snaps behind the truck (retry, tests).</summary>
    public void SnapToRest()
    {
        _initialized = false;
        _orbitYaw = 0f;
        _orbitPitch = 0f;
        _nudge = 0f;
    }

    protected override void Awake()
    {
        base.Awake();
        _controls = new StormChaserControls();
        DisableCompetingComponents();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _controls.Driving.Enable();
        _controls.Driving.StormCam.performed += OnStormCam;
    }

    private void OnDisable()
    {
        _controls.Driving.StormCam.performed -= OnStormCam;
        _controls.Driving.Disable();
    }

    protected override void OnDestroy()
    {
        _controls?.Dispose();
        base.OnDestroy();
    }

    private void OnStormCam(InputAction.CallbackContext ctx) => _stormCamRequested = true;

    private void Update()
    {
        if (_stormCamRequested)
        {
            _stormCamRequested = false;
            ToggleStormCam();
        }
        _mouseAccum += _controls.Driving.OrbitMouse.ReadValue<Vector2>();
    }

    private void DisableCompetingComponents()
    {
        foreach (CinemachineComponentBase c in GetComponents<CinemachineComponentBase>())
        {
            if (c.Stage == CinemachineCore.Stage.Body || c.Stage == CinemachineCore.Stage.Aim) c.enabled = false;
        }
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body) return;
        if (!ResolveTarget(vcam)) return;

        Vector3 targetPos = _target.position;
        bool snap = !_initialized || deltaTime < 0f
                    || (targetPos - _lastTargetPos).sqrMagnitude > _teleportSnapDistance * _teleportSnapDistance;
        float dt = Mathf.Max(0f, deltaTime);
        Vector3 velocity = _targetBody != null ? _targetBody.linearVelocity : Vector3.zero;
        _lastTargetPos = targetPos;

        _restYaw = ChaseCameraMath.RestYaw(velocity, _target.forward, _initialized ? _restYaw : ChaseCameraMath.Yaw(_target.forward), _restMinSpeed);
        _pivotY = snap ? targetPos.y : ChaseCameraMath.Damp(_pivotY, targetPos.y, _heightDamping, dt);
        Vector3 pivot = new Vector3(targetPos.x, _pivotY + _pivotHeight, targetPos.z);

        Vector2 stick = _controls.Driving.AimCamera.ReadValue<Vector2>();
        bool stickActive = stick.magnitude > _stickDeadzone;
        if (!stickActive) stick = Vector2.zero;
        Vector2 mouse = _mouseAccum;
        _mouseAccum = Vector2.zero;
        _mouseIdle = mouse.sqrMagnitude > 0f ? 0f : (_mouseIdle == float.MaxValue ? _mouseIdle : _mouseIdle + dt);
        bool orbiting = stickActive || _mouseIdle < _mouseRecenterDelay;

        UpdateStormTarget(targetPos);

        Vector3 position;
        Quaternion orientation;
        if (StormCamTarget != null)
        {
            FrameStorm(pivot, velocity, stick, mouse, orbiting, snap, dt, out position, out orientation);
        }
        else
        {
            if (_wasFramingStorm)
            {
                // Hand back from Storm Cam without a jump: keep the current yaw as an orbit offset.
                _orbitYaw = Mathf.DeltaAngle(_restYaw, _stormAimYaw);
                _baseYaw = _restYaw;
                _wasFramingStorm = false;
            }
            Chase(pivot, stick, mouse, orbiting, snap, dt, out position, out orientation);
            StormCamOffset = 0f;
        }

        _initialized = true;
        state.RawPosition = position;
        state.RawOrientation = orientation;
        AimYaw = orientation.eulerAngles.y;
    }

    /// <summary>Settings: look sensitivity multiplier (mouse and stick), 1 = default.</summary>
    public float SensitivityScale { get; set; } = 1f;
    /// <summary>Settings: invert the vertical look axis.</summary>
    public bool InvertY { get; set; }

    /// <summary>
    /// Settings camera preset (design/ux/run-screens.md): SKY = the 12° default (Andy 2026-10-04: more sky), CLASSIC =
    /// the 0.7 framing (26.6°), HIGH = overhead (40°). Distance is unchanged, so the truck keeps its size.
    /// </summary>
    public void ApplyPreset(string preset)
    {
        switch (preset)
        {
            case "CLASSIC": _defaultPitch = 26.57f; _pivotHeight = 0f; break;
            case "HIGH": _defaultPitch = 40f; _pivotHeight = 0f; break;
            default: _defaultPitch = 12f; _pivotHeight = 1.2f; break;
        }
    }

    /// <summary>Current default pitch (tests and HUD).</summary>
    public float DefaultPitch => _defaultPitch;

    private void Chase(Vector3 pivot, Vector2 stick, Vector2 mouse, bool orbiting, bool snap, float dt,
        out Vector3 position, out Quaternion orientation)
    {
        _baseYaw = snap ? _restYaw : ChaseCameraMath.DampAngle(_baseYaw, _restYaw, _yawDamping, dt);

        float look = SensitivityScale, pitchSign = InvertY ? -1f : 1f;
        _orbitYaw += (stick.x * _stickYawSpeed * dt + mouse.x * _mouseSensitivity) * look;
        _orbitPitch -= (stick.y * _stickPitchSpeed * dt + mouse.y * _mouseSensitivity) * look * pitchSign;
        _orbitYaw = Mathf.DeltaAngle(0f, _orbitYaw);
        _orbitPitch = Mathf.Clamp(_orbitPitch, _minPitch - _defaultPitch, _maxPitch - _defaultPitch);
        if (!orbiting)
        {
            _orbitYaw = ChaseCameraMath.DampAngle(_orbitYaw, 0f, _recenterTime, dt);
            _orbitPitch = ChaseCameraMath.Damp(_orbitPitch, 0f, _recenterTime, dt);
        }

        orientation = Quaternion.Euler(_defaultPitch + _orbitPitch, _baseYaw + _orbitYaw, 0f);
        position = pivot - orientation * Vector3.forward * _distance;
    }

    private void FrameStorm(Vector3 pivot, Vector3 velocity, Vector2 stick, Vector2 mouse, bool orbiting,
        bool snap, float dt, out Vector3 position, out Quaternion orientation)
    {
        Vector3 funnel = StormCamTarget.transform.position;
        float lineYaw = ChaseCameraMath.Yaw(ChaseCameraMath.Flat(funnel - pivot));
        if (!_wasFramingStorm)
        {
            // Start from where the chase camera is looking, then damp onto the funnel.
            float current = _baseYaw + _orbitYaw;
            _stormPosYaw = snap ? lineYaw : current;
            _stormAimYaw = snap ? lineYaw : current;
            _orbitYaw = 0f;
            _orbitPitch = 0f;
            _nudge = 0f;
            _wasFramingStorm = true;
        }

        _stormPosYaw = snap ? lineYaw : ChaseCameraMath.DampAngle(_stormPosYaw, lineYaw, _stormCamDamping, dt);
        position = pivot - Quaternion.Euler(_defaultPitch, _stormPosYaw, 0f) * Vector3.forward * _distance;

        _nudge += (stick.x * _stickYawSpeed * dt + mouse.x * _mouseSensitivity) * SensitivityScale;
        _nudge = Mathf.Clamp(_nudge, -_nudgeMax, _nudgeMax);
        if (!orbiting) _nudge = ChaseCameraMath.Damp(_nudge, 0f, _recenterTime, dt);

        Vector3 cameraToFunnel = funnel - position;
        float vTop = _vehicle != null && _vehicle.MaxSpeed > 0f ? _vehicle.MaxSpeed : _fallbackTopSpeed;
        float vPerp = ChaseCameraMath.PerpendicularSpeed(velocity, cameraToFunnel);
        StormCamOffset = ChaseCameraMath.StormCamOffset(vPerp, vTop, Time.time,
            _stormCamLag, _stormCamSway, _stormCamMaxOffset);
        float aimTarget = ChaseCameraMath.Yaw(ChaseCameraMath.Flat(cameraToFunnel)) + StormCamOffset + _nudge;
        _stormAimYaw = snap ? aimTarget : ChaseCameraMath.DampAngle(_stormAimYaw, aimTarget, _stormCamDamping, dt);

        orientation = Quaternion.Euler(_stormCamLookPitch, _stormAimYaw, 0f);
        _baseYaw = _stormAimYaw;
    }

    private void UpdateStormTarget(Vector3 from)
    {
        if (!StormCamEnabled)
        {
            StormCamTarget = null;
            return;
        }

        DisasterEntity nearest = null;
        float nearestDist = _stormCamRange;
        var active = DisasterEntity.Active;
        for (int i = 0; i < active.Count; i++)
        {
            DisasterEntity e = active[i];
            if (e == null) continue;
            float d = ChaseCameraMath.Flat(e.transform.position - from).magnitude;
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = e;
            }
        }

        DisasterEntity current = StormCamTarget;
        if (current != null)
        {
            float currentDist = ChaseCameraMath.Flat(current.transform.position - from).magnitude;
            if (!current.isActiveAndEnabled || currentDist > _stormCamRange) current = null;
            else if (nearest != null && nearest != current
                     && ChaseCameraMath.ShouldSwitchTarget(currentDist, nearestDist, _switchRatio)) current = nearest;
        }
        if (current == null) current = nearest;
        StormCamTarget = current;
    }

    private bool ResolveTarget(CinemachineVirtualCameraBase vcam)
    {
        Transform follow = vcam.Follow;
        if (follow == null) return false;
        if (follow != _target)
        {
            _target = follow;
            _targetBody = follow.GetComponent<Rigidbody>();
            _vehicle = follow.GetComponent<PlayerVehicle>();
            _initialized = false;
        }
        return true;
    }
}
