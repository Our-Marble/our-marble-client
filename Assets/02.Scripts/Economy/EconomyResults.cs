using System.Collections.Generic;

/// <summary>도착한 칸의 상황.</summary>
public enum LandingType
{
    NotProperty,    // 부동산 칸이 아님 (특수 칸 등)
    Unowned,        // 주인 없음 → 구매 선택
    OwnProperty,    // 내 땅 → 건설 선택
    TollPaid,       // 남의 땅 → 통행료 지불 완료
    NeedForcedSale  // 남의 땅 → 돈 부족, 강제매각 필요
}

/// <summary>건설 선택지 하나. Cost는 현재 단계부터 해당 단계까지의 누적 비용.</summary>
public class BuildOption
{
    public BuildingLevel Level;
    public long Cost;
}

/// <summary>OnLanded 결과. Type에 따라 채워지는 값이 다르다.</summary>
public class LandingResult
{
    public LandingType Type;

    // Unowned
    public long Price;
    public bool CanPurchase;

    // TollPaid, NeedForcedSale
    public long Toll;
    public IEconomyPlayer TollReceiver;

    // OwnProperty
    public List<BuildOption> BuildOptions = new List<BuildOption>();
}

/// <summary>GetTileInfo 결과. 칸 클릭 시 보여줄 정보.</summary>
public class TileInfo
{
    public string CityName;
    public IEconomyPlayer Owner;
    public BuildingLevel Level;
    public long CurrentToll;
    public long LandPrice;
    public long[] BuildCosts; // 별장, 빌딩, 호텔 순서
}