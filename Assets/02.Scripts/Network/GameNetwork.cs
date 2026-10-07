using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GameNetwork : Singleton<GameNetwork>
{
    private Queue<GameBroadCastMessage> messageQueue = new Queue<GameBroadCastMessage>();
    public int QueuedMessageCount => messageQueue.Count;
    
    private async void SendGameMessage(string message)
    {
        await WebSocketNetwork.Instance.SendMessage(message);
    }

    public void ReceiveGameMessage(string json)
    {
        JObject jsonObject = JObject.Parse(json);
        string typeString = jsonObject["type"]?.ToString() ?? ""; // type 키가 없으면 null 대신 빈 문자열 할당
        
        if (Enum.TryParse<BroadcastType>(typeString, out BroadcastType parsedType))
        {
            Debug.Log($"식별할 수 없는 type입니다.: {typeString}");
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
    
    public void TryDequeueMessage(BroadcastType waitingType)
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
        
        BroadcastType type = message.Type;
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
            case BroadcastType.BUILT:
            {
                BuiltBroadcast builtBroadcast =
                    JsonUtility.FromJson<BuiltBroadcast>(json);

                OnBuilt?.Invoke(builtBroadcast.playerId, builtBroadcast.propertyId, builtBroadcast.isAccept);
                break;
            }
            case BroadcastType.CARD_DRAWN:
            {
                CardDrawnBroadcast cardDrawnBroadcast =
                    JsonUtility.FromJson<CardDrawnBroadcast>(json);
                
                OnCardDrawn?.Invoke(cardDrawnBroadcast.playerId, cardDrawnBroadcast.cardId);
                break;
            }
            case BroadcastType.DESTINATION_CHOSEN:
            {
                DestinationChosenBroadcast destinationChosenBroadcast =
                    JsonUtility.FromJson<DestinationChosenBroadcast>(json);
                
                OnDestinationChosen?.Invoke(destinationChosenBroadcast.playerId, destinationChosenBroadcast.destinationPosition);
                break;
            }
            case BroadcastType.DICE_ROLLED:
            {
                DiceRolledBroadcast diceRolledBroadcast =
                    JsonUtility.FromJson<DiceRolledBroadcast>(json);
                
                OnDiceRolled?.Invoke(diceRolledBroadcast.playerId, diceRolledBroadcast.dice1, diceRolledBroadcast.dice2);
                break;
            }
            case BroadcastType.PROPERTIES_SOLD:
            {
                PropertiesSoldBroadcast propertiesSoldBroadcast =
                    JsonUtility.FromJson<PropertiesSoldBroadcast>(json);
                
                OnPropertiesSold?.Invoke(propertiesSoldBroadcast.playerId, propertiesSoldBroadcast.propertyIds);
                break;
            }
            case BroadcastType.PROPERTY_ACQUIRED:
            {
                PropertyAcquiredBroadcast propertyAcquiredBroadcast =
                    JsonUtility.FromJson<PropertyAcquiredBroadcast>(json);
                
                OnPropertyAcquired?.Invoke(propertyAcquiredBroadcast.playerId, propertyAcquiredBroadcast.propertyId, propertyAcquiredBroadcast.isAccept);
                break;
            }
            case BroadcastType.PROPERTY_PURCHASED:
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
            type = RequestType.BUILD,
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
            type = RequestType.DRAW_CARD
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendChooseDestinationRequest(int destinationPosition)
    {
        ChooseDestinationRequest message = new ChooseDestinationRequest
        {
            type = RequestType.CHOOSE_DESTINATION,
            destinationPosition = destinationPosition
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendRollDiceRequest()
    {
        RollDiceRequest message = new RollDiceRequest
        {
            type = RequestType.ROLL_DICE
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }

    public void SendSellPropertiesRequest(List<int> propertyIds)
    {
        SellPropertiesRequest message = new SellPropertiesRequest
        {
            type = RequestType.SELL_PROPERTIES,
            propertyIds = propertyIds
        };
        
        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
    
    public void SendAcquirePropertyRequest(int propertyId, bool isAccept)
    {
        PropertyDecisionRequest message = new PropertyDecisionRequest
        {
            type = RequestType.ACQUIRE_PROPERTY,
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
            type = RequestType.PURCHASE_PROPERTY,
            propertyId = propertyId,
            isAccept = isAccept
        };

        string json = JsonConvert.SerializeObject(message);
        WebSocketNetwork.Instance.SendMessage(json);
    }
}

public class GameBroadCastMessage
{
    public BroadcastType Type;
    public string Json;
}