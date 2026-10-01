using UnityEngine;

// 특수칸 담당 (Singleton)
// - 대상: 시작, 무인도, 세무조사(DONATION), 푸드 페스티벌(CHARITY), 세계 여행
// - 특수칸 연출 재생
public class SpecialTileManager : Singleton<SpecialTileManager>
{
    #region 연출
    // TODO: 연출 완료 후 다음 흐름 진행이 필요해지면 처리 방식 결정 (콜백 전달 또는 코루틴)
    public void PlayTaxPaid(long playerId, long amount)
    {
        // TODO: 세무조사 벌금 납부 연출
    }

    public void PlayWelfareFundReceived(long playerId, long amount)
    {
        // TODO: 푸드 페스티벌 적립금 획득 연출
    }

    public void PlayIslandArrived(long playerId)
    {
        // TODO: 무인도 도착 연출
    }

    public void PlaySentToIsland(long playerId, int fromPosition, int islandPosition)
    {
        // TODO: 무인도로 순간이동하는 연출
    }

    public void PlayIslandEscapeFailed(long playerId, int remainingTurns)
    {
        // TODO: 무인도 탈출 실패 연출
    }

    public void PlayIslandEscaped(long playerId)
    {
        // TODO: 무인도 탈출 연출
    }

    public void PlayWorldTravel(long playerId, int fromPosition, int toPosition)
    {
        // TODO: 세계 여행 이동 연출
    }
    #endregion
}
