using UnityEngine;
using System.Collections.Generic;
using System;

// 카드 덱
[CreateAssetMenu(menuName = "Card/Card Deck Data", fileName = "Card Deck SO")]
public class CardDeckData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public CardData card;
    }

    public List<Entry> entries = new List<Entry>();

    // cardId로 표시용 데이터(이름, 설명, 아이콘) 찾기. UI 매니저가 카드 연출할 때 사용
    public CardData FindById(int id)
    {
        foreach (var entry in entries)
        {
            if (entry.card != null && entry.card.id == id)
                return entry.card;
        }

        return null;
    }

    // 덱에 등록된 CardId 목록 (같은 id는 한 번만). 카드 장수, 사용 여부와 무관
    // GameManager가 랜덤 CardId를 결정할 때 사용
    public List<int> GetCardIds()
    {
        var ids = new List<int>();
        foreach (var entry in entries)
        {
            if (entry.card == null)
                continue;

            if (!ids.Contains(entry.card.id))
                ids.Add(entry.card.id);
        }

        return ids;
    }
}