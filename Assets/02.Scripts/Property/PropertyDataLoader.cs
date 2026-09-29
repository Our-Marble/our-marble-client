using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/Data/properties.json을 읽어서 땅 번호별 데이터 테이블을 만든다.
/// </summary>
public static class PropertyDataLoader
{
    private const string Path = "Data/properties";

    public static Dictionary<int, PropertyData> Load()
    {
        var table = new Dictionary<int, PropertyData>();

        // TODO: Resources.Load<TextAsset>(Path)로 파일 읽기
        // TODO: JsonUtility.FromJson<PropertyDataList>()로 파싱
        // TODO: id 기준으로 table에 추가, 중복 id 경고

        Debug.Log($"[PropertyDataLoader] 땅 데이터 {table.Count}개 로드");
        return table;
    }
}