using NUnit.Framework;

public class VehicleDamageModelTests
{
    [Test]
    public void StartsAtMaxHealth()
    {
        var m = new VehicleDamageModel(3, 1.5f);
        Assert.AreEqual(3, m.CurrentHealth);
        Assert.IsFalse(m.IsWrecked);
    }

    [Test]
    public void Damage_IsBlockedDuringInvulnerability()
    {
        var m = new VehicleDamageModel(3, 1.5f);
        Assert.IsTrue(m.TryDamage(1, 10f));
        Assert.IsFalse(m.TryDamage(1, 11f));
        Assert.AreEqual(2, m.CurrentHealth);
        Assert.IsTrue(m.TryDamage(1, 11.5f));
        Assert.AreEqual(1, m.CurrentHealth);
    }

    [Test]
    public void ThreeHits_WreckTheDefaultTruck_AndHealthFloorsAtZero()
    {
        var m = new VehicleDamageModel(3, 0f);
        m.TryDamage(1, 0f);
        m.TryDamage(1, 1f);
        m.TryDamage(5, 2f);
        Assert.AreEqual(0, m.CurrentHealth);
        Assert.IsTrue(m.IsWrecked);
        Assert.IsFalse(m.TryDamage(1, 3f));
    }

    [Test]
    public void MaxHealth_FloorsToOne()
    {
        Assert.AreEqual(1, new VehicleDamageModel(0, 1f).MaxHealth);
    }
}

public class TornadoLifecycleTests
{
    private readonly TornadoLifecycle _l = new TornadoLifecycle(3f, 20f, 4f);

    [TestCase(0f, 0f)]
    [TestCase(1.5f, 0.5f)]
    [TestCase(3f, 1f)]
    [TestCase(15f, 1f)]
    [TestCase(25f, 0.5f)]
    [TestCase(27f, 0f)]
    public void Intensity_FollowsArc(float age, float expected)
    {
        Assert.AreEqual(expected, _l.GetIntensity(age), 0.0001f);
    }

    [Test]
    public void Phases_InOrder()
    {
        Assert.AreEqual(TornadoLifecycle.Phase.Forming, _l.GetPhase(1f));
        Assert.AreEqual(TornadoLifecycle.Phase.Mature, _l.GetPhase(10f));
        Assert.AreEqual(TornadoLifecycle.Phase.Dissipating, _l.GetPhase(24f));
        Assert.AreEqual(TornadoLifecycle.Phase.Done, _l.GetPhase(27f));
    }
}

public class SpawnTableTests
{
    private static readonly float[] Early = { 10f, 0f };
    private static readonly float[] Late = { 0f, 10f };

    [Test]
    public void AtStart_UsesEarlyWeights()
    {
        Assert.AreEqual(0, SpawnTable.Pick(Early, Late, 0f, 0.99f));
    }

    [Test]
    public void AtEnd_UsesLateWeights()
    {
        Assert.AreEqual(1, SpawnTable.Pick(Early, Late, 1f, 0.01f));
    }

    [Test]
    public void Midway_SplitsEvenly()
    {
        Assert.AreEqual(0, SpawnTable.Pick(Early, Late, 0.5f, 0.49f));
        Assert.AreEqual(1, SpawnTable.Pick(Early, Late, 0.5f, 0.51f));
    }

    [Test]
    public void AllZero_ReturnsMinusOne()
    {
        Assert.AreEqual(-1, SpawnTable.Pick(new[] { 0f }, new[] { 0f }, 0.5f, 0.5f));
    }

    [Test]
    public void MismatchedLengths_Throw()
    {
        Assert.Throws<System.ArgumentException>(() => SpawnTable.Pick(new[] { 1f }, new[] { 1f, 2f }, 0f, 0f));
    }
}

public class WindFieldTests
{
    [Test]
    public void OutsideRadius_IsCalm()
    {
        Assert.AreEqual(UnityEngine.Vector3.zero, WindField.Vortex(new UnityEngine.Vector3(30f, 0f, 0f), 20f, 5f, 5f));
    }

    [Test]
    public void AtCenter_IsCalm()
    {
        Assert.AreEqual(UnityEngine.Vector3.zero, WindField.Vortex(UnityEngine.Vector3.zero, 20f, 5f, 5f));
    }

    [Test]
    public void PullsInward_AndSwirls()
    {
        // Truck due east of the funnel: inflow points west (-x), swirl points north or south (±z).
        UnityEngine.Vector3 w = WindField.Vortex(new UnityEngine.Vector3(10f, 0f, 0f), 20f, 4f, 8f);
        Assert.Less(w.x, 0f, "Inflow should pull toward center");
        Assert.AreNotEqual(0f, w.z, "Swirl should push tangentially");
        // f = (1 - 10/20)^2 = 0.25 → inflow 1, swirl 2
        Assert.AreEqual(-1f, w.x, 0.0001f);
        Assert.AreEqual(2f, UnityEngine.Mathf.Abs(w.z), 0.0001f);
    }

    [Test]
    public void GetsStrongerCloser()
    {
        float far = WindField.Vortex(new UnityEngine.Vector3(15f, 0f, 0f), 20f, 4f, 8f).magnitude;
        float near = WindField.Vortex(new UnityEngine.Vector3(5f, 0f, 0f), 20f, 4f, 8f).magnitude;
        Assert.Greater(near, far);
    }
}
