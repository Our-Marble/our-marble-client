using Newtonsoft.Json.Linq;
using UnityEngine;

public class LobbyNetwork : Singleton<LobbyNetwork>
{
    public void ReceiveLobbyMessage(string json)
    {
        JObject jsonObject = JObject.Parse(json);
        string type = jsonObject["type"]?.ToString() ?? ""; // type 키가 없으면 null 대신 빈 문자열 할당
        
        switch (type)
        {
            case "TEMP":
            {
                
                break;
            }
            default:
            {
                Debug.Log($"식별할 수 없는 type입니다.: {type}");
                break;
            }
        }
    }
}
