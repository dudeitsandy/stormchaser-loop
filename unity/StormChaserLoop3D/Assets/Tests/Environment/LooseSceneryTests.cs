using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>Native EditMode checks of generated geometry and its physics configuration.</summary>
public class LooseSceneryTests
{
    private GameObject _host;

    private EnvironmentScatter Build(int count = 32)
    {
        _host = new GameObject("LooseSceneryTest");
        var scatter = _host.AddComponent<EnvironmentScatter>();
        scatter.enabled = false;
        typeof(EnvironmentScatter).GetField("_loosePropCount", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scatter, count);
        typeof(EnvironmentScatter).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scatter, null);
        Physics.SyncTransforms();
        return scatter;
    }

    private Rigidbody[] NewBodies() => _host.GetComponentsInChildren<Rigidbody>()
        .Where(body => body.name != "RuralFence").ToArray();

    [TearDown]
    public void Cleanup() { if (_host != null) Object.DestroyImmediate(_host); }

    [Test]
    public void NewProps_HaveSharedCompoundBodiesAndRequestedMasses()
    {
        Build();
        var bodies = NewBodies();
        Assert.That(bodies.Length, Is.InRange(4, 32));
        foreach (var pair in new[] { ("RuralHayBale", 250f), ("RuralMailbox", 20f), ("RuralRoadSign", 15f), ("RuralCrates", 30f) })
        {
            var matching = bodies.Where(body => body.name == pair.Item1).ToArray();
            Assert.That(matching.Length, Is.GreaterThan(0), pair.Item1);
            foreach (var body in matching)
            {
                Assert.That(body.mass, Is.EqualTo(pair.Item2));
                Assert.That(body.isKinematic, Is.False);
                Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
                Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
                Assert.That(body.GetComponent<ImpactSurface>().Kind, Is.EqualTo(ImpactKind.Destructible));
                foreach (var collider in body.GetComponentsInChildren<Collider>())
                    Assert.That(collider.attachedRigidbody, Is.SameAs(body));
            }
        }
    }

    [Test]
    public void NewPropFootprints_LeaveRoadAndSpawnClear()
    {
        Build();
        var player = Object.FindAnyObjectByType<PlayerVehicle>();
        Vector3 spawn = player != null ? player.transform.position : Vector3.zero;
        foreach (var body in NewBodies())
        {
            Assert.That(Vector3.Distance(body.position, spawn), Is.GreaterThanOrEqualTo(13f));
            foreach (var collider in body.GetComponentsInChildren<Collider>())
            {
                Bounds bounds = collider.bounds;
                Assert.That(bounds.min.z >= 10f || bounds.max.z <= -10f, Is.True, body.name);
            }
        }
    }

    [Test]
    public void ExcessCount_IsCappedAtFortyBodies()
    {
        Build(1000);
        Assert.That(NewBodies().Length, Is.InRange(4, 40));
    }

    [Test]
    public void Rebuild_ReproducesPropTypesAndTransforms()
    {
        Build();
        var first = NewBodies().Select(body => (body.name, body.position, body.rotation)).ToArray();
        Object.DestroyImmediate(_host);
        Build();
        var second = NewBodies().Select(body => (body.name, body.position, body.rotation)).ToArray();
        Assert.That(second, Is.EqualTo(first));
    }
}
