using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GameState gameState;

    private System.Random random; // 선턴 정하기나 랜덤 주사위 값을 계산할 때 사용합니다.

    private const int IslandTurns = 3; // 무인도 영업정지 턴 수
    
    [SerializeField] private long salaryAmount = 100000;   // TODO: 월급 금액 확정 필요
    [SerializeField] private long taxAmount = 100000;      // 세무조사 벌금 
    [SerializeField] private long startMoney = 500000;     // 초기 자금 (테스트할 때 인스펙터에서 늘려서 사용)
    [SerializeField, Min(1)] private int maxRound = 30;     // 최대 라운드 (모든 플레이어가 한 번씩 하면 1라운드). 넘으면 게임 종료
    
    private bool isMoving;
    
    // ────────────────────────── 기능 테스트용 주사위 값 지정 필드 ──────────────────────────
    [Header("기능 테스트용 주사위 값 강제")]
    public int forceDice1 = -1;
    public int forceDice2 = -1;
    
    // ────────────────────────── MVP 단계에서 봇 테스트용 ──────────────────────────
    [Header("Bot (Test)")]
    [SerializeField] private bool autoPlayAllPlayers = false;   // 테스트용: 켜면 사람도 자동 진행
    [SerializeField] private float botActionDelay = 3f;         // 봇이 행동하기 전 대기 시간(초)
    private Dictionary<long, IBotStrategy> botStrategies; // 플레이어별 봇 전략. 보드·가격 정보가 필요해서 Start에서 생성합니다.
    
    [System.Serializable]
    private class PlayerSetup
    {
        public long playerId;   // 플레이어 고유 번호 (겹치면 안 됨)
        public bool isBot;      // 봇 여부
        public BotPersonality personality = BotPersonality.Balanced; // 봇 성향 (봇일 때만 사용)
    }

    [Header("Players")] [SerializeField] private List<PlayerSetup> playerSetups = new List<PlayerSetup>();
    
    
    // 플레이어 색상 관련 필드
    private Color[] playerColors = new Color[]{Color.red, Color.green, Color.blue, Color.yellow};
    private Dictionary<long, Color> playerColorMap;
    public Color GetPlayerColor(long playerId) => playerColorMap[playerId];
    
    
    protected override void Awake()
    {
        // gameState 초기화. PropertyStates의 초기화는 Start에서 진행.
        gameState = new  GameState();
        
        gameState.TurnNumber = 0;
        gameState.RoundNumber = 0;
        gameState.CurrentPlayerId = 0;
        gameState.WelfareFund = 0;
        
        foreach (PlayerSetup setup in playerSetups)
            gameState.PlayerStates.Add(new PlayerState(setup.playerId));

        // 그 외 필드 변수 초기화
        random = new System.Random();
        gameState.PlayerOrder = new List<long>();
        gameState.IsDouble = false;
        gameState.ConsecutiveDoubleCount = 0;
        
        // player의 색상 지정. gameState.playerStates 의 순서대로 배정
        playerColorMap = new Dictionary<long, Color>();
        for (int i = 0; i < gameState.PlayerStates.Count; i++)
        {
            PlayerState playerState = gameState.PlayerStates[i];
            playerColorMap.Add(playerState.PlayerId, playerColors[i]);
        }
    }

    void Start()
    {
        // gameState.PropertyStates의 초기화는 PropertyManager의 초기화가 선행되어야 하기 때문에 Start에서 진행
        foreach (PropertyData propertyData in  PropertyManager.Instance.GetAllDataByMapId(1))
        {
            gameState.PropertyStates.Add(new PropertyState(propertyData.Id));
        }
        // 봇 전략 생성 (보드 배치와 가격 계산이 필요해서 매니저 초기화 이후에 생성)
        botStrategies = new Dictionary<long, IBotStrategy>();
        foreach (PlayerSetup setup in playerSetups)
        {
            botStrategies[setup.playerId] = new BasicBotStrategy(
                BoardManager.Instance.BoardData.Tiles,
                PropertyManager.Instance.GetToll,
                PropertyManager.Instance.GetData,
                BotSettings.For(setup.personality),
                maxRound,
                Log);
        }
            
        // MVP 단계에서 플레이어의 playerId는 123, 봇의 playerId는 456 입니다.
        PlayerManager.Instance.Initialize(gameState.PlayerOrder); // 다른 Monobehaviour 클래스를 참조하여 초기화할때는 Awake말고 Start에서 하는게 안전
        
        StartGame();
    }

    void Update()
    {
        if (gameState.WaitingType == null) return;

        BroadcastType waitingType = gameState.WaitingType.Value;
        GameNetwork.Instance.TryDequeueMessage(waitingType);
    }

    // ────────────────────────── 로그 / 봇 도우미 ──────────────────────────
    // UI가 없는 동안 진행 상황을 콘솔에서 볼 수 있도록 로그를 남깁니다.

    private void Log(string message)
    {
        Debug.Log($"[Game] {message}");
    }

    // 로그용 이름: "1P[123]", "2P(봇)[456]"처럼 순서와 봇 여부를 보여줍니다.
    private string P(long playerId)
    {
        int order = gameState.PlayerOrder.IndexOf(playerId) + 1;
        PlayerSetup setup = playerSetups.Find(s => s.playerId == playerId);
        bool isBotSetup = setup != null && setup.isBot;
        return $"{order}P{(isBotSetup ? "(봇)" : "")}[{playerId}]";
    }

    private string Won(long amount)
    {
        return $"{amount:N0}원";
    }

    private string CityName(int propertyId)
    {
        PropertyData data = PropertyManager.Instance.GetData(propertyId);
        return data != null ? data.CityName : $"땅{propertyId}";
    }

    private string TileName(int position)
    {
        TileData tile = BoardManager.Instance.GetTileData(position);
        if (tile == null) return $"{position}번";

        if (tile.Type == TileType.PROPERTY)
            return $"{CityName(tile.PropertyId)}({position}번)";

        return $"{tile.Type}({position}번)";
    }

    private bool IsBot(long playerId)
    {
        if (autoPlayAllPlayers) return true;

        PlayerSetup setup = playerSetups.Find(s => s.playerId == playerId);
        return setup != null && setup.isBot;
    }

    private IBotStrategy GetBotStrategy(long playerId)
    {
        return botStrategies[playerId];
    }

    private void RunAfterDelay(Action action)
    {
        StartCoroutine(RunAfterDelayRoutine(action));
    }

    private IEnumerator RunAfterDelayRoutine(Action action)
    {
        yield return new WaitForSeconds(botActionDelay);
        action?.Invoke();
    }

    // 파산하지 않은 플레이어가 1명 이하면 게임 종료
    private bool IsGameOver()
    {
        int remaining = 0;
        foreach (var p in gameState.PlayerStates)
            if (!p.IsBankrupt) remaining++;
        return remaining <= 1;
    }

    // nextPlayerId의 차례가 몇 번째 라운드인지 계산합니다.
    // 턴 순서(playerOrder)가 처음으로 돌아오면 1 증가합니다. 파산한 플레이어는 GetNextPlayerId가 건너뛰므로 남은 플레이어 기준으로 셉니다.
    private int GetNextRound(long nextPlayerId)
    {
        if (gameState.RoundNumber == 0) return 1; // 게임의 첫 차례

        int prevIndex = gameState.PlayerOrder.IndexOf(gameState.CurrentPlayerId);
        int nextIndex = gameState.PlayerOrder.IndexOf(nextPlayerId);
        return nextIndex <= prevIndex ? gameState.RoundNumber + 1 : gameState.RoundNumber;
    }

    // 살아남은 플레이어의 최종 등수를 정합니다. (파산한 플레이어는 파산할 때 이미 등수가 정해짐)
    // PlayerRanking 순서대로 1위부터 부여합니다. (총자산 → 현금 → 원래 순서)
    private void AssignSurvivorRanks()
    {
        List<PlayerState> ranked = PlayerRanking.Rank(gameState.PlayerStates, gameState.PropertyStates);
        int rank = 1;
        foreach (PlayerState player in ranked)
        {
            if (player.IsBankrupt) continue;
            player.FinalRank = rank++;
        }
    }

    // 최종 순위를 FinalRank 순으로 로그에 남깁니다.
    private void LogRanking()
    {
        var ranked = new List<PlayerState>(gameState.PlayerStates);
        ranked.Sort((a, b) => a.FinalRank.CompareTo(b.FinalRank));
        foreach (PlayerState player in ranked)
        {
            long totalAsset = PropertyManager.Instance.GetTotalAsset(player.PlayerId, player.Money, gameState.PropertyStates);
            string result = player.IsBankrupt ? "파산" : (player.FinalRank == 1 ? "승리" : "패배");
            Log($"  {player.FinalRank}위 {P(player.PlayerId)}: 총자산 {Won(totalAsset)} (현금 {Won(player.Money)}) - {result}");
        }
    }

    /// <summary>
    /// 게임을 종료합니다. (남은 플레이어 1명 이하, 최대 라운드 도달)
    /// 결과 창은 한 번만 띄우고, 이후에는 턴을 진행하지 않습니다.
    /// </summary>
    private void EndGame(string reason)
    {
        if (gameState.IsGameOver) return;
        gameState.IsGameOver = true;

        Log($"[게임 종료] {reason}");
        AssignSurvivorRanks(); // 생존자 최종 등수 확정 (파산자는 이미 정해짐)
        LogRanking();

        // 게임 결과 창을 띄웁니다. (총자산 순으로 순위 표시)
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameResultPopup();
    }
    
    // ────────────────────────── 게임 진행 ──────────────────────────
    
    void StartGame()
    {
        // 선턴 정하기 이벤트 발생. MVP에서는 생략합니다.

        // 결과를 playerOrder 에 저장. MVP에서는 플레이어가 무조건 선턴입니다.
        foreach (PlayerSetup setup in playerSetups)
            gameState.PlayerOrder.Add(setup.playerId);
        
        PlayerManager.Instance.Initialize(playerSetups.ConvertAll(s => s.playerId));
        
        // 모든 플레이어에게 초기자금 지급
        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            playerState.Money += startMoney;
        }
        
        Log($"게임 시작! 플레이어 {gameState.PlayerStates.Count}명, 초기 자금 {Won(startMoney)}, 자동 진행(전원) = {autoPlayAllPlayers}");
        
        // UI를 초기화합니다. (이 화면을 조작하는 사람 플레이어를 지정하고, 플레이어 정보 카드를 GameState에 맞춰 채웁니다.)
        if (UIManager.Instance != null)
        {
            PlayerSetup human = playerSetups.Find(s => !s.isBot);
            if (human != null) UIManager.Instance.SetLocalPlayer(human.playerId);
            UIManager.Instance.SetMaxRound(maxRound); // 최대 라운드는 GameManager가 관리하고 UI에 알려줍니다.
            UIManager.Instance.InitPlayers();
        }

        // 첫 번째 순서부터 턴 시작
        HandleTurnChanged(gameState.PlayerOrder[0]);
    }

    public void HandleTurnChanged(long playerId)
    {
        // 게임 종료 판정: 새 턴을 시작하기 전에 확인합니다.
        if (gameState.IsGameOver) return;
        if (IsGameOver())
        {
            EndGame("남은 플레이어가 1명입니다.");
            return;
        }
        int nextRound = GetNextRound(playerId);
        if (nextRound > maxRound)
        {
            EndGame($"최대 라운드({maxRound}라운드)에 도달했습니다.");
            return;
        }

        // gameState.CurrentPlayerId 를 playerId로 갱신합니다. gameState.TurnNumber를 1 증가시키고, 라운드 수를 갱신합니다.
        gameState.CurrentPlayerId = playerId;
        gameState.TurnNumber += 1;
        gameState.RoundNumber = nextRound;
        
        // 'OO의 턴'이라는 UI를 표시합니다.
        PlayerState playerState = GetPlayerState(playerId);
        Log($"===== {gameState.RoundNumber}/{maxRound}라운드 ({gameState.TurnNumber}번째 턴): {P(playerId)} 차례 (현금 {Won(playerState.Money)}, 위치 {TileName(playerState.Position)}) =====");
        if (UIManager.Instance != null)
            UIManager.Instance.ShowTurn(playerId);

        // 필드 변수를 갱신합니다.
        gameState.IsDouble = false;
        gameState.ConsecutiveDoubleCount = 0;
        
        // 새 차례가 온 플레이어의 위치가 자유여행인 경우
        if(BoardManager.Instance.GetTileData(GetPlayerState(playerId).Position).Type == TileType.WORLD_TRAVEL)
        {
            // 주사위를 굴리는 창 대신, 원하는 타일을 선택하는 창을 띄웁니다.
            HandleChooseDestinationPrompt();
        }
        else
        {
            HandleRollDicePrompt();
        }
    }

    public void HandleChooseDestinationPrompt()
    {
        // 봇의 경우, botStrategy가 고른 목적지로 이동합니다.
        long playerId = gameState.CurrentPlayerId;
        if (IsBot(playerId))
        {
            RunAfterDelay(() =>
            {
                int destination = GetBotStrategy(playerId).ChooseTravelDestination(gameState, playerId);
                Log($"[봇] {P(playerId)}: 세계여행 목적지 {TileName(destination)} 선택");
                ChooseDestination(destination);
            });
            return;
        }
        
        // 플레이어의 경우, 자유 여행할 타일을 선택하는 UI를 표시합니다.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowChooseDestinationPopup();
        }
    }

    public void ChooseDestination(int destinationPosition)
    {
        HandleDestinationChosen(destinationPosition);
    }
    
    /// <summary>
    /// 화면에서 자유 이동할 위치를 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void HandleDestinationChosen(int destinationPosition)
    {
        // 말이 목적지로 이동하는 것을 연출합니다. (주사위 굴림으로 이동하는것과 연출이 다를 수 있음.)
        MovePlayerDirectly(gameState.CurrentPlayerId, destinationPosition);
    }

    public void HandleRollDicePrompt()
    {
        if (IsGameOver())
        {
            Log("게임 종료: 남은 플레이어가 1명 이하입니다.");
            return;
        }
        
        // 봇의 경우 RollDice를 호출합니다.
        if ((IsBot(gameState.CurrentPlayerId)))
        {
            Log($"[봇] {P(gameState.CurrentPlayerId)}: 주사위를 굴립니다.");
            RunAfterDelay(RollDice);
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowRollDicePopup();
            return;
        }
        // 플레이어인 경우 주사위 굴림 UI를 표시합니다.
        Debug.Log("주사위 굴림 창 뜨는 기능 미구현... 인스펙터에서 RollDiceTest를 직접 호출하세요");
    }

    [ContextMenu("RollDiceTest")]   // 플레이 모드에서 테스트용
    private void RollDiceTest() => RollDice();
    
    /// <summary>
    /// 화면의 '주사위 굴리기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void RollDice()
    {
        // 주사위 굴림 결과를 생성합니다.
        int dice1 = random.Next(1, 7);
        int dice2 = random.Next(1, 7);

        // TODO: 기능 테스트 끝나면 forceDice1, forceDice2와 함께 코드 제거
        if(forceDice1 != -1)
        {
            dice1 = forceDice1;
            forceDice1 = -1;
        }
        if(forceDice2 != -1)
        {
            dice2 = forceDice2;
            forceDice2 = -1;
        }
        
        
        // HandleRollDice를 호출합니다.
        HandleDiceRolled(dice1, dice2);
    }

    public void HandleDiceRolled(int dice1, int dice2)
    {
        long playerId = gameState.CurrentPlayerId;
        PlayerState player = GetPlayerState(playerId);
        
        Log($"[주사위] {P(playerId)}: {dice1}+{dice2}={dice1 + dice2}{(dice1 == dice2 ? " (더블!)" : "")}");
        if (UIManager.Instance != null)
            UIManager.Instance.ShowDiceResult(dice1, dice2);
        
        // 더블 처리
        if (dice1 == dice2)
        {
            gameState.IsDouble = true;
            gameState.ConsecutiveDoubleCount++;
        }
        else
        {
            gameState.IsDouble = false;
            gameState.ConsecutiveDoubleCount = 0;
        }
        
        // if (현재 위치가 무인도 && PlayerStates.IslandTurnsRemaining > 0 && !isDouble )  무인도 탈출 실패 판정.
        bool onIslandTile = BoardManager.Instance.GetTileData(player.Position).Type == TileType.ISLAND;
        bool isIslandTurnsRemaining = player.IslandTurnsRemaining > 0;
        if (onIslandTile && isIslandTurnsRemaining && !gameState.IsDouble)
        {
            // 남은 감금 턴수 차감
            player.IslandTurnsRemaining -= 1;
            
            Log($"[무인도] {P(playerId)}: 탈출 실패 (남은 영업정지 {player.IslandTurnsRemaining}턴)");
            SpecialTileManager.Instance.PlayIslandEscapeFailed(playerId, player.IslandTurnsRemaining); // 탈출 실패 연출
            HandleTurnChanged(GetNextPlayerId()); // 강제로 턴을 넘깁니다.
            return;
        }

        if (onIslandTile && gameState.IsDouble)
        {
            player.IslandTurnsRemaining = 0;
            Log($"[무인도 탈출] {P(playerId)}: 더블로 탈출했습니다!");
            SpecialTileManager.Instance.PlayIslandEscaped(playerId); // 탈출 연출
            
            // isDouble을 false로 갱신하여 추가턴 진행을 막습니다.
            gameState.IsDouble = false;
        }

        if (isMoving) return;
        
        // 3연속 더블이면 이동하지 않고 무인도로 보낸다.
        if (gameState.ConsecutiveDoubleCount >= 3)
        {
            int islandPosition = FindIslandTileIndex();
            if (islandPosition >= 0)
            {
                Log($"[3연속 더블] {P(playerId)}: 무인도로 이동합니다.");
                gameState.IsDouble = false;
                gameState.ConsecutiveDoubleCount = 0;
                MovePlayerDirectly(playerId, islandPosition); // 순간이동 (월급 없음) → 도착 처리에서 영업정지 설정 + 턴 넘김
                return;
            }
        }
        
        int tileCount = BoardManager.Instance.TileCount;
        int fromPosition = player.Position;
        int toPosition = (fromPosition + dice1 + dice2) % tileCount;
        bool shouldReceiveSalary = toPosition < fromPosition;   // 출발 지점을 지나침

        Log($"[이동] {P(playerId)}: {TileName(fromPosition)} → {TileName(toPosition)}");
        
        HandlePlayerMoved(playerId, fromPosition, toPosition, shouldReceiveSalary);
    }
    
    public void HandlePlayerMoved(long playerId, int fromPosition, int toPosition, bool shouldReceiveSalary)
    {
        PlayerState player = GetPlayerState(playerId);
        if (player == null) return;

        int tileCount = BoardManager.Instance.TileCount;
        int steps = (toPosition - fromPosition + tileCount) % tileCount;

        // 1) GameState는 즉시 갱신
        player.Position = toPosition;
        if (shouldReceiveSalary)
        {
            long before = player.Money;
            player.Money += salaryAmount;
            EconomyManager.NotifyMoneyChanged(playerId, before, player.Money);
            Log($"[월급] {P(playerId)}: 출발 지점을 지나 월급 {Won(salaryAmount)} 지급 (현금 {Won(player.Money)})");
        }

        // 2) 말 이동 연출 → 끝나면 도착 처리
        isMoving = true;
        PlayerManager.Instance.MoveBySteps(
            playerId, fromPosition, steps,
            onTileReached: tileIndex =>
            {
                if (shouldReceiveSalary && tileIndex == 0)
                {
                    // 월급 획득 연출
                }
            },
            onCompleted: () =>
            {
                isMoving = false;
                ProcessArrival(playerId, toPosition);
            });
    }

#region ProcessArrival
    private void ProcessArrival(long playerId, int toPosition)
    {
        PlayerState player = GetPlayerState(playerId);
        TileData arrivalTile = BoardManager.Instance.GetTileData(toPosition);

        Log($"[도착] {P(playerId)} → {TileName(toPosition)} (현금 {Won(player.Money)})");
        
        switch (arrivalTile.Type) 
        { 
            case TileType.START: // 시작지점에 도착
                Log("  출발 칸: 특별한 효과 없음");
                ProcessEndTurn(); // 턴을 종료합니다. 
                break;

            case TileType.ISLAND: // 무인도에 도착
                player.IslandTurnsRemaining = IslandTurns;
                Log("  무인도: 턴이 넘어갑니다.");
                SpecialTileManager.Instance.PlayIslandArrived(playerId); // 무인도 도착 연출
                HandleTurnChanged(GetNextPlayerId()); // 강제로 턴을 넘깁니다.
                break;

            case TileType.CHARITY: // 기부금수령(푸드 페스티벌)에 도착
                HandleWelfareFundReceived(playerId, gameState.WelfareFund); // 쌓인 적립금을 모두 받습니다.
                ProcessEndTurn();
                break;

            case TileType.DONATION: // 기부금납부(세무조사)에 도착
                HandleDonationPaid(playerId, taxAmount); // 벌금을 내고 적립금에 쌓습니다.
                ProcessEndTurn();
                break;

            case TileType.WORLD_TRAVEL: // 자유여행에 도착
                // 강제로 턴을 넘깁니다.
                Log("  세계여행 칸: 턴이 넘어갑니다.");
                HandleTurnChanged(GetNextPlayerId());
                break;

            case TileType.GOLDEN_KEY: // 황금열쇠에 도착
                // 황금 열쇠 카드 드로우 (턴 처리는 각 CardEffect 실행 메서드에서 진행하므로 ProcessEndTurn 호출하지 않음)
                Log("  황금열쇠 카드를 뽑습니다.");
                DrawCard(playerId);
                break;

            case TileType.PROPERTY: // 자산 유형의 타일에 도착
                int propertyId = arrivalTile.PropertyId;
                PropertyState propertyState = GetPropertyState(propertyId);
                PropertyData propertyData = PropertyManager.Instance.GetData(propertyId);
                
                if (propertyState == null || propertyData == null)
                {
                    Debug.LogError($"[GameManager] 땅 데이터 없음: propertyId {propertyId}");
                    ProcessEndTurn();
                    break;
                }
                
                if (propertyState.OwnerId == null) // 주인 없는 땅인 경우
                {
                    long landPrice = PropertyManager.Instance.GetLandPrice(propertyState.PropertyId);
                    bool canAffordToPurchase = player.Money >= landPrice;
                    if (canAffordToPurchase) // 땅 구매할 돈이 충분하면
                    {
                        Log($"  빈 땅: {CityName(propertyId)} 구매 가능 (땅값 {Won(landPrice)})");
                        HandlePurchasePropertyPrompt(playerId, propertyId, true);
                    }
                    else // 땅 구매할 돈이 충분하지 않으면
                    {
                        Log($"  빈 땅: {CityName(propertyId)} 구매 불가능 (땅값 {Won(landPrice)})");
                        HandlePurchasePropertyPrompt(playerId, propertyId, false);
                    }
                }
                else if (propertyState.OwnerId.Value == playerId) // 본인 소유의 땅인 경우
                {
                    bool canBuild = propertyData.CanBuild;
                    
                    // 건설 불가 자산 Case 처리
                    if(!canBuild)
                    {
                        Log($"  내 땅: {CityName(propertyId)} 현재 BuildingLevel : {BuildingLevel.Land} / 건설 불가능 - 건설 불가 칸");
                        HandleBuildPrompt(playerId, propertyId, false);
                        return;
                    }
                    
                    // 이미 최고 레벨인 Case 처리
                    BuildingLevel currentLevel = propertyState.BuildingLevel;
                    bool isMaxLevel = currentLevel == BuildingLevel.Hotel;
                    if (isMaxLevel)
                    {
                        Log($"  내 땅: {CityName(propertyId)} 현재 BuildingLevel : {currentLevel} / 건설 불가능 - 이미 최고 단계");
                        HandleBuildPrompt(playerId, propertyId, false);
                        return;
                    }

                    // 돈이 부족한 Case 처리
                    long buildCost = PropertyManager.Instance.GetBuildCost(propertyId, (BuildingLevel)(currentLevel + 1));
                    if (player.Money < buildCost)
                    {
                        Log($"  내 땅: {CityName(propertyId)} 현재 BuildingLevel : {currentLevel}) / 건설 불가능 - 현금 부족 (건설 비용 : {Won(buildCost)})");
                        HandleBuildPrompt(playerId, propertyId, false);
                        return;
                    }
                    
                    // 건설 가능한 Case 처리
                    Log($"  내 땅: {CityName(propertyId)} 현재 BuildingLevel : {currentLevel}) 건설 가능 (비용 {Won(buildCost)})");
                    HandleBuildPrompt(playerId, propertyId, true);
                }
                else // 타인 소유의 땅인 경우
                {
                    long ownerId = propertyState.OwnerId.Value;
                    long toll =PropertyManager.Instance.GetToll(propertyState);
                    Log($"  남의 땅: {CityName(propertyId)} (주인 {P(ownerId)}, 통행료 {Won(toll)})");
                    if (player.Money >= toll) // 통행료 납부 가능하면
                    {
                        HandleTollPaid(playerId, ownerId, toll); // 통행료 납부 처리. GameState를 갱신합니다.
                    
                        long acquireValue = PropertyManager.Instance.GetAcquireValue(propertyState);
                        if (player.Money >= acquireValue) // 납부하고도 인수할 돈이 있다면 
                        {
                            Log($"  인수 가능 (인수가 {Won(acquireValue)})");
                            HandleAcquirePropertyPrompt(playerId, propertyId, acquireValue); // 인수 선택지 UI를 표시합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                        }
                        else
                        {
                            Log($"  인수 불가 (인수가 {Won(acquireValue)}, 현금 {Won(player.Money)})");
                            ProcessEndTurn();
                        }
                    }
                    else if (player.Money + PropertyManager.Instance.GetTotalSellValue(playerId, gameState.PropertyStates) >= toll) // 통행료 납부 불가지만, 자산을 팔면 납부 가능하면
                    {
                        Log($"  현금 부족: 자산을 팔아서 통행료를 내야 합니다.");
                        HandleSellPropertiesPrompt(playerId, ownerId, toll); // 자산 매각 선택지 UI를 표시합니다. ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                    }
                    else 
                    {
                        Log($"  현금과 자산을 모두 팔아도 통행료를 낼 수 없습니다. 파산!");
                        HandleBankruptcy(playerId, ownerId); // 재산 현금화 -> 수납자 지급 -> 파산 처리. GameState를 갱신합니다.                  
                        HandleTurnChanged(GetNextPlayerId()); // 강제로 턴을 넘깁니다.
                    }
                }
                
                break;
        }
        
    }
#endregion

    /// <summary>
    /// 말이 목적지로 직접 이동합니다. (자유여행, 뒤로 이동 카드, 무인도행 등) 월급은 없으며, 이동이 끝나면 도착 처리를 합니다.
    /// </summary>
    private void MovePlayerDirectly(long playerId, int toPosition)
    {
        PlayerState player = GetPlayerState(playerId);
        if (player == null) return;

        Log($"[직접 이동] {P(playerId)}: {TileName(player.Position)} → {TileName(toPosition)}");
        
        player.Position = toPosition;
        isMoving = true;
        PlayerManager.Instance.MoveToTile(playerId, toPosition, () =>
        {
            isMoving = false;
            ProcessArrival(playerId, toPosition);
        });
    }
    
    /// <summary>
    /// 턴을 종료하는 함수입니다. 무조건 턴을 넘기는게 아닙니다! 추가턴 진행 조건(isDouble == true)를 만족하면 추가턴을 진행합니다.
    /// </summary>
    private void ProcessEndTurn()
    {
        // isDouble인 경우 추가턴을 진행합니다.
        if(gameState.IsDouble)
            HandleRollDicePrompt();
        else
        {
            HandleTurnChanged(GetNextPlayerId());
        }
    }

    private long GetNextPlayerId()
    {
        List<long> playerOrder = gameState.PlayerOrder;
        int currentIndex = playerOrder.IndexOf(gameState.CurrentPlayerId);

        // 현재 플레이어 다음 순서부터 한 바퀴 돌면서, 파산하지 않은 플레이어를 찾습니다.
        for (int i = 1; i <= playerOrder.Count; i++)
        {
            long candidateId = playerOrder[(currentIndex + i) % playerOrder.Count];
            PlayerState candidate = GetPlayerState(candidateId);

            if (candidate != null && !candidate.IsBankrupt)
                return candidateId;
        }

        return gameState.CurrentPlayerId;   // 모두 파산한 경우 (게임 종료 상황)
    }

    public void ProcessBankruptcy(long playerId)
    {
        // playerId에 해당하는 GameState.PlayerStates의 PlayerState.IsBankrupt 값을 true로 갱신합니다.
        var player = GetPlayerState(playerId);
        if (player == null || player.IsBankrupt) return; // 이미 파산한 플레이어는 다시 처리하지 않습니다.

        // 최종 등수를 정합니다. 파산하는 시점에 남아 있던 인원 수가 등수입니다. (예: 4명 중 첫 파산 → 4위)
        int aliveCount = 0;
        foreach (var p in gameState.PlayerStates)
        {
            if (!p.IsBankrupt) aliveCount++;
        }
        player.FinalRank = aliveCount;

        player.IsBankrupt = true;
        Log($"[파산] {P(playerId)}가 파산했습니다. (최종 {player.FinalRank}위)");

        // 파산 연출을 재생합니다. (GameState를 먼저 갱신한 뒤 부르므로 UI는 IsBankrupt를 그대로 읽으면 됨)
        if (UIManager.Instance != null)
            UIManager.Instance.PlayBankruptEffect(playerId);
        PlayerManager.Instance.HidePawn(playerId);

        // 남은 플레이어 수가 1이라면 게임 종료 함수를 호출합니다. (팀전의 경우 조건이 바뀔 수 있음.)
        // 파산 연출 뒤에 판정해서, 결과 창이 파산 연출보다 먼저 뜨지 않게 합니다.
        int remaining = 0;
        foreach (var p in gameState.PlayerStates)
        {
            if (!p.IsBankrupt) remaining++;
        }
        if (remaining <= 1)
        {
            // 게임 종료 함수를 호출합니다.
            EndGame("남은 플레이어가 1명입니다.");
        }
    }

#region Card Effect
    // ────────────────────────── 황금 열쇠 CardEffect 실행 ──────────────────────────
    // HandleCardDrawn → ExecuteCardEffect에서 카드의 EffectType을 확인한 뒤 호출합니다.
    // 대상은 현재 턴 플레이어(gameState.CurrentPlayerId)이며, 게임 상태 변경 후 다음 흐름(턴 종료, 도착 칸 처리)까지 진행합니다.

    // 카드의 EffectType을 확인하고 해당 CardEffect 실행 메서드를 호출합니다.
    private void ExecuteCardEffect(CardData card)
    {
        switch (card.effectType)
        {
            case CardEffectType.Bonus:
                ExecuteBonusEffect(card.amount);
                break;

            case CardEffectType.Penalty:
                ExecutePenaltyEffect(card.amount, card.penaltyToFestivalPool);
                break;

            case CardEffectType.MoveTo:
                ExecuteMoveToEffect(card.targetTileId);
                break;

            case CardEffectType.MoveBy:
                ExecuteMoveByEffect(card.steps);
                break;

            case CardEffectType.GoToInspection:
                ExecuteGoToInspectionEffect();
                break;

            default:
                Debug.LogError($"[GameManager] 처리하지 않은 CardEffectType: {card.effectType}");
                ProcessEndTurn(); // 턴이 멈추지 않도록 종료
                break;
        }
    }

    // CardEffect: Bonus - 은행에서 돈을 받는다
    private void ExecuteBonusEffect(int amount)
    {
        PlayerState player = GetPlayerState(gameState.CurrentPlayerId);
        if (player == null)
        {
            ProcessEndTurn();
            return;
        }

        long before = player.Money;
        player.Money += amount;
        EconomyManager.NotifyMoneyChanged(player.PlayerId, before, player.Money);

        // 재화 획득 연출을 재생합니다.

        ProcessEndTurn();
    }

    // CardEffect: Penalty - 은행 또는 기부금(WelfareFund)에 돈을 낸다
    private void ExecutePenaltyEffect(int amount, bool toWelfareFund)
    {
        PlayerState player = GetPlayerState(gameState.CurrentPlayerId);
        if (player == null)
        {
            ProcessEndTurn();
            return;
        }

        // 현금 한도 안에서만 냅니다. (매각/파산 없음)
        long paid = DeductMoneyWithinBalance(player, amount);
        if (toWelfareFund)
            gameState.WelfareFund += paid;
        Log($"[카드:벌금] {P(player.PlayerId)}: 벌금 {Won(amount)} 중 {Won(paid)} 납부 (현금 {Won(player.Money)})");

        // 재화 손실 연출을 재생합니다.

        ProcessEndTurn();
    }

    // CardEffect: MoveTo - 지정한 칸으로 앞으로 이동한다 (출발지를 지나면 월급)
    private void ExecuteMoveToEffect(int targetTileId)
    {
        PlayerState player = GetPlayerState(gameState.CurrentPlayerId);
        if (player == null)
        {
            ProcessEndTurn();
            return;
        }

        int fromPosition = player.Position;
        int toPosition = targetTileId;
        bool shouldReceiveSalary = toPosition < fromPosition; // RollDice와 같은 규칙: toPosition < fromPosition 이면 출발지 통과

        Log($"[카드:이동] {P(player.PlayerId)}: {TileName(toPosition)}(으)로 이동");
        
        // 이동이 끝나면 HandlePlayerMoved가 알아서 ProcessArrival을 호출합니다.
        HandlePlayerMoved(player.PlayerId, fromPosition, toPosition, shouldReceiveSalary);
    }

    // CardEffect: MoveBy - N칸 이동한다 (음수면 뒤로, 뒤로 갈 때는 월급 없음)
    private void ExecuteMoveByEffect(int steps)
    {
        PlayerState player = GetPlayerState(gameState.CurrentPlayerId);
        int boardSize = BoardManager.Instance.BoardData.Tiles.Count;
        if (player == null || boardSize <= 0)
        {
            Debug.LogError($"[GameManager] MoveBy 카드 실패: 플레이어 또는 보드 데이터 없음 (보드 칸 수 {boardSize})");
            ProcessEndTurn();
            return;
        }

        int fromPosition = player.Position;
        int toPosition = ((fromPosition + steps) % boardSize + boardSize) % boardSize;
        bool shouldReceiveSalary = steps > 0 && fromPosition + steps >= boardSize;

        Log($"[카드:이동] {P(player.PlayerId)}: {steps}칸 이동 ({TileName(fromPosition)} → {TileName(toPosition)})");
        
        // 이동이 끝나면 두 함수 모두 알아서 ProcessArrival을 호출합니다.
        if (steps > 0)
            HandlePlayerMoved(player.PlayerId, fromPosition, toPosition, shouldReceiveSalary);
        else
            MovePlayerDirectly(player.PlayerId, toPosition);   // 뒤로 이동
    }

    // CardEffect: GoToInspection - 무인도로 바로 이동한다 (월급 없음, 더블이어도 추가 턴 없음)
    private void ExecuteGoToInspectionEffect()
    {
        PlayerState player = GetPlayerState(gameState.CurrentPlayerId);
        int islandPosition = FindIslandTileIndex();
        if (player == null || islandPosition < 0)
        {
            Debug.LogError($"[GameManager] 무인도 카드 실패: 플레이어 또는 무인도 칸 없음");
            ProcessEndTurn();
            return;
        }

        Log($"[카드:무인도] {P(player.PlayerId)}: 무인도로 이동합니다.");

        // 순간이동 (월급 없음) → 도착 처리에서 영업정지 설정 + 추가 턴 없이 턴 넘김
        MovePlayerDirectly(player.PlayerId, islandPosition);
    }

    // 보드에서 무인도(ISLAND) 칸 번호를 찾는다. 없으면 -1
    private int FindIslandTileIndex()
    {
        foreach (TileData tile in BoardManager.Instance.BoardData.Tiles)
        {
            if (tile != null && tile.Type == TileType.ISLAND)
                return tile.Index;
        }

        return -1;
    }
#endregion


    // ────────────────────────── 토지 구매 ──────────────────────────
    public void HandlePurchasePropertyPrompt(long playerId, int propertyId, bool isPurchasable)
    {
        // 봇의 경우, 살 수 있으면 botStrategy의 판단에 따라 구매하거나 거절합니다.
        if (IsBot(playerId))
        {
            if (!isPurchasable)
            {
                DeclinePropertyPurchase(playerId, propertyId);
                return;
            }

            RunAfterDelay(() =>
            {
                long price = PropertyManager.Instance.GetLandPrice(propertyId);
                if (GetBotStrategy(playerId).ShouldPurchase(gameState, playerId, propertyId, price))
                    PurchaseProperty(playerId, propertyId);
                else
                    DeclinePropertyPurchase(playerId, propertyId);
            });
            return;
        }
        // 플레이어의 경우, 땅을 구매할 것인지 선택 가능한 UI를 표시합니다.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPurchasePropertyPopup(playerId, propertyId, isPurchasable);
            return;
        }
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
        Log($"[구매 안 함] {P(playerId)}: {CityName(propertyId)}");
        ProcessEndTurn();
    }

    public void HandlePropertyPurchased(long playerId, int propertyId, long amount)
    {
        // GameState를 갱신합니다. (플레이어 현금 차감, 자산 주인 갱신)
        var player = GetPlayerState(playerId); //사는 사람
        var property = GetPropertyState(propertyId); //사는 땅
        if (player == null || property == null)  return;

        player.Money -= amount;
        SetOwnerId(property, playerId);
        SetBuildingLevel(property, BuildingLevel.Land); // 새로 산 땅은 건물 없음
        Log($"[구매] {P(playerId)}: {CityName(propertyId)} 구매 (-{Won(amount)}, 남은 현금 {Won(player.Money)})");
        
        // 자산 구매 연출을 재생합니다.
        if (UIManager.Instance != null)
            UIManager.Instance.PlayMoneyChange(playerId, -amount, "땅 구매");

        // 턴을 종료합니다.
        ProcessEndTurn();
    }


    // ────────────────────────── 건물 건설 ──────────────────────────
    public void HandleBuildPrompt(long playerId, int propertyId, bool isBuildable)
    {
        // 봇의 경우, 지을 수 있으면 botStrategy의 판단에 따라 건설하거나 거절합니다.
        if (IsBot(playerId))
        {
            if (!isBuildable)
            {
                DeclineBuild(playerId, propertyId);
                return;
            }

            RunAfterDelay(() =>
            {
                BuildingLevel nextLevel = GetPropertyState(propertyId).BuildingLevel + 1;
                long cost = PropertyManager.Instance.GetBuildCost(propertyId, nextLevel);
                if (GetBotStrategy(playerId).ShouldBuild(gameState, playerId, propertyId, cost))
                    Build(playerId, propertyId);
                else
                    DeclineBuild(playerId, propertyId);
            });
            return;
        }
        // 플레이어의 경우, 건설할 것인지 선택 가능한 UI를 표시합니다.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBuildPopup(playerId, propertyId, isBuildable);
            return;
        }
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
        Log($"[건설 안 함] {P(playerId)}: {CityName(propertyId)}");
        ProcessEndTurn();
    }

    public void HandleBuilt(long playerId, int propertyId, long amount)
    {
        // GameState를 갱신합니다. (플레이어 현금 차감, 건설 레벨 갱신)
        var player = GetPlayerState(playerId); //짓는 사람
        var property = GetPropertyState(propertyId); //짓는 땅
        if (player == null || property == null) return;

        player.Money -= amount; //건설비 차감
        SetBuildingLevel(property, property.BuildingLevel + 1); // 건설 레벨 증가
        // 건설 연출을 재생합니다.
        if (UIManager.Instance != null)
            UIManager.Instance.PlayMoneyChange(playerId, -amount, "건설");

        // 턴을 종료합니다.
        ProcessEndTurn();
    }


    // ────────────────────────── 자산 인수 ──────────────────────────
    public void HandleAcquirePropertyPrompt(long playerId, int propertyId, long amount)
    {
        // 봇의 경우, botStrategy의 판단에 따라 인수하거나 거절합니다.
        if (IsBot(playerId))
        {
            RunAfterDelay(() =>
            {
                if (GetBotStrategy(playerId).ShouldAcquire(gameState, playerId, propertyId, amount))
                    AcquireProperty(playerId, propertyId);
                else
                    DeclineAcquireProperty(playerId, propertyId);
            });
            return;
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowAcquirePropertyPopup(playerId, propertyId);
            return;
        }
        // 플레이어의 경우, 인수할 것인지 선택 가능한 UI를 표시합니다.
        Debug.Log("인수 결정 창 뜨는 기능 미구현...");
    }

    /// <summary>
    /// 화면의 '인수하기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void AcquireProperty(long playerId, int propertyId)
    {
   
        HandlePropertyAcquired(playerId, propertyId);
    }

    /// <summary>
    /// 화면의 '인수하지 않기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
    public void DeclineAcquireProperty(long playerId, int propertyId)
    {
        Log($"[인수 안 함] {P(playerId)}: {CityName(propertyId)}");
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
        SetOwnerId(property, playerId); //소유권 이전
        
        // 인수 연출을 재생합니다.
        if (UIManager.Instance != null)
            UIManager.Instance.PlayMoneyTransfer(playerId, owner.PlayerId, amount, "인수", "인수 대금");

        // 턴을 종료합니다.
        ProcessEndTurn();
    }
    
    
    // ────────────────────────── 자산 매각 ──────────────────────────
    public void HandleSellPropertiesPrompt(long payerId, long receiverId, long requiredAmount)
    {
        // 봇의 경우, botStrategy가 고른 땅을 팝니다.
        if (IsBot(payerId))
        {
            RunAfterDelay(() =>
            {
                List<int> chosen = GetBotStrategy(payerId).ChooseSellProperties(
                    gameState, payerId, requiredAmount, PropertyManager.Instance.GetSellValue);
                SellProperties(payerId, receiverId, chosen, requiredAmount);
            });
            return;
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSellPropertiesPopup(payerId, receiverId, requiredAmount);
            return;
        }
        // 플레이어의 경우, 청산할 자산들을 선택 가능한 UI를 표시합니다.
        Debug.Log("매각 결정 창 뜨는 기능 미구현...");
    }

        /// <summary>
    /// 유저 매각 창의 "자동 선택" 버튼에서 호출합니다.
    /// 부족분을 채울 땅 목록을 골라 돌려주기만 하고, 실제 매각은 유저가 확인하면 SellProperties에서 합니다.
    /// 봇은 이 함수를 쓰지 않고 BotStrategy.ChooseSellProperties로 고릅니다.
    /// </summary>
    public List<int> AutoChooseSellProperties(long payerId, long requiredAmount, SellSelectionMode mode)
    {
        PlayerState payer = GetPlayerState(payerId);
        if (payer == null) return new List<int>();

        // 수익 지키기는 봇 매각과 같은 판단(상대 위치·도착 확률 계산)이라 봇 전략을 그대로 재사용합니다.
        if (mode == SellSelectionMode.ProtectIncome)
            return GetBotStrategy(payerId).ChooseSellProperties(
                gameState, payerId, requiredAmount, PropertyManager.Instance.GetSellValue);

        long shortage = requiredAmount - payer.Money;
        return PropertyManager.Instance.AutoChooseSellProperties(payerId, shortage, gameState.PropertyStates, mode);
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

        // 플레이어 현금 + 선택한 자산 가치 총합이 요구치보다 낮을 경우 (클라이언트 측 data에 오류가 있거나 치팅한 경우 발생)
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
            
            SetOwnerId(property, null); // 주인 없는 땅으로
            SetBuildingLevel(property, BuildingLevel.Land); // 건설 레벨 초기화
            
        }

        long toll = requiredAmount; // 통행료 전액 
        payer.Money = payer.Money + totalAmount - toll; // 판 돈 받고 통행료 냄
        receiver.Money += toll; //통행료 전액 지급

        // 자산 청산 및 재화 이동 연출을 재생합니다.
        if (UIManager.Instance != null)
            UIManager.Instance.PlayMoneyTransfer(payerId, receiverId, toll, "통행료", "통행료 수입");

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
            SetOwnerId(state, null); // 주인 없는 땅으로
            SetBuildingLevel(state, BuildingLevel.Land); // 건설 레벨 초기화
        }

        receiver.Money += payer.Money + liquidated; // 납부자 현금 전액 + 땅 매각가 합계
        payer.Money = 0; // 납부자 현금 0

        ProcessBankruptcy(payerId); // 납부자 파산 처리 (GameState 갱신 → 파산 연출 → 게임 종료 판정)

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
        if (UIManager.Instance != null)
            UIManager.Instance.PlayTollEffect(payerId, receiverId, amount);
    }

    public void HandleDonationPaid(long playerId, long amount)
    {
        // GameState를 갱신합니다. (현금 감소, 적립금 증가)
        PlayerState player = GetPlayerState(playerId);
        if (player == null) return;

        // 현금 한도 안에서만 냅니다. (매각/파산 없음)
        long paid = DeductMoneyWithinBalance(player, amount);
        gameState.WelfareFund += paid;
        Log($"  [세무조사] {P(playerId)}: 벌금 {Won(amount)} 중 {Won(paid)} 납부 (현금 {Won(player.Money)}, 적립금 {Won(gameState.WelfareFund)})");

        // 재화 손실 연출을 재생합니다.
        SpecialTileManager.Instance.PlayTaxPaid(playerId, paid);
    }

    /// <summary>
    /// 현재 현금 한도 안에서만 돈을 뺀다. (카드 벌금, 세무조사처럼 매각/파산 없이 걷는 돈)
    /// 실제로 낸 금액을 반환한다. (예: 벌금 1000, 현금 500 → 500 납부, 현금 0)
    /// </summary>
    private long DeductMoneyWithinBalance(PlayerState player, long amount)
    {
        long paid = Math.Min(amount, Math.Max(0, player.Money));
        long before = player.Money;
        player.Money -= paid;
        EconomyManager.NotifyMoneyChanged(player.PlayerId, before, player.Money);
        return paid;
    }

    public void HandleWelfareFundReceived(long playerId, long amount)
    {
        // GameState를 갱신합니다. (현금 증가, 적립금 감소)
        PlayerState player = GetPlayerState(playerId);
        if (player == null) return;

        if (amount <= 0)
        {
            Log($"  [푸드 페스티벌] {P(playerId)}: 쌓인 적립금이 없습니다.");
            return;
        }

        long before = player.Money;
        player.Money += amount;
        gameState.WelfareFund -= amount;
        EconomyManager.NotifyMoneyChanged(playerId, before, player.Money);
        Log($"  [푸드 페스티벌] {P(playerId)}: 적립금 {Won(amount)} 획득 (현금 {Won(player.Money)})");

        // 재화 획득 연출을 재생합니다.
        SpecialTileManager.Instance.PlayWelfareFundReceived(playerId, amount);
    }

    public void DrawCard(long playerId)
    {
        // 덱에 등록된 CardId 중 하나를 랜덤으로 결정합니다. (카드 장수, 사용 여부는 고려하지 않음)
        List<int> cardIds = CardManager.Instance.GetCardIds();
        if (cardIds.Count == 0)
        {
            Debug.LogError("[GameManager] 황금 열쇠 카드 없음: CardDeckData 확인 필요");
            ProcessEndTurn();
            return;
        }

        int cardId = cardIds[random.Next(cardIds.Count)];
        HandleCardDrawn(cardId);
    }

    public void HandleCardDrawn(int cardId)
    {
        // CardManager에서 CardId로 카드 데이터를 조회합니다.
        CardData card = CardManager.Instance.FindCard(cardId);
        if (card == null)
        {
            ProcessEndTurn(); // 카드 조회 실패 시 턴이 멈추지 않도록 종료
            return;
        }

        long playerId = gameState.CurrentPlayerId;
        Log($"[황금열쇠] {P(playerId)}: '{card.cardName}' 카드를 뽑았습니다.");

        if (UIManager.Instance == null)
        {
            ExecuteCardEffect(card); // UI가 없으면 바로 실행합니다.
            return;
        }

        // 카드 연출을 재생합니다. 연출이 끝나면 EffectType에 맞는 CardEffect 실행 메서드를 호출합니다.
        // 내 카드면 확인을 눌렀을 때, 다른 플레이어 카드면 자동으로 닫힐 때 실행됩니다. (전원 자동 진행 테스트에서는 모두 자동으로 닫힘)
        UIManager.Instance.ShowCardDrawPopup(playerId, card.id, autoPlayAllPlayers, () => ExecuteCardEffect(card));
    }

    private void SetOwnerId(PropertyState state, long? playerId)
    {
        state.OwnerId = playerId;
        
        int propertyId = state.PropertyId;
        BoardManager.Instance.UpdatePropertyTileColor(propertyId, playerId);
    }
    
    private void SetBuildingLevel(PropertyState state, BuildingLevel buildingLevel)
    {
        state.BuildingLevel = buildingLevel;
        
        int propertyId = state.PropertyId;
        BoardManager.Instance.UpdatePropertyBuildingVisual(propertyId, buildingLevel);
    }

    /// <summary>
    /// GameState.PlayerStates에서 playerId가 같은 플레이어 상태를 찾는다.
    /// 구매, 건설, 통행료, 인수, 매각 처리에서 돈을 바꿀 대상을 찾을 때 쓴다.
    /// </summary>
    /// 
    /// 플레이어 상태, 없으면 null. (호출한 쪽에서 null 확인 필요)
    public PlayerState GetPlayerState(long playerId)
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

    public PropertyState GetPropertyState(long propertyId)
    {
        PropertyState propertyState = gameState.PropertyStates.Find(p => p.PropertyId == propertyId);

        if(propertyState == null)
            Debug.LogError($"GameState - PropertyState not found: {propertyId}");

        return propertyState;
    }
}