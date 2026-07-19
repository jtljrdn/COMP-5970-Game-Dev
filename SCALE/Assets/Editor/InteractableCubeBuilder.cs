using System.IO;
using UnityEngine;
using UnityEditor;

// Procedurally draws a flat, low-poly "calibration cube" texture (panel border,
// corner bolts, a nested-square + outward-arrow scale emblem) plus a matching
// emission map, and wires them into a URP Lit material. The generated look reads
// as "this is the object you shrink/expand and carry", and its cyan accents match
// the observation window + Trim_Cyan palette. Nothing photographic, so it stays
// consistent with the flat-shaded art direction.
//
// Run: Tools -> SCALE -> Build Interactable Cube Material.
// The material appears in Assets/Materials/ - assign it to your Scalable cubes.
public static class InteractableCubeBuilder
{
    const int S = 512;                 // texture resolution
    const string TexFolder = "Assets/Textures/Generated";
    const string MatFolder = "Assets/Materials";

    static readonly Color32 Cyan = Hex("2BB8FF");
    static readonly Color32 White = new Color32(255, 255, 255, 255);
    static readonly Color32 Black = new Color32(0, 0, 0, 255);

    [MenuItem("Tools/SCALE/Build Interactable Cube Material")]
    public static void Build()
    {
        var baseMap = new Color32[S * S];
        var emisMap = new Color32[S * S];
        Fill(baseMap, Hex("8A9099")); // mid-gray panel, contrasts light walls
        Fill(emisMap, Black);

        // Panel border + rivets.
        int inset = 34;
        FrameRect(baseMap, inset, inset, S - 1 - inset, S - 1 - inset, 12, Hex("2C3036"));
        foreach (var (bx, by) in Corners(inset + 24))
            FillCircle(baseMap, bx, by, 10, Hex("4A4F57"));

        // Inner glowing edge (drawn to both albedo and emission).
        int edge = inset + 30;
        FrameRect(baseMap, edge, edge, S - 1 - edge, S - 1 - edge, 5, Cyan);
        FrameRect(emisMap, edge, edge, S - 1 - edge, S - 1 - edge, 5, White);

        // Scale emblem: two nested squares = "the object", + outward arrows = "resizable".
        int c = S / 2;
        DrawGlowFrame(baseMap, emisMap, c - 74, c - 74, c + 74, c + 74, 9);
        DrawGlowFrame(baseMap, emisMap, c - 40, c - 40, c + 40, c + 40, 6);

        int reach = 128, wing = 34, th = 11;
        DrawArrow(baseMap, emisMap, c, c - reach, 0, -1, wing, th); // up
        DrawArrow(baseMap, emisMap, c, c + reach, 0, 1, wing, th);  // down
        DrawArrow(baseMap, emisMap, c - reach, c, -1, 0, wing, th); // left
        DrawArrow(baseMap, emisMap, c + reach, c, 1, 0, wing, th);  // right

        // Save textures.
        EnsureFolder(TexFolder);
        string basePath = $"{TexFolder}/InteractableCube_BaseMap.png";
        string emisPath = $"{TexFolder}/InteractableCube_Emission.png";
        WritePng(baseMap, basePath);
        WritePng(emisMap, emisPath);
        AssetDatabase.Refresh();

        // Build material.
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Interactable Cube Builder",
                "URP Lit shader not found - is this a URP project?", "OK");
            return;
        }
        EnsureFolder(MatFolder);
        string matPath = $"{MatFolder}/InteractableCube.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath) ?? new Material(shader);
        mat.shader = shader;
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath));
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.4f);
        mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(emisPath));
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", (Color)Cyan * 2.0f);

        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
            AssetDatabase.CreateAsset(mat, matPath);
        else
            EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Selection.activeObject = mat;
        EditorGUIUtility.PingObject(mat);
        Debug.Log($"Built '{matPath}'. Assign it to your Scalable cubes. Note: ScaleTool tints " +
                  "_BaseColor on highlight, which multiplies over this texture - leave _BaseColor white.");
    }

    // --- drawing helpers ---

    static void DrawGlowFrame(Color32[] baseMap, Color32[] emisMap, int x0, int y0, int x1, int y1, int t)
    {
        FrameRect(baseMap, x0, y0, x1, y1, t, Cyan);
        FrameRect(emisMap, x0, y0, x1, y1, t, White);
    }

    // Arrowhead at (tipX,tipY) pointing along (dx,dy); drawn to albedo (cyan) + emission (white).
    static void DrawArrow(Color32[] baseMap, Color32[] emisMap, int tipX, int tipY, int dx, int dy, int wing, int th)
    {
        // Perpendicular direction for the two wings.
        int px = -dy, py = dx;
        int bx = tipX - dx * wing, by = tipY - dy * wing; // base of the arrowhead
        int w1x = bx + px * wing, w1y = by + py * wing;
        int w2x = bx - px * wing, w2y = by - py * wing;
        DrawLine(baseMap, tipX, tipY, w1x, w1y, th, Cyan);
        DrawLine(baseMap, tipX, tipY, w2x, w2y, th, Cyan);
        DrawLine(emisMap, tipX, tipY, w1x, w1y, th, White);
        DrawLine(emisMap, tipX, tipY, w2x, w2y, th, White);
    }

    static void Fill(Color32[] a, Color32 c)
    {
        for (int i = 0; i < a.Length; i++) a[i] = c;
    }

    static void SetPx(Color32[] a, int x, int y, Color32 c)
    {
        if (x < 0 || x >= S || y < 0 || y >= S) return;
        a[y * S + x] = c;
    }

    static void FillRect(Color32[] a, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                SetPx(a, x, y, c);
    }

    static void FrameRect(Color32[] a, int x0, int y0, int x1, int y1, int t, Color32 c)
    {
        FillRect(a, x0, y0, x1, y0 + t - 1, c);         // top
        FillRect(a, x0, y1 - t + 1, x1, y1, c);         // bottom
        FillRect(a, x0, y0, x0 + t - 1, y1, c);         // left
        FillRect(a, x1 - t + 1, y0, x1, y1, c);         // right
    }

    static void FillCircle(Color32[] a, int cx, int cy, int r, Color32 c)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (dx * dx + dy * dy <= r * r) SetPx(a, cx + dx, cy + dy, c);
    }

    static void DrawLine(Color32[] a, int x0, int y0, int x1, int y1, int thickness, Color32 c)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        int r = Mathf.Max(1, thickness / 2);
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            FillCircle(a, x, y, r, c);
        }
    }

    static System.Collections.Generic.IEnumerable<(int, int)> Corners(int m)
    {
        yield return (m, m);
        yield return (S - 1 - m, m);
        yield return (m, S - 1 - m);
        yield return (S - 1 - m, S - 1 - m);
    }

    // --- io helpers ---

    static void WritePng(Color32[] pixels, string assetPath)
    {
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
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
