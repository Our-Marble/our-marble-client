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
    private string serverUrl => HttpNetwork.Instance.isLocal? "ws://localhost:8080/ws" : "ws://" + HttpNetwork.Instance.EC2PublicAdress + "/ws";
    
    protected override bool PersistAcrossScenes => true;
    
    private WebSocket websocket;
    public long LocalPlayerId { get; private set; }

    /// <summary>서버와 연결이 열려 있는지.</summary>
    public bool IsOpen => websocket != null && websocket.State == WebSocketState.Open;
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
        WebSocket socket = websocket; // 이 연결이 닫혔을 때, 우리가 직접 닫은 것인지 구분하는 데 쓴다

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

            // closeWebSocketSession/OnApplicationQuit으로 직접 닫은 경우 websocket을 먼저 비워 두므로 여기서 걸러진다.
            // (로비의 나가기 → ToLogin → 닫힘 → 다시 ToLogin 되는 것과, 그때 '끊겼다' 토스트가 뜨는 것을 막는다)
            if (websocket != socket) return;

            // 의도하지 않은 끊김: 로그인 화면으로 돌아가 안내한다
            websocket = null;
            SceneFlow.ToLogin("연결이 끊겼습니다");
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
                if (connectedResponse.portraitId.HasValue) PortraitLibrary.ApplyFromServer(connectedResponse.portraitId.Value); // 서버에 저장된 내 초상화

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
            WebSocket oldWebSocket = websocket;
            websocket = null;
            await oldWebSocket.Close();
        }
    }

    public async void closeWebSocketSession()
    {
        if (websocket != null)
        {
            WebSocket oldWebSocket = websocket;
            websocket = null;
            await oldWebSocket.Close();
        }
    }
}
