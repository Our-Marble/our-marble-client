using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 땅 관련 계산 창구. GameManager가 판정·금액이 필요할 때 호출한다.
/// 상태(돈, 소유자, 단계)는 바꾸지 않고 계산 결과만 돌려준다.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    private PropertyManager Properties => PropertyManager.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ───────────── 조회 ─────────────

    /// <summary>땅값. PurchaseProperty의 amount.</summary>
    public long GetLandPrice(int propertyId)
    {
        var data = Properties.GetData(propertyId);
        return data != null ? data.LandPrice : 0;
    }

    /// <summary>칸 정보. 칸 클릭 시 UI가 사용. 땅이 아니면 null.</summary>
    public TileInfo GetTileInfo(PropertyState state)
    {
        var data = Properties.GetData(state.PropertyId);
        if (data == null) return null;

        return new TileInfo
        {
            CityName = data.CityName,
            OwnerId = state.OwnerId,
            Level = (BuildingLevel)state.BuildingLevel,
            CurrentToll = GetToll(state),
            LandPrice = data.LandPrice,
            BuildCosts = new[]
            {
                data.GetBuildCost(BuildingLevel.Villa),
                data.GetBuildCost(BuildingLevel.Building),
                data.GetBuildCost(BuildingLevel.Hotel)
            }
        };
    }

    // ───────────── 판정 ─────────────
    // TODO: 아래 판정은 Economy/Server/의 Handler로 옮긴다. (Manager는 호출만)

    /// <summary>통행료. 주인 없는 땅이면 0. TODO: TollCalculator로 이동</summary>
    public long GetToll(PropertyState state)
    {
        if (!state.OwnerId.HasValue) return 0;

        var data = Properties.GetData(state.PropertyId);
        if (data == null) return 0;

        return data.GetToll((BuildingLevel)state.BuildingLevel);
    }

    /// <summary>
    /// 지금 돈으로 지을 수 있는 단계 목록. Cost는 누적 비용.
    /// 비어 있으면 건설 불가. TODO: BuildHandler로 이동
    /// </summary>
    public List<BuildOption> GetBuildOptions(PropertyState state, long money)
    {
        var options = new List<BuildOption>();
        if (!state.OwnerId.HasValue) return options;

        var data = Properties.GetData(state.PropertyId);
        if (data == null) return options;

        long totalCost = 0;
        for (int lv = state.BuildingLevel + 1; lv <= (int)BuildingLevel.Hotel; lv++)
        {
            var level = (BuildingLevel)lv;
            totalCost += data.GetBuildCost(level);
            if (totalCost > money) break;

            options.Add(new BuildOption { Level = level, Cost = totalCost });
        }
        return options;
    }
}