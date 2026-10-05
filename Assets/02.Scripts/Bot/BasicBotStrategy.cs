using System;
using System.Collections.Generic;

/// <summary>
/// 기본 봇 전략입니다.
/// 돈을 쓰고 난 뒤 다음 턴에 통행료를 현금으로 못 낼 위험이 크면 구매·건설·인수를 하지 않고,
/// 땅은 수익 비율이 좋은 땅 위주로 삽니다.
/// 매각할 때는 상대가 밟을 확률까지 고려해 기대 통행료 손실이 적은 땅부터 팝니다.
/// 건설·인수는 늘어나는 통행료로 비용을 회수하는 기간(라운드)을 따져 결정합니다.
/// </summary>
public class BasicBotStrategy : IBotStrategy
{
    private const int DefaultTravelDestination = 16;       // 기부금수령 칸 (기존 동작 유지)
    private const double MaxCashShortProbability = 0.2;    // 다음 턴 현금 부족 확률이 이보다 크면 돈을 쓰지 않음
                                                           // 소심하면 올리고, 파산이 잦으면 낮춘다.
    private const double GoodLandReturn = 0.75;            // 최대 통행료 ÷ 총 투자금이 이 이상이면 좋은 땅
    private const long SafeCashReserve = 200000;           // 보통 땅 구매·회수가 느린 건설은 이만큼 현금이 남을 때만 진행
    private const double AverageHitRate = 1.0 / 7;         // 주사위 평균 7칸 → 한 번 굴릴 때 특정 칸에 도착할 평균 확률
    private const long WelfareFundWorthTrip = 200000;      // 적립금이 이 이상이면 세계여행 최우선 목적지
    private const double MaxPaybackRounds = 10;            // 투자 비용을 이 라운드 안에 통행료로 회수할 수 있으면 건설·인수

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
        bool buy = IsWorthBuying(data, cashAfter);

        log?.Invoke($"[봇 판단] {botId}: {data.CityName} 수익 비율 {landReturn:F2}" +
                    $"({(isGoodLand ? "좋은 땅" : "보통 땅")}), 남는 현금 {cashAfter:N0} → {(buy ? "구매" : "포기")}");
        return buy;
    }

    public bool ShouldBuild(GameState state, long botId, int propertyId, long cost)
    {
        // 1) 통행료 위험이 크면 짓지 않습니다.
        if (!CanAffordSafely(state, botId, cost)) return false;

        PropertyState property = state.PropertyStates.Find(p => p.PropertyId == propertyId);
        PropertyData data = getPropertyData(propertyId);
        if (property == null || data == null) return true;
        if (!data.CanBuild || property.BuildingLevel >= BuildingLevel.Hotel) return false;

        // 2) 통행료 상승분으로 건설비를 빨리 회수할 수 있거나, 현금이 넉넉하면 짓습니다.
        BuildingLevel next = property.BuildingLevel + 1;
        long tollGain = data.GetToll(next) - data.GetToll(property.BuildingLevel);
        double payback = PaybackRounds(state, botId, propertyId, cost, tollGain);
        long cashAfter = GetMoney(state, botId) - cost;
        bool build = payback <= MaxPaybackRounds || cashAfter >= SafeCashReserve;

        log?.Invoke($"[봇 판단] {botId}: {data.CityName} {next} 건설, 통행료 +{tollGain:N0}, " +
                    $"회수 {FormatPayback(payback)}, 남는 현금 {cashAfter:N0} → {(build ? "건설" : "포기")}");
        return build;
    }

    public bool ShouldAcquire(GameState state, long botId, int propertyId, long price)
    {
        // 1) 통행료 위험이 크면 인수하지 않습니다.
        if (!CanAffordSafely(state, botId, price)) return false;

        PropertyState property = state.PropertyStates.Find(p => p.PropertyId == propertyId);
        PropertyData data = getPropertyData(propertyId);
        if (property == null || data == null) return true;

        // 2) 인수가가 비싸므로, 통행료 수입으로 빨리 회수할 수 있는 땅만 인수합니다.
        long toll = getToll(property);
        double payback = PaybackRounds(state, botId, propertyId, price, toll);
        bool acquire = payback <= MaxPaybackRounds;

        log?.Invoke($"[봇 판단] {botId}: {data.CityName} 인수가 {price:N0}, 통행료 {toll:N0}, " +
                    $"회수 {FormatPayback(payback)} → {(acquire ? "인수" : "포기")}");
        return acquire;
    }

    /// <summary>
    /// 매각가 1원당 잃는 기대 통행료가 적은 땅부터 담아 부족분을 채웁니다.
    /// 그 뒤 지킬 가치가 큰 땅부터, 빼도 부족분이 채워지면 다시 빼서 수익이 좋은 땅을 최대한 지킵니다.
    /// </summary>
    public List<int> ChooseSellProperties(GameState state, long botId, long requiredAmount,
                                          Func<PropertyState, long> getSellValue)
    {
        long shortage = requiredAmount - GetMoney(state, botId);

        List<PropertyState> owned = state.PropertyStates.FindAll(p => p.OwnerId == botId);
        owned.Sort((a, b) => TollLossPerWon(state, botId, a, getSellValue)
                            .CompareTo(TollLossPerWon(state, botId, b, getSellValue)));

        List<PropertyState> selected = new List<PropertyState>();
        long sum = 0;
        foreach (PropertyState p in owned)
        {
            if (sum >= shortage) break;
            selected.Add(p);
            sum += getSellValue(p);
        }

        // 지킬 가치가 큰 땅(기대 통행료 손실이 큰 땅)부터, 빼도 부족분을 채운다면 매각 목록에서 제외합니다.
        selected.Sort((a, b) => TollLossPerWon(state, botId, b, getSellValue)
                                .CompareTo(TollLossPerWon(state, botId, a, getSellValue)));
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

        log?.Invoke($"[봇 판단] {botId}: 부족분 {shortage:N0} → 매각 {selected.Count}개 (매각가 합 {sum:N0})");
        return selected.ConvertAll(p => p.PropertyId);
    }

    /// <summary>
    /// 이 땅을 팔 때 매각가 1원당 잃는 기대 통행료입니다. 값이 작을수록 팔아도 손해가 적은 땅입니다.
    /// 기대 통행료 = 통행료 × (상대별 장기 평균 도착 확률 + 다음 턴 실제 도착 확률)
    /// </summary>
    private double TollLossPerWon(GameState state, long botId, PropertyState property,
                                Func<PropertyState, long> getSellValue)
    {
        long sellValue = getSellValue(property);
        if (sellValue <= 0) return double.MaxValue; // 팔아도 돈이 안 되는 땅은 맨 뒤로

        double hitRate = OpponentHitRate(state, botId, property.PropertyId);
        return getToll(property) * hitRate / sellValue;
    }

    /// <summary>
    /// 살아 있는 상대들이 이 땅에 도착할 확률의 합입니다. (장기 평균 + 다음 주사위 기준 실제 확률)
    /// </summary>
    private double OpponentHitRate(GameState state, long botId, int propertyId)
    {
        int tilePosition = Array.FindIndex(tileByIndex, t => t != null && t.Type == TileType.PROPERTY && t.PropertyId == propertyId);
        if (tilePosition < 0) return 0;

        double rate = 0;
        foreach (PlayerState opponent in state.PlayerStates)
        {
            if (opponent.PlayerId == botId || opponent.IsBankrupt) continue;

            int distance = (tilePosition - opponent.Position + tileByIndex.Length) % tileByIndex.Length;
            double nextTurn = distance >= 2 && distance <= 12 ? DiceSumProbability(distance) : 0;
            rate += AverageHitRate + nextTurn;
        }
        return rate;
    }

    /// <summary>
    /// 투자 비용을 통행료로 회수하는 데 걸리는 라운드 수입니다.
    /// 한 라운드 기대 수입 = 통행료 × 상대들이 한 라운드에 이 땅을 밟을 확률의 합
    /// </summary>
    private double PaybackRounds(GameState state, long botId, int propertyId, long cost, long toll)
    {
        double incomePerRound = toll * OpponentHitRate(state, botId, propertyId);
        return incomePerRound > 0 ? cost / incomePerRound : double.MaxValue;
    }

    private static string FormatPayback(double payback)
    {
        return payback == double.MaxValue ? "불가" : $"{payback:F1}라운드";
    }
 
    /// <summary>
    /// 보드의 모든 칸을 평가해서 우선순위(tier)가 가장 높고, 같은 순위에서는 점수가 가장 높은 칸으로 갑니다.
    /// 갈 만한 칸이 없으면 기본 목적지로 갑니다.
    /// </summary>
    public int ChooseTravelDestination(GameState state, long botId)
    {
        PlayerState bot = state.PlayerStates.Find(p => p.PlayerId == botId);
        if (bot == null || tileByIndex.Length == 0) return DefaultTravelDestination;

        int bestPosition = -1;
        int bestTier = int.MaxValue;
        double bestScore = double.MinValue;

        for (int position = 0; position < tileByIndex.Length; position++)
        {
            if (position == bot.Position) continue;
            if (!TryScoreDestination(state, bot, position, out int tier, out double score)) continue;

            if (tier < bestTier || (tier == bestTier && score > bestScore))
            {
                bestPosition = position;
                bestTier = tier;
                bestScore = score;
            }
        }

        if (bestPosition < 0) bestPosition = DefaultTravelDestination;
        log?.Invoke($"[봇 판단] {botId}: 세계여행 목적지 {bestPosition}번 (순위 {bestTier}, 점수 {bestScore:F2})");
        return bestPosition;
    }

    /// <summary>
    /// 목적지 후보 칸의 우선순위(tier, 작을수록 좋음)와 같은 순위 안에서 비교할 점수를 구합니다.
    /// 가면 손해인 칸은 false를 돌려 후보에서 뺍니다.
    /// </summary>
    private bool TryScoreDestination(GameState state, PlayerState bot, int position, out int tier, out double score)
    {
        tier = 5;
        score = 0;

        TileData tile = tileByIndex[position];
        if (tile == null) return false;

        switch (tile.Type)
        {
            case TileType.CHARITY:
                if (state.WelfareFund <= 0) return true;                       // 받을 게 없으면 중립
                tier = state.WelfareFund >= WelfareFundWorthTrip ? 1 : 4;
                score = state.WelfareFund;
                return true;

            case TileType.GOLDEN_KEY:
            case TileType.START:
                return true;                                                    // 중립 (세계여행은 월급 없음)

            case TileType.PROPERTY:
                return TryScorePropertyDestination(state, bot, position, tile.PropertyId, out tier, out score);

            default:
                return false;                                                   // 무인도, 세무조사, 세계여행
        }
    }

    /// <summary>
    /// 땅 칸 평가: 살 만한 빈 땅(2순위), 건물을 올릴 내 땅(3순위), 그 외 빈 땅·내 땅(중립), 남의 땅(제외).
    /// </summary>
    private bool TryScorePropertyDestination(GameState state, PlayerState bot, int position, int propertyId,
                                            out int tier, out double score)
    {
        tier = 5;
        score = 0;

        PropertyState property = state.PropertyStates.Find(p => p.PropertyId == propertyId);
        PropertyData data = getPropertyData(propertyId);
        if (property == null || data == null) return false;

        // 빈 땅: 구매 규칙을 통과하면 수익 비율로 비교
        if (property.OwnerId == null)
        {
            long price = data.LandPrice;
            if (IsSafeToSpendAt(state, bot, position, price) && IsWorthBuying(data, bot.Money - price))
            {
                tier = 2;
                score = GetMaxTollReturn(data);
            }
            return true;
        }

        // 내 땅: 다음 단계를 지을 수 있으면 통행료 상승폭으로 비교
        if (property.OwnerId == bot.PlayerId)
        {
            if (data.CanBuild && property.BuildingLevel < BuildingLevel.Hotel)
            {
                BuildingLevel next = property.BuildingLevel + 1;
                long cost = data.GetBuildCost(next);
                if (IsSafeToSpendAt(state, bot, position, cost))
                {
                    tier = 3;
                    score = data.GetToll(next) - data.GetToll(property.BuildingLevel);
                }
            }
            return true;
        }

        return false; // 남의 땅은 통행료만 내므로 제외
    }

    /// <summary>
    /// position 칸에서 cost를 쓰고 난 뒤, 다음 턴 현금 부족 확률이 기준 이하인지 확인합니다. (로그 없음)
    /// </summary>
    private bool IsSafeToSpendAt(GameState state, PlayerState bot, int position, long cost)
    {
        if (bot.Money < cost) return false;
        return CashShortProbability(state, bot.PlayerId, position, bot.Money - cost) <= MaxCashShortProbability;
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

    /// <summary>
    /// 구매 규칙: 좋은 땅이면 사고, 보통 땅은 사고 나서 현금이 넉넉히 남을 때만 삽니다.
    /// </summary>
    private static bool IsWorthBuying(PropertyData data, long cashAfter)
    {
        return GetMaxTollReturn(data) >= GoodLandReturn || cashAfter >= SafeCashReserve;
    }
}