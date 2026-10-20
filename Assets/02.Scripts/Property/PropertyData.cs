using System;
using UnityEngine;

/// <summary>
/// 도시별 고정 데이터(가격표) 틀. 게임 중 바뀌지 않는다.
/// </summary>
[Serializable]
public class PropertyData
{
    [SerializeField] private int id; // PropertyData의 id 값의 백의 자리 수는 mapId와 일치해야합니다.
    [SerializeField] private int boardIndex;
    [SerializeField] private string cityName;
    [SerializeField] private bool canBuild;
    [SerializeField] private long landPrice;

    [Tooltip("별장, 빌딩, 호텔 순서 (3개)")]
    [SerializeField] private long[] buildCosts = new long[3];

    [Tooltip("땅, 별장, 빌딩, 호텔 순서 (4개)")]
    [SerializeField] private long[] tolls = new long[4];

    public int Id => id;
    public int BoardIndex => boardIndex;
    public bool CanBuild => canBuild;
    public string CityName => cityName;
    public long LandPrice => landPrice;


    public int MapId => (int)(id / 100);
    

    /// <summary>해당 단계 하나를 짓는 비용. 땅은 0.</summary>
    public long GetBuildCost(BuildingLevel level)
    {
        if (level > BuildingLevel.Hotel) return 0;
        
        if (level == BuildingLevel.Land) return 0;
        return buildCosts[(int)level - 1];
    }

    /// <summary>해당 단계일 때 통행료.</summary>
    public long GetToll(BuildingLevel level)
    {
        return tolls[(int)level];
    }
}
