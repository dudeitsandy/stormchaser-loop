using NUnit.Framework;
using UnityEngine;

/// <summary>vehicle-feel.md F10 collision side: contact speed, E12, E13, event exemption. Thresholds: VehicleModelTests. S7-06.</summary>
public class ImpactSeverityTests
{
    private static readonly VehicleFeelValues V = VehicleFeelValues.Defaults;
    private static ArchetypeParams Pickup => ArchetypeParams.Derive(Stars.Pickup, V);

    [Test]
    public void CollisionSpeed_UsesOnlyTheNormalComponent()
    {
        // Arrange: 20 m/s glancing at 60° off the wall normal.
        Vector3 rel = Quaternion.Euler(0f, 60f, 0f) * new Vector3(0f, 0f, 20f);
        // Act
        float s = ImpactSeverity.CollisionSpeed(rel, Vector3.back);
        // Assert: 20 · cos 60° = 10 m/s, below Light → scraping a wall is free.
        Assert.AreEqual(10f, s, 1e-3f);
    }

    [TestCase(50f, 2100f, false, 50f / 1050f)]   // fence plank: tiny fraction
    [TestCase(1050f, 2100f, false, 1f)]          // half the truck's mass: full severity
    [TestCase(5000f, 2100f, false, 1f)]          // heavier than the truck: capped at 1
    [TestCase(50f, 2100f, true, 1f)]             // static geometry: full mass
    public void MassScale_FollowsE13(float otherMass, float truckMass, bool isStatic, float expected)
    {
        // Act
        float k = ImpactSeverity.MassScale(otherMass, truckMass, isStatic);
        // Assert
        Assert.AreEqual(expected, k, 1e-4f);
    }

    [Test]
    public void FenceAtFullSpeed_CostsNoHp()
    {
        // Arrange: 21.5 m/s into a 50 kg fence (E13: "plowing through fences never hurts").
        ArchetypeParams p = Pickup;
        float s = 21.5f * ImpactSeverity.MassScale(50f, p.Mass, false);
        // Act / Assert
        Assert.AreEqual(0, ImpactSeverity.HpLoss(s, p.LightImpact, p.SevereImpact, ImpactKind.Destructible));
    }

    [Test]
    public void StepAccumulator_KeepsOnlyTheHighestSeverity()
    {
        // Arrange (E12)
        var step = new ImpactSeverity.StepMax();
        // Act
        step.Offer(8f, ImpactKind.World, Vector3.zero);
        step.Offer(23f, ImpactKind.World, Vector3.one);
        step.Offer(14f, ImpactKind.Destructible, Vector3.up);
        // Assert
        Assert.IsTrue(step.Has);
        Assert.AreEqual(23f, step.Speed);
        Assert.AreEqual(Vector3.one, step.Point);
        step.Clear();
        Assert.IsFalse(step.Has);
    }

    [Test]
    public void EventObstacle_NeverCostsF10Hp()
    {
        // Arrange: ramming obstacles resolve through event-system.md, not F10.
        ArchetypeParams p = Pickup;
        // Act
        int hp = ImpactSeverity.HpLoss(30f, p.LightImpact, p.SevereImpact, ImpactKind.EventObstacle);
        // Assert
        Assert.AreEqual(0, hp);
    }
}
