using UnityEngine;
using Unity.Cinemachine;

/// <summary>Cinemachine pipeline extension — pulses lens FOV when a DisasterEntity is near the Follow target.</summary>
[AddComponentMenu("Cinemachine/Extensions/Disaster Proximity Fov")]
public class DisasterProximityFov : CinemachineExtension
{
    [SerializeField] private float _maxFovBoost = 5f;
    [SerializeField] private float _triggerDistance = 40f;
    [SerializeField] private float _lerpSpeed = 3f;
    [SerializeField] private float _refreshInterval = 0.5f;

    private DisasterEntity[] _entities = new DisasterEntity[0];
    private float _refreshTimer;
    private float _currentBoost;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize) return;

        float targetBoost = 0f;
        Transform follow = vcam.Follow;
        if (follow != null)
        {
            RefreshEntities(deltaTime);

            float nearestDist = _triggerDistance;
            foreach (var entity in _entities)
            {
                if (entity == null) continue;
                float dist = Vector3.Distance(follow.position, entity.transform.position);
                if (dist < nearestDist) nearestDist = dist;
            }

            float t = 1f - Mathf.Clamp01(nearestDist / _triggerDistance);
            targetBoost = _maxFovBoost * t;
        }

        _currentBoost = Mathf.Lerp(_currentBoost, targetBoost, deltaTime * _lerpSpeed);

        var lens = state.Lens;
        lens.FieldOfView += _currentBoost;
        state.Lens = lens;
    }

    private void RefreshEntities(float deltaTime)
    {
        _refreshTimer -= deltaTime;
        if (_refreshTimer > 0f) return;
        _entities = FindObjectsByType<DisasterEntity>(FindObjectsSortMode.None);
        _refreshTimer = _refreshInterval;
    }
}
