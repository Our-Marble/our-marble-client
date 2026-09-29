using UnityEngine;

// 에디터에서 카드 한 장
[CreateAssetMenu(menuName = "Card/Card Data", fileName = "CardSO")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public int id;
    public string cardName;
    [TextArea] public string description;
    public Sprite icon; // 클라이언트 표시 아이콘

    [Header("효과")]
    public CardEffectType effectType;
    [Min(0)] public int amount;
    public int targetTileId;
    public int steps;
    public bool penaltyToFestivalPool;

    public CardDefinition ToDefinition()
    {
        return new CardDafinition
        {
            id = id,
            name = cardName,
            description = description,
            effectType = effectType,
            amount = amount,
            targetTileId = targetTileId,
            steps = steps,
            penaltyToFestivalPool = penaltyToFestivalPool
        };
    }
}
