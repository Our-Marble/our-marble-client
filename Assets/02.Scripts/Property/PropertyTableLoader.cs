using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GM용. Resources/Data/properties.json을 읽어서 땅 번호별 가격표 테이블을 만든다.
/// </summary>
public static class PropertyTableLoader
{
    private const string Path = "Data/properties";

    public static Dictionary<int, PropertyTableData> Load()
    {
        var table = new Dictionary<int, PropertyTableData>();

        var file = Resources.Load<TextAsset>(Path);
        if (file == null)
        {
            Debug.LogError($"[PropertyTableLoader] Resources/{Path}.json 없음");
            return table;
        }

        var list = JsonUtility.FromJson<PropertyTableDataList>(file.text);
        if (list == null || list.properties == null)
        {
            Debug.LogError("[PropertyTableLoader] JSON 파싱 실패 ({ \"properties\": [...] } 형식인지 확인)");
            return table;
        }

        foreach (var data in list.properties)
        {
            if (!table.TryAdd(data.Id, data))
            {
                Debug.LogWarning($"[PropertyTableLoader] 땅 번호 중복: {data.Id} ({data.CityName})");
            }
        }

        Debug.Log($"[PropertyTableLoader] 땅 데이터 {table.Count}개 로드");
        return table;
    }
}