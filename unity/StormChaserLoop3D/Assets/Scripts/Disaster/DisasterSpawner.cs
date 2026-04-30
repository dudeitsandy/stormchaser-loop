using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisasterSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> _disasterPrefabs;
    [SerializeField] private float _spawnInterval = 10f;
    [SerializeField] private float _spawnRangeZ = 30f;
    [SerializeField] private float _spawnEdgeX = -55f;
    [SerializeField] private SessionTimer _sessionTimer;

    private Coroutine _spawnLoop;

    private void Start()
    {
        _sessionTimer.OnSessionEnd.AddListener(StopSpawning);
        _spawnLoop = StartCoroutine(SpawnLoop());
    }

    private void OnDestroy()
    {
        _sessionTimer.OnSessionEnd.RemoveListener(StopSpawning);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);
            SpawnRandom();
        }
    }

    private void SpawnRandom()
    {
        // TODO S3-01: implement
    }

    private Vector3 GetSpawnPosition()
    {
        // TODO S3-01: implement
        return Vector3.zero;
    }

    private void StopSpawning()
    {
        if (_spawnLoop != null)
            StopCoroutine(_spawnLoop);
    }
}
