using UnityEngine;

/// <summary>
/// The roof-mounted camcorder: the pose every photo is taken and scored from, and the pose the viewfinder
/// (PiP, Codex lane) renders (Andy 2026-10-03: "cab cam, zoomed"). It sits on the cab roof, aims along the
/// main camera's flattened yaw (so Storm Cam and orbit aim it, and over-the-shoulder shots still work), and
/// is stabilised: truck pitch and roll never tilt it. Its horizontal field of view matches the AimScore
/// cone (±AimHalfAngle), so a subject inside the viewfinder scores and a centered one is PERFECT.
/// </summary>
public class CamcorderMount : MonoBehaviour
{
    [Tooltip("Mount point in truck-local space (cab roof).")]
    [SerializeField] private Vector3 _mountLocal = new Vector3(0f, 1f, 0.25f);
    [Tooltip("Upward tilt in degrees, to fit tall funnels. Cosmetic: AimScore is measured flat.")]
    [SerializeField] private float _tiltUp = 6f;
    [Tooltip("Camera whose yaw aims the camcorder. Empty = the main camera, found once in Start.")]
    [SerializeField] private Transform _aimCamera;

    /// <summary>Viewfinder aspect (320×240).</summary>
    public const float Aspect = 4f / 3f;

    /// <summary>Horizontal field of view in degrees: the full AimScore cone.</summary>
    public float HorizontalFov => 2f * ScoringSystem.AimHalfAngle;

    /// <summary>Vertical field of view in degrees for a 4:3 viewfinder (Camera.fieldOfView).</summary>
    public float VerticalFov => VerticalFovFor(HorizontalFov, Aspect);

    private void Start()
    {
        if (_aimCamera == null && Camera.main != null) _aimCamera = Camera.main.transform;
    }

    /// <summary>Current camcorder position and rotation.</summary>
    public void GetPose(out Vector3 position, out Quaternion rotation)
    {
        Vector3 aimForward = _aimCamera != null ? _aimCamera.forward : transform.forward;
        position = transform.TransformPoint(_mountLocal);
        rotation = PoseRotation(aimForward, transform.forward, _tiltUp);
    }

    /// <summary>
    /// Stabilised camcorder rotation: the aim camera's flattened yaw, tilted up by <paramref name="tiltUp"/>
    /// degrees, never rolled. Falls back to <paramref name="fallbackForward"/> when the aim is vertical.
    /// </summary>
    public static Quaternion PoseRotation(Vector3 aimForward, Vector3 fallbackForward, float tiltUp)
    {
        Vector3 flat = ChaseCameraMath.Flat(aimForward);
        if (flat.sqrMagnitude < 1e-6f) flat = ChaseCameraMath.Flat(fallbackForward);
        if (flat.sqrMagnitude < 1e-6f) flat = Vector3.forward;
        return Quaternion.Euler(-tiltUp, ChaseCameraMath.Yaw(flat), 0f);
    }

    /// <summary>Vertical FOV that gives <paramref name="horizontalFov"/> at <paramref name="aspect"/> (w / h).</summary>
    public static float VerticalFovFor(float horizontalFov, float aspect)
    {
        float halfH = horizontalFov * 0.5f * Mathf.Deg2Rad;
        return 2f * Mathf.Atan(Mathf.Tan(halfH) / aspect) * Mathf.Rad2Deg;
    }
}
