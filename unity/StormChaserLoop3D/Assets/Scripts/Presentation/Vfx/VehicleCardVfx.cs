using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Unity.Profiling;

/// <summary>Pooled tire smoke, landing dust, impact sparks and authoritative boost exhaust, plus style feedback.</summary>
public sealed class VehicleCardVfx : MonoBehaviour
{
    [SerializeField, Range(8, 48)] private int _smokePool = 24;
    [SerializeField, Range(4, 24)] private int _sparkPool = 12;
    [SerializeField, Range(2, 16)] private int _boostPool = 8;
    [SerializeField, Min(0f)] private float _boostRate = 30f;
    [SerializeField] private Vector3 _exhaustOffset = new Vector3(0f, 0.1f, -1f);
    [SerializeField, Min(0f)] private float _smokeRate = 16f;
    [SerializeField, Min(0f)] private float _minimumSlideSpeed = 2f;
    [SerializeField, Min(0.1f)] private float _landingThreshold = 2f;
    [SerializeField, Min(0.1f)] private float _popupSeconds = 1.2f;
    private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("Presentation.VehicleVfx.Update");
    private static readonly ProfilerMarker CameraMarker = new ProfilerMarker("Presentation.VehicleVfx.Camera");
    private struct Card
    {
        internal Transform Transform;
        internal Vector3 Velocity;
        internal float Age, Life, Size, Roll;
        internal bool Active, Spark, Boost;
    }
    private Card[] _cards;
    private CardVfxAssets _assets;
    private PlayerVehicle _vehicle;
    private System.Random _random;
    private float _emission, _boostEmission, _popupAge = float.PositiveInfinity;
    private int _cursor, _wheel;
    private Label _popup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install);
    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() == null || Object.FindAnyObjectByType<VehicleCardVfx>() != null) return;
        new GameObject(nameof(VehicleCardVfx)).AddComponent<VehicleCardVfx>();
    }
    private void Awake()
    {
        _assets = new CardVfxAssets();
        _random = new System.Random(702);
        int smoke = Mathf.Clamp(_smokePool, 8, 48);
        int sparks = Mathf.Clamp(_sparkPool, 4, 24);
        _cards = new Card[smoke + sparks + Mathf.Clamp(_boostPool, 2, 16)];
        for (int i = 0; i < _cards.Length; i++)
        {
            bool boost = i >= smoke + sparks;
            bool spark = i >= smoke && !boost;
            _cards[i].Spark = spark;
            _cards[i].Boost = boost;
            _cards[i].Transform = _assets.Create(transform, boost ? CardVfxAssets.Shape.Flame :
                spark ? CardVfxAssets.Shape.Streak : CardVfxAssets.Shape.Dust,
                boost ? "BoostExhaust" : spark ? "ImpactSpark" : "VehicleDust");
            if (_cards[i].Transform != null) _cards[i].Transform.gameObject.SetActive(false);
        }
        _assets.SetColor(CardVfxAssets.Shape.Dust, new Color(0.72f, 0.68f, 0.58f, 0.38f));
        _assets.SetColor(CardVfxAssets.Shape.Streak, new Color(1f, 0.7f, 0.18f, 0.95f));
        _assets.SetColor(CardVfxAssets.Shape.Flame, new Color(1f, 0.45f, 0.12f, 0.85f));
        var root = PresentationOverlay.Create(transform, "VehicleStyleUI", 30);
        if (root == null) return;
        _popup = PresentationOverlay.Label("", 24, new Color(1f, 0.8f, 0.3f));
        _popup.style.position = Position.Absolute;
        _popup.style.top = Length.Percent(44);
        _popup.style.left = _popup.style.right = 0;
        _popup.style.opacity = 0;
        root.Add(_popup);
    }
    private void Start() => _vehicle = FindAnyObjectByType<PlayerVehicle>();
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnCamera;
        GameEvents.Landed += Landed;
        GameEvents.Tossed += Tossed;
        GameEvents.VehicleImpact += Impact;
        GameEvents.StyleEvent += Style;
        GameEvents.RunStarted += Clear;
        GameEvents.RunEnded += End;
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnCamera;
        GameEvents.Landed -= Landed;
        GameEvents.Tossed -= Tossed;
        GameEvents.VehicleImpact -= Impact;
        GameEvents.StyleEvent -= Style;
        GameEvents.RunStarted -= Clear;
        GameEvents.RunEnded -= End;
        Clear();
    }
    private bool Running => _vehicle != null && _vehicle.InputEnabled && Time.timeScale > 0f;
    private float Random01() => (float)_random.NextDouble();
    private void Update()
    {
        using var sample = UpdateMarker.Auto();
        if (!Running) { Clear(); return; }
        float dt = Time.deltaTime;
        for (int i = 0; i < _cards.Length; i++)
        {
            ref Card card = ref _cards[i];
            if (!card.Active) continue;
            card.Age += dt;
            if (card.Age >= card.Life)
            {
                card.Active = false;
                card.Transform.gameObject.SetActive(false);
                continue;
            }
            card.Transform.position += card.Velocity * dt;
            card.Velocity += (card.Spark ? Vector3.down * 9f : _vehicle.CurrentWind * 0.15f) * dt;
            float t = card.Age / card.Life;
            float size = card.Size * Mathf.Clamp01((1f - t) * 4f) * (card.Spark ? 1f : 0.6f + t);
            card.Transform.localScale = card.Boost ? new Vector3(size * 3f, size * 0.8f, 1f) :
                card.Spark ? new Vector3(size * 2f, size * 0.15f, 1f) : new Vector3(size, size * 0.7f, 1f);
        }
        float skid = VehicleFeedbackLevels.Skid(_vehicle.State, _vehicle.GroundedWheels, _vehicle.CurrentSpeed, _vehicle.SlipAngle);
        if (skid > 0f && Mathf.Abs(_vehicle.CurrentSpeed) >= _minimumSlideSpeed)
        {
            _emission += dt * _smokeRate * skid;
            int count = Mathf.Min(_cards.Length, Mathf.FloorToInt(_emission));
            _emission -= Mathf.Floor(_emission);
            for (int i = 0; i < count; i++)
            {
                if (TryRearGround(out Vector3 point)) Spawn(point + Vector3.up * 0.1f, Vector3.up * 0.4f, false, 0.7f);
            }
        }
        else _emission = 0;
        if (_vehicle.BoostActive)
        {
            _boostEmission += dt * Mathf.Max(0f, _boostRate);
            int count = Mathf.Min(_cards.Length, Mathf.FloorToInt(_boostEmission));
            _boostEmission -= Mathf.Floor(_boostEmission);
            Vector3 position = _vehicle.transform.TransformPoint(_exhaustOffset);
            Vector3 velocity = -_vehicle.transform.forward * 7f + _vehicle.CurrentWind * 0.1f;
            for (int i = 0; i < count; i++) Spawn(position, velocity, false, 0.4f, true);
        }
        else _boostEmission = 0;
        if (_popup != null)
        {
            _popupAge += Time.unscaledDeltaTime;
            _popup.style.opacity = Mathf.Clamp01((_popupSeconds - _popupAge) / 0.25f);
        }
    }
    private bool TryRearGround(out Vector3 point)
    {
        WheelLayout layout = _vehicle.Data != null ? _vehicle.Data.Layout : WheelLayout.Pickup;
        float side = (_wheel++ % 2 == 0 ? -1f : 1f) * layout.HalfTrack;
        Vector3 origin = _vehicle.transform.TransformPoint(new Vector3(side, layout.AnchorY + 0.2f, layout.RearAxleZ));
        if (Physics.Raycast(origin, -_vehicle.transform.up, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore)
            && !hit.collider.transform.IsChildOf(_vehicle.transform)) { point = hit.point; return true; }
        point = origin;
        return false;
    }
    private void Spawn(Vector3 point, Vector3 velocity, bool spark, float size, bool boost = false)
    {
        for (int attempt = 0; attempt < _cards.Length; attempt++)
        {
            int index = _cursor;
            _cursor = (_cursor + 1) % _cards.Length;
            ref Card card = ref _cards[index];
            if (card.Active || card.Spark != spark || card.Boost != boost || card.Transform == null) continue;
            card.Transform.position = point;
            card.Velocity = velocity;
            card.Age = 0;
            card.Life = boost ? 0.15f + Random01() * 0.1f : spark ? 0.2f + Random01() * 0.25f : 0.6f + Random01() * 0.4f;
            card.Size = size;
            card.Roll = Random01() * 360f;
            card.Active = true;
            card.Transform.localScale = Vector3.zero;
            card.Transform.gameObject.SetActive(true);
            return;
        }
    }
    private void Burst(Vector3 point, int count, bool sparks, float strength)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random01() * Mathf.PI * 2f;
            Vector3 velocity = new Vector3(Mathf.Cos(angle), 0.2f + Random01(), Mathf.Sin(angle)) * strength;
            Spawn(point, velocity, sparks, sparks ? 0.18f + strength * 0.03f : 0.7f + strength * 0.12f);
        }
    }
    private void Landed(float speed)
    {
        if (!Running || Mathf.Abs(speed) < _landingThreshold) return;
        if (TryRearGround(out var point)) Burst(point + Vector3.up * 0.12f, 8, false, Mathf.Clamp(Mathf.Abs(speed) * 0.3f, 0.5f, 4f));
    }
    private void Tossed()
    {
        if (!Running) return;
        Burst(_vehicle.transform.position, 6, false, 2f);
    }
    private void Impact(ImpactInfo impact)
    {
        if (!Running || impact.Speed < 2f) return;
        Burst(impact.Point, Mathf.Clamp(3 + impact.HpLoss * 3, 3, 12), true, Mathf.Clamp(impact.Speed * 0.25f, 1f, 6f));
        Burst(impact.Point, 3, false, 1f);
    }
    private void Style(StyleKind kind, float amount)
    {
        if (!Running || _popup == null) return;
        string duration = Mathf.Max(0f, amount).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s";
        _popup.text = kind == StyleKind.NearMiss ? "NEAR MISS" : (kind == StyleKind.Drift ? "DRIFT " : "AIR ") + duration;
        _popupAge = 0;
    }
    private void End(RunSummary summary) => Clear();
    private void Clear()
    {
        _emission = 0;
        _boostEmission = 0;
        _popupAge = float.PositiveInfinity;
        if (_popup != null) _popup.style.opacity = 0;
        if (_cards == null) return;
        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].Active = false;
            if (_cards[i].Transform != null) _cards[i].Transform.gameObject.SetActive(false);
        }
    }
    private void OnCamera(ScriptableRenderContext context, Camera camera)
    {
        using var sample = CameraMarker.Auto();
        if (_cards == null) return;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (!_cards[i].Active) continue;
            float roll = _cards[i].Roll;
            if (_cards[i].Boost)
            {
                Vector3 bearing = camera.transform.InverseTransformDirection(_cards[i].Velocity);
                roll = Mathf.Atan2(bearing.y, bearing.x) * Mathf.Rad2Deg;
            }
            CardVfxAssets.FaceCamera(_cards[i].Transform, camera, roll);
        }
    }
    private void OnDestroy()
    {
        if (_cards != null) foreach (var card in _cards) if (card.Transform != null) Destroy(card.Transform.gameObject);
        _assets?.Dispose();
    }
}
