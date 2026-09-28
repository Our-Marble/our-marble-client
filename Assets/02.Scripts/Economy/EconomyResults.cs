using System.Collections.Generic;

/// <summary>건설 선택지 하나. Cost는 현재 단계부터 해당 단계까지의 누적 비용.</summary>
public class BuildOption
{
    public BuildingLevel Level;
    public long Cost;
}

/// <summary>GetTileInfo 결과. 칸 클릭 시 보여줄 정보.</summary>
public class TileInfo
{
    public string CityName;
    public long? OwnerId;
    public BuildingLevel Level;
    public long CurrentToll;
    public long LandPrice;
    public long[] BuildCosts; // 별장, 빌딩, 호텔 순서
}