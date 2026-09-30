using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType // TileData 와 매칭되게 TileType으로 수정했습니다. 타일데이터 명이 BoardTileData 였다면 타입 명도 BoardTileType으로 유지했을것입니다. <- 김건욱
{
    START,
    PROPERTY,
    GOLDEN_KEY,
    ISLAND,         // 무인도
    DONATION,       // 기부금 납부
    CHARITY,        // 기부금 수령
    WORLD_TRAVEL    // 자유여행
}

[Serializable]
public class TileData
{
    [SerializeField, Min(0)] private int index;
    [SerializeField] private TileType type;
    [Tooltip("PROPERTY 타입에서만 사용합니다.")]
    [SerializeField] private int propertyId;

    public int Index => index;
    public TileType Type => type;
    public int PropertyId => propertyId;
}

[CreateAssetMenu(fileName = "NewBoardInfo", menuName = "Board/BoardData")]
public class BoardData : ScriptableObject
{
    [SerializeField, Min(1)] private int mapId = 1;
    [SerializeField] private List<TileData> tiles = new();

    public int MapId => mapId;
    public IReadOnlyList<TileData> Tiles => tiles;
}
