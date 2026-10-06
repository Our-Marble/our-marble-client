using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class WebSocketNetwork : Singleton<WebSocketNetwork>
{
    private WebSocket websocket;
    
    private void Awake()
    {
        // NativeWebSocket 공식 문서에는 WebGL에서 브라우저 탭이 포커스를 잃었을 때 Unity 게임 루프가 멈출 수 있어서 Application.runInBackground = true를 설정하는 것을 권장
        Application.runInBackground = true;
    }
    
    private async void Start()
    {
        websocket = new WebSocket("ws://localhost:8080/ws");

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket 연결 성공");
        };

        websocket.OnError += (errorMsg) =>
        {
            Debug.LogError($"WebSocket 오류: {errorMsg}");
        };

        websocket.OnClose += (closeCode) =>
        {
            Debug.Log($"WebSocket 연결 종료: {closeCode}");
        };

        websocket.OnMessage += (bytes) =>
        {
            string json = Encoding.UTF8.GetString(bytes);

            Debug.Log($"서버로부터 메시지 수신: {json}");
            
            JObject jsonObject = JObject.Parse(json);
            string topic = jsonObject["topic"]?.ToString() ?? ""; // topic 키가 없으면 null 대신 빈 문자열 할당
            
            switch (topic)
            {
                case "LOBBY":
                {
                    LobbyNetwork.Instance.ReceiveLobbyMessage(json);
                    break;
                }
                case "GAME":
                {
                    GameNetwork.Instance.ReceiveGameMessage(json);
                    break;
                }
                default:
                {
                    Debug.Log($"식별할 수 없는 topic입니다.: {topic}");
                    break;
                }
            }
            
        };

        await websocket.Connect();
    }
    
    public async Task SendMessage(string message)
    {
        if (websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket이 연결되어 있지 않습니다.");
            return;
        }

        await websocket.SendText(message);
    }
    
    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}
