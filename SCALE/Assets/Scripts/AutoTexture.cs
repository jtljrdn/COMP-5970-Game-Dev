using UnityEngine;

// Keeps a texture's tiling proportional to the object's world scale so the
// greybox pattern stays the same size across differently-scaled surfaces
// instead of stretching. Runs in the editor and on load.
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class AutoTexture : MonoBehaviour
{
    public float unitsPerTile = 1f;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Update()
    {
        // Keeps tiling correct while you drag the scale handles in the editor.
        if (!Application.isPlaying)
        {
            Apply();
        }
    }

    private void Apply()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null || unitsPerTile <= 0f)
        {
            return;
        }

        Vector3 scale = transform.lossyScale;

        // Use the two largest dimensions so each face reads correctly for
        // floors (X,Z), and walls (X,Y or Z,Y) alike.
        float u = Mathf.Max(scale.x, scale.z);
        float v = Mathf.Max(scale.y, Mathf.Min(scale.x, scale.z));

        Material material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        if (material == null)
        {
            return;
        }

        material.mainTextureScale = new Vector2(u / unitsPerTile, v / unitsPerTile);
    }
}
