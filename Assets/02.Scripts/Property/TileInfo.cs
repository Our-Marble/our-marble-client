/// <summary>
/// 칸 정보. PropertyManager.GetTileInfo의 결과로, 칸 클릭 시 UI에 보여줄 값을 담는다.
/// </summary>
public class TileInfo
{
    public string CityName;
    public long? OwnerId;
    public BuildingLevel Level;
    public long CurrentToll;
    public long LandPrice;
    public long[] BuildCosts; // 별장, 빌딩, 호텔 순서
}