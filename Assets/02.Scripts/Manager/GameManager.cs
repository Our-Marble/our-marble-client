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
        
        ProcessArrival(toPosition);
    }

    private void ProcessArrival(int toPosition)
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
                // HandlePurchasePropertyPrompt 호출합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
            }
            else if(내 땅)
            {
                if(건설 가능)
                // HandleBuildPrompt 호출합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
            }
            else if(남의 땅)
            {
                if(현금으로 통행료 지불 가능)
                {
                    // HandleTollPaid 호출합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                    
                    ProcessEndTurn();
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
        return 0; // 컴파일 에러를 막기 위해 임시로 0을 적어뒀습니다. GameState와 playerOrder를 참조하여 다음 차례인 플레이어의 playerId를 찾아 리턴하면 됩니다.
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
        long amount = 0; // 컴파일 에러를 막기 위해 임시로 0을 적어뒀습니다. PropertyData.landPrice를 조회하여 할당하면 됩니다.
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
        
        // 자산 구매 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }

    
    
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
        long amount = 0; // 컴파일 에러를 막기 위해 임시로 0을 적어뒀습니다. PropertyData.landPrice를 조회하여 할당하면 됩니다.
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
        
        // 건설 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }
    
    
    
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
        long totalAmount = 0; // 컴파일 에러를 막기 위해 임시로 0을 적어뒀습니다. PropertyData.landPrice를 조회하여 자산 가치 총합을 할당하면 됩니다.
        
        // 선택한 자산 가치 총합이 요구치보다 낮을 경우
        if (totalAmount < requiredAmount)
        {
            // 다시 청산할 자산들을 선택 가능한 UI를 표시합니다.
        }

        HandlePropertiesSold(payerId, receiverId, propertyIds, requiredAmount, totalAmount);
    }
    
    public void HandlePropertiesSold(long payerId, long receiverId, List<int> propertyIds, long requiredAmount, long totalAmount)
    {
        // GameState를 갱신합니다. (payer 현금 갱신, 자산 소유주 갱신, receiver 현금 갱신)
        
        // 자산 청산 및 재화 이동 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }
    
    
    
    public void HandleTollPaid(long payerId, long receiverId, long amount)
    {
        // GameState를 갱신합니다.
        
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
}
