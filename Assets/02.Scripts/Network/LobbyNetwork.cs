using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class LobbyNetwork : Singleton<LobbyNetwork>
{
    private Queue<LobbyServerMessage> messageQueue = new Queue<LobbyServerMessage>();
    
    private async void SendGameMessage(string message)
    {
        await WebSocketNetwork.Instance.SendMessage(message);
    }

    public void ReceiveLobbyMessage(LobbyMessageType parsedType, string json)
    {
        LobbyServerMessage serverMessage = new LobbyServerMessage
        {
            Type = parsedType,
            Json = json
        };

        lock (messageQueue) // 메인 스레드와 네트워크 스레드 충돌 대비 락 (네트워크 스레드가 Enqueue하는 동안에는 메인 스레드의 접근 차단)
        {
            messageQueue.Enqueue(serverMessage);
        }
        
        Debug.Log($"[GameNetwork] 메시지 큐 적재: {parsedType} (현재 큐 대기 수: {messageQueue.Count})");
    }
    
    public void TryDequeueMessage()
    {
        LobbyServerMessage serverMessage;
        
        lock (messageQueue)
        {
            if (messageQueue.Count > 0)
            {
                serverMessage = messageQueue.Dequeue();
            }
            else
            {
                return;
            }
        }
        
        LobbyMessageType type = serverMessage.Type;
        string json = serverMessage.Json;

        switch (type)
        {
            case LobbyMessageType.CONNECTED:
            {
                ConnectedResponse connectedResponse =
                    JsonConvert.DeserializeObject<ConnectedResponse>(json);

                OnCreateRoom?.Invoke();
                break;
            }
            case LobbyMessageType.ROOM_ENTERED:
            {
                RoomEnteredResponse roomEnteredResponse =
                    JsonConvert.DeserializeObject<RoomEnteredResponse>(json);
                
                break;
            }
            case LobbyMessageType.ROOM_LEFT:
            {
                RoomLeftResponse roomLeftResponse =
                    JsonConvert.DeserializeObject<RoomLeftResponse>(json);
                
                break;
            }
            case LobbyMessageType.ROOM_LIST:
            {
                RoomListResponse roomListResponse =
                    JsonConvert.DeserializeObject<RoomListResponse>(json);
                
                break;
            }
            case LobbyMessageType.ROOM_UPDATED:
            {
                RoomUpdatedBroadcast roomUpdatedBroadcast =
                    JsonConvert.DeserializeObject<RoomUpdatedBroadcast>(json);
                
                break;
            }
            case LobbyMessageType.LOBBY_ERROR:
            {
                LobbyErrorResponse lobbyErrorResponse =
                    JsonConvert.DeserializeObject<LobbyErrorResponse>(json);
                
                Debug.LogError($"에러 메시지 수신. code: {lobbyErrorResponse.code}, message: {lobbyErrorResponse.message}");
                
                // 여기서 뭘해야하지..? 떠잇는 로딩 창 닫기?
                
                break;
            }
            default:
            {
                Debug.LogError($"식별할 수 없는 type입니다.: {type}");
                break;
            }
        }
    }
    
    public event Action OnCreateRoom;
   
    public void SendCreateRoomRequest()
    {

    }
}

public class LobbyServerMessage
{
    public LobbyMessageType Type;
    public string Json;
}
