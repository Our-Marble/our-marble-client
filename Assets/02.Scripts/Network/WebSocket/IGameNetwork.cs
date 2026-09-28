using UnityEngine;
using System;
using System.Threading.Tasks;

public interface IGameNetwork
{
    // 웹소켓 연결
    Task Connect();
    void Disconnect();

    // 클라 -> 서버
    void RollDice();
    void BuyProperty(int propertyId);
	//void DeclineBuyProperty(int propertyId);
    //void Build(int propertyId);
	//void DeclineBuild(int propertyId);
	//void AcquireProperty(int propertyId);
	//void DeclineAcquireProperty(int propertyId);

	// 서버 -> 모든 Client : 상태 변경 전파
	event Action<DiceRolledEvent> OnDiceRolled;
	event Action<PlayerMovedEvent> OnPlayerMoved;
	event Action<PropertyPurchasedEvent> OnPropertyPurchased;
	event Action<BuildingBuiltEvent> OnBuildingBuilt;
	event Action<PropertyAcquiredEvent> OnPropertyAcquired;
	event Action<TurnChangedEvent> OnTurnChanged;
	event Action<TollPaidEvent> OnTollPaid;
	event Action<StartPassedEvent> OnStartPassed;
	event Action<DonationPaidEvent> OnDonationPaid;
	event Action<WelfareFundReceivedEvent> OnWelfareFundReceived;
	
    // 서버 -> 특정 Client : 플레이어 입력 요구
    event Action<RollDicePrompt> OnRollDicePrompt;
    event Action<PurchasePropertyPrompt> OnPurchasePropertyPrompt;
    event Action<BuildPrompt> OnBuildPrompt;
    event Action<AcquirePropertyPrompt> OnAcquirePropertyPrompt;
    event Action<FreeTravelPrompt> OnFreeTravelPrompt;
    
    // 서버 -> 모든 Client || 서버 -> 재접속한 Client
    event Action<GameState> OnGameStateReceived;
}