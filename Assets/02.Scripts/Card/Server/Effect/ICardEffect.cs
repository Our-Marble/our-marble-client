using UnityEngine;

// 카드 효과 하나
public interface ICardEffect
{
    void Apply(CardDefinition card, long playerId, ICardContext ctx, CardResult result);
}
