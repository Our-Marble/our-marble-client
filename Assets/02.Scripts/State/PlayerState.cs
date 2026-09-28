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
    
}
