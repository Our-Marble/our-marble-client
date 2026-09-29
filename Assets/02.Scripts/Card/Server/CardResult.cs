using System;
using System.Collections.Generic;

public enum CardOutcomeType
{
    MoneyGained,
    MoneyPaid,
    Moved,
    SentToInspection,
}

// 카드 효과로 일어날 일
[Serializable]
public class CardOutcome
{
    public CardOutcomeType type;
    public long playerId;
    public long amount;
    public int fromTileId = -1;
    public int toTileId = -1;
    public bool passedStart;
    public bool toFestivalPool;
}

// 카드 한장의 결과
// 추후 서버 > 클라 응답 DTO로 예상
[Serializable]
public class CardResult
{
    public long playerId;
    public int cardId;
    public List<CardOutcome> outcomes = new List<CardOutcome>();

    // 이동 카드처럼 새 칸에 도착한 경우 true
    // 이 값이 true면 GameManager가 ProcessArrival(playerId, landedTileId)로 도착 칸을 처리
    public bool requiresTileResolve;
    public int landedTileId = -1;

    // 무인도행 카드면 true. GameManager가 이 값을 보고 턴을 넘김
    public bool sentToInspection;

    public CardResult(long playerId, int cardId)
    {
        this.playerId = playerId;
        this.cardId = cardId;
    }
}
