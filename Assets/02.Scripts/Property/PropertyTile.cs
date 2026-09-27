using UnityEngine;

/// <summary>
/// 보드의 도시 칸에 붙이는 컴포넌트.
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

    private string CityName => data != null ? data.CityName : name;

    private void Start()
    {
        Debug.Log($"[Tile] {CityName}({tileIndex}) 준비");
        ShowLevel(BuildingLevel.Land);
        HideOwner();
    }

    // ───────────── 비주얼 ─────────────

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