/// <summary>
/// 칸 하나의 지금 상태 (누구 땅인지, 몇 단계인지).
/// </summary>
public class PropertyState
{
    public IEconomyPlayer Owner { get; private set; }
    public BuildingLevel Level { get; private set; } = BuildingLevel.Land;

    public bool IsOwned => Owner != null;

    public void SetOwner(IEconomyPlayer owner)
    {
        Owner = owner;
    }

    public void SetLevel(BuildingLevel level)
    {
        Level = level;
    }

    /// <summary>매각·파산 시 주인 없는 땅으로 되돌림.</summary>
    public void Reset()
    {
        Owner = null;
        Level = BuildingLevel.Land;
    }
}