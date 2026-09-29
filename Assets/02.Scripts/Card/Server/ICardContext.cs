using UnityEngine;

// 카드 효과가 게임 상태를 바꿀 때 사용하는 연결 지점
// 돈 관련 함수는 경제 로직 거쳐 처리
public interface ICardContext
{
    int BoardSize { get; }
    int InspectionTileId { get; }

    int GetPosition(long playerId);
}
