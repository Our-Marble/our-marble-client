using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 보드의 모든 도시 칸 목록을 들고, 칸 번호로 찾아준다.
/// 칸 상태를 바꾸는 건 PropertyTile이 한다.
/// </summary>
public class PropertyManager : MonoBehaviour
{
    public static PropertyManager Instance { get; private set; }

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

        RegisterAllTiles();
    }

    // ───────────── 등록 ─────────────

    /// <summary>씬에 있는 PropertyTile을 칸 번호로 등록.</summary>
    private void RegisterAllTiles()
    {
        tiles.Clear();

        foreach (var tile in FindObjectsByType<PropertyTile>(FindObjectsSortMode.None))
        {
            if (!tiles.TryAdd(tile.TileIndex, tile))
            {
                Debug.LogWarning($"[PropertyManager] 칸 번호 중복: {tile.TileIndex} ({tile.name})");
            }
        }

        Debug.Log($"[PropertyManager] 칸 {tiles.Count}개 등록");
    }

    // ───────────── 조회 ─────────────

    public bool IsPropertyTile(int tileIndex)
    {
        return tiles.ContainsKey(tileIndex);
    }

    public bool TryGetTile(int tileIndex, out PropertyTile tile)
    {
        return tiles.TryGetValue(tileIndex, out tile);
    }

    public List<PropertyTile> GetTilesOwnedBy(IEconomyPlayer player)
    {
        return tiles.Values.Where(t => t.State.Owner == player).ToList();
    }
}