using System;
using System.Collections.Generic;

/// <summary>
/// 기존 GameManager에 있던 봇 판단을 그대로 옮긴 기본 전략입니다.
/// "돈이 있으면 무조건 한다" 수준이며, 경제 판단 고도화는 여기에 추가합니다.
/// </summary>
public class BasicBotStrategy : IBotStrategy
{
    private const int DefaultTravelDestination = 16; // 기부금수령 칸 (기존 동작 유지)

    public bool ShouldPurchase(GameState state, long botId, int propertyId, long price)
    {
        return GetMoney(state, botId) >= price;
    }

    public bool ShouldBuild(GameState state, long botId, int propertyId, long cost)
    {
        return GetMoney(state, botId) >= cost;
    }

    public bool ShouldAcquire(GameState state, long botId, int propertyId, long price)
    {
        return GetMoney(state, botId) >= price;
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

    private static long GetMoney(GameState state, long botId)
    {
        PlayerState player = state.PlayerStates.Find(p => p.PlayerId == botId);
        return player != null ? player.Money : 0;
    }
}