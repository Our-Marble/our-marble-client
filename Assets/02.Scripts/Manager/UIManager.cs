using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 화면 UI 진입점. GameManager는 여기서 창과 연출을 꺼내 쓴다.
/// UIManager_temp의 형식을 따른다: 창을 띄우는 함수가 (playerId, propertyId 같은) 값만 받아 필요한 데이터를 스스로 조회하고,
/// 버튼을 누르면 주석에 적힌 GameManager 함수를 호출한다.
/// 뷰 참조는 메뉴 Tools > Our Marble > Game UI 연결 로 채운다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private PlayerInfoView[] playerInfos = new PlayerInfoView[4];
    [SerializeField] private TopBarView topBar;
    [SerializeField] private CurrentTurnView currentTurn;
    [SerializeField] private DiceRollView diceRoll;
    [SerializeField] private DiceResultView diceResult;

    [Header("팝업")]
    [SerializeField] private RoomSetupView roomSetup;
    [SerializeField] private PurchasePopupView purchase;
    [SerializeField] private TakeoverPopupView takeover;
    [SerializeField] private SellPopupView sell;
    [SerializeField] private IslandEscapeView islandEscape;
    [SerializeField] private CardDrawView cardDraw;
    [SerializeField] private DestinationSelectView destinationSelect;
    [SerializeField] private TileInfoView tileInfo;
    [SerializeField] private GameResultView gameResult;

    [Header("데이터")]
    [Tooltip("황금열쇠 카드의 이름·설명·아이콘을 찾는 덱. CardManager가 쓰는 덱과 같은 에셋을 연결한다.")]
    [SerializeField] private CardDeckData cardDeck;
    [Tooltip("상단바에 '턴 N / 최대'로 보여줄 최대 턴")]
    [SerializeField] private int maxTurn = 30;

    public TopBarView TopBar => topBar;
    public CurrentTurnView CurrentTurn => currentTurn;
    public DiceRollView DiceRoll => diceRoll;
    public DiceResultView DiceResult => diceResult;
    public RoomSetupView RoomSetup => roomSetup;
    public PurchasePopupView Purchase => purchase;
    public TakeoverPopupView Takeover => takeover;
    public SellPopupView Sell => sell;
    public IslandEscapeView IslandEscape => islandEscape;
    public CardDrawView CardDraw => cardDraw;
    public DestinationSelectView DestinationSelect => destinationSelect;
    public TileInfoView TileInfo => tileInfo;
    public GameResultView GameResult => gameResult;

    // ───────────── 밖에서 연결하는 곳 ─────────────

    /// <summary>
    /// 건설 단계를 잠그는 규칙. 잠가야 하면 사유(예: "2바퀴부터")를, 열려 있으면 null을 돌려준다.
    /// 바퀴 수처럼 UI가 모르는 규칙은 GameManager 쪽에서 이 함수를 지정해서 알려준다.
    /// </summary>
    public Func<BuildingLevel, string> BuildLockReason { get; set; }

    /// <summary>
    /// 특수 칸(PropertyData.CanBuild == false, 예: 카페)의 효과 설명을 돌려주는 함수. propertyId를 받아 문장을 돌려줍니다.
    /// 지정하지 않거나 빈 문장이면 기본 안내가 나옵니다. (설명 데이터가 PropertyData에 생기면 여기서 읽으면 됩니다.)
    /// </summary>
    public Func<int, string> PropertyEffectDescription { get; set; }

    /// <summary>
    /// 매각 창의 '자동 선택' 버튼이 부르는 함수 (payerId, 더 필요한 금액) → 팔 땅 번호들. 조합 계산은 UIManager가 하지 않으므로 PropertyManager/GameManager 쪽에서 지정합니다.
    /// 지정하지 않으면 자동 선택 버튼은 아무것도 하지 않습니다.
    /// </summary>
    public Func<long, long, IEnumerable<int>> AutoSellSelector { get; set; }

    /// <summary>매각 중 보드에서 땅을 고르거나 해제할 때 (propertyId, 선택됨). 보드가 선택 표시를 켜고 끌 때 쓴다.</summary>
    public event Action<int, bool> SellSelectionChanged;

    /// <summary>매각 창의 '은행 대출' 버튼 (payerId, receiverId). 구독자가 없으면 버튼이 잠긴다.</summary>
    public event Action<long, long> BankLoanRequested;

    /// <summary>매각 창의 '파산 신청' 버튼 (payerId, receiverId).</summary>
    public event Action<long, long> BankruptRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CloseAllPopups(immediate: true);

        if (diceRoll != null)
        {
            // 굴리기 버튼은 기본적으로 잠겨 있고, 내 턴에 ShowRollDicePopup()을 부르면 켜진다
            diceRoll.SetInteractable(false);
            diceRoll.SetGauge(0f);
            // 무인도 표시는 갇힌 플레이어의 주사위 UI를 열 때만 켠다
            diceRoll.SetIslandTurns(0);
        }
        // 주사위 결과창이 닫히면 게이지를 비운다
        if (diceResult != null && diceRoll != null) diceResult.Closed += () => diceRoll.SetGauge(0f);

        // 상단바 방 설정 버튼: 방 설정 팝업 열기/닫기
        if (topBar != null && roomSetup != null) topBar.SetRoomSetupCallback(roomSetup.Toggle);
    }

    // 돈이 바뀌면 EconomyManager가 알려준다. 돈 표시와 연출은 여기서 갱신한다.
    private void OnEnable() => EconomyManager.OnMoneyChanged += HandleMoneyChanged;
    private void OnDisable() => EconomyManager.OnMoneyChanged -= HandleMoneyChanged;

    #region 창 띄우기 (GameManager가 호출)

    // 자유여행으로 가고싶은 칸을 선택하는 창을 띄워주는 함수입니다.
    // 칸을 선택하면, GameManager.HandleDestinationChosen(int 해당칸인덱스) 를 호출합니다.
    // 보드의 칸을 누르는 쪽에서는 OnBoardTileClicked(칸인덱스)를 불러주세요. 선택 중이면 자동으로 위 함수로 이어집니다.
    public void ShowChooseDestinationPopup()
    {
        chooseDestinationMode = true;
        if (diceRoll != null) diceRoll.SetInteractable(false); // 목적지를 고르는 동안에는 주사위를 굴릴 수 없다
        if (destinationSelect != null) destinationSelect.Show();
    }

    // 주사위 굴리기 버튼이 있는 창을 띄워주는 함수입니다.
    // 굴리기 버튼을 누르면 GameManager.RollDice() 를 호출합니다.
    // 현재 차례 플레이어가 무인도에 갇혀 있으면 남은 턴도 함께 보여줍니다.
    public void ShowRollDicePopup()
    {
        if (diceRoll == null) return;

        PlayerState player = CurrentPlayer;
        int islandTurns = player != null ? player.IslandTurnsRemaining : 0;
        diceRoll.Show(power =>
        {
            // 버튼을 누르는 즉시 결과를 요청하고, 결과(ShowDiceResult)가 올 때까지 주사위는 구르는 상태로 둡니다.
            // 나중에는 Game.RollDice() 대신 서버 요청을 보내고, 응답을 받을 때 ShowDiceResult를 부르면 됩니다.
            if (diceResult != null) diceResult.StartRolling();
            if (Game != null) Game.RollDice();
        }, islandTurns);
    }

    // 빈 땅에 대하여  사기 & 사지않기 버튼이 있는 창을 띄워주는 함수입니다.
    // 사기 버튼을 누르면 GameManager.PurchaseProperty(playerId, propertyId) 를 호출합니다.
    // 사지않기 버튼을 누르면 GameManager.DeclinePropertyPurchase(playerId, propertyId) 를 호출합니다.
    // 땅 구매 창은 '건물(땅 구매)'만 고를 수 있고, 별 카드는 구매 뒤 건설 단계라 잠겨서 보입니다.
    public void ShowPurchasePropertyPopup(long playerId, long propertyId, int amount)
    {
        if (purchase == null) return;

        int id = (int)propertyId;
        PropertyData data = PropertyManager.Instance.GetData(id);
        PlayerState player = GetPlayer(playerId);

        // 내 땅이면 가진 단계까지 "보유 중" (Land=건물, Villa=별1, Building=별2, Hotel=별3)
        // 남의 땅은 인수 선택을 거치므로 여기서 따로 막지 않는다
        PropertyState owned = GetProperty(id);
        bool mine = owned != null && owned.OwnerId.HasValue && owned.OwnerId.Value == playerId;
        int ownedLevel = mine ? (int)owned.BuildingLevel : -1;

        // 이미 내 땅이고 더 지을 수 있으면, 다음 단계(별 추가)를 고르는 건설 창과 같다 (바퀴 제한은 BuildLockReason)
        bool special = data != null && !data.CanBuild; // 특수 칸은 건물(땅)만 살 수 있고 별 건설이 없다
        if (mine && !special && ownedLevel < (int)BuildingLevel.Hotel && data != null)
        {
            ShowBuildPopup(playerId, id);
            return;
        }

        var options = new List<PurchasePopupView.Option>
        {
            new PurchasePopupView.Option
            {
                TargetLevel = BuildingLevel.Land,
                Cost = amount,
                Toll = data != null ? data.GetToll(BuildingLevel.Land) : 0,
                Locked = mine,
                LockReason = mine ? "보유 중" : null,
            }
        };

        if (special)
        {
            string description = SpecialDescription(id);
            purchase.Show(TileName(id), null, options, player != null ? player.Money : 0,
                level => { if (Game != null) Game.PurchaseProperty(playerId, id); },
                () => { if (Game != null) Game.DeclinePropertyPurchase(playerId, id); },
                OwnerLabel(owned), description);
            return;
        }

        long buildCostSum = 0; // 별 1개, 2개, 3개까지 짓는 데 드는 누적 건설비 (참고용)
        for (int i = (int)BuildingLevel.Villa; i <= (int)BuildingLevel.Hotel; i++)
        {
            var level = (BuildingLevel)i;
            if (data != null) buildCostSum += data.GetBuildCost(level);

            string reason = BuildLockReason != null ? BuildLockReason(level) : null;
            options.Add(new PurchasePopupView.Option
            {
                TargetLevel = level,
                Cost = buildCostSum,
                Toll = data != null ? data.GetToll(level) : 0,
                Locked = true,
                LockReason = i <= ownedLevel ? "보유 중"
                    : string.IsNullOrEmpty(reason) ? "구매 후 건설" : reason,
            });
        }

        purchase.Show(TileName(id), null, options, player != null ? player.Money : 0,
            level => { if (Game != null) Game.PurchaseProperty(playerId, id); },
            () => { if (Game != null) Game.DeclinePropertyPurchase(playerId, id); },
            OwnerLabel(owned));
    }

    // 건설하기 & 건설하지않기 버튼이 있는 창을 띄워주는 함수입니다.
    // 건설하기 버튼을 누르면 GameManager.Build(playerId, propertyId) 를 호출합니다.
    // 건설하지않기 버튼을 누르면 GameManager.DeclineBuild(playerId, propertyId) 를 호출합니다.
    // 건물 → 별 1개 → 2개 → 3개 카드가 모두 보이고, 지금 지을 수 있는 다음 단계만 고를 수 있습니다.
    public void ShowBuildPopup(long playerId, int propertyId)
    {
        if (purchase == null) return;

        PropertyState state = GetProperty(propertyId);
        PropertyData data = PropertyManager.Instance.GetData(propertyId);
        PlayerState player = GetPlayer(playerId);
        if (state == null || data == null) return;

        int current = (int)state.BuildingLevel;
        var options = new List<PurchasePopupView.Option>();
        long pending = 0; // 다음 단계부터 해당 단계까지 더한 건설비

        for (int i = (int)BuildingLevel.Land; i <= (int)BuildingLevel.Hotel; i++)
        {
            var level = (BuildingLevel)i;
            var option = new PurchasePopupView.Option { TargetLevel = level, Toll = data.GetToll(level) };

            if (i <= current)
            {
                option.Locked = true;
                option.LockReason = "보유 중";
            }
            else
            {
                pending += data.GetBuildCost(level);
                option.Cost = pending;

                if (i > current + 1)
                {
                    option.Locked = true;
                    option.LockReason = "이전 단계 먼저";
                }
                else
                {
                    string reason = BuildLockReason != null ? BuildLockReason(level) : null;
                    if (!string.IsNullOrEmpty(reason))
                    {
                        option.Locked = true;
                        option.LockReason = reason;
                    }
                }
            }
            options.Add(option);
        }

        purchase.Show(TileName(propertyId), null, options, player != null ? player.Money : 0,
            level => { if (Game != null) Game.Build(playerId, propertyId); },
            () => { if (Game != null) Game.DeclineBuild(playerId, propertyId); },
            OwnerLabel(state));
    }

    // 특수 칸(별 건설 없음)의 효과 설명. PropertyEffectDescription이 없거나 비어 있으면 기본 안내
    private string SpecialDescription(int propertyId)
    {
        string description = PropertyEffectDescription != null ? PropertyEffectDescription(propertyId) : null;
        return string.IsNullOrEmpty(description) ? "특수 칸이에요. 별 건설 없이 건물만 구매할 수 있어요." : description;
    }

    // 구매·건설 창의 타일 사진에 붙는 소유자 표시. 주인이 없으면 "빈 땅", 있으면 인수창처럼 "● 이름 님의 땅"
    private string OwnerLabel(PropertyState property)
    {
        if (property == null || !property.OwnerId.HasValue) return "빈 땅";
        long ownerId = property.OwnerId.Value;
        return $"<color={UIPalette.Hex(UIPalette.PlayerColor(ColorIndexOf(ownerId)))}>●</color> {NameOf(ownerId)} 님의 땅";
    }

    // 인수하기 & 인수하지않기 버튼이 있는 창을 띄워주는 함수입니다.
    // 인수하기 버튼을 누르면 GameManager.AcquireProperty(playerId, propertyId) 를 호출합니다.
    // 인수하지않기 버튼을 누르면 GameManager.DeclineAcquireProperty(playerId, propertyId) 를 호출합니다.
    public void ShowAcquirePropertyPopup(long playerId, int propertyId)
    {
        if (takeover == null) return;

        PropertyState state = GetProperty(propertyId);
        PlayerState player = GetPlayer(playerId);
        if (state == null) return;

        long ownerId = state.OwnerId ?? 0;
        long price = PropertyManager.Instance.GetAcquireValue(state);

        takeover.Show(TileName(propertyId), null, state.BuildingLevel,
            NameOf(ownerId), ColorIndexOf(ownerId), price, player != null ? player.Money : 0,
            () => { if (Game != null) Game.AcquireProperty(playerId, propertyId); },
            () => { if (Game != null) Game.DeclineAcquireProperty(playerId, propertyId); });
    }

    // 매각할 자산들을 선택할 수 있는 창을 띄워주는 함수입니다.
    // 선택 완료 버튼을 누르면 GameManager.SellProperties(payerId, receiverId, List<int> propertyIds, long requiredAmount) 를 호출합니다.
    // 클라측에서 판단한 '선택한 자산 가치 총합' + 현금값이 requiredAmount보다 작으면 선택 완료 버튼이 활성화되지 않습니다.
    // 팔 땅은 보드에서 고릅니다. 보드의 칸을 누르는 쪽에서는 OnBoardTileClicked(칸인덱스)를 불러주세요. (내 땅이면 선택/해제)
    public void ShowSellPropertiesPopup(long payerId, long receiverId, long requiredAmount)
    {
        if (sell == null) return;

        ExitSellMode();
        sellMode = true;
        sellPayerId = payerId;
        sellReceiverId = receiverId;
        sellRequired = requiredAmount;

        PlayerState payer = GetPlayer(payerId);
        bool loanAvailable = BankLoanRequested != null && !loanUsed.Contains(payerId);

        sell.Show(requiredAmount, payer != null ? payer.Money : 0, loanAvailable,
            AutoSelectSellProperties, RequestBankLoan, RequestBankrupt, CompleteSell);
    }

    #endregion

    #region 보드 칸 클릭

    /// <summary>
    /// 보드의 칸을 눌렀을 때 불러주세요. 지금 상황에 맞게 알아서 처리합니다.
    /// 목적지를 고르는 중이면 그 칸으로 이동, 매각 중이면 내 땅 선택/해제, 그 외에는 땅 정보 창을 띄웁니다.
    /// </summary>
    public void OnBoardTileClicked(int tileIndex)
    {
        if (chooseDestinationMode)
        {
            SelectDestination(tileIndex);
            return;
        }

        TileData tile = BoardManager.Instance.GetTileData(tileIndex);
        if (tile == null || tile.Type != TileType.PROPERTY) return;

        if (sellMode) ToggleSellSelection(tile.PropertyId);
        else ShowTileInfoPopup(tile.PropertyId);
    }

    private bool chooseDestinationMode;

    private void SelectDestination(int tileIndex)
    {
        chooseDestinationMode = false;
        if (destinationSelect != null) destinationSelect.Close();
        if (Game != null) Game.HandleDestinationChosen(tileIndex);
    }

    #endregion

    #region 매각

    private bool sellMode;
    private long sellPayerId;
    private long sellReceiverId;
    private long sellRequired;
    private readonly HashSet<int> selectedSell = new();
    private long sellSelectedTotal; // 마지막으로 구한 선택 합계 (표시용)
    private readonly HashSet<long> loanUsed = new(); // 은행 대출은 1회

    /// <summary>매각 중 고른 땅들</summary>
    public IReadOnlyCollection<int> SelectedSellPropertyIds => selectedSell;

    /// <summary>매각할 땅을 고르거나 해제한다. 매각 중이 아니거나 내 땅이 아니면 무시한다.</summary>
    public void ToggleSellSelection(int propertyId)
    {
        if (!sellMode) return;
        PropertyState state = GetProperty(propertyId);
        if (state == null || state.OwnerId != sellPayerId) return;

        bool selected = selectedSell.Add(propertyId);
        if (!selected) selectedSell.Remove(propertyId);

        SellSelectionChanged?.Invoke(propertyId, selected);
        RefreshSellTotal();
    }

    // 고른 땅들의 매각가 합(PropertyManager.GetSellValue)을 구해 표시한다.
    // 이 합 + 현금이 requiredAmount보다 작으면 매각 창의 '매각 완료' 버튼이 켜지지 않는다. (UIManager_temp 요구사항)
    private void RefreshSellTotal()
    {
        long total = 0;
        foreach (int id in selectedSell)
        {
            PropertyState state = GetProperty(id);
            if (state != null) total += PropertyManager.Instance.GetSellValue(state);
        }
        sellSelectedTotal = total;
        if (sell != null) sell.SetSelectedTotal(sellSelectedTotal);
    }

    // '자동 선택' 버튼: 어떤 땅을 팔지는 AutoSellSelector가 정하고, 여기서는 선택 표시만 맞춘다
    private void AutoSelectSellProperties()
    {
        PlayerState payer = GetPlayer(sellPayerId);
        if (payer == null) return;
        if (AutoSellSelector == null)
        {
            Debug.LogWarning("[UIManager] 자동 선택이 아직 연결되지 않았습니다. AutoSellSelector를 지정해주세요.");
            return;
        }

        var picked = new HashSet<int>(AutoSellSelector(sellPayerId, sellRequired - payer.Money) ?? new List<int>());
        foreach (int id in new List<int>(selectedSell))
            if (!picked.Contains(id)) { selectedSell.Remove(id); SellSelectionChanged?.Invoke(id, false); }
        foreach (int id in picked)
            if (selectedSell.Add(id)) SellSelectionChanged?.Invoke(id, true);
        RefreshSellTotal();
    }

    private void RequestBankLoan()
    {
        if (BankLoanRequested == null) return;
        loanUsed.Add(sellPayerId);
        if (sell != null) sell.SetLoanAvailable(false);
        BankLoanRequested.Invoke(sellPayerId, sellReceiverId);
    }

    private void RequestBankrupt()
    {
        if (BankruptRequested == null)
        {
            Debug.LogWarning("[UIManager] 파산 신청 처리가 아직 연결되지 않았습니다. BankruptRequested를 구독해주세요.");
            return;
        }
        long payerId = sellPayerId, receiverId = sellReceiverId;
        if (sell != null) sell.Close();
        ExitSellMode();
        BankruptRequested.Invoke(payerId, receiverId);
    }

    private void CompleteSell()
    {
        long payerId = sellPayerId, receiverId = sellReceiverId, required = sellRequired;
        var propertyIds = new List<int>(selectedSell);
        ExitSellMode();
        if (Game != null) Game.SellProperties(payerId, receiverId, propertyIds, required);
    }

    // 매각 모드를 끝내고 보드의 선택 표시도 모두 끈다
    private void ExitSellMode()
    {
        if (selectedSell.Count > 0)
        {
            foreach (int id in new List<int>(selectedSell)) SellSelectionChanged?.Invoke(id, false);
            selectedSell.Clear();
        }
        sellMode = false;
        sellSelectedTotal = 0;
    }

    #endregion

    #region 턴 · 주사위

    // 'OO의 턴'이라는 UI를 표시합니다. GameManager.HandleTurnChanged에서 부릅니다.
    // 차례 표시, 플레이어 카드 강조, 상단바 턴 수를 함께 바꾸고, 내 차례가 아니면 주사위 굴리기 버튼을 잠급니다.
    public void ShowTurn(long playerId)
    {
        int index = IndexOf(playerId);
        if (index < 0) return;

        SetTurn(index, NameOf(playerId), PortraitOf(playerId));
        if (topBar != null) topBar.SetTurn(DisplayTurn(), maxTurn);
        if (playerId != LocalPlayerId && diceRoll != null) diceRoll.SetInteractable(false);
    }

    // 요청에 실패하거나 결과가 오지 않을 때, 구르고 있는 주사위를 멈추고 창을 닫습니다.
    public void CancelDiceRolling() { if (diceResult != null) diceResult.CancelRolling(); }

    // 주사위 굴림 연출을 재생합니다. GameManager.HandleDiceRolled에서 부릅니다.
    // 굴리기 버튼을 누른 뒤라면 이미 주사위가 구르는 중이므로, 결과를 받는 즉시 멈춥니다.
    // 눈이 흔들리다 멈추고 합계가 나오며, 같은 눈이면 '더블!'이 뜹니다. 1.5초 뒤 창이 스스로 닫힙니다.
    // 내가 무인도에 갇혀 있을 때 굴렸다면, 결과가 나온 뒤 더블이면 탈출하고 아니면 남은 턴이 1 줄어듭니다.
    // onRevealed는 결과 표시가 끝난 뒤 호출됩니다. (말 이동을 이 뒤에 시작하면 연출과 순서가 맞습니다.)
    public void ShowDiceResult(int dice1, int dice2, Action onRevealed = null)
    {
        if (diceResult == null)
        {
            onRevealed?.Invoke();
            return;
        }

        bool mine = State != null && State.CurrentPlayerId == LocalPlayerId;
        diceResult.Show(dice1, dice2, () =>
        {
            if (mine && diceRoll != null) diceRoll.ResolveIslandRoll(dice1 == dice2);
            onRevealed?.Invoke();
        });
    }

    // 무인도에 갇혔을 때 탈출 방법을 고르는 창을 띄워주는 함수입니다.
    // 주사위 더블 / 비용 지불 / 카드 사용 중 누른 것에 해당하는 콜백을 호출합니다.
    // (GameManager에 탈출 함수가 아직 없어서 콜백으로 받습니다. 현금이 모자라면 비용 지불, 카드가 없으면 카드 사용이 잠깁니다.)
    public void ShowIslandEscapePopup(long playerId, long escapeCost, int escapeCardCount,
                                      Action onRollDouble, Action onPay, Action onUseCard)
    {
        if (islandEscape == null) return;

        PlayerState player = GetPlayer(playerId);
        int turns = player != null ? Mathf.Max(1, player.IslandTurnsRemaining) : 1;
        islandEscape.Show(turns, escapeCost, player != null ? player.Money : 0, escapeCardCount,
            onRollDouble, onPay, onUseCard);
    }

    #endregion

    #region 플레이어 정보 · 돈

    private readonly Dictionary<long, string> nextMoneyReason = new();
    private readonly HashSet<long> transferPlayers = new();   // 이동 연출이 돈 표시를 대신 처리하는 동안
    private readonly HashSet<long> bankruptShown = new();     // 파산 연출을 이미 보여준 플레이어
    private readonly Dictionary<long, long> bankruptShortfall = new();

    private class Profile
    {
        public string Name;
        public Sprite Portrait;
    }
    private readonly Dictionary<long, Profile> profiles = new();
    private long localPlayerId;
    private bool hasLocalPlayer;

    /// <summary>이 화면을 보는 플레이어. 지정하지 않으면 첫 번째 플레이어입니다.</summary>
    public long LocalPlayerId
    {
        get
        {
            if (hasLocalPlayer) return localPlayerId;
            GameState state = State;
            return state != null && state.PlayerStates.Count > 0 ? state.PlayerStates[0].PlayerId : 0;
        }
    }

    public void SetLocalPlayer(long playerId)
    {
        localPlayerId = playerId;
        hasLocalPlayer = true;
        if (roomSetup != null) roomSetup.SetLocalPlayer(Mathf.Max(0, IndexOf(playerId)));
    }

    /// <summary>플레이어 이름과 초상화. 지정하지 않으면 "플레이어 N"과 기본 초상화를 씁니다.</summary>
    public void SetPlayerProfile(long playerId, string playerName, Sprite portrait = null)
    {
        profiles[playerId] = new Profile { Name = playerName, Portrait = portrait };

        int index = IndexOf(playerId);
        PlayerInfoView view = GetPlayerInfo(index);
        if (view != null) view.SetProfile(NameOf(playerId), PortraitOf(playerId), index);
    }

    /// <summary>
    /// 게임을 시작할 때 한 번 부릅니다. GameState의 플레이어 수에 맞춰 카드를 채웁니다.
    /// (플레이어 이름은 그 전에 SetPlayerProfile로 넣어주세요.)
    /// </summary>
    public void InitPlayers()
    {
        GameState state = State;
        if (state == null) return;

        bankruptShown.Clear();
        bankruptShortfall.Clear();
        transferPlayers.Clear();

        int count = state.PlayerStates.Count;
        SetPlayerCount(count);
        for (int i = 0; i < count; i++)
        {
            PlayerInfoView view = GetPlayerInfo(i);
            long id = state.PlayerStates[i].PlayerId;
            if (view != null) view.SetProfile(NameOf(id), PortraitOf(id), i);
        }
        RefreshAllPlayers(animate: false);

        if (topBar != null)
        {
            topBar.SetPlayerCount(count, playerInfos.Length);
            topBar.SetTurn(DisplayTurn(), maxTurn);
        }
    }

    /// <summary>GameState를 다시 읽어 모든 플레이어 카드(돈, 총 자산, 등수, 파산)를 맞춥니다. 돈 변화 표시는 띄우지 않습니다.</summary>
    public void RefreshAllPlayers(bool animate = true)
    {
        GameState state = State;
        if (state == null) return;

        for (int i = 0; i < state.PlayerStates.Count; i++)
        {
            PlayerInfoView view = GetPlayerInfo(i);
            if (view == null) continue;
            PlayerState player = state.PlayerStates[i];
            view.SetMoney(player.Money, TotalAssetOf(player.PlayerId, player.Money), animate);
            view.SetBankrupt(IsBankrupt(player), animate);
        }
        UpdateRanks();
    }

    // 돈이 바뀐 것을 카드에 반영하고, 바뀐 만큼 돈 변화 표시를 띄웁니다. (EconomyManager.OnMoneyChanged)
    private void HandleMoneyChanged(long playerId, long before, long after)
    {
        if (sellMode && playerId == sellPayerId && sell != null) sell.SetCash(after, sellSelectedTotal);

        PlayerInfoView view = GetPlayerInfo(IndexOf(playerId));
        if (view == null || transferPlayers.Contains(playerId)) return;

        view.SetMoney(after, TotalAssetOf(playerId, after));
        if (after != before) view.ShowMoneyChange(TakeMoneyReason(playerId, after - before), after - before);
        UpdateRanks();
    }

    /// <summary>다음 돈 변화 표시에 붙일 사유(예: "황금열쇠"). 지정하지 않으면 "입금"/"출금"으로 표시됩니다.</summary>
    public void SetNextMoneyReason(long playerId, string reason) => nextMoneyReason[playerId] = reason;

    private string TakeMoneyReason(long playerId, long delta)
    {
        if (nextMoneyReason.TryGetValue(playerId, out string reason))
        {
            nextMoneyReason.Remove(playerId);
            return reason;
        }
        return delta > 0 ? "입금" : "출금";
    }

    // 재화 획득/손실 연출을 재생합니다. GameManager.HandleDonationPaid, HandleWelfareFundReceived에서 부릅니다.
    // GameState를 먼저 갱신한 뒤 부릅니다. (EconomyManager.NotifyMoneyChanged를 이미 부르는 곳은 이 함수를 따로 부르지 않습니다.)
    public void PlayMoneyChange(long playerId, long amount, string reason)
    {
        PlayerInfoView view = GetPlayerInfo(IndexOf(playerId));
        PlayerState player = GetPlayer(playerId);
        if (view == null || player == null) return;

        view.SetMoney(player.Money, TotalAssetOf(playerId, player.Money));
        if (amount != 0) view.ShowMoneyChange(reason, amount);
        UpdateRanks();
    }

    // 재화 이동 연출을 재생합니다. GameManager.HandleTollPaid에서 부릅니다.
    public void PlayTollEffect(long payerId, long receiverId, long amount)
        => PlayMoneyTransfer(payerId, receiverId, amount, "통행료", "통행료 수입");

    /// <summary>
    /// 돈이 payer에서 receiver로 넘어가는 연출입니다. 내는 사람은 바로 줄어들고, 받는 사람은 코인이 도착할 때 늘어납니다.
    /// (인수, 매각 후 통행료 지불 등에도 씁니다. GameState를 먼저 갱신한 뒤 부릅니다.)
    /// </summary>
    public void PlayMoneyTransfer(long payerId, long receiverId, long amount, string payerReason, string receiverReason)
    {
        int from = IndexOf(payerId), to = IndexOf(receiverId);
        PlayerInfoView payerView = GetPlayerInfo(from), receiverView = GetPlayerInfo(to);
        PlayerState payer = GetPlayer(payerId), receiver = GetPlayer(receiverId);
        if (payerView == null || receiverView == null || payer == null || receiver == null) return;

        // 이 연출이 끝날 때까지 두 사람의 돈 표시는 여기서만 바꾼다 (같은 변화가 두 번 뜨지 않게)
        transferPlayers.Add(payerId);
        transferPlayers.Add(receiverId);

        payerView.SetMoney(payer.Money, TotalAssetOf(payerId, payer.Money));
        payerView.ShowMoneyChange(payerReason, -amount);

        void Finish()
        {
            transferPlayers.Remove(payerId);
            transferPlayers.Remove(receiverId);
        }

        PlayCoinFlight(from, to, () =>
        {
            receiverView.SetMoney(receiver.Money, TotalAssetOf(receiverId, receiver.Money));
            receiverView.ShowMoneyChange(receiverReason, amount);
            UpdateRanks();
            Finish();
        });
        // 코인 연출이 끊겨도 돈 표시가 잠겨 있지 않게 한다
        DOVirtual.DelayedCall(3f, Finish, ignoreTimeScale: true);
    }

    // 파산 연출을 재생합니다. GameManager.HandleBankruptcy에서 부릅니다.
    // shortfall은 못 낸 금액으로, 게임 결과 창에 마이너스 자산으로 표시됩니다. (모르면 생략)
    public void PlayBankruptEffect(long playerId, long shortfall = 0)
    {
        bankruptShown.Add(playerId);
        if (shortfall > 0) bankruptShortfall[playerId] = shortfall;

        PlayerInfoView view = GetPlayerInfo(IndexOf(playerId));
        if (view != null) view.SetBankrupt(true);
        RefreshAllPlayers();
    }

    private bool IsBankrupt(PlayerState player) => player.IsBankrupt || bankruptShown.Contains(player.PlayerId);

    // 총 자산 = 현금 + 가진 땅의 투자금(땅값 + 지은 건물 비용)
    private long TotalAssetOf(long playerId, long cash)
    {
        long total = cash;
        GameState state = State;
        if (state == null) return total;

        foreach (PropertyState property in state.PropertyStates)
            if (property.OwnerId == playerId)
                total += PropertyManager.Instance.GetInvestedAmount(property);
        return total;
    }

    // 총 자산 순으로 등수를 매긴다. 파산한 사람은 맨 뒤.
    private void UpdateRanks()
    {
        GameState state = State;
        if (state == null) return;

        var order = new List<int>();
        for (int i = 0; i < state.PlayerStates.Count; i++) order.Add(i);
        order.Sort((a, b) =>
        {
            PlayerState pa = state.PlayerStates[a], pb = state.PlayerStates[b];
            if (IsBankrupt(pa) != IsBankrupt(pb)) return IsBankrupt(pa) ? 1 : -1;
            int byAsset = TotalAssetOf(pb.PlayerId, pb.Money).CompareTo(TotalAssetOf(pa.PlayerId, pa.Money));
            return byAsset != 0 ? byAsset : a.CompareTo(b);
        });

        for (int rank = 0; rank < order.Count; rank++)
        {
            PlayerInfoView view = GetPlayerInfo(order[rank]);
            if (view != null) view.SetRank(rank + 1);
        }
    }

    // 상단바에 보여줄 턴. GameState.TurnNumber는 플레이어 한 명의 차례마다 늘어나므로 한 바퀴를 1턴으로 센다.
    private int DisplayTurn()
    {
        GameState state = State;
        if (state == null || state.PlayerStates.Count == 0) return 1;
        return Mathf.Max(1, (state.TurnNumber - 1) / state.PlayerStates.Count + 1);
    }

    /// <summary>플레이어 순서(0~3)의 정보 카드.</summary>
    public PlayerInfoView GetPlayerInfo(int index)
    {
        if (index < 0 || index >= playerInfos.Length) return null;
        return playerInfos[index];
    }

    /// <summary>게임 인원(1~4)에 맞춰 플레이어 정보 카드를 보이거나 숨긴다. 앞에서부터 count장만 보인다.</summary>
    public void SetPlayerCount(int count)
    {
        for (int i = 0; i < playerInfos.Length; i++)
            if (playerInfos[i] != null) playerInfos[i].gameObject.SetActive(i < count);
    }

    /// <summary>차례 표시와 카드 강조를 함께 바꾼다. (카드 번호 기준. playerId 기준은 ShowTurn)</summary>
    public void SetTurn(int playerIndex, string playerName, Sprite portrait)
    {
        for (int i = 0; i < playerInfos.Length; i++)
            if (playerInfos[i] != null) playerInfos[i].SetTurn(i == playerIndex);
        if (currentTurn != null) currentTurn.Show(playerName, portrait, playerIndex);
    }

    #endregion

    #region 땅 정보 · 카드 · 게임 결과

    // 땅 정보 창을 띄웁니다. 보드의 땅을 눌렀을 때 OnBoardTileClicked가 부릅니다.
    // 주인, 지금 단계, 단계별 통행료(현재 단계만 굵게)를 보여줍니다.
    public void ShowTileInfoPopup(int propertyId)
    {
        if (tileInfo == null) return;

        PropertyData data = PropertyManager.Instance.GetData(propertyId);
        PropertyState state = GetProperty(propertyId);
        if (data == null) return;

        var tolls = new long[4];
        for (int i = 0; i < tolls.Length; i++) tolls[i] = data.GetToll((BuildingLevel)i);

        bool owned = state != null && state.OwnerId.HasValue;
        long ownerId = owned ? state.OwnerId.Value : 0;
        tileInfo.Show(data.CityName, null, owned ? NameOf(ownerId) : null, ColorIndexOf(ownerId),
            state != null ? state.BuildingLevel : BuildingLevel.Land, tolls,
            data.CanBuild ? null : SpecialDescription(propertyId));
    }

    // 황금열쇠 카드를 뽑는 연출입니다. CardManager.PlayCard가 카드 효과를 실행하기 전에 부릅니다.
    // 카드가 뒤집혀 이름·설명이 나오고, 확인을 누르면 onConfirm을 호출합니다. (여기서 카드 효과를 실행하면 됩니다.)
    public void ShowCardDrawPopup(int cardId, Action onConfirm)
    {
        if (cardDraw == null)
        {
            onConfirm?.Invoke();
            return;
        }

        CardData card = cardDeck != null ? cardDeck.FindById(cardId) : null;
        string cardName = card != null ? card.cardName : $"황금열쇠 {cardId}";
        string description = card != null ? card.description : "";
        Sprite icon = card != null ? card.icon : null;

        long currentId = State != null ? State.CurrentPlayerId : 0;
        cardDraw.Show(cardName, description, icon, false, () =>
        {
            SetNextMoneyReason(currentId, "황금열쇠"); // 카드로 돈이 바뀌면 사유가 '황금열쇠'로 뜬다
            onConfirm?.Invoke();
        });
    }

    // 게임 결과 창을 띄우는 함수입니다. 게임이 끝났을 때 GameManager가 호출합니다.
    // 등수 순(파산한 사람은 맨 뒤)으로 승리/패배/파산과 최종 자산을 보여주고, 닫기를 누르면 onClose를 호출합니다.
    public void ShowGameResultPopup(Action onClose = null)
    {
        GameState state = State;
        if (gameResult == null || state == null) return;

        var ranked = new List<PlayerState>(state.PlayerStates);
        ranked.Sort((a, b) =>
        {
            if (IsBankrupt(a) != IsBankrupt(b)) return IsBankrupt(a) ? 1 : -1;
            return TotalAssetOf(b.PlayerId, b.Money).CompareTo(TotalAssetOf(a.PlayerId, a.Money));
        });

        var entries = new List<GameResultView.Entry>();
        for (int i = 0; i < ranked.Count; i++)
        {
            PlayerState player = ranked[i];
            bool bankrupt = IsBankrupt(player);
            long shortfall = bankruptShortfall.TryGetValue(player.PlayerId, out long value) ? value : 0;
            entries.Add(new GameResultView.Entry
            {
                Name = NameOf(player.PlayerId),
                Portrait = PortraitOf(player.PlayerId),
                ColorIndex = ColorIndexOf(player.PlayerId),
                FinalAsset = bankrupt ? -shortfall : TotalAssetOf(player.PlayerId, player.Money),
                IsWinner = i == 0 && !bankrupt,
                IsBankrupt = bankrupt,
            });
        }
        gameResult.Show(entries, onClose);
    }

    #endregion

    #region 코인 이동

    private const int CoinCount = 7;
    private const float CoinBurstSeconds = 0.14f;   // 금화에서 흩어져 나오는 시간
    private const float CoinFlightSeconds = 0.55f;  // 기본 비행 시간 (코인마다 조금씩 다름)
    private const float CoinStagger = 0.045f;
    private const int CoinLayerSortingOrder = 95;   // HUD(0~50) 위, 선택 팝업(100~) 아래
    private RectTransform coinLayer;

    /// <summary>
    /// 코인이 from 카드에서 to 카드로 날아간다. 첫 코인이 도착할 때 onArrived를 부른다. (카드 번호 기준)
    /// 돈 표시까지 함께 처리하려면 PlayMoneyTransfer를 쓴다.
    /// </summary>
    public void PlayCoinFlight(int fromIndex, int toIndex, Action onArrived = null)
    {
        var from = GetPlayerInfo(fromIndex);
        var to = GetPlayerInfo(toIndex);
        if (from == null || to == null || from.CashIcon == null || to.CashIcon == null)
        {
            onArrived?.Invoke();
            return;
        }

        var layer = GetCoinLayer(from);
        Vector2 start = layer.InverseTransformPoint(from.CashIcon.position);
        Vector2 end = layer.InverseTransformPoint(to.CashIcon.position);
        Vector2 bendDirection = BendTowardCenter(start, end);
        float distance = Vector2.Distance(start, end);
        bool arrivedOnce = false;

        // 보내는 쪽 금화가 살짝 움츠러들었다가 돌아온다
        PulseIcon(from.CashIcon, 0.82f, 0.1f);

        for (int i = 0; i < CoinCount; i++)
        {
            var rt = CreateCoin(from.CashIcon, layer);
            rt.anchoredPosition = start;
            rt.localScale = Vector3.zero;

            // 코인마다 흩어지는 방향, 휘는 정도, 비행 시간을 조금씩 다르게
            Vector2 burst = start + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(16f, 30f);
            Vector2 control = (burst + end) * 0.5f + bendDirection * distance * UnityEngine.Random.Range(0.18f, 0.34f);
            float flight = CoinFlightSeconds + UnityEngine.Random.Range(0f, 0.12f);
            float delay = i * CoinStagger + UnityEngine.Random.Range(0f, 0.02f);

            float t = 0f;
            DOTween.Sequence().SetUpdate(true).SetLink(rt.gameObject)
                .AppendInterval(delay)
                // 1) 금화에서 톡 튀어나와 주변으로 흩어진다
                .Append(rt.DOAnchorPos(burst, CoinBurstSeconds).SetEase(Ease.OutCubic))
                .Join(rt.DOScale(1f, CoinBurstSeconds).SetEase(Ease.OutBack))
                // 2) 화면 안쪽으로 휘는 곡선을 따라 가속했다 감속하며 날아간다
                .Append(DOTween.To(() => t, v =>
                {
                    t = v;
                    rt.anchoredPosition = Bezier(burst, control, end, v);
                    // 도착 직전에 빨려 들어가듯 작아진다
                    float s = v < 0.8f ? 1f : Mathf.Lerp(1f, 0.5f, (v - 0.8f) / 0.2f);
                    rt.localScale = new Vector3(s, s, 1f);
                }, 1f, flight).SetEase(Ease.InOutCubic))
                // 3) 도착: 받는 금화가 코인을 흡수하며 조금씩 부푼다
                .AppendCallback(() =>
                {
                    AbsorbPulse(to.CashIcon);
                    if (!arrivedOnce)
                    {
                        arrivedOnce = true;
                        onArrived?.Invoke();
                    }
                    UnityEngine.Object.Destroy(rt.gameObject);
                });
        }
    }

    private static RectTransform CreateCoin(RectTransform icon, RectTransform layer)
    {
        var coin = UnityEngine.Object.Instantiate(icon.gameObject, layer, false);
        coin.name = "Coin";
        var le = coin.GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;
        foreach (var graphic in coin.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;

        var rt = (RectTransform)coin.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = icon.rect.size;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    /// <summary>경로에 수직인 방향 중 화면 가운데 쪽. 코인이 화면 밖으로 솟지 않고 안쪽으로 휜다.</summary>
    private static Vector2 BendTowardCenter(Vector2 start, Vector2 end)
    {
        Vector2 dir = (end - start).normalized;
        var normal = new Vector2(-dir.y, dir.x);
        Vector2 mid = (start + end) * 0.5f;
        // 코인 레이어의 원점이 화면 가운데
        if (Vector2.Dot(normal, -mid) < 0f) normal = -normal;
        return normal;
    }

    private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    // 코인이 닿을 때마다 조금씩 커졌다가 부드럽게 돌아온다 (매번 처음부터 튀지 않게 누적)
    private static void AbsorbPulse(RectTransform icon)
    {
        icon.DOKill();
        float current = icon.localScale.x;
        icon.localScale = Vector3.one * Mathf.Min(current + 0.08f, 1.35f);
        icon.DOScale(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(icon.gameObject);
    }

    private static void PulseIcon(RectTransform icon, float scale, float duration)
    {
        icon.DOKill();
        icon.localScale = Vector3.one;
        DOTween.Sequence().SetUpdate(true).SetLink(icon.gameObject)
            .Append(icon.DOScale(scale, duration).SetEase(Ease.OutQuad))
            .Append(icon.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
    }

    // 플레이어 정보 캔버스 안에 코인 전용 하위 캔버스를 만든다.
    // 움직이는 동안 카드가 다시 그려지지 않게 하고, 정렬 순서를 따로 줘서 다른 HUD 위로 날아가게 한다.
    private RectTransform GetCoinLayer(PlayerInfoView anyCard)
    {
        if (coinLayer != null) return coinLayer;
        var go = new GameObject("CoinLayer", typeof(RectTransform));
        go.layer = anyCard.gameObject.layer;
        coinLayer = (RectTransform)go.transform;
        coinLayer.SetParent(anyCard.transform.parent, false);
        coinLayer.SetAsLastSibling();
        coinLayer.anchorMin = Vector2.zero;
        coinLayer.anchorMax = Vector2.one;
        coinLayer.offsetMin = coinLayer.offsetMax = Vector2.zero;
        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = CoinLayerSortingOrder;
        return coinLayer;
    }

    #endregion

    /// <summary>
    /// 선택형 팝업과 일시적인 표시를 모두 닫는다. HUD(플레이어 정보, 상단바)는 그대로 둔다.
    /// immediate면 닫힘 연출 없이 바로 끈다.
    /// </summary>
    public void CloseAllPopups(bool immediate = false)
    {
        ExitSellMode();

        UIView[] popups =
        {
            diceResult, roomSetup, purchase, takeover, sell, islandEscape,
            cardDraw, destinationSelect, tileInfo, gameResult
        };
        foreach (var popup in popups)
        {
            if (popup == null) continue;
            if (immediate) popup.CloseImmediate();
            else popup.Close();
        }
    }

    #region 조회 도우미

    private GameManager Game => GameManager.Instance != null ? GameManager.Instance : null;
    private GameState State => Game != null ? Game.gameState : null;

    private PlayerState GetPlayer(long playerId)
        => State != null ? State.PlayerStates.Find(p => p.PlayerId == playerId) : null;

    private PlayerState CurrentPlayer => State != null ? GetPlayer(State.CurrentPlayerId) : null;

    private PropertyState GetProperty(int propertyId)
        => State != null ? State.PropertyStates.Find(p => p.PropertyId == propertyId) : null;

    // 플레이어 카드 번호(0~3). GameState.PlayerStates의 순서와 같다. 없으면 -1
    private int IndexOf(long playerId)
        => State != null ? State.PlayerStates.FindIndex(p => p.PlayerId == playerId) : -1;

    private int ColorIndexOf(long playerId) => Mathf.Max(0, IndexOf(playerId));

    private string NameOf(long playerId)
    {
        if (profiles.TryGetValue(playerId, out Profile profile) && !string.IsNullOrEmpty(profile.Name)) return profile.Name;
        int index = IndexOf(playerId);
        return index >= 0 ? $"플레이어 {index + 1}" : $"플레이어 {playerId}";
    }

    private Sprite PortraitOf(long playerId)
        => profiles.TryGetValue(playerId, out Profile profile) ? profile.Portrait : null;

    private static string TileName(int propertyId)
    {
        PropertyData data = PropertyManager.Instance.GetData(propertyId);
        return data != null ? data.CityName : $"땅 {propertyId}";
    }

    #endregion
}
