using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드의 모든 도시 칸을 땅 번호(propertyId)로 들고, 찾아주고, 비주얼 갱신을 전달한다.
/// 데이터는 JSON 테이블에서, 비주얼은 씬의 PropertyTile에서 가져온다.
/// 상태 변경은 GameManager가 GameState에서 한다.
/// </summary>
public class PropertyManager : MonoBehaviour
{
    public static PropertyManager Instance { get; private set; }

    private Dictionary<int, PropertyData> dataTable = new Dictionary<int, PropertyData>();
    private readonly Dictionary<int, PropertyTile> tiles = new Dictionary<int, PropertyTile>();

    public IEnumerable<PropertyTile> AllTiles => tiles.Values;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        dataTable = PropertyDataLoader.Load();
        RegisterAllTiles();
    }

    // ───────────── 등록 ─────────────

    /// <summary>씬에 있는 PropertyTile을 땅 번호로 등록.</summary>
    private void RegisterAllTiles()
    {
        tiles.Clear();

        foreach (var tile in FindObjectsByType<PropertyTile>(FindObjectsSortMode.None))
        {
            if (!tiles.TryAdd(tile.PropertyId, tile))
            {
                Debug.LogWarning($"[PropertyManager] 땅 번호 중복: {tile.PropertyId} ({tile.name})");
            }
        }

        // TODO: 데이터 없는 타일 / 타일 없는 데이터 경고

        Debug.Log($"[PropertyManager] 칸 {tiles.Count}개 등록");
    }

    // ───────────── 조회 ─────────────

    public bool IsProperty(int propertyId)
    {
        return dataTable.ContainsKey(propertyId);
    }

    public bool TryGetTile(int propertyId, out PropertyTile tile)
    {
        return tiles.TryGetValue(propertyId, out tile);
    }

    /// <summary>땅의 가격표. 없으면 null.</summary>
    public PropertyData GetData(int propertyId)
    {
        return dataTable.TryGetValue(propertyId, out var data) ? data : null;
    }

    /// <summary>보드 칸 번호(PlayerState.Position)로 땅 가격표 찾기. 땅이 아니면 false.</summary>
    public bool TryGetByBoardIndex(int boardIndex, out PropertyData data)
    {
        // TODO: boardIndex → PropertyData 찾기
        data = null;
        return false;
    }

    // ───────────── 비주얼 갱신 ─────────────

    /// <summary>GameState가 바뀐 뒤 GameManager가 호출. 해당 칸 비주얼 갱신.</summary>
    public void Refresh(PropertyState state, Color ownerColor)
    {
        if (!tiles.TryGetValue(state.PropertyId, out var tile))
        {
            Debug.LogWarning($"[PropertyManager] {state.PropertyId}번 땅 없음");
            return;
        }
        tile.Refresh(state, ownerColor);
    }
}