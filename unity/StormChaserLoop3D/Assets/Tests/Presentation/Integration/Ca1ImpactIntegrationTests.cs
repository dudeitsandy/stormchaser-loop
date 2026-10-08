using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>CA-1: real scenery collisions must reach the installed presentation in a running scene.</summary>
public sealed class Ca1ImpactIntegrationTests
{
    private sealed class ScriptedInput : IVehicleInput
    {
        public VehicleInputFrame Frame;
        public VehicleInputFrame Read() => Frame;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        var empty = SceneManager.CreateScene("Ca1Empty");
        SceneManager.SetActiveScene(empty);
        if (SceneManager.GetSceneByName("ArtTest").isLoaded)
            yield return SceneManager.UnloadSceneAsync("ArtTest");
    }

    [UnityTest]
    public IEnumerator SolidScenery_ProducesImpactSparksAndStopsTruck(
        [Values("RuralSilo", "RuralPole")] string propName)
    {
        SceneManager.LoadScene("ArtTest");
        yield return new WaitForSecondsRealtime(4f);
        Object.FindAnyObjectByType<RunManager>().StartRun();
        yield return null;
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var vfx = Object.FindAnyObjectByType<VehicleCardVfx>();
        Assert.IsNotNull(vfx);
        GameObject prop = GameObject.Find(propName);
        Assert.IsNotNull(prop);
        Vector3 target = prop.transform.position;
        Vector3 direction = (target - truck.transform.position);
        direction.y = 0f;
        direction.Normalize();
        truck.Teleport(new Vector3(target.x, truck.transform.position.y, target.z) - direction * 7f,
            Quaternion.LookRotation(direction));
        yield return new WaitForFixedUpdate();
        var impacts = new List<ImpactInfo>();
        System.Action<ImpactInfo> onImpact = info => impacts.Add(info);
        GameEvents.VehicleImpact += onImpact;
        bool sparksSeen = false;
        try
        {
            truck.GetComponent<Rigidbody>().linearVelocity = direction * 15f;
            for (int i = 0; i < 75; i++)
            {
                yield return new WaitForFixedUpdate();
                if (!sparksSeen && vfx.GetComponentsInChildren<Renderer>()
                    .Any(renderer => renderer.gameObject.name == "ImpactSpark" && renderer.enabled))
                {
                    sparksSeen = true;
                }
            }
            float past = Vector3.Dot(truck.transform.position - target, direction);
            Debug.Log($"[CA1] {propName}: impacts={impacts.Count}, sparks={sparksSeen}, past={past:F2}");
            Assert.That(impacts.Count, Is.GreaterThan(0), "A real collision must raise VehicleImpact.");
            Assert.That(sparksSeen, Is.True, "The installed VFX must display sparks for the real impact.");
            Assert.That(past, Is.LessThan(0f), "The truck must stay on the approach side of the solid prop.");
        }
        finally
        {
            GameEvents.VehicleImpact -= onImpact;
        }
    }

    [UnityTest]
    public IEnumerator RealDriftAndNearMiss_DisplayStyleLabels()
    {
        SceneManager.LoadScene("ArtTest");
        yield return new WaitForSecondsRealtime(4f);
        Object.FindAnyObjectByType<RunManager>().StartRun();
        yield return null;
        var truck = Object.FindAnyObjectByType<PlayerVehicle>();
        var vfx = Object.FindAnyObjectByType<VehicleCardVfx>();
        var label = vfx.GetComponentInChildren<UIDocument>().rootVisualElement.Q<Label>();
        var input = new ScriptedInput();
        truck.InputSource = input;
        truck.Teleport(new Vector3(-50f, truck.transform.position.y, 0f), Quaternion.LookRotation(Vector3.right));
        bool driftSeen = false, nearMissSeen = false;
        System.Action<StyleKind, float> onStyle = (kind, amount) =>
        {
            Debug.Log($"[CA1] style={kind}, amount={amount:F2}, label={label.text}");
            if (kind == StyleKind.Drift) driftSeen |= label.text.StartsWith("DRIFT ");
            if (kind == StyleKind.NearMiss) nearMissSeen |= label.text == "NEAR MISS";
        };
        GameEvents.StyleEvent += onStyle;
        try
        {
            input.Frame = new VehicleInputFrame { Throttle = 1f };
            yield return new WaitForSeconds(3f);
            input.Frame = new VehicleInputFrame { Throttle = 1f, Steer = 1f, Handbrake = true };
            yield return new WaitForSeconds(2f);
            input.Frame = new VehicleInputFrame { Throttle = 1f, Steer = -1f };
            yield return new WaitForSeconds(1f);
            input.Frame = new VehicleInputFrame { Brake = 1f };
            yield return new WaitForSeconds(3f);
            Assert.That(driftSeen, Is.True, "Ending a real sustained slide must display DRIFT.");
            input.Frame = default;
            DisasterEntity storm = null;
            for (int i = 0; i < 600 && storm == null; i++)
            {
                storm = DisasterEntity.Active.FirstOrDefault(entity => entity.DamageRadius > 0f);
                if (storm == null) yield return new WaitForSeconds(0.1f);
            }
            Assert.IsNotNull(storm, "The real director must supply a live disaster.");
            Vector3 origin = storm.transform.position;
            float radius = storm.DamageRadius;
            Vector3 outward = (truck.transform.position - origin).normalized;
            outward.y = 0f;
            outward.Normalize();
            Vector3 start = origin + outward * (radius + 3f);
            start.y = truck.transform.position.y;
            truck.Teleport(start, Quaternion.LookRotation(outward));
            yield return new WaitForFixedUpdate();
            truck.GetComponent<Rigidbody>().linearVelocity = outward * 22f;
            for (int i = 0; i < 75 && !nearMissSeen; i++) yield return new WaitForFixedUpdate();
            Assert.That(nearMissSeen, Is.True, "Leaving the real disaster near-miss band must display NEAR MISS.");
        }
        finally { GameEvents.StyleEvent -= onStyle; }
    }
}
