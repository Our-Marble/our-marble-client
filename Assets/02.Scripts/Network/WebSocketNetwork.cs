using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class WebSocketNetwork : Singleton<WebSocketNetwork>
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 로컬 테스트용 (HTTP / WS)
    private string serverUrl = "ws://localhost:8080/ws";
#else
    // 실제 EC2 배포용 (HTTPS / WSS)
    private string serverUrl = "wss://your-domain.com/ws";
#endif
    
    private WebSocket websocket;
    public long LocalPlayerId { get; private set; }

    private void Awake()
    {
        // NativeWebSocket 공식 문서에는 WebGL에서 브라우저 탭이 포커스를 잃었을 때 Unity 게임 루프가 멈출 수 있어서 Application.runInBackground = true를 설정하는 것을 권장
        Application.runInBackground = true;
    }
    
    public async void Connect()
    {
        if (websocket != null)
        {
            Debug.LogWarning("이미 TCP연결이 되어있습니다.");
            return;
        }
        
        // 저장된 토큰 가져오기
        string token = HttpNetwork.Instance.JwtToken;
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("JWT 토큰이 없습니다. 먼저 로그인해주세요.");
            return;
        }
        
        // 핸드셰이크 헤더 설정 (Authorization 헤더 추가)
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer " + token }
        };
        
        websocket = new WebSocket(serverUrl, headers);

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
            string type = jsonObject["topic"]?.ToString() ?? ""; // topic 키가 없으면 null 대신 빈 문자열 할당
            
            if(Enum.TryParse(type, out LobbyMessageType lobbyMessageTypeType))
            {
                LobbyNetwork.Instance.ReceiveLobbyMessage(lobbyMessageTypeType, json);
            }
            else if(Enum.TryParse(type, out GameMessageType gameMessageType))
            {
                GameNetwork.Instance.ReceiveGameMessage(gameMessageType, json);
            }
            else
            {
                Debug.Log($"식별할 수 없는 type입니다.: {type}");
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
