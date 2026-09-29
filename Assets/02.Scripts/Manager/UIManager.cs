using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 화면 UI 진입점. GameManager는 여기서 뷰를 꺼내 쓴다.
/// 예) UIManager.Instance.Purchase.Show(...), UIManager.Instance.GetPlayerInfo(0).SetMoney(...)
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
            // 굴리기 버튼은 기본적으로 잠겨 있고, 내 턴에 DiceRoll.Show(...)를 부르면 켜진다
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

    /// <summary>차례 표시와 카드 강조를 함께 바꾼다.</summary>
    public void SetTurn(int playerIndex, string playerName, Sprite portrait)
    {
        for (int i = 0; i < playerInfos.Length; i++)
            if (playerInfos[i] != null) playerInfos[i].SetTurn(i == playerIndex);
        if (currentTurn != null) currentTurn.Show(playerName, portrait, playerIndex);
    }

    // ───────────── 코인 이동 ─────────────

    private const int CoinCount = 7;
    private const float CoinBurstSeconds = 0.14f;   // 금화에서 흩어져 나오는 시간
    private const float CoinFlightSeconds = 0.55f;  // 기본 비행 시간 (코인마다 조금씩 다름)
    private const float CoinStagger = 0.045f;
    private const int CoinLayerSortingOrder = 95;   // HUD(0~50) 위, 선택 팝업(100~) 아래
    private RectTransform coinLayer;

    /// <summary>
    /// 코인이 from 카드에서 to 카드로 날아간다. 첫 코인이 도착할 때 onArrived를 부른다.
    /// 통행료 예) 내는 사람 ShowMoneyChange 즉시 → PlayCoinFlight(payer, receiver, 받는 사람 ShowMoneyChange)
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

    /// <summary>
    /// 선택형 팝업과 일시적인 표시를 모두 닫는다. HUD(플레이어 정보, 상단바)는 그대로 둔다.
    /// immediate면 닫힘 연출 없이 바로 끈다.
    /// </summary>
    public void CloseAllPopups(bool immediate = false)
    {
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
}
