using System;
using System.Collections.Generic;
using UnityEngine;

public class BonusEffect : ICardEffect
{
    public void Apply(CardDefinition card, long playerId, ICardContext ctx, CardResult result)
    {
        result.outcomes.Add(new CardOutcome
        {
            type = CardOutcomeType.MoneyGained,
            playerId = playerId,
            amount = card.amount,
        });
    }
}

public class PenaltyEffect : ICardEffect
{
    public void Apply(CardDefinition card, long playerId, ICardContext ctx, CardResult result)
    {
        result.outcomes.Add(new CardOutcome
        {
            type = CardOutcomeType.MoneyPaid,
            playerId = playerId,
            amount = card.amount,
            toFestivalPool = card.penaltyToFestivalPool,
        });
    }
}

/// <summary>지정 칸으로 앞으로 이동. 출발 지점을 지나면 월급</summary>
public class MoveToEffect : ICardEffect
{
    public void Apply(CardDefinition card, long playerId, ICardContext ctx, CardResult result)
    {
        int from = ctx.GetPosition(playerId);
        int to = card.targetTileId;
        bool passedStart = to < from; // GameManager와 같은 규칙: toPosition < fromPosition 이면 월급

        CardEffectUtil.AddMove(result, playerId, from, to, passedStart);
    }
}

/// <summary>N칸 이동. 음수면 뒤로 가고, 뒤로 갈 때는 월급 없음</summary>
public class MoveByEffect : ICardEffect
{
    public void Apply(CardDefinition card, long playerId, ICardContext ctx, CardResult result)
    {
        int size = ctx.BoardSize;
        int from = ctx.GetPosition(playerId);
        int to = ((from + card.steps) % size + size) % size;
        bool passedStart = card.steps > 0 && from + card.steps >= size;

        CardEffectUtil.AddMove(result, playerId, from, to, passedStart);
    }
}

public class GoToInspectionEffect : ICardEffect
{
    public void Apply(CardDefinition card, long playerId, ICardContext ctx, CardResult result)
    {
        result.outcomes.Add(new CardOutcome
        {
            type = CardOutcomeType.SentToInspection,
            playerId = playerId,
            fromTileId = ctx.GetPosition(playerId),
            toTileId = ctx.InspectionTileId,
        });
        // 무인도행은 턴을 넘기므로 칸 효과를 다시 처리하지 않음
    }
}

static class CardEffectUtil
{
    public static void AddMove(CardResult result, long playerId, int from, int to, bool passedStart)
    {
        result.outcomes.Add(new CardOutcome
        {
            type = CardOutcomeType.Moved,
            playerId = playerId,
            fromTileId = from,
            toTileId = to,
            passedStart = passedStart,
        });
        result.requiresTileResolve = true;
        result.landedTileId = to;
    }
}

public static class CardEffectFactory
{
    static readonly Dictionary<CardEffectType, ICardEffect> effects = new Dictionary<CardEffectType, ICardEffect>
    {
        { CardEffectType.Bonus,          new BonusEffect() },
        { CardEffectType.Penalty,        new PenaltyEffect() },
        { CardEffectType.MoveTo,         new MoveToEffect() },
        { CardEffectType.MoveBy,         new MoveByEffect() },
        { CardEffectType.GoToInspection, new GoToInspectionEffect() },
    };

    public static ICardEffect Get(CardEffectType type)
    {
        if (effects.TryGetValue(type, out ICardEffect effect)) return effect;
        throw new NotImplementedException($"등록되지 않은 카드 효과: {type}");
    }
}