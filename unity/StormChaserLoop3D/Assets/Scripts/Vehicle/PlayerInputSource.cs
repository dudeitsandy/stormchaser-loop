using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player input → VehicleInputFrame (ADR-0005), using the vehicle-feel.md Core Rule 10 map: RT/W throttle,
/// LT/S brake-reverse, left stick/A-D steer, X/Left Ctrl handbrake, A/Space jump, B/Left Shift boost.
/// Left stick/WASD also drive air control (pitch + yaw).
/// </summary>
public sealed class PlayerInputSource : IVehicleInput, System.IDisposable
{
    private readonly StormChaserControls _controls;
    private readonly InputAction _move, _throttle, _brake, _handbrake, _jump, _boost;
    // A jump tap can land between physics steps; latch it until the next Read consumes it.
    private bool _jumpLatched;

    public PlayerInputSource()
    {
        _controls = new StormChaserControls();
        StormChaserControls.DrivingActions driving = _controls.Driving;
        _move = driving.Move;
        _throttle = driving.Throttle;
        _brake = driving.Brake;
        _handbrake = driving.Handbrake;
        _jump = driving.Jump;
        _boost = driving.Boost;
        _jump.performed += OnJump;
        driving.Enable();
    }

    private void OnJump(InputAction.CallbackContext context) => _jumpLatched = true;

    public VehicleInputFrame Read()
    {
        Vector2 move = _move.ReadValue<Vector2>();
        bool jump = _jumpLatched;
        _jumpLatched = false;
        return new VehicleInputFrame
        {
            Throttle = Mathf.Clamp01(_throttle.ReadValue<float>()),
            Brake = Mathf.Clamp01(_brake.ReadValue<float>()),
            Steer = move.x,
            Air = move,
            Handbrake = _handbrake.IsPressed(),
            JumpPressed = jump,
            Boost = _boost.IsPressed(),
        };
    }

    public void Dispose()
    {
        _jump.performed -= OnJump;
        _controls.Driving.Disable();
        _controls.Dispose();
    }
}
