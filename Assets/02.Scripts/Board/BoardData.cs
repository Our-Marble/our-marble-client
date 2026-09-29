using System;
using System.Collections.Generic;
using UnityEngine;

public enum BoardTileType
{
    START,
    PROPERTY,
    GOLDEN_KEY
}

[Serializable]
public class TileData
{
    [SerializeField, Min(0)] private int index;
    [SerializeField] private BoardTileType type;
    [Tooltip("PROPERTY 타입에서만 사용합니다.")]
    [SerializeField] private int propertyId;

    public int Index => index;
    public BoardTileType Type => type;
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
