using UnityEngine;

public class PlayerPawn : MonoBehaviour
{
    [SerializeField] private int currentTileIndex = -1;

    public int CurrentTileIndex => currentTileIndex;

    public void Move(int tileIndex, Vector3 targetPosition)
    {
        transform.position = targetPosition;
        currentTileIndex = tileIndex;
    }
}
