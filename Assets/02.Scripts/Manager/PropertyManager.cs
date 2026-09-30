using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드의 모든 도시 칸(PropertyTile)을 땅 번호(propertyId)로 들고, 찾아주고, 비주얼 갱신을 전달한다.
/// 가격표 데이터는 PropertyManager가 관리한다.
/// 상태 변경은 GameManager가 GameState에서 한다.
/// </summary>
public class PropertyManager : Singleton<PropertyManager>
{
    private readonly Dictionary<int, PropertyTile> tiles = new Dictionary<int, PropertyTile>();
    public IEnumerable<PropertyTile> AllTiles => tiles.Values;

    private const float SellRate = 0.5f; //매각가 = 투자금의 50%로 계산. 추후 밸런스 조정 필요.
    private const float AcquireRate = 2f; //인수가 = 투자금의 200%로 계산. 추후 밸런스 조정 필요.

    [Header("JSON 데이터가 있다면 자동으로 채워집니다.")]
    [SerializeField] private List<PropertyData> properties = new();
    private Dictionary<int, PropertyData> propertiesById;

    // ───────────── JSON 불러오기 (에디터용) ─────────────

    [Header("JSON Import")]
    [SerializeField] private TextAsset propertiesJson;

    [System.Serializable]
    private class PropertyDataListWrapper
    {
        public List<PropertyData> properties;
    }

    /// <summary>propertiesJson의 내용을 properties 리스트에 채웁니다. (에디터에서 우클릭 메뉴로 실행)</summary>
    [ContextMenu("Load Properties From JSON")]
    private void LoadPropertiesFromJson()
    {
        if (propertiesJson == null)
        {
            Debug.LogError("[PropertyManager] propertiesJson이 연결되지 않았습니다.", this);
            return;
        }

        var wrapper = JsonUtility.FromJson<PropertyDataListWrapper>(propertiesJson.text);
        if (wrapper == null || wrapper.properties == null || wrapper.properties.Count == 0)
        {
            Debug.LogError("[PropertyManager] JSON 파싱 실패 또는 데이터가 비어 있습니다.", this);
            return;
        }

        properties = wrapper.properties;
        propertiesById = null;   // 조회용 캐시 초기화

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log($"[PropertyManager] JSON에서 {properties.Count}개 로드 완료", this);
    }
    
    protected override void OnAwake()
    {
        LoadPropertiesFromJson();
        
        RegisterAllTiles();
    }

    // ───────────── PropertyTile 등록 ─────────────

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

        Debug.Log($"[PropertyManager] 칸 {tiles.Count}개 등록");
    }

    // ───────────── PropertyTile 조회 ─────────────

    public bool TryGetTile(int propertyId, out PropertyTile tile)
    {
        return tiles.TryGetValue(propertyId, out tile);
    }

    /// <summary>칸 정보. 칸 클릭 시 UI가 사용. 땅이 아니면 null.</summary>
    public TileInfo GetTileInfo(PropertyState state)
    {
        PropertyData data = GetData(state.PropertyId);
        if(data == null) return null;

        return new TileInfo
        {
            CityName = data.CityName,
            OwnerId = state.OwnerId,
            Level = state.BuildingLevel,
            CurrentToll = state.OwnerId.HasValue ? data.GetToll(state.BuildingLevel) : 0,
            LandPrice = data.LandPrice,
            BuildCosts = new[]
            {
                data.GetBuildCost(BuildingLevel.Villa),
                data.GetBuildCost(BuildingLevel.Building),
                data.GetBuildCost(BuildingLevel.Hotel)
            }
        };
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

    // ───────────── 부동산 계산 ─────────────

    /// <summary>땅값. PurchaseProperty의 amount.</summary>
    public long GetLandPrice(int propertyId)
    {
        var data = GetData(propertyId);
        return data != null ? data.LandPrice : 0;
    }

    public long GetBuildCost(int propertyId, BuildingLevel level)
    {
        var data = GetData(propertyId);
        
        long cost = data != null ?  data.GetBuildCost(level) : 0;

        return cost;
    }

    /// <summary>통행료. 주인 없는 땅이면 0.</summary>
    public long GetToll(PropertyState state)
    {
        if (!state.OwnerId.HasValue) return 0;

        var data = GetData(state.PropertyId);
        if (data == null) return 0;

        return data.GetToll(state.BuildingLevel);
    }

    /// <summary>
    /// 투자금 = 땅값 + 지금 단계까지 지은 건물 비용 합계.
    /// 매각가(GetSellValue)와 인수가(GetAcquireValue) 계산에 사용됨.
    /// </summary>
    /// 투자금. 가격표가 없는 땅이면 0.
    public long GetInvestedAmount(PropertyState state)
    {
        var data = GetData(state.PropertyId);
        if (data == null) return 0;

        long invested = data.LandPrice;
        for (int lv = 1; lv <= (int)state.BuildingLevel; lv++)
        {
            invested += data.GetBuildCost((BuildingLevel)lv);
        }
        return invested;
    }

    ///<summary>땅 하나의 매각가: 투자금 x SellRate(50%).</summary>
    /// 사용처
    ///   - SellProperties: 선택한 땅의 매각가 합계 계산
    ///   - GetTotalSellValue: 가진 땅 전부의 매각가 합계
    ///   - 파산 처리: 모든 재산 현금화 (매각과 같은 기준이어야 분기 판단과 실제 금액이 일치)
    /// 
    /// 매각가. 가격표가 없는 땅이면 0.
    public long GetSellValue(PropertyState state)
    {
        return (long)(GetInvestedAmount(state) * SellRate);
    }
    
    /// <summary>플레이어가 가진 땅을 전부 팔면 받는 금액의 합계</summary>
    /// 사용처
    ///   - ProcessArrival: 통행료를 현금으로 못 낼 때 분기 판단
    ///       현금 + GetTotalSellValue ≥ 통행료 → 매각 (HandleSellPropertiesPrompt)
    ///       현금 + GetTotalSellValue < 통행료 → 파산
    /// 실제로 땅을 팔지는 않고 금액만 계산한다.
    /// 
    /// 매각가 합계. 가진 땅이 없으면 0.
    public long GetTotalSellValue(long playerId, List<PropertyState> propertyStates)
    {
        long total = 0;
        foreach (var state in propertyStates)
        {
            if (state.OwnerId == playerId)  total += GetSellValue(state);
        }
        return total;
    }

    /// <summary>땅 하나의 인수가: 투자금 x AcquireRate(200%).</summary>
    /// 사용처
    ///   - ProcessArrival: 통행료를 낸 뒤 인수할 돈이 충분한지 판단
    ///   - HandleAcquirePropertyPrompt: 인수 팝업에 보여줄 금액
    ///   - AcquireProperty: 실제로 차감할 금액
    /// 
    /// 인수가. 가격표가 없는 땅이면 0.
    public long GetAcquireValue(PropertyState state)
    {
        return (long)(GetInvestedAmount(state) * AcquireRate);
    }

    // ───────────── PropertyData 조회 ─────────────

    private void RebuildLookup()
    {
        propertiesById = new Dictionary<int, PropertyData>();

        if (properties == null || properties.Count == 0)
        {
            Debug.LogWarning($"[PropertyManager] properties 초기화 안됨", this);
            return;
        }

        foreach (PropertyData data in properties)
        {
            if (data == null)
            {
                Debug.LogWarning($"[PropertyManager] properties의 원소 중 null값 존재", this);
                continue;
            }

            if (!propertiesById.TryAdd(data.Id, data))
            {
                Debug.LogWarning($"[PropertyTable] propertyId 중복: {data.Id} ({data.CityName})", this);
            }
        }
    }

    public PropertyData GetData(int id)
    {
        if (propertiesById == null)
        {
            RebuildLookup();
        }

        return propertiesById.TryGetValue(id, out PropertyData data)?  data : null;
    }

    public List<PropertyData> GetAllDataByMapId(int mapId)
    {
        List<PropertyData> mapProperties = new List<PropertyData>();
        foreach (PropertyData property in properties) 
        {
            if(property.MapId != mapId) continue;

            mapProperties.Add(property);
        }
        return mapProperties;
    }
}