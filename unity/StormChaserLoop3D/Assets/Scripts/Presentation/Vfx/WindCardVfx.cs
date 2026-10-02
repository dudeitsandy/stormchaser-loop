using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Fixed-pool wind streaks, dust and debris driven by the truck's actual wind exposure.</summary>
public sealed class WindCardVfx : MonoBehaviour
{
    [SerializeField, Range(12, 96)] private int _poolSize = 60;
    [SerializeField, Min(1f)] private float _fullWindSpeed = 15f;
    [SerializeField, Range(1f, 60f)] private float _cardsPerSecond = 30f;
    [SerializeField, Min(1f)] private float _spawnRadius = 9f;
    [SerializeField] private float _groundHeight = 0.08f;
    [SerializeField] private Color _streakColor = new Color(0.86f, 0.95f, 0.95f, 0.48f);
    [SerializeField] private Color _dustColor = new Color(0.74f, 0.6f, 0.4f, 0.35f);
    private struct Card
    {
        internal Transform Transform;
        internal float Age, Lifetime, Size, Roll;
        internal bool Active;
        internal CardVfxAssets.Shape Shape;
    }
    private PlayerVehicle _vehicle;
    private CardVfxAssets _assets;
    private Card[] _cards;
    private System.Random _random;
    private Vector3 _wind;
    private float _emissionRemainder;
    private int _cursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register() => SceneInstaller.EveryScene(Install); // AfterSceneLoad alone skips reloads

    private static void Install()
    {
        if (Object.FindAnyObjectByType<PlayerVehicle>() != null && Object.FindAnyObjectByType<WindCardVfx>() == null)
            new GameObject(nameof(WindCardVfx)).AddComponent<WindCardVfx>();
    }
    private void Awake()
    {
        _assets = new CardVfxAssets();
        _random = new System.Random(604);
        _cards = new Card[Mathf.Clamp(_poolSize, 12, 96)];
        for (int i = 0; i < _cards.Length; i++)
        {
            var shape = i % 5 == 0 ? CardVfxAssets.Shape.Debris :
                i % 3 == 0 ? CardVfxAssets.Shape.Dust : CardVfxAssets.Shape.Streak;
            _cards[i].Shape = shape;
            _cards[i].Transform = _assets.Create(transform, shape, "Wind" + shape);
            if (_cards[i].Transform != null) _cards[i].Transform.gameObject.SetActive(false);
        }
        _assets.SetColor(CardVfxAssets.Shape.Streak, _streakColor);
        _assets.SetColor(CardVfxAssets.Shape.Dust, _dustColor);
        _assets.SetColor(CardVfxAssets.Shape.Debris, new Color(0.5f, 0.32f, 0.15f, 0.9f));
    }
    private void Start() => _vehicle = FindAnyObjectByType<PlayerVehicle>();
    private void OnEnable() => RenderPipelineManager.beginCameraRendering += OnCamera;
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnCamera;
        Clear();
    }
    private void Clear()
    {
        _emissionRemainder = 0;
        if (_cards == null) return;
        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].Active = false;
            if (_cards[i].Transform != null) _cards[i].Transform.gameObject.SetActive(false);
        }
    }
    private float Random01() => (float)_random.NextDouble();
    private void Update()
    {
        if (_vehicle == null || !_vehicle.InputEnabled) { Clear(); return; }
        _wind = _vehicle.CurrentWind;
        float dt = Time.deltaTime;
        for (int i = 0; i < _cards.Length; i++)
        {
            ref Card card = ref _cards[i];
            if (!card.Active) continue;
            card.Age += dt;
            if (card.Age >= card.Lifetime)
            {
                card.Active = false;
                card.Transform.gameObject.SetActive(false);
                continue;
            }
            float t = card.Age / card.Lifetime;
            float fade = Mathf.Clamp01(t * 8f) * Mathf.Clamp01((1f - t) * 5f);
            Vector3 velocity = _wind * (card.Shape == CardVfxAssets.Shape.Streak ? 1f : 0.65f);
            if (card.Shape == CardVfxAssets.Shape.Debris) velocity.y += 1f;
            card.Transform.position += velocity * dt;
            float size = card.Size * fade;
            card.Transform.localScale = card.Shape == CardVfxAssets.Shape.Streak
                ? new Vector3(size * 3f, size * 0.35f, 1) : new Vector3(size, size * 0.7f, 1);
        }
        int emissions = WindVfxEmission.Advance(_wind.magnitude, dt, _fullWindSpeed, _cardsPerSecond, ref _emissionRemainder);
        // Consume excess requests instead of allocating or accumulating a later burst.
        for (int i = 0; i < Mathf.Min(emissions, _cards.Length); i++) Spawn();
    }
    private void Spawn()
    {
        for (int attempt = 0; attempt < _cards.Length; attempt++)
        {
            int index = _cursor;
            _cursor = (_cursor + 1) % _cards.Length;
            ref Card card = ref _cards[index];
            if (card.Active || card.Transform == null) continue;
            float angle = Random01() * Mathf.PI * 2;
            float radius = Mathf.Sqrt(Random01()) * _spawnRadius;
            Vector3 position = _vehicle.transform.position + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            position -= _wind.normalized * _spawnRadius * 0.3f;
            position.y = card.Shape == CardVfxAssets.Shape.Dust ? _groundHeight + 0.25f : _groundHeight + 0.5f + Random01() * 3.5f;
            card.Transform.position = position;
            card.Transform.localScale = Vector3.zero;
            card.Age = 0;
            card.Lifetime = 0.7f + Random01() * 0.8f;
            card.Size = card.Shape == CardVfxAssets.Shape.Streak ? 0.5f + Random01() :
                card.Shape == CardVfxAssets.Shape.Dust ? 1f + Random01() : 0.12f + Random01() * 0.18f;
            card.Roll = Random01() * 360;
            card.Active = true;
            card.Transform.gameObject.SetActive(true);
            return;
        }
    }
    private void OnCamera(ScriptableRenderContext context, Camera camera)
    {
        Vector3 bearing = camera.transform.InverseTransformDirection(_wind);
        float roll = Mathf.Atan2(bearing.y, bearing.x) * Mathf.Rad2Deg;
        for (int i = 0; i < _cards.Length; i++)
            if (_cards[i].Active)
                CardVfxAssets.FaceCamera(_cards[i].Transform, camera,
                    _cards[i].Shape == CardVfxAssets.Shape.Streak ? roll : _cards[i].Roll + _cards[i].Age * 80);
    }
    private void OnDestroy()
    {
        if (_cards != null)
            foreach (var card in _cards)
                if (card.Transform != null) Destroy(card.Transform.gameObject);
        _assets?.Dispose();
    }
}
