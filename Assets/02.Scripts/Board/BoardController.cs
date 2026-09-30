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
    [SerializeField] private Color defaultColor = Color.beige;
    [SerializeField] private Color defaultTextColor = Color.brown;
    [SerializeField] private Color cornerColor = Color.aliceBlue;
    public IReadOnlyList<BoardTile> BoardTiles => boardTiles;

    /// <summary>보드 타일 개수</summary>
    public int TileCount => boardTiles.Count;

    /// <summary>tileIndex번 타일의 월드 좌표 (말 이동용)</summary>
    public Vector3 GetTilePosition(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= boardTiles.Count)
        {
            Debug.LogError($"BoardTile index is out of range: {tileIndex}", this);
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
    public void ApplyTileTexts()
    {
        for (int i = 0; i < boardTiles.Count; i++)
        {
            TileData tileData = BoardManager.Instance.GetTileData(i);
            boardTiles[i].SetText(defaultTextColor, GetTileLabel(tileData));
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyTileTexts();
    }

    // Update is called once per frame
    void Update()
    {

    }

}