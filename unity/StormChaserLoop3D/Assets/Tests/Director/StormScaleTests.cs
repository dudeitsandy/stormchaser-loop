using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// storm-director.md F3 storm scale (story 003): AC-10, AC-10b and the TornadoData migration.
/// Values come from the six TornadoData_EF* assets, so these tests also pin the data.
/// </summary>
public class StormScaleTests
{
    private const float PickupExposure = 0.85f;
    private const float LiftCoefficient = 1.35f;
    private const float PickupTopSpeed = 21.5f;

    private static TornadoData Ef(int ef) =>
        AssetDatabase.LoadAssetAtPath<TornadoData>($"Assets/Prefabs/TornadoData_EF{ef}.asset");

    // ---------- AC-10: the table ----------

    [Test]
    public void Table_IsStrictlyMonotonicInDamageWindRadiusAndPeakWind()
    {
        for (int ef = 1; ef <= 5; ef++)
        {
            TornadoData lo = Ef(ef - 1), hi = Ef(ef);
            Assert.Greater(hi.DamageRadius, lo.DamageRadius, $"D EF{ef}");
            Assert.Greater(hi.WindRadius, lo.WindRadius, $"R EF{ef}");
            Assert.Greater(hi.PeakWind, lo.PeakWind, $"P EF{ef}");
        }
    }

    [Test]
    public void Table_EF3AndUp_OutrunTheTruckAtTheAxis_AndTracksStaySlowerThanIt()
    {
        for (int ef = 0; ef <= 5; ef++)
        {
            TornadoData d = Ef(ef);
            if (ef >= 3) Assert.Greater(d.PeakWind / PickupTopSpeed, 1f, $"EF{ef} P / v_top");
            Assert.LessOrEqual(d.MoveSpeed, 0.4f * PickupTopSpeed, $"EF{ef} track speed");
            Assert.LessOrEqual(d.MoveSpeed * 1.25f, 0.5f * PickupTopSpeed, $"EF{ef} track speed with k_H");
        }
    }

    [TestCase(3, 8.1f, 0.05f)]
    [TestCase(5, 38.3f, 0.1f)]
    public void WindSpeed_At15m_Mature_MatchesF3(int ef, float expected, float tolerance)
    {
        TornadoData d = Ef(ef);
        Assert.AreEqual(expected, StormScale.WindSpeed(d.PeakWind, d.WindRadius, 1f, 15f), tolerance);
    }

    [TestCase(0f)]
    [TestCase(0.005f)]
    [TestCase(0.0099f)]
    public void WindSpeed_BelowMinimumIntensity_IsZeroAndFinite(float intensity)
    {
        float w = StormScale.WindSpeed(62f, 70f, intensity, 0f);
        Assert.AreEqual(0f, w);
        Assert.IsFalse(float.IsNaN(w) || float.IsInfinity(w));
        Assert.AreEqual(Vector3.zero, StormScale.Wind(Vector3.right * 0.001f, 62f, 70f, intensity));
    }

    [Test]
    public void WindVector_MagnitudeEqualsWindSpeed_SplitIntoInflowAndSwirl()
    {
        Vector3 w = StormScale.Wind(new Vector3(15f, 0f, 0f), 26f, 34f, 1f);
        Assert.AreEqual(StormScale.WindSpeed(26f, 34f, 1f, 15f), w.magnitude, 0.02f);
        Assert.Less(w.x, 0f, "inflow points toward the axis");
    }

    // ---------- AC-10b: phase gating ----------

    [TestCase(0f)]
    [TestCase(5f)]
    [TestCase(20f)]
    public void Ef5_Forming_HasNoLiftAndNoDamageRadius(float d)
    {
        TornadoData e = Ef(5);
        Assert.AreEqual(0f, StormScale.Lift(StormScale.Phase.Forming, PickupExposure, e.StrengthMultiplier, 0.8f, d,
                                            e.WindRadius, LiftCoefficient));
        Assert.AreEqual(0f, StormScale.DamageRadius(StormScale.Phase.Forming, e.DamageRadius, 0.8f));
    }

    [Test]
    public void Ef5_RopeOutHalfIntensity_AxisLiftDamageAndLiftEdge()
    {
        TornadoData e = Ef(5);
        float axis = StormScale.Lift(StormScale.Phase.RopingOut, PickupExposure, e.StrengthMultiplier, 0.5f, 0f,
                                     e.WindRadius, LiftCoefficient);
        Assert.AreEqual(0.5f * 2.295f, axis, 0.01f);
        Assert.AreEqual(6.0f, StormScale.DamageRadius(StormScale.Phase.RopingOut, e.DamageRadius, 0.5f), 0.05f);
        foreach (float d in new[] { 17.5f, 20f, 35f })
            Assert.AreEqual(0f, StormScale.Lift(StormScale.Phase.RopingOut, PickupExposure, e.StrengthMultiplier, 0.5f,
                                                d, e.WindRadius, LiftCoefficient), $"d={d}");
        float quarter = StormScale.Lift(StormScale.Phase.RopingOut, PickupExposure, e.StrengthMultiplier, 0.25f, 0f,
                                        e.WindRadius, LiftCoefficient);
        Assert.Less(quarter, 0.7f, "I = 0.25 can no longer toss");
    }

    [Test]
    public void EarlyRopeOut_FromFormingI0_NoJump_ReachesZeroAt_I0TimesRope_AndNeverHarms()
    {
        TornadoData e = Ef(5);
        const float i0 = 0.4f;
        float rope = e.RopeSeconds;
        Assert.AreEqual(i0, StormScale.EarlyRopeIntensity(i0, 0f, rope), 1e-5f, "no jump");
        Assert.AreEqual(0f, StormScale.EarlyRopeIntensity(i0, i0 * rope, rope), 1e-5f);
        for (float t = 0f; t <= i0 * rope; t += 0.25f)
        {
            float i = StormScale.EarlyRopeIntensity(i0, t, rope);
            foreach (float d in new[] { 0f, 5f, 15f })
                Assert.AreEqual(0f, StormScale.Lift(StormScale.Phase.FailedTouchdown, PickupExposure, e.StrengthMultiplier,
                                                    i, d, e.WindRadius, LiftCoefficient));
            Assert.AreEqual(0f, StormScale.DamageRadius(StormScale.Phase.FailedTouchdown, e.DamageRadius, i));
        }
    }

    // ---------- Data migration ----------

    [Test]
    public void TornadoDataAssets_CarryF3Values()
    {
        float[] d = { 1.5f, 2.2f, 3.2f, 5f, 6.5f, 12f };
        float[] r = { 12f, 18f, 26f, 34f, 52f, 70f };
        float[] p = { 8f, 12f, 17f, 26f, 40f, 62f };
        float[] form = { 20f, 25f, 30f, 35f, 40f, 45f };
        float[] mature = { 30f, 40f, 50f, 60f, 75f, 90f };
        float[] rope = { 10f, 12f, 15f, 20f, 25f, 30f };
        float[] track = { 3f, 4f, 5f, 6.5f, 8f, 8f };
        float[] turn = { 24f, 20f, 16f, 12f, 9f, 7f };
        for (int ef = 0; ef <= 5; ef++)
        {
            TornadoData t = Ef(ef);
            Assert.IsNotNull(t, $"EF{ef} asset");
            Assert.AreEqual(d[ef], t.DamageRadius, 1e-4f, $"EF{ef} D");
            Assert.AreEqual(r[ef], t.WindRadius, 1e-4f, $"EF{ef} R");
            Assert.AreEqual(p[ef], t.PeakWind, 1e-4f, $"EF{ef} P");
            Assert.AreEqual(form[ef], t.FormSeconds, 1e-4f, $"EF{ef} Form");
            Assert.AreEqual(mature[ef], t.MatureSeconds, 1e-4f, $"EF{ef} Mature");
            Assert.AreEqual(rope[ef], t.RopeSeconds, 1e-4f, $"EF{ef} Rope");
            Assert.AreEqual(track[ef], t.MoveSpeed, 1e-4f, $"EF{ef} track speed");
            Assert.AreEqual(turn[ef], t.TurnRateDeg, 1e-4f, $"EF{ef} wander turn rate");
            Assert.AreEqual(d[ef] / 4.3f, t.ConeScale, 0.01f, $"EF{ef} ConeScale ≈ D / 4.3");
        }
    }

    [TestCase("_windScaleBase")]
    [TestCase("_windScalePerSqrtEF")]
    [TestCase("_baseDamageRadius")]
    [TestCase("_damageRadiusPerConeScale")]
    [TestCase("_windRadiusPerConeScale")]
    [TestCase("_playerPull")]
    public void TornadoController_OldScaleAndHomingFields_AreGone(string field)
    {
        Assert.IsNull(typeof(TornadoController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance),
                      $"{field} should be removed (F3 replaces it; no homing, Rule 5)");
    }
}
