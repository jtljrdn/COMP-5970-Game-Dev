using UnityEditor;
using UnityEngine;

// Scene-view editing for ChamberRoom: one draggable handle per face. Grab a
// wall and pull to grow or shrink that side of the room, snapped to the grid.
// Also provides the GameObject > SCALE > Chamber creation menu.
[CustomEditor(typeof(ChamberRoom))]
public class ChamberRoomEditor : Editor
{
    private const string DefaultMaterialPath =
        "Assets/Thirdparty/Ciathyza/Gridbox Prototype Materials/Materials/URP/Prototype_512x512_Grey1.mat";

    // Face descriptor: which interior-box component the handle drives.
    private struct Face
    {
        public int axis;       // 0 = x, 1 = y, 2 = z
        public bool isMax;      // true drives interiorMax, false drives interiorMin
        public Color color;
    }

    private static readonly Face[] Faces =
    {
        new Face { axis = 0, isMax = true,  color = new Color(0.9f, 0.3f, 0.3f) }, // East
        new Face { axis = 0, isMax = false, color = new Color(0.9f, 0.3f, 0.3f) }, // West
        new Face { axis = 1, isMax = true,  color = new Color(0.4f, 0.9f, 0.4f) }, // Ceiling
        new Face { axis = 1, isMax = false, color = new Color(0.4f, 0.9f, 0.4f) }, // Floor
        new Face { axis = 2, isMax = true,  color = new Color(0.4f, 0.6f, 1.0f) }, // North
        new Face { axis = 2, isMax = false, color = new Color(0.4f, 0.6f, 1.0f) }, // South
    };

    private void OnEnable()
    {
        // Build outside the inspector-construction call stack so we never edit
        // the hierarchy at a moment Unity forbids it.
        ChamberRoom room = target as ChamberRoom;
        if (room == null || Application.isPlaying)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            // Only create surfaces that are missing. Never rebuild an already
            // built chamber on selection, or we would wipe wall materials/edits.
            if (room == null || room.IsBuilt)
            {
                return;
            }
            room.BuildWalls();
            EditorUtility.SetDirty(room);
        };
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Material To All Walls"))
        {
            foreach (Object t in targets)
            {
                ChamberRoom room = (ChamberRoom)t;
                Undo.RecordObject(room, "Apply Chamber Material");
                room.ApplyMaterialToAll();
                EditorUtility.SetDirty(room);
            }
        }

        if (GUILayout.Button("Rebuild Walls (discards per-wall edits)"))
        {
            foreach (Object t in targets)
            {
                ChamberRoom room = (ChamberRoom)t;
                Undo.RecordObject(room, "Rebuild Chamber");
                room.BuildWalls();
                EditorUtility.SetDirty(room);
            }
        }
    }

    private void OnSceneGUI()
    {
        ChamberRoom room = (ChamberRoom)target;
        Transform tr = room.transform;

        foreach (Face face in Faces)
        {
            Vector3 localCenter = FaceCenter(room, face);
            Vector3 worldPos = tr.TransformPoint(localCenter);
            Vector3 localAxis = Axis(face.axis) * (face.isMax ? 1f : -1f); // point outward
            Vector3 worldDir = tr.TransformDirection(localAxis).normalized;
            float size = HandleUtility.GetHandleSize(worldPos) * 0.18f;

            EditorGUI.BeginChangeCheck();
            Handles.color = face.color;
            Vector3 newWorld = Handles.Slider(
                worldPos, worldDir, size, Handles.CubeHandleCap, 0f);

            if (!EditorGUI.EndChangeCheck())
            {
                continue;
            }

            // Convert the dragged point back to the box coordinate it controls,
            // snap it, and clamp so the box keeps at least one grid cell.
            float raw = tr.InverseTransformPoint(newWorld)[face.axis];
            float snapped = Mathf.Round(raw / room.grid) * room.grid;

            Undo.RecordObject(room, "Resize Chamber");
            ApplyFace(room, face, snapped);
            room.Layout();
            EditorUtility.SetDirty(room);
        }
    }

    private static Vector3 FaceCenter(ChamberRoom room, Face face)
    {
        Vector3 c = room.InteriorCenter;
        Vector3 p = c;
        p[face.axis] = face.isMax ? room.interiorMax[face.axis] : room.interiorMin[face.axis];
        return p;
    }

    private static void ApplyFace(ChamberRoom room, Face face, float value)
    {
        float cell = Mathf.Max(room.grid, 0.01f);
        if (face.isMax)
        {
            Vector3 max = room.interiorMax;
            max[face.axis] = Mathf.Max(value, room.interiorMin[face.axis] + cell);
            room.interiorMax = max;
        }
        else
        {
            Vector3 min = room.interiorMin;
            min[face.axis] = Mathf.Min(value, room.interiorMax[face.axis] - cell);
            room.interiorMin = min;
        }
    }

    private static Vector3 Axis(int axis)
    {
        return axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
    }

    [MenuItem("GameObject/SCALE/Chamber", false, 10)]
    private static void CreateChamber(MenuCommand command)
    {
        GameObject go = new GameObject("Chamber");
        ChamberRoom room = go.AddComponent<ChamberRoom>();
        room.material = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
        room.BuildWalls();

        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create Chamber");
        Selection.activeObject = go;
    }
}
