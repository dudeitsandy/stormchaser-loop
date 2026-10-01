using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player input → VehicleInputFrame (ADR-0005). Uses the existing Driving/Move action for throttle,
/// brake and steer. The S7-04 input remap adds dedicated actions (RT/LT, handbrake X, jump A, boost B);
/// until then the handbrake reads Left Ctrl / gamepad West directly.
/// </summary>
public sealed class PlayerInputSource : IVehicleInput, System.IDisposable
{
    private readonly StormChaserControls _controls;
    private readonly InputAction _move;

    public PlayerInputSource()
    {
        _controls = new StormChaserControls();
        _move = _controls.Driving.Move;
        _controls.Driving.Enable();
    }

    public VehicleInputFrame Read()
    {
        Vector2 move = _move.ReadValue<Vector2>();
        bool handbrake = (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed)
                         || (Gamepad.current != null && Gamepad.current.buttonWest.isPressed);
        return new VehicleInputFrame
        {
            Throttle = Mathf.Max(0f, move.y),
            Brake = Mathf.Max(0f, -move.y),
            Steer = move.x,
            Air = move,
            Handbrake = handbrake,
        };
    }

    public void Dispose()
    {
        _controls.Driving.Disable();
        _controls.Dispose();
    }
}
