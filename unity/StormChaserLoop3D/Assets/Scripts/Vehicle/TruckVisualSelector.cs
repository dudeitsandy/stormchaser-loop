using System;
using UnityEngine;

/// <summary>
/// Chooses the truck's look: the cube truck (<c>TruckVisual</c>) or the Blender hero pickup
/// (<c>TruckVisualBlender</c>, S7-07). The cube truck stays the default until the G2 gate passes; the
/// Blender truck is opt-in with URL <c>?truck=blender</c> or desktop <c>-truck=blender</c>, or by ticking
/// <see cref="_useBlender"/>. Visual only: physics, colliders and the camcorder mount are unchanged.
/// </summary>
public class TruckVisualSelector : MonoBehaviour
{
    private const string Arg = "truck=blender";

    [Tooltip("Show the Blender pickup instead of the cube truck (G2 decision).")]
    [SerializeField] private bool _useBlender;

    /// <summary>True when the Blender pickup is showing.</summary>
    public bool UsingBlender { get; private set; }

    private void Awake() => Apply(_useBlender || Requested());

    /// <summary>Shows the Blender pickup (true) or the cube truck (false).</summary>
    public void Apply(bool blender)
    {
        Transform cube = transform.Find("TruckVisual");
        Transform pickup = transform.Find("TruckVisualBlender");
        if (pickup == null) blender = false;
        if (cube != null) cube.gameObject.SetActive(!blender);
        if (pickup != null) pickup.gameObject.SetActive(blender);
        UsingBlender = blender;
    }

    private static bool Requested()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg == "-" + Arg) return true;
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token == Arg) return true;
        return false;
    }
}
