using System.Collections.Generic;

// 카드를 뽑아 CardResult 생성하여 돌려줌
public class CardSystem
{
    readonly CardDeck deck;

    public int DrawPileCount => deck.DrawPileCount;

    public CardSystem(IEnumerable<CardDefinition> cards, ICardRandom random)
    {
        deck = new CardDeck(cards, random);
    }

    public CardResult Draw(long playerId, ICardContext ctx)
    {
        CardDefinition card = deck.Draw();
        var result = new CardResult(playerId, card.id);

        CardEffectFactory.Get(card.effectType).Apply(card, playerId, ctx, result);

        // MVP는 즉시 발동 카드만 있으므로 바로 버린 더미로.
        // 보관형 카드를 추가할 때는 여기서 플레이어 보관함으로 보내는 분기가 필요
        deck.Discard(card);
        return result;
    }
}