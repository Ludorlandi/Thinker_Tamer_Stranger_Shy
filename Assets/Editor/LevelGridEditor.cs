using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelGridEditor : EditorWindow
{
    // ── Settings ──────────────────────────────────────────────────────────────
    private bool  paintMode    = false;
    private bool  eraseMode    = false;
    private float gridSize     = 1f;
    private Color tileColor    = new Color(1f, 0.75f, 0.2f, 1f);
    private int   sortingOrder = 0;
    private int   selectedLayer = 8; // Ground
    private GameObject parentGO = null;

    // ── Internals ─────────────────────────────────────────────────────────────
    private Sprite    tileSprite;
    private string[]  layerNames;
    private int[]     layerIndices;
    private Vector3   lastSnapped = Vector3.positiveInfinity;   // drag dedup

    // ── Open window ───────────────────────────────────────────────────────────
    [MenuItem("Window/Level Grid Editor")]
    public static void ShowWindow() => GetWindow<LevelGridEditor>("Grid Editor");

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/WhiteSquare.png");
        BuildLayerArrays();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
    }

    void BuildLayerArrays()
    {
        var names   = new List<string>();
        var indices = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            string n = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(n)) { names.Add(n); indices.Add(i); }
        }
        layerNames   = names.ToArray();
        layerIndices = indices.ToArray();
    }

    // ── Inspector UI ──────────────────────────────────────────────────────────
    void OnGUI()
    {
        GUILayout.Label("Level Grid Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Paint mode toggle button
        Color prevBG = GUI.backgroundColor;
        GUI.backgroundColor = paintMode ? new Color(0.4f, 1f, 0.4f) : new Color(0.9f, 0.9f, 0.9f);
        if (GUILayout.Button(paintMode ? "● PAINT MODE  ON" : "○  Paint Mode OFF", GUILayout.Height(32)))
        {
            paintMode = !paintMode;
            lastSnapped = Vector3.positiveInfinity;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = prevBG;

        if (!paintMode) return;

        EditorGUILayout.Space(6);

        // Place / Erase toggle
        int modeIdx = GUILayout.Toolbar(eraseMode ? 1 : 0,
            new[] { "✏  Piazza", "✕  Cancella" }, GUILayout.Height(26));
        eraseMode = modeIdx == 1;

        EditorGUILayout.Space(8);

        // Tile settings
        tileColor    = EditorGUILayout.ColorField("Colore tile", tileColor);
        sortingOrder = EditorGUILayout.IntField("Sorting Order", sortingOrder);
        gridSize     = Mathf.Max(0.25f, EditorGUILayout.FloatField("Grid Size", gridSize));

        // Layer popup
        int layerIdx = System.Array.IndexOf(layerIndices, selectedLayer);
        layerIdx     = EditorGUILayout.Popup("Layer", layerIdx < 0 ? 0 : layerIdx, layerNames);
        selectedLayer = layerIndices[Mathf.Clamp(layerIdx, 0, layerIndices.Length - 1)];

        EditorGUILayout.Space(6);

        // Parent picker
        parentGO = (GameObject)EditorGUILayout.ObjectField("Parent", parentGO, typeof(GameObject), true);

        if (GUILayout.Button("Usa selezione corrente come Parent"))
        {
            if (Selection.activeGameObject != null)
                parentGO = Selection.activeGameObject;
        }

        EditorGUILayout.Space(8);

        // Help box
        EditorGUILayout.HelpBox(
            eraseMode
                ? "Click sinistro o trascina: cancella tile"
                : "Click sinistro o trascina: piazza tile\nClick destro: cancella tile\nCtrl+Z: annulla",
            MessageType.Info);
    }

    // ── Scene GUI ─────────────────────────────────────────────────────────────
    void OnSceneGUI(SceneView sv)
    {
        if (!paintMode) return;

        // Block normal Unity tools when in paint mode
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;

        // World position under cursor — intersezione ray con il piano z=0
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        Vector3 mouseWorld = Vector3.zero;
        if (plane.Raycast(ray, out float dist))
            mouseWorld = ray.GetPoint(dist);
        mouseWorld.z = 0f;
        Vector3 snapped = Snap(mouseWorld);

        // Draw grid overlay
        DrawGrid(sv);

        // Draw tile preview
        DrawPreview(snapped);

        // Input handling
        bool isLeftDown = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0;
        bool isRightDown = e.type == EventType.MouseDown && e.button == 1;

        if (isLeftDown)
        {
            // Dedup during drag: only act when moving to a new cell
            if (snapped != lastSnapped)
            {
                lastSnapped = snapped;
                if (eraseMode) EraseTile(snapped);
                else           PlaceTile(snapped);
            }
            e.Use();
        }
        else if (isRightDown)
        {
            EraseTile(snapped);
            lastSnapped = snapped;
            e.Use();
        }
        else if (e.type == EventType.MouseUp)
        {
            lastSnapped = Vector3.positiveInfinity;
        }

        sv.Repaint();
    }

    // ── Grid drawing ──────────────────────────────────────────────────────────
    void DrawGrid(SceneView sv)
    {
        Camera cam = sv.camera;
        float orthoH  = cam.orthographicSize;
        float orthoW  = orthoH * cam.aspect;
        Vector3 center = cam.transform.position; center.z = 0f;

        float pad  = gridSize * 2f;
        float half = gridSize * 0.5f;
        // Le righe sono ai bordi delle celle (offset di mezzo step)
        float x0   = Mathf.Floor((center.x - orthoW - pad) / gridSize) * gridSize - half;
        float x1   = Mathf.Ceil ((center.x + orthoW + pad) / gridSize) * gridSize + half;
        float y0   = Mathf.Floor((center.y - orthoH - pad) / gridSize) * gridSize - half;
        float y1   = Mathf.Ceil ((center.y + orthoH + pad) / gridSize) * gridSize + half;

        Handles.color = new Color(0.6f, 0.8f, 1f, 0.35f);
        for (float x = x0; x <= x1; x += gridSize)
            Handles.DrawLine(new Vector3(x, y0, 0f), new Vector3(x, y1, 0f));
        for (float y = y0; y <= y1; y += gridSize)
            Handles.DrawLine(new Vector3(x0, y, 0f), new Vector3(x1, y, 0f));
    }

    void DrawPreview(Vector3 pos)
    {
        float h = gridSize * 0.5f;
        Rect r   = new Rect(pos.x - h, pos.y - h, gridSize, gridSize);

        Color fill    = eraseMode ? new Color(1f, 0.2f, 0.2f, 0.35f)
                                  : new Color(tileColor.r, tileColor.g, tileColor.b, 0.45f);
        Color outline = eraseMode ? new Color(1f, 0.1f, 0.1f, 1f) : Color.white;

        Handles.DrawSolidRectangleWithOutline(r, fill, outline);
    }

    // ── Tile placement ────────────────────────────────────────────────────────
    void PlaceTile(Vector3 pos)
    {
        if (TileExistsAt(pos)) return;

        var go = new GameObject("Tile");
        Undo.RegisterCreatedObjectUndo(go, "Place Tile");

        go.transform.position = pos;
        go.layer = selectedLayer;

        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite;
        sr.color  = tileColor;
        sr.sortingOrder = sortingOrder;

        var bc   = go.AddComponent<BoxCollider2D>();
        bc.size  = new Vector2(gridSize, gridSize);

        if (parentGO != null)
            go.transform.SetParent(parentGO.transform, true);

        EditorUtility.SetDirty(go);
    }

    void EraseTile(Vector3 pos)
    {
        GameObject found = FindTileAt(pos);
        if (found != null)
            Undo.DestroyObjectImmediate(found);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    Vector3 Snap(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float y = Mathf.Round(pos.y / gridSize) * gridSize;
        return new Vector3(x, y, 0f);
    }

    bool TileExistsAt(Vector3 pos) => FindTileAt(pos) != null;

    GameObject FindTileAt(Vector3 pos)
    {
        float threshold = gridSize * 0.1f;

        // Search inside parent first (faster)
        if (parentGO != null)
        {
            foreach (Transform child in parentGO.transform)
            {
                if (child.name == "Tile" && Vector3.Distance(child.position, pos) < threshold)
                    return child.gameObject;
            }
            return null;
        }

        // Fallback: search all scene objects
#pragma warning disable CS0618
        foreach (var sr in Object.FindObjectsOfType<SpriteRenderer>())
#pragma warning restore CS0618
        {
            if (sr.gameObject.name == "Tile" && Vector3.Distance(sr.transform.position, pos) < threshold)
                return sr.gameObject;
        }
        return null;
    }
}
