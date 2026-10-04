using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Screen-edge bearing to the nearest tornado when it is outside the gameplay view.</summary>
public sealed class OffscreenIndicator : MonoBehaviour
{
    [SerializeField] private float _edgeMargin = 75f;
    private PlayerVehicle _vehicle;
    private Camera _camera;
    private StormDirector _director;
    private VisualElement _root;
    private Label _label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install); // AfterSceneLoad alone skips reloads

    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() != null && Object.FindAnyObjectByType<OffscreenIndicator>() == null)
            new GameObject(nameof(OffscreenIndicator)).AddComponent<OffscreenIndicator>();
    }
    private void Start()
    {
        _vehicle = FindAnyObjectByType<PlayerVehicle>();
        _camera = Camera.main;
        var spawner = FindAnyObjectByType<DisasterSpawner>();
        _director = spawner != null ? spawner.Director : null;
        _root = PresentationOverlay.Create(transform, "TornadoIndicatorUI", 20);
        if (_root == null) return;
        _label = PresentationOverlay.Label("", 18, new Color(1f, 0.73f, 0.17f));
        _label.style.position = Position.Absolute;
        _label.style.width = 140;
        _label.style.height = 55;
        _label.style.backgroundColor = new Color(0.02f, 0.03f, 0.04f, 0.75f);
        _root.Add(_label);
    }
    private void LateUpdate()
    {
        if (_label == null) return;
        _label.style.display = DisplayStyle.None;
        if (_vehicle == null || _camera == null || !_vehicle.InputEnabled) return;
        TornadoController nearest = null;
        float distance = float.PositiveInfinity;
        foreach (var disaster in DisasterEntity.Active)
        {
            if (!(disaster is TornadoController tornado)) continue;
            float d = (tornado.transform.position - _vehicle.transform.position).sqrMagnitude;
            if (d < distance) { distance = d; nearest = tornado; }
        }
        if (nearest == null) return;
        Vector3 view = _camera.WorldToViewportPoint(nearest.transform.position + Vector3.up * 2f);
        if (view.z > 0 && view.x >= 0 && view.x <= 1 && view.y >= 0 && view.y <= 1) return;
        float width = _root.resolvedStyle.width;
        float height = _root.resolvedStyle.height;
        if (width <= 0 || height <= 0) return;
        Vector3 local = _camera.transform.InverseTransformPoint(nearest.transform.position);
        Vector2 direction = new Vector2(local.x, -local.y);
        if (local.z < 0) direction.y = Mathf.Abs(direction.y) + 1f;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;
        Vector2 point = IndicatorGeometry.EdgePosition(local, new Vector2(width, height), _edgeMargin);
        string arrow = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x > 0 ? "▶" : "◀") : (direction.y > 0 ? "▼" : "▲");
        // The forecast owns severity estimates; true EF must never leak through this overlay.
        string severity = "STORM";
        if (_director != null && _director.Forecast != null)
        {
            var cells = _director.LiveCells;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Tornado != nearest) continue;
                ForecastRow forecast = _director.Forecast.Evaluate(cells[i], _vehicle.transform.position);
                severity = $"{(forecast.Uncertain ? "~" : "")}EF{forecast.ShownEf}";
                break;
            }
        }
        _label.text = $"{arrow} {severity}\n{Mathf.Sqrt(distance):N0} m";
        _label.style.left = point.x - 70;
        _label.style.top = point.y - 27;
        _label.style.display = DisplayStyle.Flex;
    }
}
