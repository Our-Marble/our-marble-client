using System.Collections.Generic;
using UnityEngine;

// 황금 열쇠 카드 담당 (Singleton)
// - CardDeckData를 보유하고 CardId로 카드 데이터를 조회만 함
// - 카드 판단(랜덤 선택), 효과 분기, 게임 상태 변경은 하지 않음 (GameManager 담당)
// - GameManager를 참조하지 않음 (GameManager → CardManager 단방향)
public class CardManager : Singleton<CardManager>
{
    [SerializeField] private CardDeckData deckData; // 카드 종류 및 카드 데이터

    // 덱에 등록된 CardId 목록. GameManager가 랜덤 CardId를 결정할 때 사용
    public List<int> GetCardIds()
    {
        if (deckData == null)
        {
            Debug.LogError("[CardManager] CardDeckData가 연결되지 않음", this);
            return new List<int>();
        }

        return deckData.GetCardIds();
    }

    // GameManager.HandleCardDrawn에서 호출
    // CardId로 카드 데이터를 조회합니다. (사용 여부, 장수와 무관)
    // 덱이 연결되지 않았거나 카드를 찾지 못하면 null (GameManager가 턴이 멈추지 않도록 처리)
    public CardData FindCard(int cardId)
    {
        if (deckData == null)
        {
            Debug.LogError("[CardManager] CardDeckData가 연결되지 않음", this);
            return null;
        }

        CardData card = deckData.FindById(cardId);
        if (card == null)
            Debug.LogError($"[CardManager] CardId {cardId}에 해당하는 카드 없음", this);

        return card;
    }
}
