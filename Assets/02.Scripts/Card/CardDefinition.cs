using System;

// 카드 한 장의 규칙
// 추후 서버는 ScriptableObject 말고 이 클래스를 사용할 예정
[Serializable]
public class CardDefinition
{
    public int id;
    public string name;
    public string description;
    public CardEffectType effectType;

    // 효과별 파라미터 (필요한 값만 사용)
    public int amount;                  // 금액: Bonus, Penalty
    public int targetTileId;            // 목적지 칸 번호: MoveTo
    public int steps;                   // 이동 칸 수: MoveBy
    public bool penaltyToFestivalPool;  // true면 푸드 페스티벌 적립금
}
