using System.Collections.Generic;
using UnityEngine;

public class BoardManager : Singleton<BoardManager>
{
    [SerializeField] private BoardController boardController;
    [SerializeField] private BoardData boardData;

    public BoardController BoardController => boardController;
    public BoardData BoardData => boardData;
    
    /// <summary>보드의 전체 칸 수. (BoardData 기준)</summary>
    public int TileCount => boardData != null ? boardData.Tiles.Count : 0;
    
    private Dictionary<int, TileData> tilesByIndex;

    protected override void Awake()
    {
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
