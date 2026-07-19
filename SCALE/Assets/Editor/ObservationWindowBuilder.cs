using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

// Editor tool that builds a "Glass Research" observation window from primitives:
// a clean metal frame, a tinted transparent glass pane, and a recessed dim
// "control room" alcove behind it so the player sees depth (someone watching)
// instead of the skybox. Run it from the menu, tweak in the scene, then drag
// the result into Assets/ to save it as a reusable prefab.
public static class ObservationWindowBuilder
{
    // --- Tunable geometry (metres) ---
    const float OpeningW = 3.0f;   // glass opening width
    const float OpeningH = 2.0f;   // glass opening height
    const float OpeningCenterY = 1.5f; // height of opening centre off the floor
    const float FrameThickness = 0.2f; // width of the frame bars
    const float FrameDepth = 0.3f;
    const float RoomDepth = 1.5f;  // how deep the control-room alcove recedes
    const float WallThickness = 0.1f;

    [MenuItem("Tools/SCALE/Create Observation Window")]
    public static void Create()
    {
        Material frameMat = GetOrCreateMaterial("ObsWindow_Frame", MakeMetal);
        Material glassMat = GetOrCreateMaterial("ObsWindow_Glass", MakeGlass);
        Material roomMat = GetOrCreateMaterial("ObsWindow_Interior", MakeDark);

        float yb = OpeningCenterY - OpeningH / 2f; // opening bottom
        float yt = OpeningCenterY + OpeningH / 2f; // opening top
        float halfW = OpeningW / 2f;

        var root = new GameObject("ObservationWindow");

        // --- Frame (4 bars around the opening) ---
        var frame = new GameObject("Frame");
        frame.transform.SetParent(root.transform, false);
        Bar("TopBar", frame.transform, frameMat,
            new Vector3(OpeningW + FrameThickness * 2f, FrameThickness, FrameDepth),
            new Vector3(0, yt + FrameThickness / 2f, 0));
        Bar("BottomBar", frame.transform, frameMat,
            new Vector3(OpeningW + FrameThickness * 2f, FrameThickness, FrameDepth),
            new Vector3(0, yb - FrameThickness / 2f, 0));
        Bar("LeftPost", frame.transform, frameMat,
            new Vector3(FrameThickness, OpeningH, FrameDepth),
            new Vector3(-(halfW + FrameThickness / 2f), OpeningCenterY, 0));
        Bar("RightPost", frame.transform, frameMat,
            new Vector3(FrameThickness, OpeningH, FrameDepth),
            new Vector3(halfW + FrameThickness / 2f, OpeningCenterY, 0));

        // --- Glass pane (chamber side, faces the player at -Z) ---
        var glass = Bar("Glass", root.transform, glassMat,
            new Vector3(OpeningW, OpeningH, 0.05f),
            new Vector3(0, OpeningCenterY, 0.02f));
        // No shadow from glass, and let the player collide with it.
        glass.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        // --- Control-room alcove behind the glass (open front at -Z) ---
        var room = new GameObject("ControlRoom");
        room.transform.SetParent(root.transform, false);
        float roomW = OpeningW + FrameThickness * 2f;
        float roomH = OpeningH + FrameThickness * 2f;
        float zBack = FrameDepth / 2f + RoomDepth;      // back wall
        float zMid = FrameDepth / 2f + RoomDepth / 2f;  // centre of alcove
        float halfRoomW = roomW / 2f;

        Bar("BackWall", room.transform, roomMat,
            new Vector3(roomW, roomH, WallThickness), new Vector3(0, OpeningCenterY, zBack));
        Bar("Floor", room.transform, roomMat,
            new Vector3(roomW, WallThickness, RoomDepth),
            new Vector3(0, OpeningCenterY - roomH / 2f, zMid));
        Bar("Ceiling", room.transform, roomMat,
            new Vector3(roomW, WallThickness, RoomDepth),
            new Vector3(0, OpeningCenterY + roomH / 2f, zMid));
        Bar("LeftWall", room.transform, roomMat,
            new Vector3(WallThickness, roomH, RoomDepth),
            new Vector3(-halfRoomW, OpeningCenterY, zMid));
        Bar("RightWall", room.transform, roomMat,
            new Vector3(WallThickness, roomH, RoomDepth),
            new Vector3(halfRoomW, OpeningCenterY, zMid));

        // A console silhouette so the room reads as "monitoring equipment".
        Bar("Console", room.transform, roomMat,
            new Vector3(roomW * 0.6f, 0.9f, 0.4f),
            new Vector3(0, OpeningCenterY - roomH / 2f + 0.55f, zBack - 0.4f));

        // Dim cool interior light.
        var lightGo = new GameObject("RoomLight");
        lightGo.transform.SetParent(room.transform, false);
        lightGo.transform.localPosition = new Vector3(0, OpeningCenterY + 0.4f, zMid);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.6f, 0.85f, 1f);
        light.intensity = 2.5f;
        light.range = RoomDepth * 3f;

        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();
        Debug.Log("Observation window created. Position it in a wall opening, then " +
                  "drag it into a folder in the Project window to save it as a prefab.");
    }

    // Creates a cube primitive with the given scale/position and material.
    static GameObject Bar(string name, Transform parent, Material mat, Vector3 scale, Vector3 localPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // --- Material factory: load if it already exists, otherwise build + save ---
    static Material GetOrCreateMaterial(string name, System.Action<Material> configure)
    {
        EnsureFolder("Assets/Materials");
        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("URP Lit shader not found - is this a URP project?");
            shader = Shader.Find("Standard");
        }
        var mat = new Material(shader);
        configure(mat);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void MakeMetal(Material m)
    {
        m.SetColor("_BaseColor", new Color(0.82f, 0.85f, 0.88f)); // clean off-white metal
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.6f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.65f);
    }

    static void MakeDark(Material m)
    {
        m.SetColor("_BaseColor", new Color(0.06f, 0.07f, 0.09f)); // near-black interior
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
    }

    // URP transparent glass: tinted, glossy, no shadow.
    static void MakeGlass(Material m)
    {
        m.SetColor("_BaseColor", new Color(0.55f, 0.8f, 1f, 0.22f));
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.95f);

        // Switch URP Lit into Transparent (alpha blend) mode.
        m.SetFloat("_Surface", 1f);   // 0 = Opaque, 1 = Transparent
        m.SetFloat("_Blend", 0f);     // 0 = Alpha
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("ShadowCaster", false);
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slash = folder.LastIndexOf('/');
        string parent = folder.Substring(0, slash);
        string leaf = folder.Substring(slash + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
