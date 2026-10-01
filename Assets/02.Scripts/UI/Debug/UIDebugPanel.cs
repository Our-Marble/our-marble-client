using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// [임시] UI 확인용 테스트 버튼 창. 씬의 GameManager(GameState)를 읽어 UI를 채우고,
/// GameManager가 이미 UIManager를 직접 부르므로 이 창은 상태를 만들고(땅 주인, 현금 등) 창을 띄워 보는 일만 한다.
/// 보드(BoardManager)가 칸 선택을 연결하기 전에는, "보드 선택 시험"의 버튼이 보드 대신 칸 번호 목록을
/// UIManager.CheckTileValidForTravel / CheckTilesValidForSell에 넘겨 준다.
/// 에디터·개발 빌드에서만 그려진다. 확인이 끝나면 씬의 UIDebugPanel 오브젝트를 지우면 된다.
/// F1: 창 접기/펼치기
/// </summary>
public class UIDebugPanel : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static readonly string[] Names = { "나", "봇", "플레이어3", "플레이어4" };

    private int turnCount = 1;
    private int propertyCursor;
    private float speed = 1f;
    private bool expanded = true;
    private bool placed;
    private bool ready;
    private Rect windowRect = new Rect(16, 120, 460, 10);
    private Vector2 scroll;
    private string lastLog = "";

    private UIManager UI => UIManager.Instance;
    private GameManager Game => GameManager.Instance;
    private GameState State => Game != null ? Game.gameState : null;
    private List<PlayerState> Players => State.PlayerStates;

    private const int LocalPlayer = 0; // 이 화면의 플레이어 = PlayerStates[0] (playerId 123)

    private IEnumerator Start()
    {
        // GameManager.Start(StartGame: 초기 자금, 첫 턴)가 끝난 뒤에 읽는다
        yield return null;
        if (UI == null || Game == null) yield break;

        for (int i = 0; i < Players.Count; i++) UI.SetPlayerProfile(Players[i].PlayerId, Names[i]);
        UI.SetLocalPlayer(Players[LocalPlayer].PlayerId);
        UI.DiceRoll?.SetIslandTurns(0);
        ready = true;

        FillHud(animate: false);
    }

    private void OnDisable()
    {
        DOTween.timeScale = 1f;
    }

    private void OnGUI()
    {
        if (UI == null) return;

        var e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F1) { expanded = !expanded; e.Use(); }

        // 1080p 기준으로 글자 크기를 맞춘다
        float s = Mathf.Max(1f, Screen.height / 1080f * 1.25f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));

        // 처음에는 왼쪽 플레이어 카드 두 장 사이 빈 공간에 둔다
        float viewHeight = Screen.height / s;
        if (!placed) { windowRect.y = viewHeight * 0.27f; placed = true; }
        windowRect.height = expanded ? Mathf.Max(120, viewHeight * 0.53f) : 10;
        windowRect = GUILayout.Window(98231, windowRect, DrawWindow, expanded ? "UI 테스트 (F1 접기)" : "UI 테스트 (F1)");
    }

    private void DrawWindow(int id)
    {
        if (!expanded)
        {
            if (GUILayout.Button("펼치기")) expanded = true;
            GUI.DragWindow();
            return;
        }

        if (Game == null)
        {
            GUILayout.Label("씬에 GameManager가 없습니다.");
            GUI.DragWindow();
            return;
        }
        if (!ready)
        {
            GUILayout.Label("GameManager 준비 중...");
            GUI.DragWindow();
            return;
        }

        GUILayout.Label($"연출 속도 x{speed:0.00}");
        float next = GUILayout.HorizontalSlider(speed, 0.1f, 1f);
        if (!Mathf.Approximately(next, speed)) { speed = next; DOTween.timeScale = speed; }
        GUILayout.Space(6);

        scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

        Header($"HUD (GameState 기준, {Players.Count}명)");
        if (GUILayout.Button("GameState 다시 읽기")) { FillHud(animate: true); Log("GameState 다시 읽기"); }
        if (GUILayout.Button("다음 차례 (HandleTurnChanged)")) NextTurn();
        if (GUILayout.Button("통행료 주고받기 (HandleTollPaid)")) PayToll();
        if (GUILayout.Button("파산 켜기/끄기 (차례인 사람)")) ToggleBankrupt();

        Header("주사위");
        if (GUILayout.Button("주사위 굴리기 창 열기 (버튼 → GameManager.RollDice)")) UI.ShowRollDicePopup();

        DrawPropertySection();

        GUILayout.Space(4);
        if (GUILayout.Button("팝업 모두 닫기")) UI.CloseAllPopups();

        GUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(lastLog)) GUILayout.Label(lastLog);
        GUI.DragWindow();
    }

    // ───────────── 상태 읽기 ─────────────

    // 지금 차례인 플레이어의 번호(PlayerStates 인덱스)
    private int turnIndex
    {
        get
        {
            int i = Players.FindIndex(p => p.PlayerId == State.CurrentPlayerId);
            return i < 0 ? 0 : i;
        }
    }

    private bool IsLocalTurn => turnIndex == LocalPlayer;
    private long Cash(int index) => Players[index].Money;

    // ───────────── HUD ─────────────

    private void FillHud(bool animate)
    {
        UI.InitPlayers();
        UI.RefreshAllPlayers(animate);
        UI.TopBar?.SetRoom("테스트 방", "TEST01");
        UI.TopBar?.SetPlayerCount(Players.Count, 4);
        UI.ShowTurn(State.CurrentPlayerId);
    }

    // GameManager.HandleTurnChanged를 실제로 부른다. (다음 사람은 이 창이 정해서 넘긴다)
    private void NextTurn()
    {
        int next = turnIndex;
        for (int step = 0; step < Players.Count; step++)
        {
            next = (next + 1) % Players.Count;
            if (!Players[next].IsBankrupt) break;
        }
        if (next == 0) turnCount = Mathf.Min(turnCount + 1, 30);

        // 차례 표시, 주사위 창은 GameManager가 UIManager로 직접 띄운다
        Game.HandleTurnChanged(Players[next].PlayerId);
        Log($"{Names[next]} 차례");
    }

    // GameState의 돈을 바꾸고 EconomyManager로 알린다 (돈 변화 표시는 이 경로 하나만 쓴다)
    private void ChangeMoney(int index, long amount, string reason)
    {
        PlayerState player = Players[index];
        long before = player.Money;
        player.Money += amount;
        UI.SetNextMoneyReason(player.PlayerId, reason);
        EconomyManager.NotifyMoneyChanged(player.PlayerId, before, player.Money);
    }

    private void PayToll()
    {
        int payer = turnIndex;
        int receiver = NextPlayer(payer);
        const long toll = 2000;
        if (receiver == payer || Cash(payer) < toll) { Log("통행료를 낼 돈이 부족합니다"); return; }

        Game.HandleTollPaid(Players[payer].PlayerId, Players[receiver].PlayerId, toll); // GameState 갱신
        Log($"통행료 {toll:N0}: {Names[payer]} → {Names[receiver]}");
    }

    private void ToggleBankrupt()
    {
        PlayerState player = Players[turnIndex];
        if (!player.IsBankrupt)
        {
            Game.ProcessBankruptcy(player.PlayerId); // GameState: IsBankrupt = true
            UI.PlayBankruptEffect(player.PlayerId);
            Log($"{Names[turnIndex]} 파산");
        }
        else
        {
            player.IsBankrupt = false;
            UI.RefreshAllPlayers();
            Log($"{Names[turnIndex]} 파산 해제 (표시만 되돌립니다)");
        }
    }

    // ───────────── 땅 (PropertyManager 데이터 + GameState) ─────────────

    private void DrawPropertySection()
    {
        List<PropertyData> all = PropertyManager.Instance.GetAllDataByMapId(1);
        Header("땅 / 실제 팝업 (GameManager 함수로 이어짐)");
        if (all == null || all.Count == 0) { GUILayout.Label("PropertyManager에 땅 데이터가 없습니다."); return; }

        propertyCursor = Mathf.Clamp(propertyCursor, 0, all.Count - 1);
        PropertyData data = all[propertyCursor];
        PropertyState state = Game.gameState.PropertyStates.Find(p => p.PropertyId == data.Id);
        long me = Players[LocalPlayer].PlayerId, other = Players[NextPlayer(LocalPlayer)].PlayerId;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀")) propertyCursor = (propertyCursor + all.Count - 1) % all.Count;
        GUILayout.Label($"{data.CityName} ({data.Id})  {data.LandPrice:N0}", GUILayout.Width(180));
        if (GUILayout.Button("▶")) propertyCursor = (propertyCursor + 1) % all.Count;
        GUILayout.EndHorizontal();
        GUILayout.Label(state == null ? "상태 없음"
            : $"주인 {(state.OwnerId.HasValue ? Names[Players.FindIndex(p => p.PlayerId == state.OwnerId.Value)] : "없음")} / 단계 {state.BuildingLevel}");

        Row(("주인 없음", () => SetOwner(data.Id, null, 0)), ("내 땅", () => SetOwner(data.Id, me, 0)),
            ("봇 땅", () => SetOwner(data.Id, other, 0)));
        Row(("단계 +1", () => { if (state != null && state.BuildingLevel < BuildingLevel.Hotel) SetOwner(data.Id, state.OwnerId, (int)state.BuildingLevel + 1); }),
            ("내 돈 100,000", () => SetMyMoney(100000)), ("내 돈 100", () => SetMyMoney(100)));
        GUILayout.Label("아래 창의 버튼 → GameManager.Purchase/Build/Acquire 호출. 매각은 아래 '보드 선택 시험'에서");
        Row(("구매 창", () => UI.ShowPurchasePropertyPopup(me, data.Id, (int)PropertyManager.Instance.GetLandPrice(data.Id))),
            ("건설 창", () => UI.ShowBuildPopup(me, data.Id)));
        Row(("인수 창", () => UI.ShowAcquirePropertyPopup(me, data.Id)),
            ("타일 정보", () => UI.ShowTileInfoPopup(data.Id)));

        DrawBoardSelectSection(me, other);
    }

    // ───────────── 보드 선택 시험 (BoardManager 대신 칸 번호 목록을 넘긴다) ─────────────

    private int tileCursor;                       // 시험용으로 가리키는 칸 번호
    private readonly List<int> travelTiles = new(); // '보드에서 고른' 여행지 칸 번호들
    private readonly List<int> sellTiles = new();   // '보드에서 고른' 매각할 칸 번호들
    private static readonly long[] SellRequiredPresets = { 10000, 30000, 100000, 500000 };
    private int sellPresetIndex = 1;

    private void DrawBoardSelectSection(long me, long other)
    {
        Header("보드 선택 시험 (BoardManager 대신)");
        GUILayout.Label("보드 모드 전환(ChangeTo~)은 BoardManager 구현 전이라 주석 상태입니다.");

        int count = BoardManager.Instance.TileCount;
        if (count <= 0) { GUILayout.Label("보드 데이터가 없습니다."); return; }
        tileCursor = Mathf.Clamp(tileCursor, 0, count - 1);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀")) tileCursor = (tileCursor + count - 1) % count;
        GUILayout.Label($"{DescribeTile(tileCursor)}", GUILayout.Width(200));
        if (GUILayout.Button("▶")) tileCursor = (tileCursor + 1) % count;
        GUILayout.EndHorizontal();

        GUILayout.Label("<b>세계여행</b> (칸 종류와 상관없이 하나만 고르면 완료 버튼이 켜집니다)", new GUIStyle(GUI.skin.label) { richText = true });
        Row(("창 열기", () => { travelTiles.Clear(); UI.ShowChooseDestinationPopup(); }),
            ("창 닫기", () => UI.HideChooseDestinationPopup()));
        Row(("이 칸 선택/해제", () => { Toggle(travelTiles, tileCursor); UI.CheckTileValidForTravel(new List<int>(travelTiles)); }),
            ("모두 해제", () => { travelTiles.Clear(); UI.CheckTileValidForTravel(new List<int>()); }));
        GUILayout.Label($"고른 칸: {ListText(travelTiles)}");

        GUILayout.Label("<b>매각</b> (내 땅만 합산, 현금 + 매각가 합 ≥ 필요 금액이면 완료 버튼이 켜집니다)", new GUIStyle(GUI.skin.label) { richText = true });
        Row(($"필요 금액 {SellRequiredPresets[sellPresetIndex]:N0} (눌러서 변경)", () => sellPresetIndex = (sellPresetIndex + 1) % SellRequiredPresets.Length),
            ("창 열기", () => { sellTiles.Clear(); UI.ShowSellPropertiesPopup(me, other, SellRequiredPresets[sellPresetIndex]); }));
        Row(("이 칸 선택/해제", () => { Toggle(sellTiles, tileCursor); UI.CheckTilesValidForSell(new List<int>(sellTiles)); }),
            ("모두 해제", () => { sellTiles.Clear(); UI.CheckTilesValidForSell(new List<int>()); }));
        GUILayout.Label($"고른 칸: {ListText(sellTiles)}");

        GUILayout.Label("<b>둘러보기</b>", new GUIStyle(GUI.skin.label) { richText = true });
        if (GUILayout.Button("이 칸 클릭 (땅이면 정보창)")) UI.OnBoardTileClicked(tileCursor);
    }

    private static void Toggle(List<int> list, int value)
    {
        if (!list.Remove(value)) list.Add(value);
    }

    private static string ListText(List<int> list) => list.Count == 0 ? "없음" : string.Join(", ", list);

    // 칸 번호의 종류(땅이면 도시 이름)
    private static string DescribeTile(int index)
    {
        TileData tile = BoardManager.Instance.GetTileData(index);
        if (tile == null) return $"{index}번 (없음)";
        if (tile.Type != TileType.PROPERTY) return $"{index}번 {tile.Type}";
        PropertyData data = PropertyManager.Instance.GetData(tile.PropertyId);
        return $"{index}번 {(data != null ? data.CityName : "땅 " + tile.PropertyId)}";
    }

    private void SetOwner(int propertyId, long? owner, int level)
    {
        PropertyState state = Game.gameState.PropertyStates.Find(p => p.PropertyId == propertyId);
        if (state == null) return;
        state.OwnerId = owner;
        state.BuildingLevel = (BuildingLevel)level;
        UI.RefreshAllPlayers();
        Log($"땅 {propertyId}: 주인 {(owner.HasValue ? owner.Value.ToString() : "없음")}, 단계 {(BuildingLevel)level}");
    }

    private void SetMyMoney(long amount) => ChangeMoney(LocalPlayer, amount - Players[LocalPlayer].Money, "설정");

    // from 다음 차례의 (파산하지 않은) 플레이어
    private int NextPlayer(int from)
    {
        for (int step = 1; step <= Players.Count; step++)
        {
            int i = (from + step) % Players.Count;
            if (!Players[i].IsBankrupt && i != from) return i;
        }
        return (from + 1) % Players.Count;
    }

    // ───────────── GUI 헬퍼 ─────────────

    private static void Header(string text)
    {
        GUILayout.Space(8);
        GUILayout.Label($"<b>{text}</b>", new GUIStyle(GUI.skin.label) { richText = true });
    }

    private static void Row(params (string label, System.Action action)[] buttons)
    {
        GUILayout.BeginHorizontal();
        foreach (var (label, action) in buttons)
            if (GUILayout.Button(label)) action();
        GUILayout.EndHorizontal();
    }

    private void Log(string message)
    {
        lastLog = message;
        Debug.Log($"[UIDebugPanel] {message}");
    }
#endif
}
