using UnityEngine;

/// <summary>Panel-space geometry for an off-screen subject bearing.</summary>
public static class IndicatorGeometry
{
    /// <summary>Clamps a camera-space bearing to the inset panel edge, including subjects directly behind.</summary>
    public static Vector2 EdgePosition(Vector3 cameraLocal, Vector2 panelSize, float margin)
    {
        Vector2 direction = new Vector2(cameraLocal.x, -cameraLocal.y);
        if (cameraLocal.z < 0) direction.y = Mathf.Abs(direction.y) + 1f;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;
        float halfWidth = Mathf.Max(1, panelSize.x * 0.5f - margin);
        float halfHeight = Mathf.Max(1, panelSize.y * 0.5f - margin);
        float scale = 1f / Mathf.Max(Mathf.Abs(direction.x) / halfWidth, Mathf.Abs(direction.y) / halfHeight);
        return panelSize * 0.5f + direction * scale;
    }
}
