using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameState gameState;

    private System.Random random; // 선턴 정하기나 랜덤 주사위 값을 계산할 때 사용합니다.

    public List<long> playerOrder;

    private bool isDouble; // 추가 턴 진행 여부를 결정하는데 사용됩니다.
    private int consecutiveDoubleCount; // 추후 3연속 더블시 무인도행을 판정할 때 사용합니다.

    private void Awake()
    {
        // gameState 초기화


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
        // 선턴 정하기 이벤트 발생

        // 결과를 playerOrder 에 저장

        // 첫 번째 순서부터 턴 시작
        // HandleTurnChanged(playerOrder[0]);
    }

    public void HandleTurnChanged(long playerId)
    {
        // gameState.CurrentPlayerId 를 playerId로 갱신합니다.

        // 'OO의 턴'이라는 UI를 표시합니다.

        // 필드 변수를 갱신합니다.
        isDouble = false;
        consecutiveDoubleCount = 0;

        /*
        if(현재 위치가 자유 여행 타일)
        {
            // HandleChooseDestinationPrompt 호출합니다.
        }
        else
        {
            // HandleRollDicePrompt를 호출합니다.
        }
        */

    }

    public void HandleChooseDestinationPrompt()
    {
        // 플레이어의 경우, 자유 여행할 타일을 선택하는 UI를 표시합니다.

        // 봇의 경우, 랜덤한 타일을 선택하여 HandleDestinationChosen를 호출합니다. (빈 땅을 우선적으로 선택하는 등의 지능은 추후 개발)

        // ProcessArrival(toPosition);
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
        /*
        if(toPosition이 시작지점)
        {
            // 아무것도 안합니다.

            ProcessEndTurn();
        }
        else if(toPosition이 무인도)
        {
            // 강제로 턴을 넘깁니다.
            HandleTurnChanged(GetNextPlayerId());
        }
        else if(toPosition이 기부금 수령)
        {
            // HandleWelfareFundReceived 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )

            ProcessEndTurn();
        }
        else if(toPosition이 기부)
        {
            // HandleDonationPaid 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )

            ProcessEndTurn();
        }
        else if(toPosition이 자유 여행)
        {
            // 강제로 턴을 넘깁니다.
            HandleTurnChanged(GetNextPlayerId());
        }
        else if(toPosition이 황금 열쇠)
        {
            // 황금 열쇠 카드 드로우
            // DrawCard();

            ProcessEndTurn();
        }
        else if(toPosition이 땅)
        {
            if(주인없는 땅)
            {
                if(돈 충분)
                {
                    // HandlePurchasePropertyPrompt 호출
                }
                else
                {
                    ProcessEndTurn();
                }
            }
            else if(내 땅)
            {
                if(건설 가능한 땅 && 건설 레벨 최대 아님 && 돈 충분함)
                {
                    // HandleBuildPrompt 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                }
                else
                {
                    ProcessEndTurn();
                }
            }
            else if(남의 땅)
            {
                if(통행료 낼 돈 충분)
                {
                    // HandleTollPaid 호출합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )

                    if(통행료 내고도 인수할 돈 충분)
                    {
                        
                    }
                    else
                    {
                        ProcessEndTurn();
                    }
                }
                else if(자산 팔아서 지불 가능)
                {
                    // HandleSellPropertiesPrompt 호출합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                }
                else if(지불 불가)
                {
                    // 납부자의 모든 재산을 현금화하여 수납자에게 줍니다.

                    // 납부자를 파산 처리합니다.

                    // 강제로 턴을 넘깁니다.
                    HandleTurnChanged(GetNextPlayerId());
                }
            }
        }
        */
    }

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
        long amount = GetLandPrice(propertyId); // 땅값 조회
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
        var data = PropertyTable.Instance.GetData(propertyId); //가격표(건설비)
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
    public void HandleAcquirePropertyPrompt(long acquirerId, int propertyId, long amount)
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
        long amount = property != null ? GetAcquireValue(property) : 0; //인수가 조회
        HandlePropertyAcquired(playerId, propertyId, amount);
    }

    /// <summary>
    /// 화면의 '인수하지 않기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void DeclineAcquireProperty(long playerId, int propertyId)
    {
        ProcessEndTurn();
    }
    
    public void HandlePropertyAcquired(long playerId, int propertyId, long amount)
    {
        // GameState를 갱신합니다. (인수자 현금 차감, 인수당하는 사람 현금 증가, 자산 주인 갱신)
        var acquirer = GetPlayerState(playerId); //인수하는 사람
        var property = GetPropertyState(propertyId); //인수할 땅
        var owner = property != null && property.OwnerId.HasValue ? GetPlayerState(property.OwnerId.Value) : null; //인수당하는 사람
        if (acquirer == null || property == null || owner == null)  return;

        acquirer.Money -= amount; //인수자 돈 차감
        owner.Money += amount; //소유자 돈 증가
        property.OwnerId = playerId; //소유권 이전

        // 인수 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }
    
    
    // ────────────────────────── 자산 매각 ──────────────────────────
    public void HandleSellPropertiesPrompt(long payerId, long receiverId, List<int> propertyId, long requiredAmount)
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
                totalAmount += GetSellValue(property);
        }

        // 선택한 자산 가치 총합이 요구치보다 낮을 경우
        if (totalAmount < requiredAmount)
        {
            // 다시 청산할 자산들을 선택 가능한 UI를 표시합니다.
            return;
        }

        HandlePropertiesSold(payerId, receiverId, propertyIds, requiredAmount, totalAmount);
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

            liquidated += GetSellValue(state); // 매각가 합산
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
        // 랜덤한 카드 데이터를 선택합니다.
        int cardId = random.Next(0,10); // 황금 열쇠 카드의 cardId 규칙이 어떻게 될지 몰라서 일단 0~9까지 랜덤 정수로 작성했습니다. cardId 형식이 정해지면 그에 맞게 데이터 타입이나 계산 방식을 수정해주시길 바랍니다.

        HandleCardDrawn(cardId);
    }

    public void HandleCardDrawn(int cardId)
    {
        // (보관 가능한 카드의 경우)GameState를 갱신합니다.

        // (즉시 발동되는 카드의 경우)cardId에 맞는 효과를 연출합니다.
    }

    // ───────────── 부동산 계산 ─────────────

    /// <summary>땅값. PurchaseProperty의 amount.</summary>
    public long GetLandPrice(int propertyId)
    {
        var data = PropertyTable.Instance.GetData(propertyId);
        return data != null ? data.LandPrice : 0;
    }

    /// <summary>통행료. 주인 없는 땅이면 0.</summary>
    public long GetToll(PropertyState state)
    {
        if (!state.OwnerId.HasValue) return 0;

        var data = PropertyTable.Instance.GetData(state.PropertyId);
        if (data == null) return 0;

        return data.GetToll(state.BuildingLevel);
    }

    /// <summary>
    /// 다음 단계 하나의 건설비. 이미 호텔이거나 가격표가 없으면 false.
    /// ProcessArrival의 "건설 가능 && 돈 충분" 판단과 HandleBuildPrompt의 amount에 사용.
    /// </summary>
    public bool TryGetNextBuildCost(PropertyState state, out long cost)
    {
        cost = 0;
        if (state.BuildingLevel >= BuildingLevel.Hotel) return false;

        var data = PropertyTable.Instance.GetData(state.PropertyId);
        if (data == null) return false;

        cost = data.GetBuildCost(state.BuildingLevel + 1);
        return true;
    }

    private const float SellRate = 0.5f; //매각가 = 투자금의 50%로 계산. 추후 밸런스 조정 필요.
    private const float AcquireRate = 2f; //인수가 = 투자금의 200%로 계산. 추후 밸런스 조정 필요.

    /// <summary>
    /// 투자금 = 땅값 + 지금 단계까지 지은 건물 비용 합계.
    /// 매각가(GetSellValue)와 인수가(GetAcquireValue) 계산에 사용됨.
    /// </summary>
    /// 투자금. 가격표가 없는 땅이면 0.
    public long GetInvestedAmount(PropertyState state)
    {
        var data = PropertyTable.Instance.GetData(state.PropertyId);
        if (data == null) return 0;

        long invested = data.LandPrice;
        for (int lv = 1; lv <= (int)state.BuildingLevel; lv++)
        {
            invested += data.GetBuildCost((BuildingLevel)lv);
        }
        return invested;
    }

    ///<summary>땅 하나의 매각가: 투자금 x SellRate(50%).</summary>
    /// 사용처
    ///   - SellProperties: 선택한 땅의 매각가 합계 계산
    ///   - GetTotalSellValue: 가진 땅 전부의 매각가 합계
    ///   - 파산 처리: 모든 재산 현금화 (매각과 같은 기준이어야 분기 판단과 실제 금액이 일치)
    /// 
    /// 매각가. 가격표가 없는 땅이면 0.
    public long GetSellValue(PropertyState state)
    {
        return (long)(GetInvestedAmount(state) * SellRate);
    }
    
    /// <summary>플레이어가 가진 땅을 전부 팔면 받는 금액의 합계</summary>
    /// 사용처
    ///   - ProcessArrival: 통행료를 현금으로 못 낼 때 분기 판단
    ///       현금 + GetTotalSellValue ≥ 통행료 → 매각 (HandleSellPropertiesPrompt)
    ///       현금 + GetTotalSellValue < 통행료 → 파산
    /// 실제로 땅을 팔지는 않고 금액만 계산한다.
    /// 
    /// 매각가 합계. 가진 땅이 없으면 0.
    public long GetTotalSellValue(long playerId)
    {
        long total = 0;
        foreach (var state in gameState.PropertyStates)
        {
            if (state.OwnerId == playerId)  total += GetSellValue(state);
        }
        return total;
    }

    /// <summary>땅 하나의 인수가: 투자금 x AcquireRate(200%).</summary>
    /// 사용처
    ///   - ProcessArrival: 통행료를 낸 뒤 인수할 돈이 충분한지 판단
    ///   - HandleAcquirePropertyPrompt: 인수 팝업에 보여줄 금액
    ///   - AcquireProperty: 실제로 차감할 금액
    /// 
    /// 인수가. 가격표가 없는 땅이면 0.
    public long GetAcquireValue(PropertyState state)
    {
        return (long)(GetInvestedAmount(state) * AcquireRate);
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