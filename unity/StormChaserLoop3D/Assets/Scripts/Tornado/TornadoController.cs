using UnityEngine;

public class TornadoController : DisasterEntity
{
    [SerializeField] private float _despawnX = 60f;

    private TornadoData TornadoData => _data as TornadoData;
    public float ConeScale => TornadoData?.ConeScale ?? 1f;

    private void Update()
    {
        transform.Translate(Vector3.right * MoveSpeed * Time.deltaTime, Space.World);

        if (transform.position.x >= _despawnX)
            Destroy(gameObject);
    }
}
