using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class WebSocketNetwork : Singleton<WebSocketNetwork>
{
    private string serverUrl = "ws://localhost:8080/ws";
    
    private WebSocket websocket;
    public long LocalPlayerId { get; private set; }
    public string LocalNickname { get; private set; }
    
    protected override void OnAwake()
    {
        Debug.Log($"[WS] Awake  id={GetInstanceID()} scene={gameObject.scene.name} root={transform.parent == null}", this);
        Application.runInBackground = true;
    }

    private void OnDestroy()
    {
        Debug.Log($"[WS] OnDestroy id={GetInstanceID()}", this);
    }
    
    public async void Connect()
    {
        Debug.Log($"[WS] Connect id={GetInstanceID()}\n{Environment.StackTrace}", this);
        
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
            string type = jsonObject["type"]?.ToString() ?? ""; // type 키가 없으면 null 대신 빈 문자열 할당

            if (type == "CONNECTED")
            {
                ConnectedResponse connectedResponse = JsonConvert.DeserializeObject<ConnectedResponse>(json);

                LocalPlayerId = connectedResponse.playerId;
                LocalNickname = connectedResponse.nickname;

                LobbyNetwork.Instance.InvokeOnConnected(LocalNickname);
            }
            else if(Enum.TryParse(type, out LobbyMessageType lobbyMessageTypeType))
            {
                LobbyNetwork.Instance.ReceiveLobbyMessage(lobbyMessageTypeType, json);
            }
            else if(Enum.TryParse(type, out GameMessageType gameMessageType))
            {
                GameNetwork.Instance.ReceiveGameMessage(gameMessageType, json);
            }
            else if(type == "COMMON_ERROR")
            {
                Debug.LogError("잘못된 메시지 형식입니다. 클라이언트 측 송신 형식을 수정하세요.");
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
        try
        {
            if (websocket == null)
            {
                Debug.LogError($"[WS] websocket null. id={GetInstanceID()} scene={gameObject.scene.name}", this);
                Debug.LogError("[WS] websocket이 null입니다. (아직 Connect 전이거나 다른 인스턴스)", this);
                return;
            }
            if (websocket.State != WebSocketState.Open)
            {
                Debug.LogWarning($"[WS] WebSocket이 연결되어 있지 않습니다. 상태: {websocket.State}");
                return;
            }

            Debug.Log($"서버에게 메시지 송신: {message}");
            await websocket.SendText(message);
        }
        catch (Exception e)
        {
            Debug.LogException(e); // async 안의 예외는 여기서 잡아야 콘솔에 보인다
        }
    }
    
    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}
