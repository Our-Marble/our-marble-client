using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// [임시] UI 연출 확인용 테스트 버튼 창. 씬의 GameManager(GameState)를 그대로 읽어 UI를 채우고,
/// GameManager에 이미 있는 함수(HandleTurnChanged, HandleTollPaid, ProcessBankruptcy, RollDice)만 실제로 부른다.
/// GameManager는 아직 UIManager를 부르지 않으므로, 호출 뒤의 화면 갱신은 이 창이 UIManager로 대신한다.
/// 에디터·개발 빌드에서만 그려진다. 확인이 끝나면 씬의 UIDebugPanel 오브젝트를 지우면 된다.
/// F1: 창 접기/펼치기
/// </summary>
public class UIDebugPanel : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static readonly string[] Names = { "나", "봇", "플레이어3", "플레이어4" };

    private int turnCount = 1;
    private bool holdTurn = true; // 턴 고정: 팝업 흐름이 끝나도 같은 사람이 계속 (GetNextPlayerId가 아직 0을 돌려줘서)
    private int propertyCursor;
    private readonly Dictionary<long, long> moneySnapshot = new();
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
        UI.BuildLockReason = LapLockReason;
        SnapshotMoney();
        ready = true;

        FillHud(animate: false);
        // 시작은 내 차례이므로 주사위 버튼을 켠다. 다른 사람 차례에는 잠긴다
        if (IsLocalTurn) OpenDiceRoll();
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
        if (GUILayout.Button("주사위 굴리기 UI 열기 (버튼 → GameManager.RollDice)")) OpenDiceRoll();

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

    // GameManager.HandleTurnChanged를 실제로 부른다. (다음 사람 결정은 GetNextPlayerId가 아직 없어서 이 창이 한다)
    private void NextTurn()
    {
        int next = turnIndex;
        for (int step = 0; step < Players.Count; step++)
        {
            next = (next + 1) % Players.Count;
            if (!Players[next].IsBankrupt) break;
        }
        if (next == 0) turnCount = Mathf.Min(turnCount + 1, 30);

        Game.HandleTurnChanged(Players[next].PlayerId); // GameState 갱신 (차례, 턴 번호)
        UI.ShowTurn(Players[next].PlayerId);            // GameManager가 아직 UI를 부르지 않아 여기서 대신 호출

        // 내 차례에만 주사위 버튼이 켜진다 (ShowTurn이 남의 차례에는 잠근다)
        if (IsLocalTurn)
        {
            OpenDiceRoll();
            Log($"{Names[next]} 차례 (내 차례: 주사위 버튼 켜짐)");
        }
        else Log($"{Names[next]} 차례 (주사위 버튼 잠김)");
    }

    // GameState의 돈을 바꾸고 EconomyManager로 알린다 (돈 변화 표시는 이 경로 하나만 쓴다)
    private void ChangeMoney(int index, long amount, string reason)
    {
        PlayerState player = Players[index];
        long before = player.Money;
        player.Money += amount;
        moneySnapshot[player.PlayerId] = player.Money; // 아래 알림이 표시를 맡으므로 Update 감시가 또 띄우지 않게
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
        SnapshotMoney();                                                                // 연출이 표시를 맡는다
        UI.PlayTollEffect(Players[payer].PlayerId, Players[receiver].PlayerId, toll);   // 코인 이동 연출
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

    // ───────────── 주사위 ─────────────

    // 굴리기 버튼을 누르면: 구르기 시작 → 실제 GameManager.RollDice() 요청 → (서버 응답 대신) 잠시 뒤 결과 표시.
    // 지금 GameManager는 결과를 UI로 넘기지 않아서, 응답 역할을 이 창이 한다.
    private void OpenDiceRoll()
    {
        UI.DiceRoll.Show(power =>
        {
            UI.DiceResult.StartRolling();
            Game.RollDice();
            DOVirtual.DelayedCall(1f, ShowDice, ignoreTimeScale: false);
        }, 0);
    }

    private void ShowDice()
    {
        int a = Random.Range(1, 7);
        int b = Random.Range(1, 7);
        UI.ShowDiceResult(a, b);
        Log($"주사위 {a} + {b} = {a + b}");
    }

    private int lap = 1; // 현재 바퀴 (별 1개는 2바퀴, 2개는 3바퀴, 3개는 4바퀴부터)

    private void NextLap()
    {
        lap = Mathf.Min(lap + 1, 4);
        Log($"{lap}바퀴째");
    }

    // 별(건설) 단계는 바퀴 수로 잠근다: 건물 1바퀴, 별 1개 2바퀴, 별 2개 3바퀴, 별 3개 4바퀴부터
    private string LapLockReason(BuildingLevel level)
    {
        int unlockLap = (int)level + 1;
        return lap < unlockLap ? $"{unlockLap}바퀴부터" : null;
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
        if (GUILayout.Button($"바퀴 +1 (현재 {lap}바퀴째, 건설창 별 잠금)")) NextLap();
        GUILayout.Label("아래 팝업 버튼 → 누르면 GameManager.Purchase/Build/Acquire/Sell 호출");
        Row(("구매 창", () => UI.ShowPurchasePropertyPopup(me, data.Id, (int)PropertyManager.Instance.GetLandPrice(data.Id))),
            ("건설 창", () => UI.ShowBuildPopup(me, data.Id)));
        Row(("인수 창", () => UI.ShowAcquirePropertyPopup(me, data.Id)),
            ("매각 창 (통행료 30,000)", () => UI.ShowSellPropertiesPopup(me, other, 30000)));
        if (GUILayout.Button("타일 정보")) UI.ShowTileInfoPopup(data.Id);
        holdTurn = GUILayout.Toggle(holdTurn, "턴 고정 (팝업이 끝나도 차례를 넘기지 않음)");
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

    private void SnapshotMoney()
    {
        foreach (PlayerState p in Players) moneySnapshot[p.PlayerId] = p.Money;
    }

    // GameManager가 UI를 아직 부르지 않으므로, 구매·건설·인수·매각으로 돈이 바뀌면 여기서 감지해 화면에 반영한다
    private void Update()
    {
        if (!ready || Game == null) return;

        if (holdTurn)
            typeof(GameManager).GetField("isDouble", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(Game, true);

        foreach (PlayerState p in Players)
        {
            if (!moneySnapshot.TryGetValue(p.PlayerId, out long before)) before = p.Money;
            if (before == p.Money) continue;
            moneySnapshot[p.PlayerId] = p.Money;
            UI.PlayMoneyChange(p.PlayerId, p.Money - before, p.Money > before ? "입금" : "출금");
        }
    }

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
