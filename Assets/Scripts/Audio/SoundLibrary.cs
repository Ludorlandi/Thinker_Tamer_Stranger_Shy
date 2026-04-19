using UnityEngine;

public enum SoundID
{
    PlayerJump,
    PlaceableDragStart,
    PlaceableAnchored,
    PlaceableFailedSnap,
    PlaceableLockedClick,
    PlaceableUnlocked,
    GateTraversal,
    UnlockScreenAppear,
    JumpPadBounce,
    OrbActivate,
    PlaceableReturn
}

public enum MusicID
{
    None,
    MainRoom,
    OtherAreas
}

[System.Serializable]
public class SoundEntry
{
    public SoundID id;

    [Tooltip("Varianti del suono — ne viene scelta una a caso ogni volta")]
    public AudioClip[] variants;

    [Range(0f, 1f)]
    [Tooltip("Volume base di questo suono")]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    [Tooltip("Pitch minimo (randomizzato ad ogni play)")]
    public float pitchMin = 0.9f;

    [Range(0.5f, 2f)]
    [Tooltip("Pitch massimo (randomizzato ad ogni play)")]
    public float pitchMax = 1.1f;

    public AudioClip GetRandomClip()
    {
        if (variants == null || variants.Length == 0) return null;
        return variants[Random.Range(0, variants.Length)];
    }
}

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("SFX")]
    public SoundEntry[] sounds;

    [Header("Musica")]
    [Tooltip("Traccia musicale per la Main Room")]
    public AudioClip mainRoomMusic;

    [Tooltip("Traccia musicale per tutte le altre aree")]
    public AudioClip otherAreasMusic;

    public SoundEntry GetEntry(SoundID id)
    {
        if (sounds == null) return null;
        foreach (var entry in sounds)
            if (entry.id == id) return entry;
        return null;
    }

    public AudioClip GetMusic(MusicID id)
    {
        return id switch
        {
            MusicID.MainRoom   => mainRoomMusic,
            MusicID.OtherAreas => otherAreasMusic,
            _                  => null
        };
    }
}
