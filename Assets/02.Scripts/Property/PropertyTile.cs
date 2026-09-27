using UnityEngine;

/// <summary>
/// 보드의 도시 칸에 붙이는 컴포넌트.
/// 상태를 보관하고, 상태가 바뀔 때마다 건물·소유자 표시를 갱신한다.
/// </summary>
public class PropertyTile : MonoBehaviour
{
    [Header("칸 정보")]
    [SerializeField] private int tileIndex;
    [SerializeField] private PropertyData data;

    [Header("비주얼")]
    [Tooltip("별장, 빌딩, 호텔 순서 (3개)")]
    [SerializeField] private GameObject[] buildingModels = new GameObject[3];

    [Tooltip("소유자 색을 표시할 오브젝트 (깃발, 바닥 등)")]
    [SerializeField] private Renderer ownerMarker;

    public int TileIndex => tileIndex;
    public PropertyData Data => data;
    public PropertyState State { get; } = new PropertyState();

    private string CityName => data != null ? data.CityName : name;

    private void Start()
    {
        Debug.Log($"[Tile] {CityName}({tileIndex}) 준비");
        RefreshVisual();
    }

    // ───────────── 상태 변경 ─────────────

    public void SetOwner(IEconomyPlayer owner)
    {
        State.SetOwner(owner);
        Debug.Log($"[Tile] {CityName}({tileIndex}) 소유자 → {owner?.PlayerName ?? "없음"}");
        RefreshVisual();
    }

    public void SetLevel(BuildingLevel level)
    {
        State.SetLevel(level);
        Debug.Log($"[Tile] {CityName}({tileIndex}) 단계 → {level}");
        RefreshVisual();
    }

    public void ResetTile()
    {
        State.Reset();
        Debug.Log($"[Tile] {CityName}({tileIndex}) 초기화");
        RefreshVisual();
    }

    // ───────────── 비주얼 ─────────────

    /// <summary>현재 State에 맞게 비주얼 전체 갱신.</summary>
    private void RefreshVisual()
    {
        ShowLevel(State.Level);

        if (State.IsOwned) ShowOwner(State.Owner.PlayerColor);
        else HideOwner();
    }

    /// <summary>해당 단계 모델만 켠다. 땅이면 전부 꺼짐.</summary>
    private void ShowLevel(BuildingLevel level)
    {
        for (int i = 0; i < buildingModels.Length; i++)
        {
            if (buildingModels[i] == null) continue;
            buildingModels[i].SetActive((int)level == i + 1);
        }
    }

    private void ShowOwner(Color color)
    {
        if (ownerMarker == null) return;
        ownerMarker.gameObject.SetActive(true);
        ownerMarker.material.color = color;
    }

    private void HideOwner()
    {
        if (ownerMarker == null) return;
        ownerMarker.gameObject.SetActive(false);
    }
}