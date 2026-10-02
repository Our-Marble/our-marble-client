using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Board Layout")]
    [SerializeField, Min(1)] private int tilesPerSide = 7;
    [SerializeField] private Vector2 tileSpacing = new(0.8f, 0.45f);
    [SerializeField, Min(1f)] private float cornerSpacingMultiplier = 1.5f;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject cornerTilePrefab;
    [SerializeField] private GameObject leftTopTilePrefab;
    [SerializeField] private GameObject rightTopTilePrefab;
    [SerializeField] private GameObject rightBottomTilePrefab;
    [SerializeField] private GameObject leftBottomTilePrefab;
#endif

    [Header("Board Tiles")]
    [SerializeField] private List<BoardTile> boardTiles = new();
    
    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.gray8;
    [SerializeField] private Color defaultTextColor = Color.brown;
    [SerializeField] private Color cornerColor = Color.aliceBlue;
    public IReadOnlyList<BoardTile> BoardTiles => boardTiles;
    public int TileCount => boardTiles.Count;
    
    /// <summary>tileIndex번 타일의 월드 좌표 (말 이동용)</summary>
    public Vector3 GetTilePosition(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= boardTiles.Count)
        {
            Debug.LogError($"BoardTile index is out of range: {tileIndex}");
            return Vector3.zero;
        }
        return boardTiles[tileIndex].transform.position;
    }
    
    public void SetBoardTileColor(int tileIndex, Color color, Color textColor)
    {
        boardTiles[tileIndex].SetSpriteColor(color);
        boardTiles[tileIndex].SetText(defaultTextColor);
    }
    
    [ContextMenu("SetDefaultColors")]
    public void SetDefaultColors()
    {
        for (int i = 0; i < boardTiles.Count; i++)
            SetBoardTileColor(i, i % 8 == 0 ? cornerColor : defaultColor, defaultTextColor);

    }
    
    /// <summary>
    /// 각 타일의 글자를 BoardData / 재산 데이터(JSON)에 맞게 표시합니다.
    /// 땅(PROPERTY)이면 도시 이름, 그 외에는 칸 종류 이름을 보여줍니다.
    /// </summary>
    public void InitializeBoardTiles()
    {
        for (int i = 0; i < boardTiles.Count; i++)
        {
            // 인덱스 설정
            boardTiles[i].Index = i;
            
            // 글자 설정
            TileData tileData = BoardManager.Instance.GetTileData(i);
            boardTiles[i].SetText(defaultTextColor, GetTileLabel(tileData));
            
            // 색상 설정
            if(i % 8 == 0)
                boardTiles[i].SetSpriteColor(cornerColor);
            else
                boardTiles[i].SetSpriteColor(defaultColor);
        }
    }

    private string GetTileLabel(TileData tileData)
    {
        if (tileData == null)
            return "-";

        switch (tileData.Type)
        {
            case TileType.PROPERTY:
                PropertyData property = PropertyManager.Instance.GetData(tileData.PropertyId);
                return property != null ? property.CityName : $"?{tileData.PropertyId}";
            case TileType.START:        return "출발";
            case TileType.GOLDEN_KEY:   return "황금열쇠";
            case TileType.ISLAND:       return "무인도";
            case TileType.DONATION:     return "기부금 납부";
            case TileType.CHARITY:      return "기부금 수령";
            case TileType.WORLD_TRAVEL: return "세계여행";
            default:                    return tileData.Type.ToString();
        }
    }
    
    
    public void UpdatePropertyTileColor(int propertyId, long? playerId)
    {
        PropertyData propertyData = PropertyManager.Instance.GetData(propertyId);
        
        int tileIndex = propertyData.BoardIndex;

        BoardTile boardTile = boardTiles[tileIndex];

        Color newColor;
        if (playerId == null) newColor = defaultColor;
        else newColor = GameManager.Instance.GetPlayerColor(playerId.Value);
        
        boardTile.SetSpriteColor(newColor);
    }
    
    public void UpdatePropertyBuildingVisual(int propertyId, BuildingLevel buildingLevel)
    {
        PropertyData propertyData = PropertyManager.Instance.GetData(propertyId);
        
        int tileIndex = propertyData.BoardIndex;

        BoardTile boardTile = boardTiles[tileIndex];

        boardTile.SetBuildingSpriteRenderers(buildingLevel);
    }
    
    
    
    
    // 타일 선택 기능
    
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
        
        ClearTileHighlights();
        
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
                // tileIndex가 여행 가능한 타일인지 확인 (자유여행칸으로 이동은 안됨.)
                if (tileIndex == 24) return;
                
                // selectedIndexes에 이미 있는 값인지 확인 
                if (selectedIndexes.Contains(tileIndex))
                {
                    RemoveFromSelectedIndexes(tileIndex);
                    UIManager.Instance.CheckTileValidForTravel(selectedIndexes);
                    return;
                }

                // 이미 선택된 타일 개수가 최대인지 확인
                if (selectedIndexes.Count >= maxSelectCount)
                {
                    int firxtIndex =  selectedIndexes[0];
                    RemoveFromSelectedIndexes(firxtIndex);
                    AddToSelectedIndexes(tileIndex);
                    UIManager.Instance.CheckTileValidForTravel(selectedIndexes);
                    return;
                }
                
                // selectedIndexes에 값 추가
                AddToSelectedIndexes(tileIndex);
                UIManager.Instance.CheckTileValidForTravel(selectedIndexes);
                return;
            
            case TileClickMode.SelectSell:
                // selectedIndexes에 이미 있는 값인지 확인 
                if (selectedIndexes.Contains(tileIndex))
                {
                    RemoveFromSelectedIndexes(tileIndex);
                    UIManager.Instance.CheckTilesValidForSell(selectedIndexes);
                    return;
                }
                
                // 이미 선택된 타일 개수가 최대인지 확인
                if (selectedIndexes.Count >= maxSelectCount) return;
                
                // tileIndex가 자산 타일인지 확인
                TileData tileDatadata = BoardManager.Instance.GetTileData(tileIndex);
                if (tileDatadata.Type != TileType.PROPERTY) return;
                
                // tileIndex가 현재 차례인 플레이어 소유인지 확인
                int propertyId = tileDatadata.PropertyId;
                PropertyState propertyState = GameManager.Instance.GetPropertyState(propertyId);
                if (propertyState.OwnerId != GameManager.Instance.gameState.CurrentPlayerId) return;
                
                // selectedIndexes에 값 추가
                AddToSelectedIndexes(tileIndex);
                UIManager.Instance.CheckTilesValidForSell(selectedIndexes);
                return;
        }
    }

    private void AddToSelectedIndexes(int index)
    {
        selectedIndexes.Add(index);
        boardTiles[index].SetTileHighlight(true);
    }

    private void RemoveFromSelectedIndexes(int index)
    {
        selectedIndexes.Remove(index);
        boardTiles[index].SetTileHighlight(false);
    }
    
    private void ClearTileHighlights()
    {
        foreach (BoardTile boardTile in boardTiles)
        {
            boardTile.SetTileHighlight(false);
        }
    }
}

enum TileClickMode
{
    Inspect,
    SelectTravel,
    SelectSell
}
