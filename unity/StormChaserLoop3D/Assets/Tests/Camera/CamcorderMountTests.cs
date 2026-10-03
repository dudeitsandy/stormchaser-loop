using NUnit.Framework;
using UnityEngine;

/// <summary>Roof camcorder pose (Andy 2026-10-03: viewfinder is a zoomed cab cam aimed by the camera).</summary>
public class CamcorderMountTests
{
    [Test]
    public void VerticalFovFor_ThirtyDegreesAtFourByThree_IsAboutTwentyTwoPointSeven()
    {
        Assert.AreEqual(22.73f, CamcorderMount.VerticalFovFor(30f, 4f / 3f), 0.05f);
    }

    [Test]
    public void PoseRotation_UsesCameraYaw_IgnoresCameraPitchAndRoll()
    {
        // Arrange: camera looking down 26.6° and rolled by wind shake, yawed 40° right.
        Vector3 aim = Quaternion.Euler(26.6f, 40f, 3f) * Vector3.forward;

        // Act
        Quaternion pose = CamcorderMount.PoseRotation(aim, Vector3.forward, 6f);

        // Assert
        Vector3 euler = pose.eulerAngles;
        Assert.AreEqual(40f, euler.y, 1e-3f);
        Assert.AreEqual(0f, Mathf.DeltaAngle(0f, euler.z), 1e-3f);
        Assert.AreEqual(-6f, Mathf.DeltaAngle(0f, euler.x), 1e-3f);
    }

    [Test]
    public void PoseRotation_VerticalAim_FallsBackToTruckForward()
    {
        Quaternion pose = CamcorderMount.PoseRotation(Vector3.down, Vector3.right, 0f);
        Assert.AreEqual(90f, pose.eulerAngles.y, 1e-3f);
    }

    [Test]
    public void HorizontalFov_IsTheFullAimScoreCone()
    {
        // A subject at the viewfinder's edge sits exactly where AimScore reaches 0.
        Assert.AreEqual(0f, ScoringSystem.AimScore(ScoringSystem.AimHalfAngle), 1e-5f);
        Assert.AreEqual(30f, 2f * ScoringSystem.AimHalfAngle, 1e-5f);
    }
}
