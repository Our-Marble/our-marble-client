using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private BoardController boardController;
    [SerializeField] private BoardData boardInfo;

    public BoardController BoardController => boardController;
    public BoardData BoardInfo => boardInfo;
}
