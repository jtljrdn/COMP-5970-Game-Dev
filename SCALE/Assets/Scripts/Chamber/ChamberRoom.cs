using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

// A resizable greybox chamber shell built from ProBuilder meshes: floor,
// ceiling and four walls generated around a local-space interior box. Drag the
// wall handles in the Scene view (see ChamberRoomEditor) to size the room; each
// face moves independently, Hammer-style. Every surface is a real ProBuilderMesh
// so you can keep editing it with ProBuilder (cut a doorway, extrude a ledge)
// after the blockout.
//
// Wall creation (BuildWalls) is driven from the editor so it never runs during
// OnValidate. Geometry is rebuilt in place on resize, so the transforms stay at
// scale 1 and ProBuilder edits behave normally afterwards.
[ExecuteAlways]
[SelectionBase]
public class ChamberRoom : MonoBehaviour
{
    [Header("Interior (local space)")]
    [Tooltip("Interior box, minimum corner. The floor surface sits at min.y.")]
    public Vector3 interiorMin = new Vector3(-4f, 0f, -4f);

    [Tooltip("Interior box, maximum corner.")]
    public Vector3 interiorMax = new Vector3(4f, 4f, 4f);

    [Header("Build")]
    [Min(0.01f)]
    [Tooltip("Thickness of the shell surfaces.")]
    public float wallThickness = 0.5f;

    [Min(0.01f)]
    [Tooltip("Snap increment used when dragging the wall handles, in units.")]
    public float grid = 1f;

    [Tooltip("Turn the ceiling off while building or lighting a chamber from above.")]
    public bool includeCeiling = true;

    [Tooltip("Greybox material applied to every surface.")]
    public Material material;

    // Persisted so BuildWalls reshapes the same six meshes instead of leaking a
    // fresh set every time. Hidden because they are driven, not hand-edited.
    [SerializeField, HideInInspector] private ProBuilderMesh floor;
    [SerializeField, HideInInspector] private ProBuilderMesh ceiling;
    [SerializeField, HideInInspector] private ProBuilderMesh wallEast;
    [SerializeField, HideInInspector] private ProBuilderMesh wallWest;
    [SerializeField, HideInInspector] private ProBuilderMesh wallNorth;
    [SerializeField, HideInInspector] private ProBuilderMesh wallSouth;

    // Interior box the current geometry was last generated for. Used to skip
    // regeneration when nothing about the size changed, so selecting the chamber
    // or reloading the scene never wipes materials or hand edits on the walls.
    [SerializeField, HideInInspector] private Vector3 builtMin;
    [SerializeField, HideInInspector] private Vector3 builtMax;
    [SerializeField, HideInInspector] private float builtThickness = -1f;

    public Vector3 InteriorSize => interiorMax - interiorMin;
    public Vector3 InteriorCenter => (interiorMin + interiorMax) * 0.5f;

    // True once all six surfaces exist. The editor only builds when this is false.
    public bool IsBuilt =>
        floor && ceiling && wallEast && wallWest && wallNorth && wallSouth;

    private void OnEnable()
    {
        // Geometry is serialized, so a load only needs a no-op layout that
        // reconciles the ceiling toggle. The size guard keeps it from rebuilding.
        if (!Application.isPlaying)
        {
            Layout();
        }
    }

    private void OnValidate()
    {
        float min = Mathf.Max(grid, 0.01f);
        interiorMax = Vector3.Max(interiorMax, interiorMin + Vector3.one * min);
        if (!Application.isPlaying)
        {
            Layout();
        }
    }

    // Creates any missing surfaces, then forces a fit. Must be called from editor
    // code (menu / custom inspector), never from OnValidate.
    public void BuildWalls()
    {
        floor = EnsureSurface(floor, "Floor");
        ceiling = EnsureSurface(ceiling, "Ceiling");
        wallEast = EnsureSurface(wallEast, "Wall_East");
        wallWest = EnsureSurface(wallWest, "Wall_West");
        wallNorth = EnsureSurface(wallNorth, "Wall_North");
        wallSouth = EnsureSurface(wallSouth, "Wall_South");
        Layout(true);
    }

    // Fits the six surfaces to the current interior box. Geometry is only
    // regenerated when the box actually changed (or force is true), so routine
    // calls from selection/load leave existing walls — and their materials and
    // ProBuilder edits — untouched.
    public void Layout(bool force = false)
    {
        if (!IsBuilt)
        {
            return;
        }

        bool sizeChanged = force
            || builtMin != interiorMin
            || builtMax != interiorMax
            || !Mathf.Approximately(builtThickness, wallThickness);

        if (sizeChanged)
        {
            Vector3 c = InteriorCenter;
            Vector3 s = InteriorSize;
            float t = Mathf.Max(0.01f, wallThickness);

            // Floor and ceiling overlap the wall tops so there are no corner seams.
            Fit(floor, new Vector3(c.x, interiorMin.y - t * 0.5f, c.z), new Vector3(s.x + 2f * t, t, s.z + 2f * t));
            Fit(ceiling, new Vector3(c.x, interiorMax.y + t * 0.5f, c.z), new Vector3(s.x + 2f * t, t, s.z + 2f * t));
            Fit(wallEast, new Vector3(interiorMax.x + t * 0.5f, c.y, c.z), new Vector3(t, s.y, s.z));
            Fit(wallWest, new Vector3(interiorMin.x - t * 0.5f, c.y, c.z), new Vector3(t, s.y, s.z));
            Fit(wallNorth, new Vector3(c.x, c.y, interiorMax.z + t * 0.5f), new Vector3(s.x, s.y, t));
            Fit(wallSouth, new Vector3(c.x, c.y, interiorMin.z - t * 0.5f), new Vector3(s.x, s.y, t));

            builtMin = interiorMin;
            builtMax = interiorMax;
            builtThickness = wallThickness;
        }

        if (ceiling != null && ceiling.gameObject.activeSelf != includeCeiling)
        {
            ceiling.gameObject.SetActive(includeCeiling);
        }
    }

    // Reapplies the chamber's default material to every surface. Use this
    // deliberately (inspector button) rather than on every layout, so
    // per-wall textures are only replaced when you ask.
    public void ApplyMaterialToAll()
    {
        if (material == null)
        {
            return;
        }

        foreach (ProBuilderMesh mesh in new[] { floor, ceiling, wallEast, wallWest, wallNorth, wallSouth })
        {
            Renderer renderer = mesh != null ? mesh.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    private ProBuilderMesh EnsureSurface(ProBuilderMesh existing, string surfaceName)
    {
        if (existing != null)
        {
            return existing;
        }

        Transform found = transform.Find(surfaceName);
        if (found != null)
        {
            ProBuilderMesh reused = found.GetComponent<ProBuilderMesh>();
            if (reused != null)
            {
                return reused;
            }
        }

        ProBuilderMesh mesh = ProBuilderMesh.Create();
        mesh.name = surfaceName;
        mesh.transform.SetParent(transform, false);

        if (mesh.GetComponent<MeshCollider>() == null)
        {
            mesh.gameObject.AddComponent<MeshCollider>();
        }

        // The default material is applied once, at creation. After this the tool
        // never overwrites it, so a texture you drag onto a wall is left alone.
        Renderer renderer = mesh.GetComponent<Renderer>();
        if (material != null && renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return mesh;
    }

    private void Fit(ProBuilderMesh mesh, Vector3 localPos, Vector3 size)
    {
        if (mesh == null)
        {
            return;
        }

        mesh.transform.localPosition = localPos;
        mesh.transform.localRotation = Quaternion.identity;
        mesh.transform.localScale = Vector3.one;

        // Preserve whatever material(s) the surface carries across the rebuild,
        // since ToMesh resets the renderer's material array.
        Renderer renderer = mesh.GetComponent<Renderer>();
        Material[] keep = renderer != null ? renderer.sharedMaterials : null;

        BoxGeometry(size, out List<Vector3> positions, out List<Face> faces);
        mesh.RebuildWithPositionsAndFaces(positions, faces);
        mesh.ToMesh();
        mesh.Refresh();
        GenerateLightmapUVs(mesh);

        if (renderer != null && keep != null && keep.Length > 0 && keep[0] != null)
        {
            renderer.sharedMaterials = keep;
        }
    }

    // Rebuilding a ProBuilderMesh replaces its Mesh, and the replacement carries
    // no secondary (lightmap) UV set. A surface marked Contribute GI without one
    // still gets an atlas slot at bake time, but samples it through the tiling
    // UV1 instead -- so a 25-texel chart is addressed as a several-hundred-texel
    // swath and the wall renders a patchwork of other objects' lighting. Unwrap
    // here so a resized chamber is always bakeable.
    private static void GenerateLightmapUVs(ProBuilderMesh mesh)
    {
#if UNITY_EDITOR
        if (Application.isPlaying || mesh == null)
        {
            return;
        }

        MeshFilter filter = mesh.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            UnityEditor.Unwrapping.GenerateSecondaryUVSet(filter.sharedMesh);
        }
#endif
    }

    // A box of the given size centred on the origin, wound for outward-facing
    // normals. Four unique verts per face so ProBuilder auto-UVs and normals
    // resolve cleanly.
    private static void BoxGeometry(Vector3 size, out List<Vector3> positions, out List<Face> faces)
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;

        positions = new List<Vector3>(24)
        {
            // +X
            new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(hx, -hy, hz),
            // -X
            new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz), new Vector3(-hx, -hy, -hz),
            // +Y
            new Vector3(-hx, hy, -hz), new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz),
            // -Y
            new Vector3(-hx, -hy, hz), new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz),
            // +Z
            new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, -hy, hz),
            // -Z
            new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, -hy, -hz),
        };

        faces = new List<Face>(6);
        for (int k = 0; k < 6; k++)
        {
            int b = k * 4;
            faces.Add(new Face(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }));
        }
    }
}
