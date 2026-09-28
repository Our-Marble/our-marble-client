using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameState gameState;
    private IGameNetwork network;

    private async void Start()
    {
        network = new MockGameNetwork();
        //network = new WebSocketGameNetwork("serverUrl");
        
        network.OnDiceRolled += HandleDiceRolled;
        network.OnPlayerMoved += HandlePlayerMoved;
        network.OnPropertyPurchased += HandlePropertyPurchased;
        network.OnBuildingBuilt += HandleBuildingBuilt;
        network.OnPropertyAcquired += HandlePropertyAcquired;
        network.OnTollPaid += HandleTollPaid;
        network.OnStartPassed += HandleStartPassed;
        network.OnDonationPaid += HandleDonationPaid;
        network.OnWelfareFundReceived += HandleWelfareFundReceived;
        network.OnTurnChanged += HandleTurnChanged;

        network.OnRollDicePrompt += HandleRollDicePrompt;
        
        network.OnGameStateReceived += HandleGameStateReceived;
        
        await network.Connect();
    }

    private void Update()
    {
        // 웹소켓 서버 구현시 주석 해제
        //network.DispatchMessageQueue();
    }
    
    // 웹소켓 연결 직후 or 재접속시 게임 진행 상태 갱신 
    private void HandleGameStateReceived(GameState _gameState)
    {
        gameState = _gameState;
    }
    
    // '상태 변경 전파' 메시지 핸들
    private void HandleDiceRolled(DiceRolledEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId}, 주사위: {data.Dice1}, {data.Dice2}");
    }
    
    private void HandlePlayerMoved(PlayerMovedEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId}, 이동 전 위치: {data.FromPosition}, 이동 후 위치: {data.ToPosition}");
    }
    
    private void HandlePropertyPurchased(PropertyPurchasedEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId}, 구매한 자산: {data.PropertyId}, 지불 금액: {data.Amount}");
    }
    
    private void HandleBuildingBuilt(BuildingBuiltEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId}, 건설한 자산: {data.PropertyId}, 건설 레벨: {data.BuildingLevel}, 지불 금액: {data.Amount}");
    }
    
    private void HandlePropertyAcquired(PropertyAcquiredEvent data)
    {
        Debug.Log($"인수인: {data.NewOwnerId}, 피인수인: {data.PreviousOwnerId}, 인수한 자산: {data.PropertyId}, 인수 비용: {data.Amount}");
    }
    
    private void HandleTollPaid(TollPaidEvent data)
    {
        Debug.Log($"납부자: {data.PayerId}, 수납자: {data.ReceiverId}, 인수 비용: {data.Amount}");
    }
    
    private void HandleStartPassed(StartPassedEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId}, 받은 금액: {data.Amount}");
    }
    
    private void HandleDonationPaid(DonationPaidEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId}, 기부한 금액: {data.Amount}");
    }
    
    private void HandleWelfareFundReceived(WelfareFundReceivedEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId}, 받은 금액: {data.Amount}");
    }
    
    private void HandleTurnChanged(TurnChangedEvent data)
    {
        Debug.Log($"플레이어: {data.PlayerId} 의 차례");
    }
    
    // '플레이어 입력 요구' 메시지 핸들 
    private void HandleRollDicePrompt(RollDicePrompt data)
    {
        Debug.Log("주사위를 굴리세요");
    }
    
    private void OnDestroy()
    {
        if (network == null)
            return;

        network.OnDiceRolled -= HandleDiceRolled;
        network.OnPlayerMoved -= HandlePlayerMoved;
        network.OnPropertyPurchased -= HandlePropertyPurchased;
        network.OnBuildingBuilt -= HandleBuildingBuilt;
        network.OnPropertyAcquired -= HandlePropertyAcquired;
        network.OnTollPaid -= HandleTollPaid;
        network.OnStartPassed -= HandleStartPassed;
        network.OnDonationPaid -= HandleDonationPaid;
        network.OnWelfareFundReceived -= HandleWelfareFundReceived;
        network.OnTurnChanged -= HandleTurnChanged;

        network.OnRollDicePrompt -= HandleRollDicePrompt;
        
        network.OnGameStateReceived -= HandleGameStateReceived;
    }
}
