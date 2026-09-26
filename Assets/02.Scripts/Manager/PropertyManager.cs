using UnityEngine;

/// <summary>
/// 보드의 모든 부동산 칸 목록을 관리하고 칸을 찾아준다.
/// 칸 상태를 바꾸는 건 PropertyTile이 한다.
/// </summary>
public class PropertyManager : MonoBehaviour
{
    public static PropertyManager Instance { get; private set; }

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

    /// <summary>씬에 있는 부동산 칸을 칸 번호로 등록.</summary>
    private void RegisterAllTiles()
    {
        // TODO: PropertyTile 추가 후 구현
    }

    // ───────────── 조회 ─────────────

    /// <summary>해당 칸 번호가 부동산 칸인지.</summary>
    public bool IsPropertyTile(int tileIndex)
    {
        // TODO: PropertyTile 추가 후 구현
        return false;
    }

    // TODO: PropertyTile 추가 후
    // public bool TryGetTile(int tileIndex, out PropertyTile tile)
    // public List<PropertyTile> GetTilesOwnedBy(IEconomyPlayer player)
}