using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GameNetwork : Singleton<GameNetwork>
{
    private async void SendGameMessage(string message)
    {
        await WebSocketNetwork.Instance.SendMessage(message);
    }

    public void ReceiveGameMessage(string json)
    {
        JObject jsonObject = JObject.Parse(json);
        string type = jsonObject["type"]?.ToString() ?? ""; // type 키가 없으면 null 대신 빈 문자열 할당
        
        switch (type)
        {
            case "BUILT":
            {
                BuiltBroadcast message =
                    JsonUtility.FromJson<BuiltBroadcast>(json);

                OnBuilt?.Invoke(message.playerId, message.propertyId);
                break;
            }
            case "CARD_DRAWN":
            {
                CardDrawnBroadcast message =
                    JsonUtility.FromJson<CardDrawnBroadcast>(json);
                
                OnCardDrawn?.Invoke(message.playerId, message.cardId);
                break;
            }
            case "DESTINATION_CHOSEN":
            {
                DestinationChosenBroadcast message =
                    JsonUtility.FromJson<DestinationChosenBroadcast>(json);
                
                OnDestinationChosen?.Invoke(message.playerId, message.destinationPosition);
                break;
            }
            case "DICE_ROLLED":
            {
                DiceRolledBroadcast message =
                    JsonUtility.FromJson<DiceRolledBroadcast>(json);
                
                OnDiceRolled?.Invoke(message.playerId, message.dice1, message.dice2);
                break;
            }
            case "PROPERTIES_SOLD":
            {
                PropertiesSoldBroadcast message =
                    JsonUtility.FromJson<PropertiesSoldBroadcast>(json);
                
                OnPropertiesSold?.Invoke(message.playerId, message.propertyIds);
                break;
            }
            case "PROPERTY_ACQUIRED":
            {
                PropertyAcquiredBroadcast message =
                    JsonUtility.FromJson<PropertyAcquiredBroadcast>(json);
                
                OnPropertyAcquired?.Invoke(message.playerId, message.propertyId);
                break;
            }
            case "PROPERTY_PURCHASED":
            {
                PropertyPurchasedBroadcast message =
                    JsonUtility.FromJson<PropertyPurchasedBroadcast>(json);
                
                OnPropertyPurchased?.Invoke(message.playerId, message.propertyId);
                break;
            }
            default:
            {
                Debug.Log($"식별할 수 없는 type입니다.: {type}");
                break;
            }
        }
    }

    // GameNetwork는 GameManager의 존재를 몰라도 되게 하기 위해서, GameManager의 함수를 직접 호출하는 대신, 이벤트 방식을 사용할것이다.
    public event Action<long, int> OnBuilt; // GameManager의 OnEnable에서  GameNetwork.Instance.OnDiceRolled += HandleDiceRolled; 하면 된다.
    public event Action<long, int> OnCardDrawn;
    public event Action<long, int> OnDestinationChosen;
    public event Action<long, int, int> OnDiceRolled;
    public event Action<long, List<int>> OnPropertiesSold;
    public event Action<long, int> OnPropertyAcquired;
    public event Action<long, int> OnPropertyPurchased;
   
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
}
