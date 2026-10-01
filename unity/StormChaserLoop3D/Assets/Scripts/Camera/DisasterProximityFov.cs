using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine pipeline extension — pulses lens FOV when a DisasterEntity is near the Follow target,
/// and shakes/rolls the camera in proportion to the wind acting on the target.
/// </summary>
[AddComponentMenu("Cinemachine/Extensions/Disaster Proximity Fov")]
public class DisasterProximityFov : CinemachineExtension
{
    [Header("FOV Pulse")]
    [SerializeField] private float _maxFovBoost = 5f;
    [SerializeField] private float _triggerDistance = 40f;
    [SerializeField] private float _lerpSpeed = 3f;

    [Header("Wind Shake")]
    [Tooltip("Wind speed (units/sec) that produces full shake.")]
    [SerializeField] private float _fullShakeWind = 14f;
    [Tooltip("Max positional shake in units at full wind.")]
    [SerializeField] private float _maxShakeOffset = 0.35f;
    [Tooltip("Max roll in degrees at full wind.")]
    [SerializeField] private float _maxShakeRoll = 3f;
    [SerializeField] private float _shakeFrequency = 9f;

    private float _currentBoost;
    private float _currentShake;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize) return;

        float targetBoost = 0f;
        float targetShake = 0f;
        Transform follow = vcam.Follow;
        if (follow != null)
        {
            float nearestDist = _triggerDistance;
            foreach (DisasterEntity entity in DisasterEntity.Active)
            {
                float dist = Vector3.Distance(follow.position, entity.transform.position);
                if (dist < nearestDist) nearestDist = dist;
            }

            float t = 1f - Mathf.Clamp01(nearestDist / _triggerDistance);
            targetBoost = _maxFovBoost * t;
            targetShake = Mathf.Clamp01(DisasterEntity.TotalWindAt(follow.position).magnitude / _fullShakeWind);
        }

        float dtSafe = deltaTime >= 0f ? deltaTime : Time.deltaTime;
        _currentBoost = Mathf.Lerp(_currentBoost, targetBoost, dtSafe * _lerpSpeed);
        _currentShake = Mathf.Lerp(_currentShake, targetShake, dtSafe * _lerpSpeed * 2f);

        var lens = state.Lens;
        lens.FieldOfView += _currentBoost;
        state.Lens = lens;

        if (_currentShake > 0.001f)
        {
            float time = Time.time * _shakeFrequency;
            float s = _currentShake * _currentShake; // ease in so light breezes barely register
            var offset = new Vector3(
                Mathf.PerlinNoise(time, 0.1f) * 2f - 1f,
                Mathf.PerlinNoise(0.7f, time) * 2f - 1f,
                0f) * (_maxShakeOffset * s);
            float roll = (Mathf.PerlinNoise(time * 0.5f, 3.3f) * 2f - 1f) * _maxShakeRoll * s;

            state.PositionCorrection += state.GetCorrectedOrientation() * offset;
            state.OrientationCorrection *= Quaternion.Euler(0f, 0f, roll);
        }
    }
}
