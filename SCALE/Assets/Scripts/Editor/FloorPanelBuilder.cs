using System.IO;
using UnityEngine;
using UnityEditor;

// Draws a SEAMLESS low-poly floor-panel texture (light-gray panels, darker grid
// seams, corner bolts) and wires it into a URP Lit material. Flat/stylized to match
// the palette - just gives the floor a panel grid so it isn't a featureless void.
// Seams are drawn only on the top + left edges (and bolts only at the 0,0 corner) so
// the texture tiles perfectly: neighbouring tiles supply the other half of each seam
// and the other three quarters of each bolt.
//
// Run: Tools -> SCALE -> Build Floor Panel Material.
// Assign FloorPanel.mat to your floor, then add AutoTexture.cs and set unitsPerTile
// (~1.5) so one panel covers ~1.5m instead of stretching.
public static class FloorPanelBuilder
{
    const int S = 512;
    const string TexFolder = "Assets/Textures/Generated";
    const string MatFolder = "Assets/Materials";

    [MenuItem("Tools/SCALE/Build Floor Panel Material")]
    public static void Build()
    {
        var map = new Color32[S * S];
        Fill(map, Hex("5C626B"));            // panel face - mid gray, not black

        int seam = 10;
        Color32 seamCol = Hex("363B42");     // dark grout line
        Color32 bevel = Hex("6E747C");       // subtle lighter bevel just inside the seam

        // Seam on top + left edges only -> uniform grid when tiled.
        FillRect(map, 0, 0, S - 1, seam - 1, seamCol);         // top seam
        FillRect(map, 0, 0, seam - 1, S - 1, seamCol);         // left seam
        FillRect(map, 0, seam, S - 1, seam + 1, bevel);        // bevel below top seam
        FillRect(map, seam, 0, seam + 1, S - 1, bevel);        // bevel right of left seam

        // Bolt at the 0,0 corner -> full bolt at every grid intersection once tiled.
        FillCircle(map, 0, 0, 16, Hex("2C3036"));
        FillCircle(map, 0, 0, 8, Hex("454A52"));

        EnsureFolder(TexFolder);
        string basePath = $"{TexFolder}/FloorPanel_BaseMap.png";
        WritePng(map, basePath);
        AssetDatabase.Refresh();

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Floor Panel Builder",
                "URP Lit shader not found - is this a URP project?", "OK");
            return;
        }
        EnsureFolder(MatFolder);
        string matPath = $"{MatFolder}/FloorPanel.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath) ?? new Material(shader);
        mat.shader = shader;
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath));
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.2f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.5f);

        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
            AssetDatabase.CreateAsset(mat, matPath);
        else
            EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Selection.activeObject = mat;
        EditorGUIUtility.PingObject(mat);
        Debug.Log($"Built '{matPath}'. Assign to the floor + add AutoTexture.cs (unitsPerTile ~1.5) so it tiles.");
    }

    // --- raster + io helpers ---

    static void Fill(Color32[] a, Color32 c) { for (int i = 0; i < a.Length; i++) a[i] = c; }

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

    static void FillCircle(Color32[] a, int cx, int cy, int r, Color32 c)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (dx * dx + dy * dy <= r * r) SetPx(a, cx + dx, cy + dy, c);
    }

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
