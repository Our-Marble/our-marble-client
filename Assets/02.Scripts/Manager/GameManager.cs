using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState gameState;

    private System.Random random; // 선턴 정하기나 랜덤 주사위 값을 계산할 때 사용합니다.

    public List<long> playerOrder;

    private bool isDouble; // 추가 턴 진행 여부를 결정하는데 사용됩니다.
    private int consecutiveDoubleCount; // 추후 3연속 더블시 무인도행을 판정할 때 사용합니다.

    private const int IslandTurns = 3; // 무인도 영업정지 턴 수

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private long salaryAmount = 100000;   // TODO: 월급 금액 확정 필요
    [SerializeField] private long startMoney = 500000;     // 초기 자금 (테스트할 때 인스펙터에서 늘려서 사용)
    private bool isMoving;

    [Header("Bot (Test)")]
    [SerializeField] private bool autoPlayAllPlayers = false;   // 테스트용: 켜면 사람도 자동 진행
    [SerializeField] private float botActionDelay = 3f;         // 봇이 행동하기 전 대기 시간(초)
    [System.Serializable]
    private class PlayerSetup
    {
        public long playerId;   // 플레이어 고유 번호 (겹치면 안 됨)
        public bool isBot;      // 봇 여부
    }

    [Header("Players")]
    [SerializeField] private List<PlayerSetup> playerSetups = new List<PlayerSetup>
    {
        new PlayerSetup { playerId = 123, isBot = false },   // 1P
        new PlayerSetup { playerId = 456, isBot = true },    // 2P (봇)
    };

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
        foreach (PlayerSetup setup in playerSetups)
            gameState.PlayerStates.Add(new PlayerState(setup.playerId));

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

    // ────────────────────────── 로그 / 봇 도우미 ──────────────────────────
    // UI가 없는 동안 진행 상황을 콘솔에서 볼 수 있도록 로그를 남깁니다.

        private void Log(string message)
    {
        Debug.Log($"[Game] {message}");
    }

    // 로그용 이름: "1P[123]", "2P(봇)[456]"처럼 순서와 봇 여부를 보여줍니다.
    private string P(long playerId)
    {
        int order = playerOrder.IndexOf(playerId) + 1;
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

    // ────────────────────────── 게임 진행 ──────────────────────────

    void StartGame()
    {
        // 선턴 정하기 이벤트 발생. MVP에서는 생략합니다.

        // 결과를 playerOrder 에 저장. MVP에서는 플레이어가 무조건 선턴입니다. 플레이어의 playerId는 123, 봇의 playerId는 456 입니다.
        foreach (PlayerSetup setup in playerSetups)
            playerOrder.Add(setup.playerId);

        playerManager.Initialize(playerOrder);

        // 모든 플레이어에게 초기자금 지급
        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            playerState.Money += startMoney;
        }

        Log($"게임 시작! 플레이어 {gameState.PlayerStates.Count}명, 초기 자금 {Won(startMoney)}, 자동 진행(전원) = {autoPlayAllPlayers}");

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
        Log($"===== {gameState.TurnNumber}번째 턴: {P(playerId)} 차례 (현금 {Won(playerState.Money)}, 위치 {TileName(playerState.Position)}) =====");

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
        MovePlayerDirectly(gameState.CurrentPlayerId, destinationPosition);
    }

    public void HandleRollDicePrompt()
    {
        if (IsGameOver())
        {
            Log("게임 종료: 남은 플레이어가 1명 이하입니다.");
            return;
        }

        long playerId = gameState.CurrentPlayerId;

        // 봇의 경우 잠시 기다린 뒤 RollDice를 호출합니다.
        if (IsBot(playerId))
        {
            Log($"[봇] {P(playerId)}: 주사위를 굴립니다.");
            RunAfterDelay(RollDice);
            return;
        }

        // 플레이어인 경우 주사위 굴림 UI를 표시합니다.
        Log($"[대기] {P(playerId)}: 주사위 굴리기 입력을 기다립니다. (테스트: GameManager 우클릭 > RollDiceTest)");
    }

    /// <summary>
    /// 화면의 '주사위 굴리기' 버튼을 누르면 이 함수가 호출됩니다.
    /// </summary>
        public void RollDice()
        {
            if (isMoving) return;   // 이동 중 연타 방지

            long playerId = gameState.CurrentPlayerId;
            PlayerState player = GetPlayerState(playerId);
            if (player == null) return;

            int dice1 = random.Next(1, 7);
            int dice2 = random.Next(1, 7);
            HandleDiceRolled(dice1, dice2);

            bool rolledDouble = dice1 == dice2;
            Log($"[주사위] {P(playerId)}: {dice1}+{dice2}={dice1 + dice2}{(rolledDouble ? " (더블!)" : "")}");

            if (player.IslandTurnsRemaining > 0)
            {
                // 무인도에 갇힌 상태: 더블이면 탈출하고 그대로 이동, 아니면 이번 턴은 쉬고 남은 턴이 줄어든다.
                isDouble = false;              // 무인도 탈출에는 추가 턴 없음
                consecutiveDoubleCount = 0;

                if (rolledDouble)
                {
                    player.IslandTurnsRemaining = 0;
                    Log($"[무인도 탈출] {P(playerId)}: 더블로 탈출했습니다!");
                }
                else
                {
                    player.IslandTurnsRemaining--;
                    Log($"[무인도] {P(playerId)}: 탈출 실패 (남은 영업정지 {player.IslandTurnsRemaining}턴)");
                    HandleTurnChanged(GetNextPlayerId());
                    return;
                }
            }
            else
            {
                if (rolledDouble)
                {
                    isDouble = true;
                    consecutiveDoubleCount++;
                }
                else
                {
                    isDouble = false;
                    consecutiveDoubleCount = 0;
                }

                // 3연속 더블이면 이동하지 않고 무인도로 보낸다.
                if (consecutiveDoubleCount >= 3)
                {
                    int islandPosition = FindIslandTileIndex();
                    if (islandPosition >= 0)
                    {
                        Log($"[3연속 더블] {P(playerId)}: 무인도로 이동합니다.");
                        isDouble = false;
                        consecutiveDoubleCount = 0;
                        HandleSentToIsland(playerId, player.Position, islandPosition,
                            () => HandleTurnChanged(GetNextPlayerId()));
                        return;
                    }
                }
            }

            int tileCount = BoardManager.Instance.TileCount;
            int fromPosition = player.Position;
            int toPosition = (fromPosition + dice1 + dice2) % tileCount;
            bool shouldReceiveSalary = toPosition < fromPosition;   // 출발 지점을 지나침

            Log($"[이동] {P(playerId)}: {TileName(fromPosition)} → {TileName(toPosition)}");

            HandlePlayerMoved(playerId, fromPosition, toPosition, shouldReceiveSalary);
        }

    [ContextMenu("RollDiceTest")]   // 플레이 모드에서 우클릭으로 테스트
    private void RollDiceTest() => RollDice();

    private void ProcessArrival(long playerId, int toPosition)
    {
        PlayerState player = GetPlayerState(playerId);
        TileData arrivalTile = BoardManager.Instance.GetTileData(toPosition);

        Log($"[도착] {P(playerId)} → {TileName(toPosition)} (현금 {Won(player.Money)})");

        if (arrivalTile == null)
        {
            Debug.LogError($"[GameManager] {toPosition}번 칸의 TileData가 없습니다. BoardData를 확인하세요.");
            ProcessEndTurn();
            return;
        }

        switch (arrivalTile.Type)
        {
            case TileType.START: // 시작지점에 도착
                Log("  출발 칸: 특별한 효과 없음");
                ProcessEndTurn(); // 턴을 종료합니다.
                break;

            case TileType.ISLAND: // 무인도에 도착
                player.IslandTurnsRemaining = IslandTurns;
                Log("  무인도: 턴이 넘어갑니다.");
                HandleTurnChanged(GetNextPlayerId()); // 강제로 턴을 넘깁니다.
                break;

            case TileType.CHARITY: // 기부금수령에 도착
                Log("  기부금 수령 칸 (아직 미구현)");
                // HandleWelfareFundReceived 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                ProcessEndTurn();
                break;

            case TileType.DONATION: // 기부금납부에 도착
                Log("  기부금 납부 칸 (아직 미구현)");
                // HandleDonationPaid 호출 ( 호출에 필요한 매개변수는 GameState 와 Data를 조회하여 얻습니다. )
                ProcessEndTurn();
                break;

            case TileType.WORLD_TRAVEL: // 자유여행에 도착
                Log("  세계여행 칸: 턴이 넘어갑니다.");
                // 강제로 턴을 넘깁니다.
                HandleTurnChanged(GetNextPlayerId());
                break;

            case TileType.GOLDEN_KEY: // 황금열쇠에 도착
                Log("  황금열쇠 카드를 뽑습니다.");
                // 황금 열쇠 카드 드로우 (턴 처리는 각 CardEffect 실행 메서드에서 진행하므로 ProcessEndTurn 호출하지 않음)
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
                        HandlePurchasePropertyPrompt(playerId, propertyId, landPrice);
                    }
                    else
                    {
                        Log($"  빈 땅: {CityName(propertyId)} 구매 불가 (땅값 {Won(landPrice)}, 현금 {Won(player.Money)})");
                        ProcessEndTurn();
                    }
                }
                else if (propertyState.OwnerId.Value == playerId) // 본인 소유의 땅인 경우
                {
                    BuildingLevel currentLevel = propertyState.BuildingLevel;
                    bool canBuild = propertyData.CanBuild && currentLevel != BuildingLevel.Hotel;
                    long buildCost = canBuild ? PropertyManager.Instance.GetBuildCost(propertyId, (BuildingLevel)(currentLevel + 1)) : 0;
                    bool canAffordToBuild = canBuild && player.Money >= buildCost;
                    if (canAffordToBuild) // 건설할 수 있으면
                    {
                        Log($"  내 땅: {CityName(propertyId)} (현재 {currentLevel}) 건설 가능 (비용 {Won(buildCost)})");
                        HandleBuildPrompt(playerId, propertyId, buildCost);
                    }
                    else
                    {
                        string reason = !propertyData.CanBuild ? "건설 불가 칸" : currentLevel == BuildingLevel.Hotel ? "이미 최고 단계" : $"현금 부족 (비용 {Won(buildCost)})";
                        Log($"  내 땅: {CityName(propertyId)} (현재 {currentLevel}) 건설 안 함 - {reason}");
                        ProcessEndTurn();
                    }
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
                            HandleAcquirePropertyPrompt(playerId, propertyId); // 인수 선택지 UI를 표시합니다.
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
                        HandleSellPropertiesPrompt(playerId, ownerId, toll); // 자산 매각 선택지 UI를 표시합니다.
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


    /// <summary>
    /// 말이 목적지로 직접 이동합니다. (자유여행, 뒤로 이동 카드 등) 이동이 끝나면 도착 처리를 합니다.
    /// </summary>
    private void MovePlayerDirectly(long playerId, int toPosition)
    {
        PlayerState player = GetPlayerState(playerId);
        if (player == null) return;

        Log($"[직접 이동] {P(playerId)}: {TileName(player.Position)} → {TileName(toPosition)}");

        player.Position = toPosition;
        isMoving = true;
        playerManager.MoveToTile(playerId, toPosition, () =>
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
        if(isDouble)
        {
            Log($"[추가 턴] {P(gameState.CurrentPlayerId)}: 더블이라서 한 번 더 굴립니다.");
            HandleRollDicePrompt();
        }
        else
        {
            HandleTurnChanged(GetNextPlayerId());
        }
    }

    private long GetNextPlayerId()
    {
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
        if (player == null) return;
        player.IsBankrupt = true;
        Log($"[파산] {P(playerId)}가 파산했습니다.");

        // 남은 플레이어 수가 1이라면 게임 종료 함수를 호출합니다. (팀전의 경우 조건이 바뀔 수 있음.)
        int remaining = 0;
        foreach (var p in gameState.PlayerStates)
        {
            if (!p.IsBankrupt) remaining++;
        }
        if (remaining <= 1)
        {
            Log("[게임 종료] 남은 플레이어가 1명입니다.");
            // 게임 종료 함수를 호출합니다.
        }
    }

    public void HandleDiceRolled(int dice1, int dice2)
    {
        // 주사위 굴림 연출을 재생합니다.
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
        playerManager.MoveBySteps(
            playerId, steps,
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



    /// <summary>
    /// 플레이어를 무인도로 바로 보낸다. (황금 열쇠 무인도 카드, 3연속 더블)
    /// 걸어서 이동하는 것이 아니므로 출발지를 지나도 월급이 없다.
    /// </summary>
    public void HandleSentToIsland(long playerId, int fromPosition, int islandPosition, Action onCompleted = null)
    {
        var player = GetPlayerState(playerId);
        if (player == null)
        {
            Debug.LogError($"[GameManager] 무인도 이동 실패: 플레이어 {playerId} 없음");
            return;
        }

        Log($"[무인도] {P(playerId)}: {TileName(fromPosition)} → 무인도 (영업정지 {IslandTurns}턴)");

        player.Position = islandPosition;
        player.IslandTurnsRemaining = IslandTurns;

        isMoving = true;
        playerManager.MoveToTile(playerId, islandPosition, () =>
        {
            isMoving = false;
            onCompleted?.Invoke();
        });
    }


#region Card Effect
    // ────────────────────────── 황금 열쇠 CardEffect 실행 ──────────────────────────
    // CardManager가 카드의 EffectType을 확인한 뒤 호출합니다.
    // 대상은 현재 턴 플레이어(gameState.CurrentPlayerId)이며, 게임 상태 변경 후 다음 흐름(턴 종료, 도착 칸 처리)까지 진행합니다.

    // CardEffect: Bonus - 은행에서 돈을 받는다
    public void ExecuteBonusEffect(int amount)
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
        Log($"[카드:보너스] {P(player.PlayerId)}: +{Won(amount)} (현금 {Won(player.Money)})");

        // 재화 획득 연출을 재생합니다.

        ProcessEndTurn();
    }

    // CardEffect: Penalty - 은행 또는 기부금(WelfareFund)에 돈을 낸다
    public void ExecutePenaltyEffect(int amount, bool toWelfareFund)
    {
        PlayerState player = GetPlayerState(gameState.CurrentPlayerId);
        if (player == null)
        {
            ProcessEndTurn();
            return;
        }

        // TODO: 현금이 벌금보다 적을 때 매각/파산 처리 (경제 담당과 협의)
        long before = player.Money;
        player.Money -= amount;
        if (toWelfareFund)
            gameState.WelfareFund += amount;
        EconomyManager.NotifyMoneyChanged(player.PlayerId, before, player.Money);
        Log($"[카드:벌금] {P(player.PlayerId)}: -{Won(amount)}{(toWelfareFund ? " (기부금으로 적립)" : "")} (현금 {Won(player.Money)})");

        // 재화 손실 연출을 재생합니다.

        ProcessEndTurn();
    }

    // CardEffect: MoveTo - 지정한 칸으로 앞으로 이동한다 (출발지를 지나면 월급)
    public void ExecuteMoveToEffect(int targetTileId)
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
    public void ExecuteMoveByEffect(int steps)
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
    public void ExecuteGoToInspectionEffect()
    {
        PlayerState player = GetPlayerState(gameState.CurrentPlayerId);
        int islandPosition = FindIslandTileIndex();
        if (player == null || islandPosition < 0)
        {
            Debug.LogError($"[GameManager] 무인도 카드 실패: 플레이어 또는 무인도 칸 없음");
            ProcessEndTurn();
            return;
        }

        // 말이 이동을 끝낸 뒤에 추가 턴 없이 턴을 넘깁니다.
        HandleSentToIsland(player.PlayerId, player.Position, islandPosition,
            () => HandleTurnChanged(GetNextPlayerId()));
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
    public void HandlePurchasePropertyPrompt(long playerId, int propertyId, long amount)
    {
        // 봇의 경우, 돈이 있다면 무조건 구매합니다.
        if (IsBot(playerId))
        {
            RunAfterDelay(() => PurchaseProperty(playerId, propertyId));
            return;
        }

        // 플레이어의 경우, 땅을 구매할 것인지 선택 가능한 UI를 표시합니다.
        Log($"[대기] {P(playerId)}: {CityName(propertyId)}을(를) {Won(amount)}에 구매할지 선택을 기다립니다.");
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
        property.OwnerId = playerId;
        property.BuildingLevel = BuildingLevel.Land; // 새로 산 땅은 건물 없음
        Log($"[구매] {P(playerId)}: {CityName(propertyId)} 구매 (-{Won(amount)}, 남은 현금 {Won(player.Money)})");

        // 자산 구매 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }


    // ────────────────────────── 건물 건설 ──────────────────────────
    public void HandleBuildPrompt(long playerId, int propertyId, long amount)
    {
        // 봇의 경우, 돈이 있다면 무조건 건설합니다.
        if (IsBot(playerId))
        {
            RunAfterDelay(() => Build(playerId, propertyId));
            return;
        }

        // 플레이어의 경우, 건설할 것인지 선택 가능한 UI를 표시합니다.
        Log($"[대기] {P(playerId)}: {CityName(propertyId)}에 {Won(amount)}으로 건설할지 선택을 기다립니다.");
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
        property.BuildingLevel += 1; // 건설 레벨 증가
        Log($"[건설] {P(playerId)}: {CityName(propertyId)} → {property.BuildingLevel} (-{Won(amount)}, 남은 현금 {Won(player.Money)})");

        // 건설 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }


    // ────────────────────────── 자산 인수 ──────────────────────────
    public void HandleAcquirePropertyPrompt(long playerId, int propertyId)
    {
        // 봇의 경우, 돈이 있다면 무조건 인수합니다.
        if (IsBot(playerId))
        {
            RunAfterDelay(() => AcquireProperty(playerId, propertyId));
            return;
        }

        // 플레이어의 경우, 인수할 것인지 선택 가능한 UI를 표시합니다.
        Log($"[대기] {P(playerId)}: {CityName(propertyId)} 인수 여부 선택을 기다립니다.");
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
        property.OwnerId = playerId; //소유권 이전
        Log($"[인수] {P(playerId)}가 {P(owner.PlayerId)}의 {CityName(propertyId)}을(를) {Won(amount)}에 인수 (현금 {Won(acquirer.Money)})");

        // 인수 연출을 재생합니다.

        // 턴을 종료합니다.
        ProcessEndTurn();
    }


    // ────────────────────────── 자산 매각 ──────────────────────────
    public void HandleSellPropertiesPrompt(long payerId, long receiverId, long requiredAmount)
    {
        // 봇의 경우, 매각가 합이 부족분 이상이 되도록 자산을 골라 팝니다.
        if (IsBot(payerId))
        {
            List<int> chosen = ChooseSellProperties(payerId, requiredAmount);
            RunAfterDelay(() => SellProperties(payerId, receiverId, chosen, requiredAmount));
            return;
        }

        // 플레이어의 경우, 청산할 자산들을 선택 가능한 UI를 표시합니다.
        Log($"[대기] {P(payerId)}: 통행료 {Won(requiredAmount)}를 내기 위해 팔 자산 선택을 기다립니다.");
    }

    /// <summary>
    /// 부족한 금액을 채우는 자산 조합을 고릅니다. (테스트용 간단 방식)
    /// 매각가가 큰 것부터 담고, 불필요하게 담긴 작은 자산은 다시 뺍니다.
    /// </summary>
    private List<int> ChooseSellProperties(long payerId, long requiredAmount)
    {
        PlayerState payer = GetPlayerState(payerId);
        long shortage = requiredAmount - payer.Money;

        List<PropertyState> owned = gameState.PropertyStates.FindAll(p => p.OwnerId == payerId);
        owned.Sort((a, b) => PropertyManager.Instance.GetSellValue(b).CompareTo(PropertyManager.Instance.GetSellValue(a)));

        List<PropertyState> selected = new List<PropertyState>();
        long sum = 0;
        foreach (PropertyState p in owned)
        {
            if (sum >= shortage) break;
            selected.Add(p);
            sum += PropertyManager.Instance.GetSellValue(p);
        }

        // 작은 것부터, 빼도 부족분을 채운다면 제거
        selected.Sort((a, b) => PropertyManager.Instance.GetSellValue(a).CompareTo(PropertyManager.Instance.GetSellValue(b)));
        for (int i = 0; i < selected.Count;)
        {
            long value = PropertyManager.Instance.GetSellValue(selected[i]);
            if (sum - value >= shortage)
            {
                sum -= value;
                selected.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }

        return selected.ConvertAll(p => p.PropertyId);
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
            Log($"[매각 부족] 선택한 자산({Won(totalAmount)})으로는 부족합니다. 다시 선택하세요.");
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

        List<string> soldNames = new List<string>();
        foreach (var id in propertyIds)
        {
            var property = GetPropertyState(id);
            if (property == null || property.OwnerId != payerId)  continue; // 소유자가 맞는지 확인

            property.OwnerId = null; // 주인 없는 땅으로
            property.BuildingLevel = BuildingLevel.Land; // 건설 레벨 초기화
            soldNames.Add(CityName(id));
        }

        long toll = requiredAmount; // 통행료 전액
        payer.Money = payer.Money + totalAmount - toll; // 판 돈 받고 통행료 냄
        receiver.Money += toll; //통행료 전액 지급
        Log($"[매각] {P(payerId)}: {string.Join(", ", soldNames)} 매각 (+{Won(totalAmount)}), 통행료 {Won(toll)}를 {P(receiverId)}에게 지불 (현금 {Won(payer.Money)})");

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

        long payout = payer.Money + liquidated;
        receiver.Money += payout; // 납부자 현금 전액 + 땅 매각가 합계
        payer.Money = 0; // 납부자 현금 0
        Log($"[파산 정산] {P(payerId)}의 전 재산 {Won(payout)}이 {P(receiverId)}에게 이전 (수령자 현금 {Won(receiver.Money)})");

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
        Log($"[통행료] {P(payerId)} → {P(receiverId)}: {Won(amount)} (내는 쪽 현금 {Won(payer.Money)}, 받는 쪽 현금 {Won(receiver.Money)})");

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
        Log($"[황금열쇠] 카드 {cardId}번 발동");

        // CardManager가 CardId로 카드를 조회하고, EffectType에 맞는 CardEffect 실행 메서드를 호출합니다.
        bool played = CardManager.Instance.PlayCard(cardId);
        if (!played)
            ProcessEndTurn(); // 카드 조회 실패 시 턴이 멈추지 않도록 종료
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