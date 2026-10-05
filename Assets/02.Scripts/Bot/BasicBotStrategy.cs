using System;
using System.Collections.Generic;

/// <summary>
/// 기본 봇 전략입니다.
/// 돈이 있어도, 쓰고 난 뒤 다음 턴에 통행료를 현금으로 못 낼 위험이 크면 구매·건설·인수를 하지 않습니다.
/// </summary>
public class BasicBotStrategy : IBotStrategy
{
    private const int DefaultTravelDestination = 16;       // 기부금수령 칸 (기존 동작 유지)
    private const double MaxCashShortProbability = 0.2;    // 다음 턴 현금 부족 확률이 이보다 크면 돈을 쓰지 않음

    private readonly TileData[] tileByIndex;               // 칸 번호 → 칸 정보
    private readonly Func<PropertyState, long> getToll;    // 통행료 계산
    private readonly Func<int, long> getLandPrice;         // 땅값 조회

    public BasicBotStrategy(IReadOnlyList<TileData> tiles,
                            Func<PropertyState, long> getToll,
                            Func<int, long> getLandPrice)
    {
        // 리스트 순서와 칸 번호가 달라도 안전하도록 Index 기준으로 다시 배치합니다.
        tileByIndex = new TileData[tiles.Count];
        foreach (TileData tile in tiles)
        {
            if (tile != null && tile.Index >= 0 && tile.Index < tileByIndex.Length)
                tileByIndex[tile.Index] = tile;
        }

        this.getToll = getToll;
        this.getLandPrice = getLandPrice;
    }

    public bool ShouldPurchase(GameState state, long botId, int propertyId, long price)
    {
        return CanAffordSafely(state, botId, price);
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
        return CashShortProbability(state, botId, bot.Position, cashAfter) <= MaxCashShortProbability;
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
}