using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Run-start entry point for storms. By default it hands the run to the <see cref="StormDirector"/>
/// (storm-director.md, story 006), which installs itself on this GameObject. The pre-director random timer
/// (tornadoes ahead of the player, stronger EFs later) remains behind URL <c>?spawner=legacy</c> or desktop
/// <c>-spawner=legacy</c>, or by unticking Use Director.
/// </summary>
public class DisasterSpawner : MonoBehaviour
{
    [Serializable]
    public struct RosterEntry
    {
        public TornadoData Data;
        [Tooltip("Spawn weight at run start.")] public float EarlyWeight;
        [Tooltip("Spawn weight at run end.")] public float LateWeight;
    }

    [SerializeField] private TornadoController _tornadoPrefab;
    [SerializeField] private List<RosterEntry> _roster = new List<RosterEntry>();
    [SerializeField] private SessionTimer _sessionTimer;
    [SerializeField] private Transform _player;

    [Header("Timing")]
    [SerializeField] private float _firstSpawnDelay = 1.5f;
    [SerializeField] private float _spawnInterval = 9f;
    [Tooltip("Retry delay when no tornado is alive, so the sky never stays empty for long.")]
    [SerializeField] private float _emptySkyRetry = 2f;
    [SerializeField] private int _maxConcurrent = 3;

    [Header("Placement")]
    [SerializeField] private float _minSpawnDistance = 45f;
    [SerializeField] private float _maxSpawnDistance = 70f;
    [Tooltip("Spawn within ± this many degrees of the player's facing, so tornadoes appear ahead.")]
    [SerializeField] private float _spawnArcDegrees = 70f;
    [SerializeField] private float _worldHalfExtent = 85f;

    [Header("Storm Director")]
    [Tooltip("Run storms from the Storm Director's seeded plan (default) instead of the legacy random timer.")]
    [SerializeField] private bool _useDirector = true;
    private StormDirector _director;

    /// <summary>The director running storms, or null in legacy mode.</summary>
    public StormDirector Director => _director;

    private readonly List<float> _early = new List<float>();
    private readonly List<float> _late = new List<float>();
    private float _nextSpawnTime = float.PositiveInfinity;
    private bool _running;

    private void Awake()
    {
        foreach (RosterEntry entry in _roster)
        {
            _early.Add(entry.EarlyWeight);
            _late.Add(entry.LateWeight);
        }
        if (_useDirector && !LegacyRequested())
        {
            _director = GetComponent<StormDirector>();
            if (_director == null) _director = gameObject.AddComponent<StormDirector>();
        }
    }

    /// <summary>Starts the run's storms. Called by RunManager when the run begins.</summary>
    public void Begin()
    {
        if (_director != null)
        {
            var roster = new List<TornadoData>(_roster.Count);
            foreach (RosterEntry entry in _roster) roster.Add(entry.Data);
            Vector3 origin = _player != null ? _player.position : Vector3.zero;
            _director.Begin(_tornadoPrefab, roster, origin, StormDirector.ResolveSeed());
            // The run lasts as long as the plan (compact T = 180 s), so the anchor's peak window fits the run.
            if (_sessionTimer != null && _director.Plan != null) _sessionTimer.SetDuration(_director.Plan.Duration);
            return;
        }
        _running = true;
        _nextSpawnTime = Time.time + _firstSpawnDelay;
    }

    /// <summary>Stops spawning. With the director, live cells raise Ended (run end); legacy tornadoes keep going.</summary>
    public void Stop()
    {
        _running = false;
        if (_director != null) _director.EndRun();
    }

    private static bool LegacyRequested()
    {
        const string arg = "spawner=legacy";
        foreach (string a in Environment.GetCommandLineArgs())
            if (a == "-" + arg) return true;
        foreach (string token in Application.absoluteURL.Split('?', '&', '#'))
            if (token == arg) return true;
        return false;
    }

    private void Update()
    {
        if (!_running || Time.time < _nextSpawnTime) return;

        if (CountTornadoes() < _maxConcurrent)
            Spawn();
        _nextSpawnTime = Time.time + (CountTornadoes() == 0 ? _emptySkyRetry : _spawnInterval);
    }

    private static int CountTornadoes()
    {
        int count = 0;
        foreach (DisasterEntity e in DisasterEntity.Active)
            if (e is TornadoController) count++;
        return count;
    }

    private void Spawn()
    {
        if (_tornadoPrefab == null || _roster.Count == 0) return;

        float progress = _sessionTimer != null ? _sessionTimer.Progress : 0f;
        int index = SpawnTable.Pick(_early, _late, progress, UnityEngine.Random.value);
        if (index < 0) return;

        TornadoController tornado = Instantiate(_tornadoPrefab, GetSpawnPosition(), Quaternion.identity);
        tornado.Initialize(_roster[index].Data, _player);
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 origin = _player != null ? _player.position : Vector3.zero;
        Vector3 forward = _player != null ? _player.forward : Vector3.forward;
        forward.y = 0f;

        float angle = UnityEngine.Random.Range(-_spawnArcDegrees, _spawnArcDegrees);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward.normalized;
        Vector3 pos = origin + dir * UnityEngine.Random.Range(_minSpawnDistance, _maxSpawnDistance);

        pos.x = Mathf.Clamp(pos.x, -_worldHalfExtent, _worldHalfExtent);
        pos.z = Mathf.Clamp(pos.z, -_worldHalfExtent, _worldHalfExtent);
        pos.y = 0f;
        return pos;
    }
}
