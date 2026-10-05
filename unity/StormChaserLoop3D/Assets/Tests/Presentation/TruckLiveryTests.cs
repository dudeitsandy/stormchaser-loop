using NUnit.Framework;
using UnityEngine;

/// <summary>Native EditMode coverage of material ownership, exact stock restoration and idempotent switches.</summary>
public class TruckLiveryTests
{
    [Test]
    public void KtvrSwitch_RestoresStockAndReusesItsRuntimePaintAndMarkings()
    {
        var root = new GameObject("LiveryTestTruck");
        var pickup = new GameObject("TruckVisualBlender"); pickup.transform.SetParent(root.transform, false);
        var cab = new GameObject("Cab"); cab.transform.SetParent(pickup.transform, false);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        Assert.That(shader, Is.Not.Null);
        var stock = new Material(shader) { name = "StockBody" };
        var renderer = cab.AddComponent<MeshRenderer>(); renderer.sharedMaterial = stock;
        try
        {
            TruckLivery.Apply(null);
            var livery = root.AddComponent<TruckLivery>();
            // EditMode never calls Awake on an added MonoBehaviour; invoke it as play mode would (Claude, 2026-10-04).
            typeof(TruckLivery).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(livery, null);
            TruckLivery.Apply(TruckLivery.KtvrId);
            var paint = renderer.sharedMaterial;
            Assert.That(paint, Is.Not.SameAs(stock));
            Assert.That(stock.GetColor("_BaseColor"), Is.Not.EqualTo(paint.GetColor("_BaseColor")), "Shared stock material must not be recolored.");
            var markings = pickup.transform.Find("KTVRRuntimeMarkings");
            Assert.That(markings, Is.Not.Null);
            Assert.That(markings.childCount, Is.EqualTo(3));
            TruckLivery.Apply("unknown");
            Assert.That(renderer.sharedMaterial, Is.SameAs(stock));
            Assert.That(markings.gameObject.activeSelf, Is.False);
            TruckLivery.Apply(TruckLivery.KtvrId);
            Assert.That(renderer.sharedMaterial, Is.SameAs(paint));
            Assert.That(markings.childCount, Is.EqualTo(3));
            TruckLivery.Apply(null);
            Assert.That(renderer.sharedMaterial, Is.SameAs(stock));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(stock); TruckLivery.Apply(null); }
    }
}
