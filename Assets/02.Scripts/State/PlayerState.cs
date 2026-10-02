using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    public long PlayerId { get; set; }
    public int Position { get; set; }
    public long Money { get; set; }
    
    // 무인도 탈출까지 남은 턴 수
    public int IslandTurnsRemaining { get; set; }
    
    // 황금열쇠에서 얻은 사용카드
    public List<int> CardIds { get; set; }

    // 파산 여부
    public bool IsBankrupt { get; set; }

    // 최종 등수. 0이면 아직 정해지지 않음
    // - 파산 시: 그 시점에 남아 있던 인원 수 (먼저 파산할수록 낮은 등수)
    // - 게임 종료 시: 살아남은 플레이어에게 1위부터 부여
    public int FinalRank { get; set; }

    public PlayerState(long playerId)
    {
        PlayerId = playerId;
        Position = 0;
        Money = 0;
        IslandTurnsRemaining = 0;
        CardIds = new List<int>();
        IsBankrupt = false;
        FinalRank = 0;
    }
}
