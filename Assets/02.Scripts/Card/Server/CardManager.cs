using UnityEngine;

// 카드 뽑기 + 효과 적용 담당
// 다음 흐름(ProcessArrival, 턴 종료)은 GameManager가 CardResult를 보고 결정
// 추후 Draw의 판단 부분은 서버로 이동
public class CardManager : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] CardDeckData deckData;

    CardSystem cardSystem;
    ICardContext context;

    void Awake()
    {
        cardSystem = new CardSystem(deckData.BuildDefinitions(), new SystemCardRandom());
        context = new GameStateCardContext(gameManager);
    }

    // GameManager.DrawCard에서 호출. 효과를 적용하고 결과를 돌려줌
    public CardResult Draw(long playerId)
    {
        CardResult result = cardSystem.Draw(playerId, context);

        gameManager.HandleCardDrawn(result.cardId); // 카드 연출 (UI 매니저 담당)
        ApplyOutcomes(result);

        return result; // requiresTileResolve, landedTileId, sentToInspection으로 GameManager가 다음 흐름 결정
    }

    void ApplyOutcomes(CardResult result)
    {
        foreach (CardOutcome o in result.outcomes)
        {
            switch (o.type)
            {
                case CardOutcomeType.MoneyGained:
                    // 은행에서 받기. 돈 처리는 GameManager에서
                    gameManager.HandleCardMoneyChanged(o.playerId, o.amount);
                    break;

                case CardOutcomeType.MoneyPaid:
                    // 돈 처리(부족 시 매각/파산 포함)는 GameManager에서
                    if (o.toFestivalPool)
                        gameManager.HandleDonationPaid(o.playerId, o.amount);       // 적립금으로
                    else
                        gameManager.HandleCardMoneyChanged(o.playerId, -o.amount); // 은행으로
                    break;

                case CardOutcomeType.Moved:
                    // 위치 갱신, 월급, 이동 연출은 HandlePlayerMoved가 처리
                    gameManager.HandlePlayerMoved(o.playerId, o.fromTileId, o.toTileId, o.passedStart);
                    break;

                case CardOutcomeType.SentToInspection:
                    // 위치, 영업정지 턴 설정과 연출은 HandleSentToIsland가 처리 (월급 없음)
                    gameManager.HandleSentToIsland(o.playerId, o.fromTileId, o.toTileId);
                    break;
            }
        }
    }
}
