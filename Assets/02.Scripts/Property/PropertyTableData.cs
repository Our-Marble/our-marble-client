using System;
using UnityEngine;

/// <summary>
/// GM용 도시별 가격표. properties.json에서 읽는다. 게임 중 바뀌지 않는다.
/// 클라이언트 타일 정보는 PropertyData(ScriptableObject)가 따로 관리한다.
/// </summary>
[Serializable]
public class PropertyTableData
{
    [SerializeField] private int id;
    [SerializeField] private int boardIndex;
    [SerializeField] private string cityName;
    [SerializeField] private bool canBuild;
    [SerializeField] private long landPrice;
    [SerializeField] private long[] buildCosts = new long[3]; // 별장, 빌딩, 호텔
    [SerializeField] private long[] tolls = new long[4];      // 땅, 별장, 빌딩, 호텔

    public int Id => id;
    public int BoardIndex => boardIndex;
    public string CityName => cityName;
    public bool CanBuild => canBuild;
    public long LandPrice => landPrice;

    /// <summary>해당 단계 하나를 짓는 비용. 땅은 0.</summary>
    public long GetBuildCost(BuildingLevel level)
    {
        if (level == BuildingLevel.Land) return 0;
        return buildCosts[(int)level - 1];
    }

    /// <summary>해당 단계일 때 통행료.</summary>
    public long GetToll(BuildingLevel level)
    {
        return tolls[(int)level];
    }
}

/// <summary>JsonUtility는 최상위 배열을 못 읽어서 감싸는 용도.</summary>
[Serializable]
public class PropertyTableDataList
{
    public PropertyTableData[] properties;
}