using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WebSocketGameNetwork : IGameNetwork
{
    private readonly string serverUrl;
    private WebSocket webSocket;
    
    public event Action<DiceRolledEvent> OnDiceRolled;
    public event Action<PlayerMovedEvent> OnPlayerMoved;
    public event Action<PropertyPurchasedEvent> OnPropertyPurchased;
    public event Action<BuildingBuiltEvent> OnBuildingBuilt;
    public event Action<PropertyAcquiredEvent> OnPropertyAcquired;
    public event Action<TurnChangedEvent> OnTurnChanged;
    public event Action<TollPaidEvent> OnTollPaid;
    public event Action<StartPassedEvent> OnStartPassed;
    public event Action<DonationPaidEvent> OnDonationPaid;
    public event Action<WelfareFundReceivedEvent> OnWelfareFundReceived;
    
    public event Action<RollDicePrompt> OnRollDicePrompt;
    public event Action<PurchasePropertyPrompt> OnPurchasePropertyPrompt;
    public event Action<BuildPrompt> OnBuildPrompt;
    public event Action<AcquirePropertyPrompt> OnAcquirePropertyPrompt;
    public event Action<FreeTravelPrompt> OnFreeTravelPrompt;
    
    public event Action<GameState> OnGameStateReceived;

    public WebSocketGameNetwork(string serverUrl)
    {
        this.serverUrl = serverUrl;
    }
    
    public async Task Connect()
    {
        webSocket = new WebSocket(serverUrl);

        webSocket.OnOpen += OnOpen;
        webSocket.OnError += OnError;
        webSocket.OnClose += OnClose;
        webSocket.OnMessage += OnMessage;

        await webSocket.Connect();
    }

    public void Disconnect()
    {
        if (webSocket == null)
            return;

        webSocket.Close();
        webSocket = null;
    }
    
    public void RollDice()
    {
        if (webSocket == null ||
            webSocket.State != WebSocketState.Open)
        {
            return;
        }

        var request = new
        {
            type = "ROLL_DICE"
        };

        string json = JsonConvert.SerializeObject(request);

        webSocket.SendText(json);
    }
    
    public void BuyProperty(int propertyId)
    {
        if (webSocket == null ||
            webSocket.State != WebSocketState.Open)
        {
            return;
        }

        var request = new
        {
            type = "BUY_PROPERTY",
            propertyId
        };

        string json = JsonConvert.SerializeObject(request);

        webSocket.SendText(json);
    }
    
    private void OnOpen()
    {
        UnityEngine.Debug.Log("WebSocket Connected");
    }

    private void OnError(string error)
    {
        UnityEngine.Debug.LogError($"WebSocket Error: {error}");
    }

    private void OnClose(WebSocketCloseCode closeCode)
    {
        UnityEngine.Debug.Log($"WebSocket Closed: {closeCode}");
    }
    
    private void OnMessage(byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);
        
        var jsonObject = JObject.Parse(json);

        string type = jsonObject["type"]?.Value<string>();
        
        if (string.IsNullOrWhiteSpace(type))
        {
            Debug.LogWarning("Received message without type.");
            return;
        }

        switch (type)
        {
            case "DICE_ROLLED":
            {
                var diceRolledEvent = JsonConvert.DeserializeObject<DiceRolledEvent>(json);
                if (diceRolledEvent != null)
                {
                    OnDiceRolled?.Invoke(diceRolledEvent);
                }
                break;
            }
            
            case "TURN_CHANGED":
            {
                var turnChangedEvent = JsonConvert.DeserializeObject<TurnChangedEvent>(json);
                if (turnChangedEvent != null)
                {
                    OnTurnChanged?.Invoke(turnChangedEvent);
                }
                break;
            }
            
            default:
                Debug.LogWarning($"Unknown message type: {type}");
                break;
        }
    }
}
