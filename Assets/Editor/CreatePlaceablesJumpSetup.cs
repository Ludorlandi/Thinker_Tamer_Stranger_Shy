using UnityEngine;
using UnityEditor;

/// <summary>
/// Genera automaticamente asset e prefab per i due nuovi Placeables:
///   • Placeables_JumpG — jump pad automatico (x2 salto base), si ancora su Blocco Lock
///   • Placeables_JumpA — orb che permette un salto in aria al tocco di Spazio
///
/// Esegui da: Tools > Create Jump Placeables Setup
///
/// DOPO l'esecuzione:
///   1. Trascina i prefab nella scena.
///   2. Assegna il riferimento "Player" nel campo omonimo su Placeable (JumpG)
///      e su PlaceableJumpA (JumpA).
///   3. Aggiungi JumpPadBounce e OrbActivate alla SoundLibrary asset.
/// </summary>
public static class CreatePlaceablesJumpSetup
{
    [MenuItem("Tools/Create Jump Placeables Setup")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/PlaceableTypes");

        var jumpGType = GetOrCreatePlaceableType("PlaceableType_JumpG");
        var jumpAType = GetOrCreatePlaceableType("PlaceableType_JumpA");

        CreateJumpGPrefab(jumpGType);
        CreateJumpAPrefab(jumpAType);
        CreateUnlockItemPrefab("UnlockItem_JumpG", jumpGType, new Color(1f, 0.9f, 0.1f, 1f));
        CreateUnlockItemPrefab("UnlockItem_JumpA", jumpAType, new Color(1f, 0.3f, 0.7f, 1f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[Jump Setup] Completato!\n" +
            "• Placeables_JumpG.prefab\n" +
            "• Placeables_JumpA.prefab\n" +
            "• UnlockItem_JumpG.prefab\n" +
            "• UnlockItem_JumpA.prefab\n" +
            "• PlaceableType_JumpG.asset\n" +
            "• PlaceableType_JumpA.asset\n\n" +
            "Ricorda di assegnare il campo 'Player' sui Placeable in scena."
        );
    }

    // ── PlaceableType ScriptableObject ───────────────────────────

    static PlaceableTypeSO GetOrCreatePlaceableType(string assetName)
    {
        string path = $"Assets/PlaceableTypes/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<PlaceableTypeSO>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<PlaceableTypeSO>();
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[Jump Setup] Asset creato: {path}");
        return asset;
    }

    // ── Placeables_JumpG ─────────────────────────────────────────

    static void CreateJumpGPrefab(PlaceableTypeSO placeableType)
    {
        string prefabPath = "Assets/Prefabs/Placeables_JumpG.prefab";

        // ── Root ──────────────────────────────────────────
        var root = new GameObject("Placeables_JumpG");

        // Rigidbody2D — kinematic, contatti completi per ricevere OnCollisionEnter2D
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.gravityScale   = 0f;
        rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        rb.useFullKinematicContacts = true;

        // Placeable — gestisce drag, snap su LockBlock, bob idle, hover
        var placeable = root.AddComponent<Placeable>();
        placeable.placeableType    = placeableType;
        placeable.overlapCheckMask = ~(1 << 9); // tutto tranne Player[9]

        // JumpPad — auto-salto al contatto del player dall'alto
        var jumpPad = root.AddComponent<JumpPad>();

        // ── Block (collider fisico, layer=Placeable[10]) ───
        var block = new GameObject("Block");
        block.transform.SetParent(root.transform, false);
        block.layer = 10; // Placeable

        var blockCol        = block.AddComponent<BoxCollider2D>();
        blockCol.isTrigger  = false;
        blockCol.size       = new Vector2(0.9f, 0.5f);
        blockCol.offset     = new Vector2(0f, 0.25f);

        // ── Visual (sprite placeholder — viene scalato dallo squash) ──
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        visual.transform.localScale    = new Vector3(0.9f, 0.5f, 1f);

        var visualSR        = visual.AddComponent<SpriteRenderer>();
        visualSR.sprite     = GetCircleSprite();
        visualSR.color      = new Color(1f, 0.9f, 0.1f, 1f); // giallo GD
        visualSR.sortingOrder = 1;

        // ── Key (punto di aggancio al LockBlock) ──────────
        var key = new GameObject("Key");
        key.transform.SetParent(root.transform, false);
        key.transform.localPosition = Vector3.zero; // fondo del pad = punto di ancoraggio

        var keyCol       = key.AddComponent<CircleCollider2D>();
        keyCol.isTrigger = true;
        keyCol.radius    = 0.15f;

        var keyComp = key.AddComponent<Key>();
        keyComp.parentPlaceable = placeable;

        // ── Collega i riferimenti ──────────────────────────
        placeable.keyTransform    = key.transform;
        jumpPad.visualTransform   = visual.transform;

        // ── Salva prefab ───────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[Jump Setup] Prefab creato: {prefabPath}");
    }

    // ── Placeables_JumpA ─────────────────────────────────────────

    static void CreateJumpAPrefab(PlaceableTypeSO placeableType)
    {
        string prefabPath = "Assets/Prefabs/Placeables_JumpA.prefab";

        // ── Root ──────────────────────────────────────────
        var root = new GameObject("Placeables_JumpA");

        // PlaceableJumpA — gestisce tutto: drag, piazzamento libero, orb, bob, pulse
        var jumpA = root.AddComponent<PlaceableJumpA>();
        jumpA.placeableType    = placeableType;
        jumpA.overlapCheckMask = ~(1 << 9); // tutto tranne Player[9]
        jumpA.overlapCheckSize = Vector2.one; // hitbox 1x1 blocco

        // CircleCollider2D trigger — rilevamento orb + click mouse
        var col       = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        // ── Visual (sprite placeholder) ───────────────────
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale    = Vector3.one;

        var visualSR        = visual.AddComponent<SpriteRenderer>();
        visualSR.sprite     = GetCircleSprite();
        visualSR.color      = new Color(1f, 0.3f, 0.7f, 1f); // rosa/magenta GD
        visualSR.sortingOrder = 1;

        // ── Salva prefab ───────────────────────────────────
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[Jump Setup] Prefab creato: {prefabPath}");
    }

    // ── UnlockItem ───────────────────────────────────────────────

    static void CreateUnlockItemPrefab(string name, PlaceableTypeSO type, Color color)
    {
        string prefabPath = $"Assets/Prefabs/{name}.prefab";

        var root = new GameObject(name);

        var sr        = root.AddComponent<SpriteRenderer>();
        sr.sprite     = GetCircleSprite();
        sr.color      = color;
        sr.sortingOrder = 2;

        var col       = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.4f;

        var item = root.AddComponent<PlaceableUnlockItem>();
        item.typeToUnlock = type;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[Jump Setup] UnlockItem creato: {prefabPath}");
    }

    // ── Utility ──────────────────────────────────────────────────

    static Sprite GetCircleSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
