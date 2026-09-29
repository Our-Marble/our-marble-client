using System;
using UnityEngine;

/// <summary>
/// 도시별 고정 데이터(가격표) 틀. 게임 중 바뀌지 않는다.
/// properties.json에서 읽는다.
/// </summary>
[Serializable]
public class PropertyData
{
    [SerializeField] private int id;
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
    public string CityName => cityName;
    public bool CanBuild => canBuild;
    public long LandPrice => landPrice;

    /// <summary>해당 단계 하나를 짓는 비용. 땅은 0.</summary>
    public long GetBuildCost(BuildingLevel level)
    {
        // TODO: 건설 불가 땅 처리
        if (level == BuildingLevel.Land) return 0;
        return buildCosts[(int)level - 1];
    }

    /// <summary>해당 단계일 때 통행료.</summary>
    public long GetToll(BuildingLevel level)
    {
        // TODO: 건설 불가 땅 처리 (tolls 1개)
        return tolls[(int)level];
    }
}

/// <summary>JsonUtility는 최상위 배열을 못 읽어서 감싸는 용도.</summary>
[Serializable]
public class PropertyDataList
{
    public PropertyData[] properties;
}