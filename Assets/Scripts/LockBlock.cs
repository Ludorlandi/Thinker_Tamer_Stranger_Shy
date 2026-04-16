using UnityEngine;

public class LockBlock : MonoBehaviour
{
    private Placeable occupiedBy = null;

    public bool IsOccupied()
    {
        return occupiedBy != null;
    }

    public void SetOccupied(Placeable placeable)
    {
        occupiedBy = placeable;
    }

    public void SetFree()
    {
        occupiedBy = null;
    }
}
