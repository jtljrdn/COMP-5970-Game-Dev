using UnityEngine;
using UnityEditor;

// One-click generator for a cohesive flat-shaded low-poly material palette for the
// S.C.A.L.E. test facility. Low-poly art gets its look from flat solid colours plus
// a few emissive accents (not photo textures), so this builds a small reusable set
// that walls, floors, and trim all share. Colours are matched to the observation
// window materials so everything reads as one clean "Glass Research" facility.
//
// Run: Tools -> SCALE -> Build Material Palette. Materials land in Assets/Materials/Palette/.
public static class PaletteBuilder
{
    const string Folder = "Assets/Materials/Palette";

    // sRGB colours (what you'd pick in the colour swatch).
    struct Entry
    {
        public string name;
        public Color color;
        public float metallic;
        public float smoothness;
        public Color emission; // black = no emission
        public float emissionIntensity;
    }

    static readonly Entry[] Palette =
    {
        // Walls / structure
        Mat("Wall_Light",  Hex("D2D6DB"), 0.0f, 0.35f),            // off-white panels
        Mat("Wall_Accent", Hex("9AA1AA"), 0.1f, 0.30f),            // darker gray paneling
        Mat("Wall_Dark",   Hex("3A3F47"), 0.1f, 0.25f),           // recessed / shadow panels
        Mat("Floor",       Hex("23262B"), 0.2f, 0.20f),           // charcoal floor
        Mat("Metal_Trim",  Hex("D1D4D8"), 0.7f, 0.65f),           // matches ObsWindow_Frame

        // Emissive accents (the "calibrated / high-tech" reads)
        Emissive("Trim_Cyan",    Hex("2BB8FF"), Hex("2BB8FF"), 3.0f), // matches glass tint
        Emissive("Trim_White",   Hex("EAF6FF"), Hex("EAF6FF"), 2.0f), // light strips
        Emissive("Warning_Amber",Hex("FF8A1E"), Hex("FF8A1E"), 3.0f), // caution
        Emissive("Warning_Red",  Hex("FF3B30"), Hex("FF3B30"), 3.0f), // hazard / lose state
    };

    [MenuItem("Tools/SCALE/Build Material Palette")]
    public static void Build()
    {
        EnsureFolder(Folder);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Palette Builder",
                "URP Lit shader not found - is this a URP project?", "OK");
            return;
        }

        int created = 0, updated = 0;
        foreach (var e in Palette)
        {
            string path = $"{Folder}/{e.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = mat == null;
            if (isNew) mat = new Material(shader) { name = e.name };
            else mat.shader = shader;

            mat.SetColor("_BaseColor", e.color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", e.metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", e.smoothness);

            if (e.emission.maxColorComponent > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", e.emission * e.emissionIntensity);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            if (isNew) { AssetDatabase.CreateAsset(mat, path); created++; }
            else { EditorUtility.SetDirty(mat); updated++; }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(Folder));
        Debug.Log($"Material palette ready in {Folder}/ ({created} created, {updated} updated). " +
                  "Assign flat materials to walls/floor; use the emissive Trim_* materials on thin strip geometry.");
    }

    // --- entry helpers ---
    static Entry Mat(string name, Color color, float metallic, float smoothness) => new Entry
    {
        name = name, color = color, metallic = metallic, smoothness = smoothness,
        emission = Color.black, emissionIntensity = 0f
    };

    static Entry Emissive(string name, Color color, Color emission, float intensity) => new Entry
    {
        name = name, color = color, metallic = 0f, smoothness = 0.5f,
        emission = emission, emissionIntensity = intensity
    };

    // Hex ("D2D6DB") -> Color, treated as sRGB.
    static Color Hex(string hex)
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
