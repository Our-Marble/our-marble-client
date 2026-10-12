using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GameNetwork : Singleton<GameNetwork>
{
    private Queue<GameBroadCastMessage> messageQueue = new Queue<GameBroadCastMessage>();
    
    private async void SendGameMessage(string message)
    {
        await WebSocketNetwork.Instance.SendMessage(message);
    }

    public void ReceiveGameMessage(string json)
    {
        JObject jsonObject = JObject.Parse(json);
        string typeString  = jsonObject["type"]?.ToString() ?? ""; // type 키가 없으면 null 대신 빈 문자열 할당
        
        if (Enum.TryParse(typeString, out GameMessageType parsedType))
        {
            Debug.Log($"식별할 수 없는 type입니다.: {typeString}");
            return;
        }
        
        // GameState 동기화에 대한 메시지인 경우 큐를 거치지 않고 즉시 처리
        if(parsedType ==  GameMessageType.GAME_STATE_SYNC)
        {
            HandleGameStateSync(json);
            return;
        }
        
        GameBroadCastMessage message = new GameBroadCastMessage
        {
            Type = parsedType,
            Json = json
        };

        lock (messageQueue) // 메인 스레드와 네트워크 스레드 충돌 대비 락 (네트워크 스레드가 Enqueue하는 동안에는 메인 스레드의 접근 차단)
        {
            messageQueue.Enqueue(message);
        }
        
        Debug.Log($"[GameNetwork] 메시지 큐 적재: {parsedType} (현재 큐 대기 수: {messageQueue.Count})");
    }
    
    public void TryDequeueMessage(GameMessageType waitingType)
    {
        GameBroadCastMessage message;
        
        lock (messageQueue)
        {
            if (messageQueue.Count > 0)
            {
                message = messageQueue.Dequeue();
            }
            else
            {
                return;
            }
        }
        
        GameMessageType type = message.Type;
        string json = message.Json;
        
        // 현재 클라이언트에서 기대하는 type과 큐의 가장 앞에있는 type이 일치하지 않는다면, 클라이언트의 시나리오와 서버의 시나리오가 다르게 흘러가고 있다는 뜻임.
        // Warning Log를 띄우고, 클라이언트에게 새로고침을 요구해야함.
        // 게임 진행이 멈추진 않지만, 흐름이 완전히 꼬여버릴것. ex) A차례인데 B가 건설하는 등의 상황 발생 가능.
        if (type != waitingType)
        {
            Debug.LogWarning("클라이언트에서 기대하는 type과 서버에서 보내준 브로드캐스트 메시지의 type이 일치하지 않습니다. 정상 진행을 위해 반드시 새로고침을 하세요");
            // 새로고침을 강제로 하지는 않고, 유저에게 알림만 보냄.
        }

         switch (type)
        {
            case GameMessageType.BUILT:
            {
                BuiltBroadcast builtBroadcast =
                    JsonUtility.FromJson<BuiltBroadcast>(json);

                OnBuilt?.Invoke(builtBroadcast.playerId, builtBroadcast.propertyId, builtBroadcast.isAccept);
                break;
            }
            case GameMessageType.CARD_DRAWN:
            {
                CardDrawnBroadcast cardDrawnBroadcast =
                    JsonUtility.FromJson<CardDrawnBroadcast>(json);
                
                OnCardDrawn?.Invoke(cardDrawnBroadcast.playerId, cardDrawnBroadcast.cardId);
                break;
            }
            case GameMessageType.DESTINATION_CHOSEN:
            {
                DestinationChosenBroadcast destinationChosenBroadcast =
                    JsonUtility.FromJson<DestinationChosenBroadcast>(json);
                
                OnDestinationChosen?.Invoke(destinationChosenBroadcast.playerId, destinationChosenBroadcast.destinationPosition);
                break;
            }
            case GameMessageType.DICE_ROLLED:
            {
                DiceRolledBroadcast diceRolledBroadcast =
                    JsonUtility.FromJson<DiceRolledBroadcast>(json);
                
                OnDiceRolled?.Invoke(diceRolledBroadcast.playerId, diceRolledBroadcast.dice1, diceRolledBroadcast.dice2);
                break;
            }
            case GameMessageType.PROPERTIES_SOLD:
            {
                PropertiesSoldBroadcast propertiesSoldBroadcast =
                    JsonUtility.FromJson<PropertiesSoldBroadcast>(json);
                
                OnPropertiesSold?.Invoke(propertiesSoldBroadcast.playerId, propertiesSoldBroadcast.propertyIds);
                break;
            }
            case GameMessageType.PROPERTY_ACQUIRED:
            {
                PropertyAcquiredBroadcast propertyAcquiredBroadcast =
                    JsonUtility.FromJson<PropertyAcquiredBroadcast>(json);
                
                OnPropertyAcquired?.Invoke(propertyAcquiredBroadcast.playerId, propertyAcquiredBroadcast.propertyId, propertyAcquiredBroadcast.isAccept);
                break;
            }
            case GameMessageType.PROPERTY_PURCHASED:
            {
                PropertyPurchasedBroadcast propertyPurchasedBroadcast =
                    JsonUtility.FromJson<PropertyPurchasedBroadcast>(json);
                
                OnPropertyPurchased?.Invoke(propertyPurchasedBroadcast.playerId, propertyPurchasedBroadcast.propertyId, propertyPurchasedBroadcast.isAccept);
                break;
            }
            default:
            {
                Debug.LogError($"식별할 수 없는 type입니다.: {type}");
                break;
            }
        }
    }

    // GameNetwork는 GameManager의 존재를 몰라도 되게 하기 위해서, GameManager의 함수를 직접 호출하는 대신, 이벤트 방식을 사용할것이다.
    public event Action<long, int, bool> OnBuilt; // GameManager의 OnEnable에서  GameNetwork.Instance.OnDiceRolled += HandleDiceRolled; 하면 된다.
    public event Action<long, int> OnCardDrawn;
    public event Action<long, int> OnDestinationChosen;
    public event Action<long, int, int> OnDiceRolled;
    public event Action<long, List<int>> OnPropertiesSold;
    public event Action<long, int, bool> OnPropertyAcquired;
    public event Action<long, int, bool> OnPropertyPurchased;
   
    public void SendBuildRequest(int propertyId, bool isAccept)
    {
        PropertyDecisionRequest message = new PropertyDecisionRequest
        {
            type = "BUILD",
            propertyId = propertyId,
            isAccept = isAccept
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendDrawCardRequest()
    {
        DrawCardRequest message = new DrawCardRequest
        {
            type = "DRAW_CARD"
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendChooseDestinationRequest(int destinationPosition)
    {
        ChooseDestinationRequest message = new ChooseDestinationRequest
        {
            type = "CHOOSE_DESTINATION",
            destinationPosition = destinationPosition
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendRollDiceRequest()
    {
        RollDiceRequest message = new RollDiceRequest
        {
            type = "ROLL_DICE"
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }

    public void SendSellPropertiesRequest(List<int> propertyIds)
    {
        SellPropertiesRequest message = new SellPropertiesRequest
        {
            type = "SELL_PROPERTIES",
            propertyIds = propertyIds
        };
        
        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendAcquirePropertyRequest(int propertyId, bool isAccept)
    {
        PropertyDecisionRequest message = new PropertyDecisionRequest
        {
            type = "ACQUIRE_PROPERTY",
            propertyId = propertyId,
            isAccept = isAccept
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendPurchasePropertyRequest(int propertyId, bool isAccept)
    {
        PropertyDecisionRequest message = new PropertyDecisionRequest
        {
            type = "PURCHASE_PROPERTY",
            propertyId = propertyId,
            isAccept = isAccept
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public event Action<GameState> OnSync;
    
    /// <summary>
    /// 새로고침 응답을 받았을 때의 특수 처리 함수
    /// </summary>
    private void HandleGameStateSync(string json)
    {
        // 정상적인 GameState가 왔는지 확인
        GameState serverGameState = JsonConvert.DeserializeObject<GameState>(json);
        if (serverGameState.WaitingType == null)
        {
            Debug.Log("[GameNetwork] 서버측 GameState가 아직 준비되지 않았습니다. 5초 뒤 재요청합니다.");
            Invoke("SendGameStateRequest", 5f);
            return;
        }

        Debug.Log("[GameNetwork] 싱크 응답 수신. 큐를 초기화하고 상태를 강제 동기화합니다.");

        // 메시지 큐 Clear
        lock (messageQueue)
        {
            messageQueue.Clear();
        }

        OnSync?.Invoke(serverGameState);
    }
    
    public void SendGameStateRequest()
    {
        GameStateRequest message = new GameStateRequest
        {
            type = "GAME_STATE_REQUEST"
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
}

public class GameBroadCastMessage
{
    public GameMessageType Type;
    public string Json;
}
