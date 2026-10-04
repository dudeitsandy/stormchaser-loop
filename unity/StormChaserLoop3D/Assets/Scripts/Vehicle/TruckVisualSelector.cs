using System;
using UnityEngine;

/// <summary>
/// Chooses the truck's look: the Blender hero pickup (<c>TruckVisualBlender</c>, S7-07; default since G2
/// passed 2026-10-03) or the old cube truck (<c>TruckVisual</c>), kept as a fallback via URL
/// <c>?truck=cube</c> or desktop <c>-truck=cube</c>. Visual only: physics, colliders and the camcorder mount
/// are unchanged.
/// </summary>
public class TruckVisualSelector : MonoBehaviour
{
    private const string CubeArg = "truck=cube";

    [Tooltip("Show the Blender pickup (G2 passed 2026-10-03). Untick for the old cube truck.")]
    [SerializeField] private bool _useBlender = true;

    /// <summary>True when the Blender pickup is showing.</summary>
    public bool UsingBlender { get; private set; }

    private void Awake() => Apply(_useBlender && !CubeRequested());

    /// <summary>Shows the Blender pickup (true) or the cube truck (false).</summary>
    public void Apply(bool blender)
    {
        Transform cube = transform.Find("TruckVisual");
        Transform pickup = transform.Find("TruckVisualBlender");
        if (pickup == null) blender = false;
        if (cube != null) cube.gameObject.SetActive(!blender);
        if (pickup != null) pickup.gameObject.SetActive(blender);
        UsingBlender = blender;
        // Only the pickup has separate wheels; they follow the suspension so the truck never sinks visually.
        var wheels = GetComponent<WheelVisuals>();
        if (wheels == null) wheels = gameObject.AddComponent<WheelVisuals>();
        wheels.Bind(blender ? pickup : null);
    }

    private static bool CubeRequested()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg == "-" + CubeArg) return true;
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token == CubeArg) return true;
        return false;
    }
}
