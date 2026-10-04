using UnityEngine;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

/// <summary>Zoomed roof-mounted photo preview sharing the authoritative camcorder scoring pose.</summary>
public sealed class PipViewfinder : MonoBehaviour
{
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.Pip.Update");
    [SerializeField] private int _textureWidth = 320;
    [SerializeField] private int _textureHeight = 240;
    [SerializeField, Min(0.01f)] private float _nearClip = 0.3f;
    private PlayerVehicle _vehicle;
    private CamcorderMount _mount;
    private Camera _main;
    private Camera _camera;
    private RenderTexture _texture;
    private VisualElement _frame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install); // AfterSceneLoad alone skips reloads

    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() != null && Object.FindAnyObjectByType<PipViewfinder>() == null)
            new GameObject(nameof(PipViewfinder)).AddComponent<PipViewfinder>();
    }
    private void Start()
    {
        _vehicle = FindAnyObjectByType<PlayerVehicle>();
        _mount = _vehicle != null ? _vehicle.GetComponent<CamcorderMount>() : null;
        if (_mount == null)
        {
            Debug.LogWarning("PipViewfinder: CamcorderMount is required for the photo-scoring pose; preview disabled.");
            return;
        }
        var main = Camera.main;
        _main = main;
        var root = PresentationOverlay.Create(transform, "ViewfinderUI", 10);
        if (root == null || _vehicle == null || main == null) return;
        _texture = new RenderTexture(Mathf.Max(32, _textureWidth), Mathf.Max(32, _textureHeight), 24)
            { name = "PhotoPreview", antiAliasing = 1 };
        _texture.Create();
        var host = new GameObject("PhotoPreviewCamera");
        host.transform.SetParent(transform, false);
        _camera = host.AddComponent<Camera>();
        _camera.CopyFrom(main);
        _camera.targetTexture = _texture;
        _camera.rect = new Rect(0, 0, 1, 1);
        _camera.usePhysicalProperties = false;
        _camera.fieldOfView = _mount.VerticalFov;
        _camera.aspect = CamcorderMount.Aspect;
        _camera.nearClipPlane = Mathf.Max(0.01f, _nearClip);
        _camera.allowHDR = false;
        _camera.allowMSAA = false;
        _camera.enabled = false;
        var data = _camera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = false;
        data.renderShadows = false;
        _frame = new VisualElement { pickingMode = PickingMode.Ignore };
        _frame.style.position = Position.Absolute;
        _frame.style.right = 24;
        _frame.style.bottom = 24;
        _frame.style.width = 256;
        _frame.style.maxWidth = Length.Percent(30);
        _frame.style.backgroundColor = new Color(0.02f, 0.03f, 0.04f, 0.9f);
        _frame.Add(PresentationOverlay.Label("VIEWFINDER", 12, Color.white));
        var preview = new Image { image = _texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
        var footage = new VisualElement { pickingMode = PickingMode.Ignore };
        footage.style.position = Position.Relative;
        footage.style.height = 192;
        footage.style.overflow = Overflow.Hidden;
        // Keep footage and overlays at the texture's aspect ratio on smaller panels as well.
        footage.RegisterCallback<GeometryChangedEvent>(evt =>
            footage.style.height = evt.newRect.width * _texture.height / _texture.width);
        preview.style.position = Position.Absolute;
        preview.style.left = preview.style.right = preview.style.top = preview.style.bottom = 0;
        preview.style.width = Length.Percent(100);
        preview.style.height = Length.Percent(100);
        footage.Add(preview);
        _frame.Add(footage);
        gameObject.AddComponent<CamcorderLens>().Initialize(_camera, _texture, footage, preview);
        var crosshair = PresentationOverlay.Label("+", 26, new Color(1f, 0.75f, 0.15f));
        crosshair.style.position = Position.Absolute;
        crosshair.style.left = crosshair.style.right = 0;
        crosshair.style.top = Length.Percent(40);
        footage.Add(crosshair);
        root.Add(_frame);
    }
    private void LateUpdate()
    {
        using var sample = UpdateMarker.Auto();
        if (_camera == null || _vehicle == null) return;
        bool active = _vehicle.InputEnabled && _mount != null;
        _camera.enabled = active;
        _frame.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        SyncPose();
    }
    private void OnEnable() => RenderPipelineManager.beginContextRendering += BeforeFrame;
    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= BeforeFrame;
        if (_camera != null) _camera.enabled = false;
        if (_frame != null) _frame.style.display = DisplayStyle.None;
    }
    // Sample after all LateUpdates, including Cinemachine, before either camera is rendered.
    private void BeforeFrame(ScriptableRenderContext context, List<Camera> cameras) => SyncPose();
    private void SyncPose()
    {
        if (_camera == null || _main == null || _mount == null) return;
        _mount.GetPose(out Vector3 position, out Quaternion rotation);
        _camera.transform.SetPositionAndRotation(position, rotation);
        _camera.fieldOfView = _mount.VerticalFov;
        _camera.aspect = CamcorderMount.Aspect;
        _camera.nearClipPlane = Mathf.Max(0.01f, _nearClip);
        _camera.farClipPlane = _main.farClipPlane;
        _camera.cullingMask = _main.cullingMask;
    }
    private void OnDestroy()
    {
        if (_camera != null) _camera.targetTexture = null;
        if (_texture != null) { _texture.Release(); Destroy(_texture); }
    }
}
