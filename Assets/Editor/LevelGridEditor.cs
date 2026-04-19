using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelGridEditor : EditorWindow
{
    // ── Tile type ─────────────────────────────────────────────────────────────
    private enum TileType { SoloSprite, SoloCollider, SpriteCollider, Prefab }
    private static readonly string[] TileTypeLabels =
        { "Solo Sprite", "Solo Collider", "Sprite + Collider", "Prefab" };

    // ── Settings ──────────────────────────────────────────────────────────────
    private bool       paintMode     = false;
    private bool       eraseMode     = false;
    private float      gridSize      = 1f;
    private Color      tileColor     = Color.white;
    private int        sortingOrder  = 0;
    private int        selectedLayer = 8; // Ground
    private GameObject parentGO      = null;
    private TileType   tileType      = TileType.SoloSprite;
    private GameObject tilePrefab    = null;   // usato solo in modalità Prefab

    // ── Internals ─────────────────────────────────────────────────────────────
    private Sprite   tileSprite;
    private Sprite[] floorSprites;
    private float    animFrameDuration = 2f;
    private string[] layerNames;
    private int[]    layerIndices;
    private Vector3  lastSnapped = Vector3.positiveInfinity;

    // ── Open window ───────────────────────────────────────────────────────────
    [MenuItem("Window/Level Grid Editor")]
    public static void ShowWindow() => GetWindow<LevelGridEditor>("Grid Editor");

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadFloorSprites();
        BuildLayerArrays();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
    }

    void LoadFloorSprites()
    {
        var all = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/FloorCube-Sheet.png");
        var list = new System.Collections.Generic.List<Sprite>();
        foreach (var a in all)
            if (a is Sprite s) list.Add(s);
        list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        floorSprites = list.ToArray();
        tileSprite = floorSprites.Length > 0 ? floorSprites[0] : null;
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

        // Paint mode toggle
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

        // ── Tipo di tile ──────────────────────────────────────────────────────
        GUILayout.Label("Tipo di blocco", EditorStyles.boldLabel);
        tileType = (TileType)GUILayout.SelectionGrid((int)tileType, TileTypeLabels, 2, GUILayout.Height(48));

        EditorGUILayout.Space(6);

        // Opzioni contestuali al tipo
        bool hasSprite   = tileType == TileType.SoloSprite    || tileType == TileType.SpriteCollider;
        bool hasCollider = tileType == TileType.SoloCollider   || tileType == TileType.SpriteCollider;
        bool isPrefab    = tileType == TileType.Prefab;

        if (isPrefab)
        {
            tilePrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", tilePrefab, typeof(GameObject), false);
            if (tilePrefab == null)
                EditorGUILayout.HelpBox("Trascina un prefab dal Project.", MessageType.Warning);
        }
        else
        {
            if (hasSprite)
            {
                tileColor         = EditorGUILayout.ColorField("Colore", tileColor);
                sortingOrder      = EditorGUILayout.IntField("Sorting Order", sortingOrder);
                animFrameDuration = EditorGUILayout.Slider("Durata frame (s)", animFrameDuration, 0.1f, 10f);
            }
            if (hasCollider && !hasSprite)
                EditorGUILayout.HelpBox("Collider invisibile (niente sprite).", MessageType.Info);
        }

        EditorGUILayout.Space(6);

        // Impostazioni comuni
        gridSize      = Mathf.Max(0.25f, EditorGUILayout.FloatField("Grid Size", gridSize));
        int layerIdx  = System.Array.IndexOf(layerIndices, selectedLayer);
        layerIdx      = EditorGUILayout.Popup("Layer", layerIdx < 0 ? 0 : layerIdx, layerNames);
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

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;

        // World position sotto il cursore
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        Vector3 mouseWorld = Vector3.zero;
        if (plane.Raycast(ray, out float dist))
            mouseWorld = ray.GetPoint(dist);
        mouseWorld.z = 0f;
        Vector3 snapped = Snap(mouseWorld);

        DrawGrid(sv);
        DrawPreview(snapped);

        bool isLeftDown  = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0;
        bool isRightDown = e.type == EventType.MouseDown && e.button == 1;

        if (isLeftDown)
        {
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
        float orthoH   = cam.orthographicSize;
        float orthoW   = orthoH * cam.aspect;
        Vector3 center = cam.transform.position; center.z = 0f;

        float pad  = gridSize * 2f;
        float half = gridSize * 0.5f;
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
        Rect r  = new Rect(pos.x - h, pos.y - h, gridSize, gridSize);

        Color fill    = eraseMode ? new Color(1f, 0.2f, 0.2f, 0.35f)
                                  : new Color(tileColor.r, tileColor.g, tileColor.b, 0.45f);
        Color outline = eraseMode ? new Color(1f, 0.1f, 0.1f, 1f) : Color.white;

        // Per collider invisibile usa un fill grigio
        if (!eraseMode && (tileType == TileType.SoloCollider))
            fill = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        Handles.DrawSolidRectangleWithOutline(r, fill, outline);
    }

    // ── Tile placement ────────────────────────────────────────────────────────
    void PlaceTile(Vector3 pos)
    {
        if (TileExistsAt(pos)) return;

        GameObject go;

        if (tileType == TileType.Prefab)
        {
            if (tilePrefab == null) return;
            go = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab);
            Undo.RegisterCreatedObjectUndo(go, "Place Tile");
            go.transform.position = pos;
        }
        else
        {
            go = new GameObject("Tile");
            Undo.RegisterCreatedObjectUndo(go, "Place Tile");
            go.transform.position = pos;

            if (tileType == TileType.SoloSprite || tileType == TileType.SpriteCollider)
            {
                var sr    = go.AddComponent<SpriteRenderer>();
                sr.sprite = tileSprite;
                sr.color  = tileColor;
                sr.sortingOrder = sortingOrder;

                if (floorSprites != null && floorSprites.Length > 1)
                {
                    var anim = go.AddComponent<FloorTileAnimator>();
                    anim.sprites = floorSprites;
                    anim.frameDuration = animFrameDuration;
                }
            }

            if (tileType == TileType.SoloCollider || tileType == TileType.SpriteCollider)
            {
                var bc  = go.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(gridSize, gridSize);
            }
        }

        go.layer = selectedLayer;

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

        if (parentGO != null)
        {
            foreach (Transform child in parentGO.transform)
            {
                if (child.name == "Tile" && Vector3.Distance(child.position, pos) < threshold)
                    return child.gameObject;
            }
            return null;
        }

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
