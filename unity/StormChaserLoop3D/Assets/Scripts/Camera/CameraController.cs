using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 6f, -10f);
    [SerializeField] private float _positionDamping = 0.15f;

    [Header("Look")]
    [SerializeField] private float _lookAheadDistance = 8f;

    [Header("FOV Pulse")]
    [SerializeField] private float _baseFov = 70f;
    [SerializeField] private float _maxFovBoost = 5f;
    [SerializeField] private float _fovTriggerDistance = 40f;
    [SerializeField] private float _fovLerpSpeed = 3f;

    private Camera _camera;
    private Vector3 _velocity;
    private float _currentFov;
    private DisasterEntity[] _entities = new DisasterEntity[0];
    private float _entityRefreshTimer;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _currentFov = _baseFov;
        _camera.fieldOfView = _baseFov;
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        FollowTarget();
        UpdateFov();
    }

    private void FollowTarget()
    {
        Vector3 desiredPos = _target.TransformPoint(_offset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, _positionDamping);

        Vector3 lookAt = _target.position + _target.forward * _lookAheadDistance;
        transform.LookAt(lookAt);
    }

    private void UpdateFov()
    {
        _entityRefreshTimer -= Time.deltaTime;
        if (_entityRefreshTimer <= 0f)
        {
            _entities = Object.FindObjectsByType<DisasterEntity>(FindObjectsSortMode.None);
            _entityRefreshTimer = 0.5f;
        }

        float nearestDist = _fovTriggerDistance + 1f;
        foreach (DisasterEntity e in _entities)
        {
            if (e == null) continue;
            float d = Vector3.Distance(_target.position, e.transform.position);
            if (d < nearestDist) nearestDist = d;
        }

        float t = 1f - Mathf.Clamp01(nearestDist / _fovTriggerDistance);
        float targetFov = _baseFov + _maxFovBoost * t;
        _currentFov = Mathf.Lerp(_currentFov, targetFov, Time.deltaTime * _fovLerpSpeed);
        _camera.fieldOfView = _currentFov;
    }
}
