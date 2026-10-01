using System;
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

    
    
    
    
    private TileClickMode mode = TileClickMode.Inspect;
    
    public List<int> selectedIndexes = new List<int>();
    int maxSelectCount = 0;

    public void ChangeToSelectTravelMode()
    {
        mode = TileClickMode.SelectTravel;

        maxSelectCount = 1;
        selectedIndexes.Clear();
    }

    public void ChangeToSelectSellMode()
    {
        mode = TileClickMode.SelectSell;

        maxSelectCount = 32;
        selectedIndexes.Clear();
    }

    public void ChangeToInspectMode()
    {
        mode = TileClickMode.Inspect;
        
        maxSelectCount = 0;
        selectedIndexes.Clear();
    }

    public void OnBoardTileClicked(int tileIndex)
    {
        switch (mode)
        {
            case TileClickMode.Inspect:
                // 타일 정보 띄우는 함수를 호출
                Debug.Log("타일 정보 : ~~~");
                return;
            
            case TileClickMode.SelectTravel:
                // selectedIndexes에 이미 있는 값인지 확인 
                if (selectedIndexes.Contains(tileIndex))
                {
                    selectedIndexes.Remove(tileIndex);
                    UIManager.Instance.CheckTileValidForTravel(selectedIndexes);
                    return;
                }

                // 이미 선택된 타일 개수가 최대인지 확인
                if (selectedIndexes.Count >= maxSelectCount) return;
                
                // tileIndex가 여행 가능한 타일인지 확인 (자유여행칸으로 이동은 안됨.)
                if (tileIndex == 24) return;
                
                // selectedIndexes에 값 추가
                selectedIndexes.Add(tileIndex);
                
                // 선택 완료 버튼 활성화 여부 결정함수 호출
                UIManager.Instance.CheckTileValidForTravel(selectedIndexes);
                return;
            
            case TileClickMode.SelectSell:
                // selectedIndexes에 이미 있는 값인지 확인 
                if (selectedIndexes.Contains(tileIndex))
                {
                    selectedIndexes.Remove(tileIndex);
                    UIManager.Instance.CheckTilesValidForSell(selectedIndexes);
                    return;
                }
                
                // 이미 선택된 타일 개수가 최대인지 확인
                if (selectedIndexes.Count >= maxSelectCount) return;
                
                // tileIndex가 현재 차례인 플레이어 소유의 자산 타일인지 확인
                TileData tileDatadata = GetTileData(tileIndex);
                if (tileDatadata.Type != TileType.PROPERTY) return;
                int propertyId = tileDatadata.PropertyId;
                PropertyState propertyState = GameManager.Instance.GetPropertyState(propertyId);
                if (propertyState.OwnerId != GameManager.Instance.gameState.CurrentPlayerId) return;
                
                // selectedIndexes에 값 추가
                selectedIndexes.Add(tileIndex);
                
                // 선택 완료 버튼 활성화 여부 결정함수 호출
                UIManager.Instance.CheckTilesValidForSell(selectedIndexes);
                return;
        }
    }






    public void UpdatePropertyTileColor(int propertyId, long? playerId)
    {
         boardController.UpdatePropertyTileColor(propertyId, playerId);
    }

    public void UpdatePropertyBuildingVisual(int propertyId, BuildingLevel buildingLevel)
    {
        boardController.UpdatePropertyBuildingVisual(propertyId, buildingLevel);
    }




    protected override void Awake()
    {
        ChangeToInspectMode();
        
        if(tilesByIndex == null)
            RebuildLookup();
    }

    private void Start()
    {
        boardController.InitializeBoardTiles();
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

enum TileClickMode
{
    Inspect,
    SelectTravel,
    SelectSell
}
