using UnityEngine;

[CreateAssetMenu(fileName = "PlaceableType_New", menuName = "Placeable/Type")]
public class PlaceableTypeSO : ScriptableObject
{
    [Tooltip("Immagine mostrata nella schermata di sblocco (Assets/UI/)")]
    public Sprite unlockSprite;
}
