using UnityEngine;

/// <summary>
/// Builds grass terrain + road stripe at runtime. Drop on any scene GameObject.
/// Delete the existing ground Plane after adding this component.
/// </summary>
public class TerrainSetup : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private float _terrainSize = 200f;
    [SerializeField] private Color _grassColor = new Color(0.18f, 0.49f, 0.15f);

    [Header("Road")]
    [SerializeField] private float _roadWidth = 8f;
    [SerializeField] private Color _roadColor = new Color(0.22f, 0.22f, 0.22f);

    [Header("Center Stripe")]
    [SerializeField] private float _stripeWidth = 0.3f;
    [SerializeField] private Color _stripeColor = new Color(0.95f, 0.85f, 0.1f);

    private void Awake()
    {
        // Unity's default Plane is 10×10 units, so divide desired size by 10 for scale
        CreatePlane("Terrain_Grass", _terrainSize, _terrainSize, _grassColor, 0f, withCollider: true);
        CreatePlane("Terrain_Road", _terrainSize, _roadWidth, _roadColor, 0.01f, withCollider: false);
        CreatePlane("Terrain_Stripe", _terrainSize, _stripeWidth, _stripeColor, 0.02f, withCollider: false);
    }

    private void CreatePlane(string objName, float sizeX, float sizeZ, Color color, float yOffset, bool withCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = objName;
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(0f, yOffset, 0f); // world space — parent Y ignored
        go.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);

        if (!withCollider)
            Destroy(go.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = objName + "_Mat";
        mat.SetColor("_BaseColor", color);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
