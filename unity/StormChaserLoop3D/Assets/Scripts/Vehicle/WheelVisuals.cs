using UnityEngine;

/// <summary>
/// Places the hero pickup's visual wheels from the suspension every frame (Andy 2026-10-04: the truck "sinks
/// below the driving surface"; the wheels were fixed to the body, so compression pushed them through the
/// road). Each wheel sits at its ground contact, or hangs at full droop in the air, spins with speed, and the
/// fronts steer. Visual only; pairs with <see cref="TruckVisualSelector"/>, which installs it.
/// </summary>
public class WheelVisuals : MonoBehaviour
{
    private static readonly string[] Names = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };

    [Tooltip("Visual tyre radius in metres (the model's, slightly larger than the physics 0.22).")]
    [SerializeField] private float _visualRadius = 0.23f;
    [Tooltip("Seconds to follow a suspension change; hides fixed-step jitter between physics steps.")]
    [SerializeField] private float _followTime = 0.03f;

    private PlayerVehicle _vehicle;
    private readonly Transform[] _wheels = new Transform[4];
    private readonly Quaternion[] _baseRotation = new Quaternion[4];
    private readonly float[] _height = new float[4];
    private float _spinDeg;
    private bool _bound;

    /// <summary>Binds to the wheels under <paramref name="visual"/> (null or a visual without wheels unbinds).</summary>
    public void Bind(Transform visual)
    {
        _vehicle = GetComponent<PlayerVehicle>();
        _bound = false;
        if (visual == null || _vehicle == null) return;
        for (int i = 0; i < Names.Length; i++)
        {
            _wheels[i] = FindDeep(visual, Names[i]);
            if (_wheels[i] == null) return;
            _baseRotation[i] = _wheels[i].localRotation;
            _height[i] = _wheels[i].localPosition.y;
        }
        _bound = true;
    }

    /// <summary>
    /// Local wheel-centre height under the anchor: on the ground when grounded (compression shortens the
    /// gap), at full droop when not. Never below the contact, so the tyre can't sink into the road.
    /// </summary>
    public static float WheelCentreHeight(float anchorY, bool grounded, float groundDistance, float restLength,
                                          float radius)
    {
        float distance = grounded ? Mathf.Min(groundDistance, restLength) : restLength;
        return anchorY - distance + radius;
    }

    private void LateUpdate()
    {
        if (!_bound || _vehicle == null) return;
        var contacts = _vehicle.WheelContacts;
        var anchors = _vehicle.WheelAnchors;
        if (contacts == null || contacts.Count < 4 || _vehicle.SuspensionRestLength <= 0f) return; // not initialised yet

        float dt = Time.deltaTime;
        float k = _followTime > 0f ? 1f - Mathf.Exp(-dt / _followTime) : 1f;
        _spinDeg = Mathf.Repeat(_spinDeg + _vehicle.CurrentSpeed * dt / _visualRadius * Mathf.Rad2Deg, 360f);
        float steer = _vehicle.SteerAngleDeg;

        for (int i = 0; i < 4; i++)
        {
            WheelContact c = contacts[i];
            float target = WheelCentreHeight(anchors[i].y, c.Grounded, c.GroundDistance,
                                             _vehicle.SuspensionRestLength, _visualRadius);
            // Snap up instantly (never sink into the road), ease down (droop) to hide step jitter.
            _height[i] = target > _height[i] ? target : Mathf.Lerp(_height[i], target, k);
            Transform w = _wheels[i];
            Vector3 p = w.localPosition;
            w.localPosition = new Vector3(p.x, _height[i], p.z);
            float yaw = i < 2 ? steer : 0f;
            w.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.AngleAxis(_spinDeg, Vector3.right)
                              * _baseRotation[i];
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform hit = FindDeep(child, name);
            if (hit != null) return hit;
        }
        return null;
    }
}
