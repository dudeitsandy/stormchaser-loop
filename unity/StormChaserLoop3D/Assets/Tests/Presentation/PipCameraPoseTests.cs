using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Native EditMode pose checks; excluded from the pure managed runner.</summary>
public class PipCameraPoseTests
{
    [TestCase(0f)]
    [TestCase(120f)]
    [TestCase(250f)]
    public void BeforeRendering_UsesLatestMountPoseAndScoringConeInsteadOfMainCameraPose(float yaw)
    {
        var mainHost = new GameObject("MainPoseTest");
        var previewHost = new GameObject("PreviewPoseTest");
        var pipHost = new GameObject("PipPoseTest");
        var truckHost = new GameObject("CamcorderTruckTest");
        try
        {
            var main = mainHost.AddComponent<Camera>();
            var preview = previewHost.AddComponent<Camera>();
            main.enabled = preview.enabled = false;
            main.fieldOfView = 75f;
            preview.fieldOfView = 60f;
            var pip = pipHost.AddComponent<PipViewfinder>();
            pip.enabled = false;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var mount = truckHost.AddComponent<CamcorderMount>();
            truckHost.transform.SetPositionAndRotation(new Vector3(3, 2, 4), Quaternion.Euler(15, 35, 25));
            typeof(CamcorderMount).GetField("_aimCamera", flags).SetValue(mount, main.transform);
            typeof(PipViewfinder).GetField("_mount", flags).SetValue(pip, mount);
            typeof(PipViewfinder).GetField("_main", flags).SetValue(pip, main);
            typeof(PipViewfinder).GetField("_camera", flags).SetValue(pip, preview);
            // Model a Cinemachine update that happened after PiP's ordinary LateUpdate.
            main.transform.SetPositionAndRotation(new Vector3(12, 5, -8), Quaternion.Euler(17, yaw, 12));
            main.nearClipPlane = 0.05f;
            main.farClipPlane = 950f;
            main.cullingMask = 123;
            typeof(PipViewfinder).GetMethod("BeforeFrame", flags).Invoke(pip,
                new object[] { default(ScriptableRenderContext), new List<Camera> { main, preview } });
            mount.GetPose(out Vector3 expectedPosition, out Quaternion expectedRotation);
            Assert.That(preview.transform.position, Is.EqualTo(expectedPosition));
            Assert.That(Vector3.Distance(preview.transform.position, main.transform.position), Is.GreaterThan(1f));
            Assert.That(Quaternion.Angle(preview.transform.rotation, expectedRotation), Is.LessThan(0.01f));
            Assert.That(preview.nearClipPlane, Is.EqualTo(0.3f));
            Assert.That(preview.farClipPlane, Is.EqualTo(main.farClipPlane));
            Assert.That(preview.cullingMask, Is.EqualTo(main.cullingMask));
            Assert.That(preview.fieldOfView, Is.EqualTo(mount.VerticalFov).Within(0.001f));
            Assert.That(preview.fieldOfView, Is.EqualTo(22.73f).Within(0.02f));
            Assert.That(preview.aspect, Is.EqualTo(4f / 3f).Within(0.001f));
            float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(preview.fieldOfView * Mathf.Deg2Rad * 0.5f) * preview.aspect) * Mathf.Rad2Deg;
            Assert.That(horizontalFov, Is.EqualTo(30f).Within(0.001f));
            Assert.That(preview.transform.forward.y, Is.EqualTo(Mathf.Sin(6f * Mathf.Deg2Rad)).Within(0.001f));
            Assert.That(preview.transform.right.y, Is.EqualTo(0f).Within(0.001f), "Camcorder must not inherit truck or gameplay camera roll.");
        }
        finally
        {
            Object.DestroyImmediate(pipHost);
            Object.DestroyImmediate(previewHost);
            Object.DestroyImmediate(mainHost);
            Object.DestroyImmediate(truckHost);
        }
    }
}
