using System;
using System.Collections.Generic;

/// <summary>
/// 기본 봇 전략입니다.
/// 돈을 쓰고 난 뒤 다음 턴에 통행료를 현금으로 못 낼 위험이 크면 구매·건설·인수를 하지 않고,
/// 땅은 수익 비율이 좋은 땅 위주로 삽니다.
/// </summary>
public class BasicBotStrategy : IBotStrategy
{
    private const int DefaultTravelDestination = 16;       // 기부금수령 칸 (기존 동작 유지)
    private const double MaxCashShortProbability = 0.2;    // 다음 턴 현금 부족 확률이 이보다 크면 돈을 쓰지 않음
                                                           // 소심하면 올리고, 파산이 잦으면 낮춘다.
    private const double GoodLandReturn = 0.75;            // 최대 통행료 ÷ 총 투자금이 이 이상이면 좋은 땅
    private const long SafeCashReserve = 200000;           // 보통 땅은 사고 나서 이만큼 현금이 남을 때만 구매

    private readonly TileData[] tileByIndex;                  // 칸 번호 → 칸 정보
    private readonly Func<PropertyState, long> getToll;       // 통행료 계산
    private readonly Func<int, PropertyData> getPropertyData; // 가격표 조회 (땅값, 건설비, 통행료, 건설 가능 여부)
    private readonly Action<string> log;                      // 판단 근거 로그 (없으면 출력 안 함)

    public BasicBotStrategy(IReadOnlyList<TileData> tiles,
                            Func<PropertyState, long> getToll,
                            Func<int, PropertyData> getPropertyData,
                            Action<string> log = null)
    {
        // 리스트 순서와 칸 번호가 달라도 안전하도록 Index 기준으로 다시 배치합니다.
        tileByIndex = new TileData[tiles.Count];
        foreach (TileData tile in tiles)
        {
            if (tile != null && tile.Index >= 0 && tile.Index < tileByIndex.Length)
                tileByIndex[tile.Index] = tile;
        }

        this.getToll = getToll;
        this.getPropertyData = getPropertyData;
        this.log = log;
    }

    public bool ShouldPurchase(GameState state, long botId, int propertyId, long price)
    {
        // 1) 통행료 위험이 크면 어떤 땅이든 사지 않습니다.
        if (!CanAffordSafely(state, botId, price)) return false;

        // 2) 좋은 땅은 사고, 보통 땅은 현금이 넉넉히 남을 때만 삽니다.
        PropertyData data = getPropertyData(propertyId);
        if (data == null) return true;

        double landReturn = GetMaxTollReturn(data);
        long cashAfter = GetMoney(state, botId) - price;
        bool isGoodLand = landReturn >= GoodLandReturn;
        bool buy = isGoodLand || cashAfter >= SafeCashReserve;

        log?.Invoke($"[봇 판단] {botId}: {data.CityName} 수익 비율 {landReturn:F2}" +
                    $"({(isGoodLand ? "좋은 땅" : "보통 땅")}), 남는 현금 {cashAfter:N0} → {(buy ? "구매" : "포기")}");
        return buy;
    }

    public bool ShouldBuild(GameState state, long botId, int propertyId, long cost)
    {
        return CanAffordSafely(state, botId, cost);
    }

    public bool ShouldAcquire(GameState state, long botId, int propertyId, long price)
    {
        return CanAffordSafely(state, botId, price);
    }

    /// <summary>
    /// 매각가가 큰 것부터 담고, 빼도 부족분을 채우는 작은 땅은 다시 뺍니다. (기존 AutoChooseSellProperties)
    /// </summary>
    public List<int> ChooseSellProperties(GameState state, long botId, long requiredAmount,
                                          Func<PropertyState, long> getSellValue)
    {
        long shortage = requiredAmount - GetMoney(state, botId);

        List<PropertyState> owned = state.PropertyStates.FindAll(p => p.OwnerId == botId);
        owned.Sort((a, b) => getSellValue(b).CompareTo(getSellValue(a)));

        List<PropertyState> selected = new List<PropertyState>();
        long sum = 0;
        foreach (PropertyState p in owned)
        {
            if (sum >= shortage) break;
            selected.Add(p);
            sum += getSellValue(p);
        }

        selected.Sort((a, b) => getSellValue(a).CompareTo(getSellValue(b)));
        for (int i = 0; i < selected.Count;)
        {
            long value = getSellValue(selected[i]);
            if (sum - value >= shortage)
            {
                sum -= value;
                selected.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }

        return selected.ConvertAll(p => p.PropertyId);
    }

    public int ChooseTravelDestination(GameState state, long botId)
    {
        return DefaultTravelDestination;
    }

    // ────────────────────────── 통행료 위험 계산 ──────────────────────────

    /// <summary>
    /// cost를 낼 현금이 있고, 낸 뒤 다음 턴에 통행료를 현금으로 못 낼 확률이 기준 이하인지 확인합니다.
    /// </summary>
   
    private bool CanAffordSafely(GameState state, long botId, long cost)
    {
        PlayerState bot = state.PlayerStates.Find(p => p.PlayerId == botId);
        if (bot == null || bot.Money < cost) return false;

        long cashAfter = bot.Money - cost;
        double risk = CashShortProbability(state, botId, bot.Position, cashAfter);
        bool safe = risk <= MaxCashShortProbability;

        log?.Invoke($"[봇 판단] {botId}: 비용 {cost:N0}, 남는 현금 {cashAfter:N0}, " +
                    $"다음 턴 현금 부족 확률 {risk:P1} → {(safe ? "진행" : "포기")}");
        return safe;
    }

    /// <summary>
    /// 다음 주사위(합 2~12)로 도착할 칸 중, 통행료가 cash보다 비싼 남의 땅에 도착할 확률입니다.
    /// </summary>
    private double CashShortProbability(GameState state, long botId, int position, long cash)
    {
        if (tileByIndex.Length == 0) return 0;

        double probability = 0;
        for (int sum = 2; sum <= 12; sum++)
        {
            int target = (position + sum) % tileByIndex.Length;
            if (GetOpponentToll(state, botId, target) > cash)
                probability += DiceSumProbability(sum);
        }
        return probability;
    }

    /// <summary>
    /// position 칸이 다른 플레이어 소유 땅이면 통행료를, 아니면 0을 돌려줍니다.
    /// </summary>
    private long GetOpponentToll(GameState state, long botId, int position)
    {
        TileData tile = tileByIndex[position];
        if (tile == null || tile.Type != TileType.PROPERTY) return 0;

        PropertyState property = state.PropertyStates.Find(p => p.PropertyId == tile.PropertyId);
        if (property == null || property.OwnerId == null || property.OwnerId == botId) return 0;

        return getToll(property);
    }

    /// <summary>
    /// 주사위 두 개의 합이 sum일 확률. (7이 6/36으로 가장 높고, 2와 12가 1/36으로 가장 낮음)
    /// </summary>
    private static double DiceSumProbability(int sum)
    {
        return (6 - Math.Abs(sum - 7)) / 36.0;
    }

    private static long GetMoney(GameState state, long botId)
    {
        PlayerState player = state.PlayerStates.Find(p => p.PlayerId == botId);
        return player != null ? player.Money : 0;
    }

    /// <summary>
    /// 땅의 수익 비율 = 최대 통행료 ÷ 그 통행료까지 드는 총 투자금.
    /// 건설 가능한 땅은 호텔 기준, 건설 불가 땅은 땅 통행료 기준입니다.
    /// </summary>
    private static double GetMaxTollReturn(PropertyData data)
    {
        if (!data.CanBuild)
            return data.LandPrice > 0 ? (double)data.GetToll(BuildingLevel.Land) / data.LandPrice : 0;

        long invested = data.LandPrice
                        + data.GetBuildCost(BuildingLevel.Villa)
                        + data.GetBuildCost(BuildingLevel.Building)
                        + data.GetBuildCost(BuildingLevel.Hotel);
        return invested > 0 ? (double)data.GetToll(BuildingLevel.Hotel) / invested : 0;
    }
}