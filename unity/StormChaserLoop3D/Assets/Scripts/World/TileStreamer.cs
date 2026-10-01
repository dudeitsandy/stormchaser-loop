using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// ADR-0004 tile streaming: keeps a (2·RenderRadius+1)² ring of 128 m tiles live around a target.
/// Tiles are pooled and built in time-sliced steps; colliders exist only within ColliderRadius,
/// because collider cooking is the expensive part on single-threaded WebGL.
/// </summary>
public class TileStreamer : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private int _seed = 1996;
    [Tooltip("Tiles rendered around the target's tile (2 → 5×5).")]
    [SerializeField] private int _renderRadius = 2;
    [Tooltip("Tiles with colliders around the target's tile (1 → 3×3).")]
    [SerializeField] private int _colliderRadius = 1;
    [Tooltip("Max milliseconds of streaming work per frame.")]
    [SerializeField] private float _frameBudgetMs = 2f;
    [SerializeField] private Material _groundMaterial;
    [SerializeField] private Material _propMaterial;
    [Tooltip("Placeholder props per tile (render + static collider load for the spike).")]
    [SerializeField] private int _propsPerTile = 14;

    private sealed class Tile
    {
        public Vector2Int Coord;
        public GameObject Root;
        public Mesh RenderMesh, ColliderMesh;
        public MeshCollider Collider;
        public bool ColliderBaked;
        public readonly List<Vector3> Grid = new List<Vector3>(TileMeshBuilder.VertsPerSide * TileMeshBuilder.VertsPerSide);
        public readonly List<Vector3> Verts = new List<Vector3>(TileMeshBuilder.RenderVertexCount);
        public readonly List<Vector3> Normals = new List<Vector3>(TileMeshBuilder.RenderVertexCount);
        public readonly List<GameObject> Props = new List<GameObject>();
    }

    private readonly Dictionary<Vector2Int, Tile> _active = new Dictionary<Vector2Int, Tile>();
    private readonly Stack<Tile> _pool = new Stack<Tile>();
    private readonly Stack<GameObject> _propPool = new Stack<GameObject>();
    private readonly List<Vector2Int> _buildQueue = new List<Vector2Int>();
    private readonly List<Vector2Int> _scratch = new List<Vector2Int>();
    private readonly Stopwatch _slice = new Stopwatch();
    private readonly DistanceComparer _byDistance = new DistanceComparer();
    private Vector2Int _lastCenter = new Vector2Int(-999, -999);
    private Mesh _cubeMesh, _cylinderMesh;

    public int Seed => _seed;
    /// <summary>Longest single frame's streaming work (ms).</summary>
    public float MaxSliceMs { get; private set; }
    /// <summary>Longest single collider cook (ms).</summary>
    public float MaxColliderBakeMs { get; private set; }
    /// <summary>Time to build the initial ring synchronously at start (ms).</summary>
    public float InitialLoadMs { get; private set; }
    public int TilesBuilt { get; private set; }
    public int QueueLength => _buildQueue.Count;
    public int ActiveTiles => _active.Count;

    /// <summary>Sets the follow target (spike driver / player vehicle).</summary>
    public void SetTarget(Transform target) => _target = target;

    private void Awake()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        _cylinderMesh = cyl.GetComponent<MeshFilter>().sharedMesh;
        Destroy(cube);
        Destroy(cyl);
    }

    private void Start()
    {
        var sw = Stopwatch.StartNew();
        Vector2Int center = CenterTile();
        UpdateRing(center);
        // Initial ring is built synchronously (counts as load time, not a hitch).
        while (_buildQueue.Count > 0)
        {
            Vector2Int c = _buildQueue[0];
            _buildQueue.RemoveAt(0);
            IEnumerator steps = BuildTile(c, sliced: false);
            while (steps.MoveNext()) { }
        }
        InitialLoadMs = (float)sw.Elapsed.TotalMilliseconds;
        StartCoroutine(StreamLoop());
    }

    private void Update()
    {
        Vector2Int center = CenterTile();
        if (center != _lastCenter) UpdateRing(center);
    }

    private Vector2Int CenterTile()
    {
        Vector3 p = _target != null ? _target.position : Vector3.zero;
        return HeightField.TileAt(p.x, p.z);
    }

    private void UpdateRing(Vector2Int center)
    {
        _lastCenter = center;

        // Recycle tiles outside the render ring.
        _scratch.Clear();
        foreach (var kv in _active)
            if (Chebyshev(kv.Key, center) > _renderRadius) _scratch.Add(kv.Key);
        foreach (Vector2Int c in _scratch) Recycle(c);

        // Queue missing tiles, nearest first (no lambdas: closures allocate every ring change).
        for (int i = _buildQueue.Count - 1; i >= 0; i--)
            if (Chebyshev(_buildQueue[i], center) > _renderRadius) _buildQueue.RemoveAt(i);
        for (int dy = -_renderRadius; dy <= _renderRadius; dy++)
        for (int dx = -_renderRadius; dx <= _renderRadius; dx++)
        {
            var c = new Vector2Int(center.x + dx, center.y + dy);
            if (HeightField.IsInMap(c) && !_active.ContainsKey(c) && !_buildQueue.Contains(c)) _buildQueue.Add(c);
        }
        _byDistance.Center = center;
        _buildQueue.Sort(_byDistance);

        // Colliders only near the target; disabling is free, cooking happens in the stream loop.
        foreach (var kv in _active)
            if (kv.Value.Collider != null)
                kv.Value.Collider.enabled = kv.Value.ColliderBaked && Chebyshev(kv.Key, center) <= _colliderRadius;
    }

    private IEnumerator StreamLoop()
    {
        while (true)
        {
            _slice.Restart();
            // Bake missing colliders first: the truck may be about to drive onto that tile.
            foreach (var kv in _active)
            {
                if (OverBudget()) break;
                Tile t = kv.Value;
                if (!t.ColliderBaked && Chebyshev(kv.Key, _lastCenter) <= _colliderRadius) BakeCollider(t);
            }
            while (_buildQueue.Count > 0 && !OverBudget())
            {
                Vector2Int c = _buildQueue[0];
                _buildQueue.RemoveAt(0);
                IEnumerator steps = BuildTile(c, sliced: true);
                while (steps.MoveNext())
                {
                    if (!OverBudget()) continue;
                    RecordSlice();
                    yield return null;
                    _slice.Restart();
                }
            }
            RecordSlice();
            yield return null;
        }
    }

    private bool OverBudget() => _slice.Elapsed.TotalMilliseconds >= _frameBudgetMs;

    private void RecordSlice()
    {
        float ms = (float)_slice.Elapsed.TotalMilliseconds;
        if (ms > MaxSliceMs) MaxSliceMs = ms;
    }

    /// <summary>Builds one tile in resumable steps (each yield = a safe point to pause).</summary>
    private IEnumerator BuildTile(Vector2Int coord, bool sliced)
    {
        Tile t = _pool.Count > 0 ? _pool.Pop() : CreateTile();
        t.Coord = coord;
        t.ColliderBaked = false;
        t.Collider.enabled = false;
        Vector2 o = HeightField.TileOrigin(coord);
        t.Root.transform.position = new Vector3(o.x, 0f, o.y);

        TileMeshBuilder.FillGrid(_seed, coord, t.Grid);
        yield return null;

        TileMeshBuilder.FillFlatShaded(t.Grid, t.Verts, t.Normals);
        yield return null;

        t.RenderMesh.Clear();
        t.RenderMesh.SetVertices(t.Verts);
        t.RenderMesh.SetNormals(t.Normals);
        t.RenderMesh.SetTriangles(TileMeshBuilder.FlatTriangles(), 0, calculateBounds: true);
        yield return null;

        PlaceProps(t);
        t.Root.SetActive(true);
        _active[coord] = t;
        TilesBuilt++;

        // The target may have moved on while this build was paused between slices.
        if (Chebyshev(coord, _lastCenter) > _renderRadius)
        {
            Recycle(coord);
            yield break;
        }

        if (!sliced && Chebyshev(coord, _lastCenter) <= _colliderRadius) BakeCollider(t);
    }

    private void BakeCollider(Tile t)
    {
        var sw = Stopwatch.StartNew();
        t.ColliderMesh.Clear();
        t.ColliderMesh.SetVertices(t.Grid);
        t.ColliderMesh.SetTriangles(TileMeshBuilder.GridTriangles(), 0, calculateBounds: true);
        t.Collider.sharedMesh = null;
        t.Collider.sharedMesh = t.ColliderMesh; // cooks the collision mesh (main thread)
        t.Collider.enabled = true;
        t.ColliderBaked = true;
        float ms = (float)sw.Elapsed.TotalMilliseconds;
        if (ms > MaxColliderBakeMs) MaxColliderBakeMs = ms;
    }

    private void PlaceProps(Tile t)
    {
        var rng = new TileRng((uint)(_seed * 73856093 ^ t.Coord.x * 19349663 ^ t.Coord.y * 83492791));
        Vector2 o = HeightField.TileOrigin(t.Coord);
        for (int i = 0; i < _propsPerTile; i++)
        {
            GameObject p = _propPool.Count > 0 ? _propPool.Pop() : CreateProp();
            bool tall = rng.NextDouble() < 0.35;
            float lx = (float)rng.NextDouble() * HeightField.TileSize, lz = (float)rng.NextDouble() * HeightField.TileSize;
            float h = HeightField.Height(_seed, o.x + lx, o.y + lz);
            Vector3 scale = tall ? new Vector3(1.5f, 6f, 1.5f) : new Vector3(3f + (float)rng.NextDouble() * 4f, 2.5f, 3f + (float)rng.NextDouble() * 4f);
            p.GetComponent<MeshFilter>().sharedMesh = tall ? _cylinderMesh : _cubeMesh;
            p.transform.SetParent(t.Root.transform, false);
            p.transform.localPosition = new Vector3(lx, h + scale.y * 0.5f, lz);
            p.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            p.transform.localScale = scale;
            p.SetActive(true);
            t.Props.Add(p);
        }
    }

    private void Recycle(Vector2Int coord)
    {
        Tile t = _active[coord];
        _active.Remove(coord);
        foreach (GameObject p in t.Props)
        {
            p.SetActive(false);
            _propPool.Push(p);
        }
        t.Props.Clear();
        t.Collider.enabled = false;
        t.Root.SetActive(false);
        _pool.Push(t);
    }

    private Tile CreateTile()
    {
        var t = new Tile { Root = new GameObject("Tile") };
        t.Root.transform.SetParent(transform, false);
        t.RenderMesh = new Mesh { name = "TileRender" };
        t.RenderMesh.MarkDynamic();
        t.ColliderMesh = new Mesh { name = "TileCollider" };
        // Collider first: added after a MeshFilter, it would auto-assign the (still empty) render mesh.
        t.Collider = t.Root.AddComponent<MeshCollider>();
        t.Root.AddComponent<MeshFilter>().sharedMesh = t.RenderMesh;
        t.Root.AddComponent<MeshRenderer>().sharedMaterial = _groundMaterial;
        t.Collider.cookingOptions = MeshColliderCookingOptions.UseFastMidphase | MeshColliderCookingOptions.CookForFasterSimulation;
        t.Collider.enabled = false;
        return t;
    }

    private GameObject CreateProp()
    {
        var p = new GameObject("Prop");
        p.AddComponent<MeshFilter>().sharedMesh = _cubeMesh;
        p.AddComponent<MeshRenderer>().sharedMaterial = _propMaterial;
        p.AddComponent<BoxCollider>();
        return p;
    }

    private static int Chebyshev(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    private sealed class DistanceComparer : IComparer<Vector2Int>
    {
        public Vector2Int Center;
        public int Compare(Vector2Int a, Vector2Int b) => Chebyshev(a, Center).CompareTo(Chebyshev(b, Center));
    }

    /// <summary>Allocation-free xorshift for deterministic per-tile placement.</summary>
    private struct TileRng
    {
        private uint _state;
        public TileRng(uint seed) { _state = seed == 0 ? 0x9E3779B9u : seed; }
        public double NextDouble()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state & 0xFFFFFF) / (double)0x1000000;
        }
    }
}
