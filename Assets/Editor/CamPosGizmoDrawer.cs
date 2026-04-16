using UnityEditor;
using UnityEngine;

/// <summary>
/// Disegna automaticamente nella Scene View il rettangolo di visuale
/// per ogni GameObject il cui nome inizia con "CamPos".
/// Nessun componente da aggiungere — funziona sempre.
/// </summary>
[InitializeOnLoad]
public static class CamPosGizmoDrawer
{
    static CamPosGizmoDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sv)
    {
        var allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var go in allObjects)
        {
            if (!go.name.StartsWith("CamPos")) continue;
            bool selected = Selection.Contains(go);
            DrawCameraFrame(go.transform.position, selected);
        }
    }

    static void DrawCameraFrame(Vector3 worldPos, bool selected)
    {
        Camera mainCam = Camera.main;
        float size   = mainCam != null ? mainCam.orthographicSize : 5f;
        float aspect = mainCam != null ? mainCam.aspect : 16f / 9f;
        float leftEx = 0f;

        if (mainCam != null)
        {
            // Controlla se c'è l'espansione asimmetrica
            var asymm = mainCam.GetComponent<CameraAsymmetricOrtho>();
            if (asymm != null) leftEx = asymm.leftExpansion;
        }

        float halfH = size;
        float halfW = size * aspect;
        Vector3 pos = new Vector3(worldPos.x, worldPos.y, 0f);

        Vector3 tl = pos + new Vector3(-(halfW + leftEx),  halfH, 0f);
        Vector3 tr = pos + new Vector3( halfW,             halfH, 0f);
        Vector3 br = pos + new Vector3( halfW,            -halfH, 0f);
        Vector3 bl = pos + new Vector3(-(halfW + leftEx), -halfH, 0f);

        Color yellow = new Color(1f, 0.92f, 0f, 1f);

        // Outline — sempre visibile, più brillante se selezionato
        Handles.color = selected
            ? yellow
            : new Color(1f, 0.92f, 0f, 0.4f);

        Handles.DrawLine(tl, tr);
        Handles.DrawLine(tr, br);
        Handles.DrawLine(br, bl);
        Handles.DrawLine(bl, tl);

        // Diagonali incrociate al centro (come mirino)
        float cross = 0.5f;
        Handles.DrawLine(pos + Vector3.left * cross, pos + Vector3.right * cross);
        Handles.DrawLine(pos + Vector3.down * cross, pos + Vector3.up   * cross);

        // Fill semi-trasparente quando selezionato
        if (selected)
        {
            Handles.color = new Color(1f, 0.92f, 0f, 0.06f);
            Handles.DrawSolidRectangleWithOutline(
                new Rect(pos.x - halfW - leftEx, pos.y - halfH, halfW + halfW + leftEx, halfH * 2f),
                new Color(1f, 0.92f, 0f, 0.06f),
                Color.clear
            );
        }

        // Label con il nome
        GUIStyle style = new GUIStyle();
        style.normal.textColor = selected ? yellow : new Color(1f, 0.92f, 0f, 0.6f);
        style.fontSize = 11;
        style.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;

        // Trova il GameObject per prendere il nome
        Handles.Label(pos + new Vector3(0f, halfH + 0.3f, 0f),
            FindCamPosName(worldPos) ?? "CamPos", style);
    }

    static string FindCamPosName(Vector3 pos)
    {
        var allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var go in allObjects)
        {
            if (go.name.StartsWith("CamPos") &&
                Vector3.Distance(go.transform.position, pos) < 0.01f)
                return go.name;
        }
        return null;
    }
}
