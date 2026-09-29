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
        [Min(1)] public int count = 1;
    }

    public List<Entry> entries = new List<Entry>();

    // 로직에 넘길 카드 목록 (장수만큼 복제)
    public List<CardDefinition> BuildDefinitions()
    {
        var list = new List<CardDefinition>();
        foreach (var entry in entries)
        {
            if (entry.card == null)
                continue;

            for (int i = 0; i < entry.count; i++)
            {
                list.Add(entry.card.ToDefinition());
            }
        }

        return list;
    }

    public CardDeckData FindById(int id)
    {
        foreach (avr entry in entries)
        {
            if (entry.card != null && entry.card.id == id)
                return entry.card;

            return null;
        }
    }
}