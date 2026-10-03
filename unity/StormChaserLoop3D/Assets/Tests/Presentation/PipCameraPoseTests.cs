using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Native EditMode pose checks; excluded from the pure managed runner.</summary>
public class PipCameraPoseTests
{
    [Test]
    public void BeforeRendering_UsesLatestGameplayPoseAndClippingWithoutReplacingPipFov()
    {
        var mainHost = new GameObject("MainPoseTest");
        var previewHost = new GameObject("PreviewPoseTest");
        var pipHost = new GameObject("PipPoseTest");
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
            typeof(PipViewfinder).GetField("_main", flags).SetValue(pip, main);
            typeof(PipViewfinder).GetField("_camera", flags).SetValue(pip, preview);
            // Model a Cinemachine update that happened after PiP's ordinary LateUpdate.
            main.transform.SetPositionAndRotation(new Vector3(12, 5, -8), Quaternion.Euler(17, 120, 0));
            main.nearClipPlane = 0.3f;
            main.farClipPlane = 950f;
            main.cullingMask = 123;
            typeof(PipViewfinder).GetMethod("BeforeFrame", flags).Invoke(pip,
                new object[] { default(ScriptableRenderContext), new List<Camera> { main, preview } });
            Assert.That(preview.transform.position, Is.EqualTo(main.transform.position));
            Assert.That(Quaternion.Angle(preview.transform.rotation, main.transform.rotation), Is.LessThan(0.01f));
            Assert.That(preview.nearClipPlane, Is.EqualTo(main.nearClipPlane));
            Assert.That(preview.farClipPlane, Is.EqualTo(main.farClipPlane));
            Assert.That(preview.cullingMask, Is.EqualTo(main.cullingMask));
            Assert.That(preview.fieldOfView, Is.EqualTo(60f));
        }
        finally
        {
            Object.DestroyImmediate(pipHost);
            Object.DestroyImmediate(previewHost);
            Object.DestroyImmediate(mainHost);
        }
    }
}
