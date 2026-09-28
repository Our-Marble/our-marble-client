using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MockGameNetwork : IGameNetwork
{
    private GameState gameState;
    System.Random rand = new System.Random();
    
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
    
    public Task Connect()
    {
        gameState = new  GameState();
        
        
        
        OnGameStateReceived?.Invoke(gameState);
        
        return Task.CompletedTask;
    }

    public void Disconnect()
    {

    }
    
    public void RollDice()
    {
        int dice1 = rand.Next(1, 6);
        int dice2 = rand.Next(1, 6);

        OnDiceRolled?.Invoke(
            new DiceRolledEvent
            {
                PlayerId = 0,
                Dice1 = dice1,
                Dice2 = dice2
            });
    }

    public void BuyProperty(int propertyId)
    {
        
    }
}