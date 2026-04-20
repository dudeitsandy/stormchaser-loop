using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerVehicle : MonoBehaviour
{
    [SerializeField] private VehicleData _data;

    private Rigidbody _rb;
    private StormChaserControls _controls;
    private UnityEngine.InputSystem.InputAction _moveAction;
    private float _currentSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ
                        | RigidbodyConstraints.FreezePositionY;

        _controls = new StormChaserControls();
        _moveAction = _controls.Driving.Move;
    }

    private void OnEnable() => _controls.Driving.Enable();
    private void OnDisable() => _controls.Driving.Disable();

    private void FixedUpdate()
    {
        var input = _moveAction.ReadValue<Vector2>();
        HandleMovement(input.y);
        HandleSteering(input.x);
    }

    private void HandleMovement(float throttle)
    {
        float targetSpeed = throttle > 0
            ? throttle * _data.MoveSpeed
            : throttle * _data.ReverseSpeed;

        float rate = Mathf.Abs(throttle) > 0.01f ? _data.Acceleration : _data.Deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        _rb.linearVelocity = transform.forward * _currentSpeed;
    }

    private void HandleSteering(float steer)
    {
        if (Mathf.Abs(_currentSpeed) < 0.5f) return;

        float turn = steer * _data.TurnSpeed * Mathf.Sign(_currentSpeed) * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turn, 0f));
    }
}
