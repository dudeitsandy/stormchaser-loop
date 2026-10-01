using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Shared runtime panel setup for non-interactive presentation overlays.</summary>
internal static class PresentationOverlay
{
    internal static VisualElement Create(Transform parent, string name, int order)
    {
        var panel = Resources.Load<PanelSettings>("UI/PanelSettings");
        if (panel == null)
        {
            Debug.LogWarning($"{name}: missing Resources/UI/PanelSettings.");
            return null;
        }
        var host = new GameObject(name);
        host.SetActive(false);
        host.transform.SetParent(parent, false);
        var document = host.AddComponent<UIDocument>();
        document.panelSettings = panel;
        document.sortingOrder = order;
        host.SetActive(true);
        var root = document.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        root.style.position = Position.Absolute;
        root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
        return root;
    }

    internal static Label Label(string text, int size, Color color)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.style.fontSize = size;
        label.style.color = color;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        return label;
    }
}
