using System.Collections.Generic;

/// <summary>
/// 플레이어 순위 계산. GameManager(게임 종료 로그)와 UIManager(등수, 결과 창)가 같은 기준을 쓰도록 한 곳에 둔다.
/// 순위 기준
///   1. 파산하지 않은 플레이어가 앞
///   2. 총자산(현금 + 가진 땅의 투자금)이 많은 순
///   3. 총자산이 같으면 현금이 많은 순
///   4. 그래도 같으면 원래 순서(PlayerStates 순서) 유지
/// </summary>
public static class PlayerRanking
{
    /// <summary>
    /// 두 플레이어의 순위를 비교한다. a가 앞이면 음수, b가 앞이면 양수, 같으면 0.
    /// 파산 여부는 호출하는 쪽에서 넘긴다. (UI는 파산 연출 중인 플레이어도 파산으로 취급하기 때문)
    /// </summary>
    public static int Compare(PlayerState a, bool aBankrupt, PlayerState b, bool bBankrupt, List<PropertyState> propertyStates)
    {
        if (aBankrupt != bBankrupt) return aBankrupt ? 1 : -1;

        long assetA = PropertyManager.Instance.GetTotalAsset(a.PlayerId, a.Money, propertyStates);
        long assetB = PropertyManager.Instance.GetTotalAsset(b.PlayerId, b.Money, propertyStates);
        if (assetA != assetB) return assetB.CompareTo(assetA);

        return b.Money.CompareTo(a.Money);
    }

    /// <summary>순위 순으로 정렬한 새 목록을 돌려준다. (원래 목록은 바꾸지 않음, 완전히 같으면 원래 순서 유지)</summary>
    public static List<PlayerState> Rank(List<PlayerState> players, List<PropertyState> propertyStates)
    {
        var order = new List<int>();
        for (int i = 0; i < players.Count; i++) order.Add(i);

        order.Sort((x, y) =>
        {
            int result = Compare(players[x], players[x].IsBankrupt, players[y], players[y].IsBankrupt, propertyStates);
            return result != 0 ? result : x.CompareTo(y);
        });

        var ranked = new List<PlayerState>();
        foreach (int index in order) ranked.Add(players[index]);
        return ranked;
    }
}
