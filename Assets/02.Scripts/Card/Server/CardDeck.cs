using System;
using System.Collections.Generic;

// 카드 더미, 뽑을 카드가 없으면 버린 더미를 섞어서 다시 사용
public class CardDeck
{
    readonly List<CardDefinition> drawPile;     // 뽑을 더미
    readonly List<CardDefinition> discardPile = new List<CardDefinition>();     // 버린 더미
    readonly ICardRandom random;    // 섞을 때 쓰는 랜덤 값

    public int DrawPileCount => drawPile.Count;         // 남은 카드 수
    public int DiscardPileCount => discardPile.Count;   // 버린 카드 수
    

    public CardDeck(IEnumerable<CardDefinition> cards, ICardRandom random)
    {
        // 카드 목록 및 랜덤 값 둘 중 하나라도 NULL이면 ArgumentNullException
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        drawPile = new List<CardDefinition>(cards ?? throw new ArgumentNullException(nameof(cards))); 
        Shuffle(drawPile);
    }

    public CardDefinition Draw()
    {
        if (drawPile.Count == 0) 
            Reshuffle();

        // 버린 더미로 섞었는데도 비었으면 오류
        if (drawPile.Count == 0)
            throw new InvalidOperationException("[CardDeck] 덱에 카드가 없음. CardDeckData 구성 확인 요함.");
            
        int last = drawPile.Count - 1;
        CardDefinition card = drawPile[last];
        drawPile.RemoveAt(last);
        return card;
    }

    // 사용 카드 버림
    public void Discard(CardDefinition card)
    {
        if (card != null)
            discardPile.Add(card);
    }

    // 버린 더미 모아서 다시 섞기
    void Reshuffle()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
    }

    // Fisher-Yates 셔플
    void Shuffle(List<CardDefinition> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            CardDefinition temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
