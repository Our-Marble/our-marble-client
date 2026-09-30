using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private static BoardManager _instance;
    public static BoardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BoardManager>(); // 씬에 이미 존재하는지 검색

                if (_instance == null) // 없으면 동적으로 생성 (선택 사항)
                {
                    GameObject go = new GameObject("BoardManager");
                    _instance = go.AddComponent<BoardManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private BoardController boardController;
    [SerializeField] private BoardData boardData;

    public BoardController BoardController => boardController;
    public BoardData BoardData => boardData;

    /// <summary>보드의 전체 칸 수. (BoardData 기준)</summary>
    public int TileCount => boardData != null ? boardData.Tiles.Count : 0;

    private Dictionary<int, TileData> tilesByIndex;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if(tilesByIndex == null)
            RebuildLookup();
    }

    private void RebuildLookup()
    {
        tilesByIndex = new Dictionary<int, TileData>();

        if (boardData == null)
        {
            Debug.LogError("[BoardManager] BoardData가 연결되지 않았습니다.", this);
            return;
        }

        if (boardData.Tiles == null || boardData.Tiles.Count == 0)
        {
            Debug.LogWarning($"[BoardManager] mapId: {boardData.MapId} BoardData.Tiles 초기화 안됨", this);
            return;
        }

        foreach (TileData data in boardData.Tiles)
        {
            if (data == null)
            {
                Debug.LogWarning($"[BoardManager] mapId: {boardData.MapId} BoardData.Tiles 원소 중 null값 존재", this);
                continue;
            }

            if (!tilesByIndex.TryAdd(data.Index, data))
            {
                Debug.LogWarning($"[BoardManager] mapId: {boardData.MapId} BoardData.Tiles 에서 TileData.index 중복: {data.Index})", this);
            }
        }
    }

    public TileData GetTileData(int index)
    {
        if (tilesByIndex == null)
        {
            RebuildLookup();
        }

        return tilesByIndex.TryGetValue(index, out TileData data)?  data : null;
    }
}