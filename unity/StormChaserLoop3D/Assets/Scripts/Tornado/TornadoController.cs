using UnityEngine;

public class TornadoController : MonoBehaviour
{
    [SerializeField] private TornadoData _data;
    [SerializeField] private float _despawnX = 60f;  // despawn when past this X position

    private void Update()
    {
        transform.Translate(Vector3.right * _data.MoveSpeed * Time.deltaTime, Space.World);

        if (transform.position.x >= _despawnX)
            Destroy(gameObject);
    }
}
