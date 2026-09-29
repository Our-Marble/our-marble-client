using System.Collections.Generic;
using UnityEngine;

public class PropertyTable : MonoBehaviour
{
    public static PropertyTable Instance { get; private set; }

    [SerializeField] private TextAsset propertyJson;
    [SerializeField] private List<PropertyData> properties = new();

    private Dictionary<int, PropertyData> propertiesById;

    public IReadOnlyList<PropertyData> Properties => properties;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (propertyJson != null)
        {
            ApplyJson(propertyJson);
        }
        else
        {
            RebuildLookup();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public PropertyData GetData(int id)
    {
        return TryGetById(id, out PropertyData data) ? data : null;
    }

    public PropertyData GetById(int id)
    {
        return TryGetById(id, out PropertyData data) ? data : null;
    }

    public bool TryGetById(int id, out PropertyData data)
    {
        if (propertiesById == null)
        {
            RebuildLookup();
        }

        return propertiesById.TryGetValue(id, out data);
    }

    public void ApplyJson(TextAsset jsonFile)
    {
        if (jsonFile == null)
        {
            Debug.LogError("[PropertyTable] JSON TextAsset이 없습니다.", this);
            return;
        }

        ApplyJson(jsonFile.text);
    }

    public void ApplyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[PropertyTable] JSON 문자열이 비어 있습니다.", this);
            return;
        }

        JsonUtility.FromJsonOverwrite(json, this);
        RebuildLookup();
    }

    private void RebuildLookup()
    {
        propertiesById = new Dictionary<int, PropertyData>();

        if (properties == null)
        {
            properties = new List<PropertyData>();
            return;
        }

        foreach (PropertyData data in properties)
        {
            if (data == null)
            {
                continue;
            }

            if (!propertiesById.TryAdd(data.Id, data))
            {
                Debug.LogWarning($"[PropertyTable] 땅 번호 중복: {data.Id} ({data.CityName})", this);
            }
        }
    }
}
