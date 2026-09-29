using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// [임시] UI 연출 확인용 테스트 버튼 창. 플레이 중에 각 UI를 예시 데이터로 띄운다.
/// 에디터·개발 빌드에서만 그려진다. 확인이 끝나면 씬의 UIDebugPanel 오브젝트를 지우면 된다.
/// F1: 창 접기/펼치기
/// </summary>
public class UIDebugPanel : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static readonly string[] Names = { "마블왕", "주사위요정", "건물부자", "여행가" };

    private readonly long[] cash = { 3000000, 3000000, 3000000, 3000000 };
    private readonly long[] total = { 5000000, 5000000, 5000000, 5000000 };
    private readonly bool[] bankrupt = new bool[4];
    private int turnIndex;
    private int playerCount = 4;
    private int turnCount = 1;
    private int islandTurns;
    private long sellSelected;
    private float speed = 1f;
    private bool expanded = true;
    private bool placed;
    private Rect windowRect = new Rect(16, 120, 300, 10);
    private Vector2 scroll;
    private string lastLog = "";

    private UIManager UI => UIManager.Instance;

    private const int LocalPlayer = 0; // 이 화면의 플레이어 = 1번(마블왕)

    private void Start()
    {
        if (UI == null) return;
        UI.SetPlayerCount(playerCount);
        FillHud(animate: false);
        UI.DiceRoll?.SetIslandTurns(0);
        // 상단바 "방 설정" 버튼으로 바로 열어도 팀을 바꿀 수 있게 방 정보를 미리 채워 둔다
        FillRoomSetup();
        // 시작은 내 차례(1번)이므로 주사위 버튼을 켠다. 다른 사람 차례에는 잠긴다
        if (turnIndex == LocalPlayer) OpenDiceRoll();
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

        GUILayout.Label($"연출 속도 x{speed:0.00}");
        float next = GUILayout.HorizontalSlider(speed, 0.1f, 1f);
        if (!Mathf.Approximately(next, speed)) { speed = next; DOTween.timeScale = speed; }
        GUILayout.Space(6);

        scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

        Header("HUD");
        GUILayout.BeginHorizontal();
        GUILayout.Label($"플레이어 {playerCount}명", GUILayout.Width(110));
        if (GUILayout.Button("−")) SetPlayerCount(playerCount - 1);
        if (GUILayout.Button("+")) SetPlayerCount(playerCount + 1);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("예시 데이터로 초기화")) ResetHud();
        if (GUILayout.Button("다음 차례")) NextTurn();
        Row(("돈 +2,000,000 (월급)", () => ChangeMoney(turnIndex, 2000000, "월급")),
            ("돈 -1,200,000 (통행료)", () => ChangeMoney(turnIndex, -1200000, "통행료")));
        if (GUILayout.Button("통행료 주고받기 (코인 이동)")) PayToll();
        if (GUILayout.Button("자산 섞기 (등수 뒤집기)")) ShuffleMoney();
        if (GUILayout.Button("파산 켜기/끄기 (차례인 사람)")) ToggleBankrupt();

        Header("주사위");
        Row(("주사위 굴리기 UI 열기", OpenDiceRoll), ("주사위 굴리기 (더블 나오게)", () => OpenDiceRoll(forceDouble: true)));
        Row(("결과 (랜덤)", () => ShowDice(false)), ("결과 (더블)", () => ShowDice(true)));
        if (GUILayout.Button($"무인도에 가두기 (현재 남은 턴 {islandTurns})")) TrapOnIsland();

        Header("팝업");
        if (GUILayout.Button("방 설정")) ShowRoomSetup();
        Row(("구매: 빈 땅", () => ShowPurchase(-1)), ("구매: 내 땅 (★1 보유)", () => ShowPurchase(1)));
        if (GUILayout.Button($"바퀴 +1 (현재 {lap}바퀴째)")) NextLap();
        if (GUILayout.Button("인수")) ShowTakeover();
        Row(("매각 열기", ShowSell), ("매각 +900,000 선택", AddSellSelection));
        if (GUILayout.Button("무인도 탈출")) ShowIslandEscape();
        Row(("카드 (보관)", () => ShowCard(true)), ("카드 (즉시)", () => ShowCard(false)));
        Row(("목적지 선택", () => UI.DestinationSelect.Show()), ("목적지 닫기", () => UI.DestinationSelect.Close()));
        if (GUILayout.Button("타일 정보")) ShowTileInfo();
        if (GUILayout.Button("게임 결과")) ShowGameResult();
        GUILayout.Space(4);
        if (GUILayout.Button("팝업 모두 닫기")) UI.CloseAllPopups();

        GUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(lastLog)) GUILayout.Label(lastLog);
        GUI.DragWindow();
    }

    // ───────────── HUD ─────────────

    private void FillHud(bool animate)
    {
        for (int i = 0; i < 4; i++)
        {
            var p = UI.GetPlayerInfo(i);
            if (p == null) continue;
            p.SetProfile(Names[i], null, i);
            p.SetMoney(cash[i], total[i], animate);
            p.SetBankrupt(bankrupt[i]);
        }
        UpdateRanks();
        UI.TopBar?.SetRoom("테스트 방", "TEST01");
        UI.TopBar?.SetPlayerCount(playerCount, 4);
        UI.TopBar?.SetTurn(turnCount, 30);
        UI.SetTurn(turnIndex, Names[turnIndex], null);
    }

    private void ResetHud()
    {
        for (int i = 0; i < 4; i++) { cash[i] = 3000000; total[i] = 5000000; bankrupt[i] = false; }
        turnIndex = 0; turnCount = 1;
        FillHud(animate: true);
        Log("HUD 초기화");
    }

    private void NextTurn()
    {
        for (int step = 0; step < playerCount; step++)
        {
            turnIndex = (turnIndex + 1) % playerCount;
            if (!bankrupt[turnIndex]) break;
        }
        if (turnIndex == 0) turnCount = Mathf.Min(turnCount + 1, 30);
        UI.TopBar?.SetTurn(turnCount, 30);
        UI.SetTurn(turnIndex, Names[turnIndex], null);

        // 내 차례에만 주사위 버튼이 켜진다
        if (turnIndex == LocalPlayer)
        {
            OpenDiceRoll();
            Log($"{Names[turnIndex]} 차례 (내 차례: 주사위 버튼 켜짐)");
        }
        else
        {
            UI.DiceRoll.SetInteractable(false);
            Log($"{Names[turnIndex]} 차례 (주사위 버튼 잠김)");
        }
    }

    private void ChangeMoney(int index, long amount, string reason)
    {
        cash[index] += amount;
        total[index] += amount;
        var p = UI.GetPlayerInfo(index);
        p.SetMoney(cash[index], total[index]);
        p.ShowMoneyChange(reason, amount);
        UpdateRanks();
    }

    private void PayToll()
    {
        int payer = turnIndex;
        int receiver = NextPlayer(turnIndex);
        ChangeMoney(payer, -1200000, "통행료");
        // 코인이 도착하는 순간 받는 쪽 금액이 바뀐다
        UI.PlayCoinFlight(payer, receiver, () => ChangeMoney(receiver, 1200000, "통행료 수입"));
    }

    private void ShuffleMoney()
    {
        for (int i = 0; i < playerCount; i++)
        {
            long delta = Random.Range(-20, 31) * 100000L;
            cash[i] = System.Math.Max(0, cash[i] + delta);
            total[i] = System.Math.Max(cash[i], total[i] + delta);
            UI.GetPlayerInfo(i).SetMoney(cash[i], total[i]);
        }
        UpdateRanks();
        Log("자산을 섞어 등수 갱신");
    }

    private void ToggleBankrupt()
    {
        bankrupt[turnIndex] = !bankrupt[turnIndex];
        UI.GetPlayerInfo(turnIndex).SetBankrupt(bankrupt[turnIndex]);
    }

    private void UpdateRanks()
    {
        var order = ActivePlayers();
        order.Sort((a, b) => total[b].CompareTo(total[a]));
        for (int r = 0; r < order.Count; r++) UI.GetPlayerInfo(order[r])?.SetRank(r + 1);
    }

    // ───────────── 주사위 ─────────────

    private void OpenDiceRoll() => OpenDiceRoll(forceDouble: false);

    private void OpenDiceRoll(bool forceDouble)
    {
        UI.DiceRoll.Show(power =>
        {
            Log($"떼는 순간 파워 {power:0.00}");
            // 파워와 상관없이 랜덤 결과를 보여준다 (연출 확인용)
            ShowDice(forceDouble);
        }, islandTurns);
    }

    private void ShowDice(bool forceDouble)
    {
        int a = Random.Range(1, 7);
        int b = forceDouble ? a : Random.Range(1, 7);
        // 결과가 다 나온 뒤 무인도 판정: 더블이면 탈출, 아니면 남은 턴 -1
        UI.DiceResult.Show(a, b, () =>
        {
            bool wasTrapped = UI.DiceRoll.IslandTurnsRemaining > 0;
            UI.DiceRoll.ResolveIslandRoll(a == b);
            islandTurns = UI.DiceRoll.IslandTurnsRemaining;
            if (wasTrapped) Log(a == b ? "더블! 무인도 탈출" : $"무인도 남은 턴 {islandTurns}");
        });
        Log($"주사위 {a} + {b} = {a + b}");
    }

    // 이 화면의 플레이어를 무인도에 2턴 가두고 주사위 UI를 연다
    private void TrapOnIsland()
    {
        islandTurns = 2;
        OpenDiceRoll();
        Log("무인도에 갇힘 (2턴). 굴릴 때마다 줄고, 더블이면 탈출");
    }

    // ───────────── 팝업 ─────────────

    private void ShowRoomSetup()
    {
        FillRoomSetup();
        UI.RoomSetup.Show(() => Log("이전 맵"), () => Log("다음 맵"), () => Log("레드팀으로 이동"), () => Log("블루팀으로 이동"), () => Log("게임 시작"));
    }

    // 방 정보만 채우고 열지는 않는다 (상단바 버튼으로 열어도 같은 내용이 보이게)
    private void FillRoomSetup()
    {
        var v = UI.RoomSetup;
        if (v == null) return;
        v.SetMap("클래식 월드", null);
        v.SetMode(true);
        v.SetLocalPlayer(LocalPlayer);
        // 인원만큼 채우고 나머지는 빈 자리. 팀은 번갈아 레드/블루
        for (int i = 0; i < 4; i++)
        {
            if (i >= playerCount) { v.SetSlot(i, new RoomSetupView.Slot { IsEmpty = true }); continue; }
            v.SetSlot(i, new RoomSetupView.Slot
            {
                Name = Names[i], ColorIndex = i, IsHost = i == 0, IsReady = i % 2 == 1,
                Team = i % 2 == 0 ? RoomSetupView.Team.Red : RoomSetupView.Team.Blue,
            });
        }
        v.SetPlayerCount(playerCount, 4);
        v.SetTeamCounts(0, 0, Mathf.Max(1, (playerCount + 1) / 2));
        v.RecountTeams();
        v.SetStartInteractable(false);
    }

    // 테스트용 가격표 (누적 금액): 건물, 별 1~3개
    private static readonly long[] StageCost = { 500000, 1100000, 1900000, 3000000 };
    private static readonly long[] StageToll = { 200000, 600000, 1200000, 2000000 };
    private int lap = 1; // 현재 바퀴 (별 1개는 2바퀴, 2개는 3바퀴, 3개는 4바퀴부터)

    private void NextLap()
    {
        lap = Mathf.Min(lap + 1, 4);
        Log($"{lap}바퀴째");
    }

    /// <summary>ownedLevel: 이미 가진 단계(-1이면 빈 땅). 보유 단계는 "보유 중", 바퀴가 모자란 별은 "N바퀴부터".</summary>
    private void ShowPurchase(int ownedLevel)
    {
        var options = new List<PurchasePopupView.Option>();
        for (int level = 0; level < 4; level++)
        {
            bool owned = level <= ownedLevel;
            int unlockLap = level + 1; // 건물 1바퀴, 별 1개 2바퀴 ...
            // 이미 가진 만큼은 빼고 차액만 낸다
            long paid = ownedLevel >= 0 ? StageCost[ownedLevel] : 0;
            options.Add(new PurchasePopupView.Option
            {
                TargetLevel = (BuildingLevel)level,
                Cost = System.Math.Max(0, StageCost[level] - paid),
                Toll = StageToll[level],
                Locked = owned || lap < unlockLap,
                LockReason = owned ? "보유 중" : $"{unlockLap}바퀴부터",
            });
        }
        UI.Purchase.Show("도쿄", null, options, cash[turnIndex],
            level =>
            {
                long cost = options[(int)level].Cost;
                ChangeMoney(turnIndex, -cost, "구매");
                Log(level == BuildingLevel.Land ? "건물 구매" : $"별 {(int)level}개로 확장");
            },
            () => Log("구매 취소"));
    }

    private void ShowTakeover()
    {
        int owner = NextPlayer(turnIndex);
        UI.Takeover.Show("뉴욕", null, BuildingLevel.Building, Names[owner], owner,
            2800000, cash[turnIndex],
            () => { ChangeMoney(turnIndex, -2800000, "인수"); Log("인수"); },
            () => Log("인수 취소"));
    }

    private void ShowSell()
    {
        sellSelected = 0;
        UI.Sell.Show(3700000, 800000, true,
            () => { sellSelected = 3000000; UI.Sell.SetSelectedTotal(sellSelected); Log("자동 선택"); },
            () => { UI.Sell.SetCash(2800000, sellSelected); UI.Sell.SetLoanAvailable(false); Log("은행 대출"); },
            () => { UI.Sell.Close(); Log("파산 신청"); },
            () => Log("매각 완료"));
    }

    private void AddSellSelection()
    {
        if (!UI.Sell.IsOpen) ShowSell();
        sellSelected += 900000;
        UI.Sell.SetSelectedTotal(sellSelected);
        Log($"선택 합계 {UIPalette.Money(sellSelected)}");
    }

    private void ShowIslandEscape()
    {
        UI.IslandEscape.Show(Mathf.Max(1, islandTurns), 500000, cash[turnIndex], 1,
            () => { Log("주사위 더블 시도"); OpenDiceRoll(); },
            () => { ChangeMoney(turnIndex, -500000, "탈출 비용"); Log("비용 지불"); },
            () => Log("탈출권 사용"));
    }

    private void ShowCard(bool keepable)
    {
        if (keepable) UI.CardDraw.Show("무인도 탈출권", "무인도에 갇혔을 때 사용하면\n바로 탈출할 수 있어요", null, true, () => Log("카드 보관"));
        else UI.CardDraw.Show("반액 세일", "다음에 도착한 땅을\n절반 가격에 살 수 있어요", null, false, () => Log("카드 확인"));
    }

    private void ShowTileInfo()
    {
        UI.TileInfo.Show("파리", null, Names[1], 1, BuildingLevel.Villa, new long[] { 200000, 600000, 1200000, 2000000 });
    }

    private void ShowGameResult()
    {
        var order = ActivePlayers();
        order.Sort((a, b) => total[b].CompareTo(total[a]));
        var entries = new List<GameResultView.Entry>();
        for (int r = 0; r < order.Count; r++)
        {
            int i = order[r];
            entries.Add(new GameResultView.Entry
            {
                Name = Names[i], ColorIndex = i, FinalAsset = bankrupt[i] ? -Mathf.Abs((int)(total[i] / 4)) : total[i],
                IsWinner = r == 0, IsBankrupt = bankrupt[i]
            });
        }
        UI.GameResult.Show(entries, () => Log("결과 닫기"));
    }

    // ───────────── 인원 ─────────────

    /// <summary>테스트 인원(2~4명)을 바꾼다. 카드·차례·등수·방 설정·결과가 모두 이 인원 기준으로 바뀐다.</summary>
    private void SetPlayerCount(int count)
    {
        count = Mathf.Clamp(count, 2, 4);
        if (count == playerCount) return;
        playerCount = count;
        if (turnIndex >= playerCount) turnIndex = 0;

        UI.CloseAllPopups();
        UI.SetPlayerCount(playerCount);
        FillHud(animate: false);
        FillRoomSetup();
        if (turnIndex == LocalPlayer) OpenDiceRoll();
        else UI.DiceRoll.SetInteractable(false);
        Log($"플레이어 {playerCount}명");
    }

    private List<int> ActivePlayers()
    {
        var list = new List<int>();
        for (int i = 0; i < playerCount; i++) list.Add(i);
        return list;
    }

    // from 다음 차례의 (파산하지 않은) 플레이어
    private int NextPlayer(int from)
    {
        for (int step = 1; step <= playerCount; step++)
        {
            int i = (from + step) % playerCount;
            if (!bankrupt[i] && i != from) return i;
        }
        return (from + 1) % playerCount;
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
