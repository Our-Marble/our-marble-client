using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public GameState gameState;

    private System.Random random; // 선턴 정하기나 랜덤 주사위 값을 계산할 때 사용합니다.

    public List<long> playerOrder;

    [SerializeField] private CardManager cardManager; // 황금 열쇠 카드 처리

    private bool isDouble; // 추가 턴 진행 여부를 결정하는데 사용됩니다.
    private int consecutiveDoubleCount; // 추후 3연속 더블시 무인도행을 판정할 때 사용합니다.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // gameState 초기화
        gameState = new  GameState();
        gameState.TurnNumber = 0;
        gameState.CurrentPlayerId = 0;
        gameState.WelfareFund = 0;
        
        gameState.PlayerStates = new List<PlayerState>();
        gameState.PlayerStates.Add(new  PlayerState(123));
        gameState.PlayerStates.Add(new  PlayerState(456));
        
        gameState.PropertyStates = new List<PropertyState>();
        foreach (PropertyData propertyData in  PropertyManager.Instance.GetAllDataByMapId(1))
        {
            gameState.PropertyStates.Add(new PropertyState(propertyData.Id));
        }
        

        // 그 외 필드 변수 초기화
        random = new System.Random();
        playerOrder = new List<long>();
        isDouble = false;
        consecutiveDoubleCount = 0;
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {

    }

    void StartGame()
    {
        // 선턴 정하기 이벤트 발생. MVP에서는 생략합니다.

        // 결과를 playerOrder 에 저장. MVP에서는 플레이어가 무조건 선턴입니다. 플레이어의 playerId는 123, 봇의 playerId는 456 입니다.
        playerOrder.Add(123);
        playerOrder.Add(456);

        // 모든 플레이어에게 초기자금 지급
        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            playerState.Money += 10000;
        }
        
        // 첫 번째 순서부터 턴 시작
        HandleTurnChanged(playerOrder[0]);
    }

    public void HandleTurnChanged(long playerId)
    {
        PlayerState playerState = GetPlayerState(playerId);
        
        // gameState.CurrentPlayerId 를 playerId로 갱신합니다. gameState.TurnNumber를 1 증가시킵니다.
        gameState.CurrentPlayerId = playerId;
        gameState.TurnNumber += 1;
        
        // 'OO의 턴'이라는 UI를 표시합니다.
        Debug.Log($"playerId : {playerId} 의 차례"); // 추후 UI띄우는 함수 호출로 변경. 일단은 로그만 찍는다.

        // 필드 변수를 갱신합니다.
        isDouble = false;
        consecutiveDoubleCount = 0;

        int playerPosition = playerState.Position;
        
        if(false) // <- 보드매니저에서 제공하는 함수를 통해 보드의 'playerPosition'번째 칸의 type이 '자유여행'인지 확인합니다.
        {
            HandleChooseDestinationPrompt();
        }
        else
        {
            HandleRollDicePrompt();
        }
    }

    public void HandleChooseDestinationPrompt()
    {
        // 봇의 경우, 시작타일(0)을 목적지로 HandleDestinationChosen를 호출합니다. (빈 땅을 우선적으로 선택하는 등의 지능은 추후 개발)
        HandleDestinationChosen(0);

        // 플레이어의 경우, 자유 여행할 타일을 선택하는 UI를 표시합니다.
        
    }

    /// <summary>
    /// 화면에서 자유 이동할 위치를 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void HandleDestinationChosen(int destinationPosition)
    {
        // 말이 목적지로 이동하는 것을 연출합니다. (주사위 굴림으로 이동하는것과 연출이 다를 수 있음.)
    }

    public void HandleRollDicePrompt()
    {
        // 플레이어인 경우 주사위 굴림 UI를 표시합니다.

        // 봇의 경우 RollDice를 호출합니다.
    }

    /// <summary>
    /// 화면의 '주사위 굴리기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void RollDice()
    {
        long playerId = gameState.CurrentPlayerId;
        
        // 주사위 굴림 결과를 생성합니다.
        int dice1 = random.Next(1, 7);
        int dice2 = random.Next(1, 7);

        // HandleRollDice를 호출합니다.
        HandleDiceRolled(dice1, dice2);

        // 더블 처리
        if (dice1 == dice2)
        {
            isDouble = true;
            consecutiveDoubleCount++;
        }
        else
        {
            isDouble = false;
            consecutiveDoubleCount = 0;
        }

        // if (현재 위치가 무인도 && PlayerStates.IslandTurnsRemaining > 0 && !isDouble )  무인도 탈출 실패 판정.

        // 플레이어 이동을 계산합니다. gameState.PlayerStates로부터 플레이어의 현재위치 fromPosition을 조회하고, 주사위 결과를 더하여 toPosition값을 계산합니다.
        // gameState.PlayerStates에서 playerId에 해당하는 원소를 찾기 위해 매번 foreach문을 도는것은 비효율적이기 때문에, 실제 서버측 구현을 할 때에는 Map자료구조를 사용할 수 있습니다.
        // 하지만, GameState는 재접속시 상태 동기화를 위한 DTO로도 사용되기 때문에, Json 변환이 가능한 List구조체를 PlayerStates 프로퍼티의 데이터타입으로 사용했습니다.

        // HandlePlayerMoved를 호출합니다.
        // 이때, toPosition < fromPosition 인 경우, 출발지점을 지나쳤다고 판단하여 ShouldReceiveSalary == true 가 됩니다.
        // '월급 획득 여부'를 이동 처리 함수의 매개변수로 추가한 이유는, 월급 획득 연출 타이밍이 출발 지점을 지날 때와 일치해야하기 때문입니다.

        // ProcessArrival(playerId, toPosition);
    }

    private void ProcessArrival(long playerId, int toPosition)
    {
        PlayerState player = GetPlayerState(playerId);
        TileData arrivalTile = BoardManager.Instance.GetTileData(toPosition);

        switch (arrivalTile.Type) 
        { 
            case TileType.START: // 시작지점에 도착
                ProcessEndTurn(); // 턴을 종료합니다. 
                break;

            case TileType.ISLAND: // 무인도에 도착
                HandleTurnChanged(GetNextPlayerId()); // 강제로 턴을 넘깁니다.
                break;

            case TileType.CHARITY: // 기부금수령에 도착
                // HandleWelfareFundReceived 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                ProcessEndTurn();
                break;

            case TileType.DONATION: // 기부금납부에 도착
                // HandleDonationPaid 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                ProcessEndTurn();
                break;

            case TileType.WORLD_TRAVEL: // 자유여행에 도착
                // 강제로 턴을 넘깁니다.
                HandleTurnChanged(GetNextPlayerId());
                break;

            case TileType.GOLDEN_KEY: // 황금열쇠에 도착
                // 황금 열쇠 카드 드로우
                // DrawCard();
                ProcessEndTurn();
                break;

            case TileType.PROPERTY: // 자산 유형의 타일에 도착
                int propertyId = arrivalTile.PropertyId;
                PropertyState propertyState = GetPropertyState(propertyId);
                PropertyData propertyData = PropertyManager.Instance.GetData(propertyId);
                
                if (propertyState.OwnerId == null) // 주인 없는 땅인 경우
                {
                    bool canAffordToPurchase = (player.Money >= PropertyManager.Instance.GetLandPrice(propertyState.PropertyId));
                    if (canAffordToPurchase) // 땅 구매할 돈이 충분하면
                    {
                        // HandlePurchasePropertyPrompt 호출
                    }
                    else
                    {
                        ProcessEndTurn();
                    }
                }
                else if (propertyState.OwnerId.Value == playerId) // 본인 소유의 땅인 경우
                {
                    BuildingLevel currentLevel = propertyState.BuildingLevel;
                    bool canAffordToBuild = (player.Money >= PropertyManager.Instance.GetBuildCost(propertyId, (BuildingLevel)(currentLevel + 1)));
                    if (propertyData.CanBuild && propertyState.BuildingLevel != BuildingLevel.Hotel  && canAffordToBuild) // 건설할 수 있으면
                    {
                        // HandleBuildPrompt 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                    }
                    else
                    {
                        ProcessEndTurn();
                    }
                }
                else // 타인 소유의 땅인 경우
                {
                    if (통행료 낼 돈 충분) // 통행료 납부 가능하면
                    {
                        // HandleTollPaid 호출합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )

                        if (통행료 내고도 인수할 돈 충분) // 납부하고도 인수할 돈이 있다면 (앞선 HandleTollPaid로 Money -= 통행료 반영된 상태에서 계산하는 것이다.)
                        {

                        }
                        else
                        {
                            ProcessEndTurn();
                        }
                    }
                    else if (자산 팔아서 지불 가능)
                    {
                        // HandleSellPropertiesPrompt 호출합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                    }
                    else if (지불 불가)
                    {
                        // 납부자의 모든 재산을 현금화하여 수납자에게 줍니다.
                        // 납부자를 파산 처리합니다.
                        // 강제로 턴을 넘깁니다.
                        HandleTurnChanged(GetNextPlayerId());
                    }
                }
                
                break;
        }
        
    }

    /// <summary>
    /// 턴을 종료하는 함수입니다. 무조건 턴을 넘기는게 아닙니다! 추가턴 진행 조건(isDouble == true)를 만족하면 추가턴을 진행합니다.
    /// </summary>
    private void ProcessEndTurn()
    {
        // isDouble인 경우 추가턴을 진행합니다.
        if(isDouble)
            HandleRollDicePrompt();
        else
        {
            HandleTurnChanged(GetNextPlayerId());
        }
    }

    private long GetNextPlayerId()
    {
        return 0; // 컴파일 에러를 막기 위해 임시로 0을 적어뒀습니다. GameState와 playerOrder를 참조하여 다음 차례인 플레이어의 playerId를 찾아 리턴하면 됩니다. 파산한 플레이어는 건너뜁니다.
    }

    public void ProcessBankruptcy(long playerId)
    {
        // playerId에 해당하는 GameState.PlayerStates의 PlayerState.IsBankrupt 값을 true로 갱신합니다.
        var player = GetPlayerState(playerId);
        if (player == null) return;
        player.IsBankrupt = true;

        // 남은 플레이어 수가 1이라면 게임 종료 함수를 호출합니다. (팀전의 경우 조건이 바뀔 수 있음.)
        int remaining = 0;
        foreach (var p in gameState.PlayerStates)
        {
            if (!p.IsBankrupt) remaining++;
        }
        if (remaining <= 1)
        {
            // 게임 종료 함수를 호출합니다.
        }
    }

    public void HandleDiceRolled(int dice1, int dice2)
    {
        // 주사위 굴림 연출을 재생합니다.
    }

    public void HandlePlayerMoved(long playerId, int fromPosition, int toPosition, bool shouldReceiveSalary)
    {
        // GameState를 갱신합니다.

        // 말 이동 연출을 재생합니다.
    }

    private const int IslandTurns = 3; // 무인도 영업정지 턴 수

    /// <summary>
    /// 플레이어를 무인도로 바로 보낸다. (황금 열쇠 무인도 카드, 3연속 더블)
    /// 걸어서 이동하는 것이 아니므로 출발지를 지나도 월급이 없다.
    /// </summary>
    public void HandleSentToIsland(long playerId, int fromPosition, int islandPosition)
    {
        // GameState를 갱신합니다. (위치를 무인도로, 영업정지 턴 설정)
        var player = GetPlayerState(playerId);
        if (player == null)
        {
            Debug.LogError($"[GameManager] 무인도 이동 실패: 플레이어 {playerId} 없음");
            return;
        }

        player.Position = islandPosition;
        player.IslandTurnsRemaining = IslandTurns;

        // 무인도 이동 연출을 재생합니다. (순간이동, 월급 없음)
    }

    /// <summary>
    /// 황금 열쇠 카드로 은행과 돈을 주고받는다. (보너스는 +, 은행행 벌금은 -)
    /// 적립금으로 가는 벌금은 HandleDonationPaid를 사용한다.
    /// </summary>
    public void HandleCardMoneyChanged(long playerId, long delta)
    {
        // GameState를 갱신합니다. (플레이어 현금 증감)
        // var player = GetPlayerState(playerId);
        // if (player == null)
        // {
        //     Debug.LogError($"[GameManager] 카드 재화 변경 실패: 플레이어 {playerId} 없음");
        //     return;
        // }
        //
        // long before = player.Money;
        // player.Money += delta;
        // EconomyManager.NotifyMoneyChanged(playerId, before, player.Money);

        // TODO: 벌금(delta < 0)이 현금보다 클 때 매각/파산 처리 (경제 담당과 협의)

        // 재화 획득/손실 연출을 재생합니다.
    }


    // ────────────────────────── 토지 구매 ──────────────────────────
    public void HandlePurchasePropertyPrompt(long playerId, int propertyId, long amount)
    {
        // 플레이어의 경우, 땅을 구매할 것인지 선택 가능한 UI를 표시합니다.

        // 봇의 경우, 돈이 있다면 무조건 구매합니다.
    }

    /// <summary>
    /// 화면의 '땅 사기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void PurchaseProperty(long playerId, int propertyId)
    {
        long amount = PropertyManager.Instance.GetLandPrice(propertyId); // 땅값 조회
        HandlePropertyPurchased(playerId, propertyId, amount);
    }

    /// <summary>
    /// 화면의 '땅 사지 않기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void DeclinePropertyPurchase(long playerId, int propertyId) // 거절 함수를 분리한 이유는, chatGPT한테 물어본 결과 bool매개변수를 사용하여 수락/거절을 표현하기보다 함수 자체를 분리하는것을 추천했기 때문입니다.
    {
        ProcessEndTurn();
    }

    public void HandlePropertyPurchased(long playerId, int propertyId, long amount)
    {
        // GameState를 갱신합니다. (플레이어 현금 차감, 자산 주인 갱신)
        var player = GetPlayerState(playerId); //사는 사람
        var property = GetPropertyState(propertyId); //사는 땅
        if (player == null || property == null)  return;

        player.Money -= amount;
        property.OwnerId = playerId;
        property.BuildingLevel = BuildingLevel.Land; // 새로 산 땅은 건물 없음
        
        // 자산 구매 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }


    // ────────────────────────── 건물 건설 ──────────────────────────
    public void HandleBuildPrompt(long playerId, int propertyId, long amount)
    {
        // 플레이어의 경우, 건설할 것인지 선택 가능한 UI를 표시합니다.

        // 봇의 경우, 돈이 있다면 무조건 건설합니다.
    }

    /// <summary>
    /// 화면의 '건설하기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void Build(long playerId, int propertyId)
    {
        var property = GetPropertyState(propertyId); //땅 상태(현재단계)
        var data = PropertyManager.Instance.GetData(propertyId); //가격표(건설비)
        if (property == null || data == null || property.BuildingLevel >= BuildingLevel.Hotel)
        {
            Debug.LogError($"[GameManager] 건설 실패: 땅 {propertyId} 없음 또는 건설 레벨 최대");
            ProcessEndTurn();
            return;
        }
        long amount = data.GetBuildCost((BuildingLevel)(property.BuildingLevel + 1)); // 다음 단계 건설비용
        HandleBuilt(playerId, propertyId, amount);
    }

    /// <summary>
    /// 화면의 '건설하지 않기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void DeclineBuild(long playerId, int propertyId) // 거절 함수를 분리한 이유는, chatGPT한테 물어본 결과 bool매개변수를 사용하여 수락/거절을 표현하기보다 함수 자체를 분리하는것을 추천했기 때문입니다.
    {
        ProcessEndTurn();
    }

    public void HandleBuilt(long playerId, int propertyId, long amount)
    {
        // GameState를 갱신합니다. (플레이어 현금 차감, 건설 레벨 갱신)
        var player = GetPlayerState(playerId); //짓는 사람
        var property = GetPropertyState(propertyId); //짓는 땅
        if (player == null || property == null) return;

        player.Money -= amount; //건설비 차감
        property.BuildingLevel += 1; // 건설 레벨 증가

        // 건설 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }


    // ────────────────────────── 자산 인수 ──────────────────────────
    public void HandleAcquirePropertyPrompt(long playerId, int propertyId)
    {
        // 플레이어의 경우, 인수할 것인지 선택 가능한 UI를 표시합니다.
        
        // 봇의 경우, 돈이 있다면 무조건 인수합니다.
    }

    /// <summary>
    /// 화면의 '인수하기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void AcquireProperty(long playerId, int propertyId)
    {
        var property = GetPropertyState(propertyId); //인수할 땅
        HandlePropertyAcquired(playerId, propertyId);
    }

    /// <summary>
    /// 화면의 '인수하지 않기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void DeclineAcquireProperty(long playerId, int propertyId)
    {
        ProcessEndTurn();
    }
    
    public void HandlePropertyAcquired(long playerId, int propertyId)
    {
        // GameState를 갱신합니다. (인수자 현금 차감, 인수당하는 사람 현금 증가, 자산 주인 갱신)
        var acquirer = GetPlayerState(playerId); //인수하는 사람
        var property = GetPropertyState(propertyId); //인수할 땅
        var owner = property != null && property.OwnerId.HasValue ? GetPlayerState(property.OwnerId.Value) : null; //인수당하는 사람
        if (acquirer == null || property == null || owner == null)  return;

        long amount = property != null ? PropertyManager.Instance.GetAcquireValue(property) : 0; //인수가 조회
        
        acquirer.Money -= amount; //인수자 돈 차감
        owner.Money += amount; //소유자 돈 증가
        property.OwnerId = playerId; //소유권 이전

        // 인수 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }
    
    
    // ────────────────────────── 자산 매각 ──────────────────────────
    public void HandleSellPropertiesPrompt(long payerId, long receiverId, long requiredAmount)
    {
        // 플레이어의 경우, 청산할 자산들을 선택 가능한 UI를 표시합니다.

        // 봇의 경우, 선택된 자산 가치 합이 amount이상이 되는 조합 중, 합이 최소인 조합을 찾습니다
    }

    /// <summary>
    /// 화면의 '선택 완료' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void SellProperties(long payerId, long receiverId, List<int> propertyIds, long requiredAmount)
    {
        long totalAmount = 0;
        foreach (var id in propertyIds)
        {
            var property = GetPropertyState(id);
            if (property != null && property.OwnerId == payerId) // 소유자가 맞는지 확인
                totalAmount += PropertyManager.Instance.GetSellValue(property);
        }

        // 플레이어 현금 + 선택한 자산 가치 총합이 요구치보다 낮을 경우
        PlayerState payer = GetPlayerState(payerId);
        if (payer.Money + totalAmount < requiredAmount)
        {
            // 매각 자산 선택창을 다시 띄웁니다.
            HandleSellPropertiesPrompt(payerId, receiverId, requiredAmount);
        }
        // 매각해서 통행료 지불이 가능해진 경우
        else
        {
            // 매각 결과를 반영하는 함수를 호출합니다.
            HandlePropertiesSold(payerId, receiverId, propertyIds, requiredAmount, totalAmount);
        }
    }

    public void HandlePropertiesSold(long payerId, long receiverId, List<int> propertyIds, long requiredAmount, long totalAmount)
    {
        // GameState를 갱신합니다. (payer 현금 갱신, 자산 소유주 갱신, receiver 현금 갱신)
        var payer = GetPlayerState(payerId); // 청산하는 사람
        var receiver = GetPlayerState(receiverId); // 통행료 받는 사람
        if (payer == null || receiver == null) return;

        foreach (var id in propertyIds)
        {
            var property = GetPropertyState(id);
            if (property == null || property.OwnerId != payerId)  continue; // 소유자가 맞는지 확인
            
            property.OwnerId = null; // 주인 없는 땅으로
            property.BuildingLevel = BuildingLevel.Land; // 건설 레벨 초기화
            
        }

        long toll = payer.Money + requiredAmount; // 통행료 전액 = 현금 전부 + 부족분
        payer.Money = payer.Money + totalAmount - toll; // 판 돈 받고 통행료 냄
        receiver.Money += toll; //통행료 전액 지급

        // 자산 청산 및 재화 이동 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }

    /// <summary>
    /// 통행료를 낼 수 없을 때 호출. 납부자의 현금 전부와 모든 땅(매각가로 현금화)을 수납자에게 주고 파산 처리한다.
    /// </summary>
    public void HandleBankruptcy(long payerId, long receiverId)
    {
        // GameState를 갱신합니다. (납부자 재산 현금화, 수납자 현금 증가, 납부자 파산)
        var payer = GetPlayerState(payerId); // 파산하는 사람
        var receiver = GetPlayerState(receiverId); // 통행료 받는 사람
        if (payer == null || receiver == null) return;

        long liquidated = 0; //땅을 전부 판 금액
        foreach (var state in gameState.PropertyStates)
        {
            if (state.OwnerId != payerId) continue;

            liquidated += PropertyManager.Instance.GetSellValue(state); // 매각가 합산
            state.OwnerId = null; // 주인 없는 땅으로
            state.BuildingLevel = BuildingLevel.Land; // 건설 레벨 초기화
        }

        receiver.Money += payer.Money + liquidated; // 납부자 현금 전액 + 땅 매각가 합계
        payer.Money = 0; // 납부자 현금 0

        // 파산 연출을 재생합니다.
        ProcessBankruptcy(payerId); // 납부자 파산 처리

        // 턴 넘기기는 ProcessArrival에서 처리 (HandleTurnChanged(GetNextPlayerId()) 호출)
    }
    public void HandleTollPaid(long payerId, long receiverId, long amount)
    {
        // GameState를 갱신합니다.
        var payer = GetPlayerState(payerId); // 통행료 내는 사람
        var receiver = GetPlayerState(receiverId); // 통행료 받는 사람
        if (payer == null || receiver == null) return;

        payer.Money -= amount; // 통행료 차감
        receiver.Money += amount; // 통행료 수령

        // 재화 이동 연출을 재생합니다.
    }

    public void HandleDonationPaid(long playerId, long amount)
    {
        // GameState를 갱신합니다.

        // 재화 손실 연출을 재생합니다.
    }

    public void HandleWelfareFundReceived(long playerId, long amount)
    {
        // GameState를 갱신합니다.

        // 재화 획득 연출을 재생합니다.
    }

    public void DrawCard(long playerId)
    {
        CardResult result = cardManager.Draw(playerId); // 카드 뽑기 + 효과 적용

        if (result.requiresTileResolve)
            ProcessArrival(playerId, result.landedTileId); // 이동 카드: 도착 칸 처리
        else if (result.sentToInspection)
            HandleTurnChanged(GetNextPlayerId());          // 무인도행: 추가 턴 없이 턴 넘김
        else
            ProcessEndTurn();                              // 그 외: 턴 종료
    }

    public void HandleCardDrawn(int cardId)
    {
        // (보관 가능한 카드의 경우)GameState를 갱신합니다.

        // (즉시 발동되는 카드의 경우)cardId에 맞는 효과를 연출합니다.
    }

    /// <summary>
    /// GameState.PlayerStates에서 playerId가 같은 플레이어 상태를 찾는다.
    /// 구매, 건설, 통행료, 인수, 매각 처리에서 돈을 바꿀 대상을 찾을 때 쓴다.
    /// </summary>
    /// 
    /// 플레이어 상태, 없으면 null. (호출한 쪽에서 null 확인 필요)
    private PlayerState GetPlayerState(long playerId)
    {
        PlayerState playerState = gameState.PlayerStates.Find(p => p.PlayerId == playerId);

        if(playerState == null)
            Debug.LogError($"GameState - PlayerState not found: {playerId}");

        return playerState;
    }
    /// <summary>
    /// GameState.PropertyStates에서 propertyId가 같은 땅 상태(소유자, 건물 단계)를 찾는다.
    /// 구매, 건설, 인수, 매각 처리에서 소유자와 단계를 바꿀 대상을 찾을 때 쓴다.
    /// </summary>
    /// 
    /// 땅 상태, 없으면 null. (호출한 쪽에서 null 확인 필요)

    private PropertyState GetPropertyState(long propertyId)
    {
        PropertyState propertyState = gameState.PropertyStates.Find(p => p.PropertyId == propertyId);

        if(propertyState == null)
            Debug.LogError($"GameState - PropertyState not found: {propertyId}");

        return propertyState;
    }
}