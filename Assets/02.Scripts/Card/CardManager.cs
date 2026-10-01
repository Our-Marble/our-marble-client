using System.Collections.Generic;
using UnityEngine;

// 황금 열쇠 카드 담당 (Singleton)
// - CardDeckData를 보유하고 CardId로 카드를 조회
// - 카드의 EffectType을 확인해 GameManager의 CardEffect 실행 메서드를 호출
// - 실제 게임 상태는 변경하지 않음 (GameManager 담당)
// - 추후 카드 연출 효과 재생 담당
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
    // CardId로 카드 조회 > 카드 연출 > EffectType에 맞는 GameManager CardEffect 실행 메서드 호출
    // 카드를 찾지 못하면 false (GameManager가 턴이 멈추지 않도록 처리)
    public bool PlayCard(int cardId)
    {
        if (deckData == null)
        {
            Debug.LogError("[CardManager] CardDeckData가 연결되지 않음", this);
            return false;
        }

        CardData card = deckData.FindById(cardId); // 사용 여부, 장수와 무관하게 CardId로만 조회
        if (card == null)
        {
            Debug.LogError($"[CardManager] CardId {cardId}에 해당하는 카드 없음", this);
            return false;
        }

        // TODO: 추후 카드 연출 효과 재생

        return CallCardEffect(card);
    }

    // 카드의 EffectType을 확인하고 GameManager의 해당 CardEffect 실행 메서드 호출
    private bool CallCardEffect(CardData card)
    {
        switch (card.effectType)
        {
            case CardEffectType.Bonus:
                GameManager.Instance.ExecuteBonusEffect(card.amount);
                return true;

            case CardEffectType.Penalty:
                GameManager.Instance.ExecutePenaltyEffect(card.amount, card.penaltyToFestivalPool);
                return true;

            case CardEffectType.MoveTo:
                GameManager.Instance.ExecuteMoveToEffect(card.targetTileId);
                return true;

            case CardEffectType.MoveBy:
                GameManager.Instance.ExecuteMoveByEffect(card.steps);
                return true;

            case CardEffectType.GoToInspection:
                GameManager.Instance.ExecuteGoToInspectionEffect();
                return true;

            default:
                Debug.LogError($"[CardManager] 처리하지 않은 CardEffectType: {card.effectType}", this);
                return false;
        }
    }
}
