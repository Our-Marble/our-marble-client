using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIPalette;

/// <summary>
/// 로비 씬 매니저(임시). 서버 없이 가짜 방 데이터로 동작한다.
/// 방 목록(필터, 상세, 새로고침), 방 만들기, 방 코드/비밀번호 입장, 설정, 접속자 목록, 연결 상태를 맡고,
/// 방에 들어오면 같은 씬 안에서 방 설정 화면(RoomSetupView)을 열어 준비/팀/모드/맵/최대 인원을 관리한다.
/// 방장이 시작하면 게임 씬으로 넘어가고, 게임이 끝나면 같은 방으로 돌아와 방 설정을 다시 연다. (씬 이동은 SceneFlow가 맡는다)
/// 웹소켓 연동 전에 화면 흐름을 확인하는 용도라서, 연동할 때는 rooms 데이터를 서버 메시지로 바꾸면 된다.
/// UI_LobbyScene의 "UI" 루트를 찾아 이름으로 연결한다.
/// 테스트용 방 코드는 SeedRooms에 있다.
/// </summary>
public class LobbyManager_temp : MonoBehaviour
{
    // ───────────── 데이터 ─────────────

    private enum RoomState { Waiting, Playing }

    private class PlayerData
    {
        public string Name;
        public int ColorIndex;
        public bool IsHost;
        public bool IsReady; // 화면에는 안 보이고, 가짜 게임 시작 판정에만 쓴다
    }

    private class RoomData
    {
        public int Id;
        public string Name;
        public string Code;
        public string Map;
        public bool IsTeam;
        public string Password; // 목업에서만 들고 있다. 실제로는 서버만 알고, 클라이언트에는 "있다/없다"만 내려간다
        public bool HasPassword => !string.IsNullOrEmpty(Password);
        public int MaxPlayers = 4;
        public RoomState State;
        public int Turn;
        public int MaxTurn = 30;
        public readonly List<PlayerData> Players = new List<PlayerData>();

        public PlayerData Host => Players.FirstOrDefault(p => p.IsHost);
        public bool IsFull => Players.Count >= MaxPlayers;
    }

    private const string CodeLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly string[] Maps = { "클래식 월드", "도시 탐험", "해변 마을" };
    // 가짜 플레이어 이름 풀. 서버에 같은 이름이 둘 보이지 않게 PickFreeName으로만 꺼내 쓴다
    private static readonly string[] NamePool =
    {
        "주사위요정", "건물부자", "여행가", "땅부자", "황금열쇠", "코인왕", "보드마스터", "행운아", "달려라",
        "새벽별", "루돌프", "별똥별", "다람쥐", "구름", "호랑이", "펭귄", "고래", "토끼", "여우",
        "새싹", "도토리", "푸딩", "머핀", "솜사탕", "별빛", "하늘", "나무늘보", "바다", "초코",
        "단풍", "은하수", "토마토", "감자", "라떼", "마카롱", "곰돌이", "햇살", "눈사람", "모래성",
        "아침이슬", "보름달", "해바라기", "파도", "오로라", "꿀벌", "복숭아", "유성우", "달팽이", "뭉게구름",
    };

    private static readonly Color RowStroke = new Color32(220, 225, 236, 255);

    // 방 행 배경: 개인전은 민트, 팀전은 따뜻한 색. 선택하면 테두리가 두껍고 진해진다
    private static readonly Color SoloRowBg = new Color32(240, 250, 245, 255);
    private static readonly Color SoloRowStroke = new Color32(196, 232, 214, 255);
    private static readonly Color SoloRowStrokeOn = new Color32(92, 201, 154, 255);
    private static readonly Color TeamRowBg = new Color32(255, 247, 232, 255);
    private static readonly Color TeamRowStroke = new Color32(245, 224, 178, 255);
    private static readonly Color TeamRowStrokeOn = new Color32(242, 181, 68, 255);
    private static readonly Color DotEmpty = new Color32(226, 229, 238, 255);

    [Header("연결")]
    [SerializeField] private Transform uiRoot;

    [Tooltip("선택한 방 행의 두꺼운 테두리 (UI_RoundedStroke_Thick).")]
    [SerializeField] private Sprite selectedStrokeSprite;

    [Tooltip("맵 초상화. Maps 배열과 같은 순서(클래식 월드, 도시 탐험, 해변 마을).")]
    [SerializeField] private Sprite[] mapSprites = new Sprite[3];

    [Header("테스트 옵션")]
    [Tooltip("켜면 다른 방의 인원·게임 진행이 몇 초마다 저절로 바뀐다 (웹소켓 푸시 흉내).")]
    [SerializeField] private bool simulateLiveUpdates = true;
    [SerializeField] private float simulateInterval = 2.5f;
    [Tooltip("입장·방 만들기·방 목록 새로고침·게임 시작 요청에 서버가 응답하기까지 걸리는 시간 흉내(초).")]
    [SerializeField] private float serverDelay = 0.6f;

    // 실행 중 F2를 누르면 연결 상태가 연결됨 → 재연결 중 → 끊김 순으로 바뀐다 (화면 확인용)
    private enum ConnectionState { Connected, Reconnecting, Disconnected }
    private ConnectionState connection = ConnectionState.Connected;

    private readonly List<RoomData> rooms = new List<RoomData>();
    private readonly Dictionary<RoomData, RowView> rows = new Dictionary<RoomData, RowView>();
    private readonly PlayerData me = new PlayerData { Name = "마블왕" };
    private RoomData selected;
    private int filter; // 0 전체, 1 개인전, 2 팀전
    private int nextRoomId = 1;

    // ───────────── UI 참조 ─────────────

    private class RowView
    {
        public GameObject Go;
        public Button Row, Join;
        public GameObject Lock;
        public Image Bg, Stroke, Thumb, ThumbStroke, ModeChip, StatusChip, JoinImage;
        public TMP_Text Number, Name, Info, ModeText, StatusText, Count, JoinLabel;
        public Image[] Dots;
    }

    private class SlotView
    {
        public GameObject Go;
        public Image Bg, Stroke, Portrait, Ring;
        public GameObject Host;
        public TMP_Text Nick;
    }

    // 상단바 / 방 목록 위 버튼
    private Button settingsButton, exitButton, refreshButton;
    private bool refreshing; // 방 목록 새로고침 요청 중이면 true. 중복 요청을 막는다

    // 방 설정 화면 (방에 들어와 있는 동안 쓴다)
    private ExitConfirmView exitConfirm;
    private RoomSetupView roomSetup;
    private Coroutine mockHostStart; // 목업: 내가 준비를 마친 뒤 방장이 잠시 후 게임을 시작하는 코루틴

    // 방 목록
    private Toggle[] filterToggles;
    private Toggle joinableOnlyToggle;
    private TMP_Text joinableOnlyLabel;
    private RectTransform rowContent;
    private GameObject rowTemplate;
    private Sprite normalStrokeSprite;

    // 오른쪽 열
    private GameObject profileCard, playerListCard, detailCard;
    private TMP_Text recordText, marbleText;
    private TMP_Text playerCountText;
    private RectTransform playerContent;
    private GameObject playerRowTemplate;
    private readonly List<PlayerRowView> playerRows = new List<PlayerRowView>();

    private readonly List<string> idlers = new List<string>(); // 방에 들어가지 않고 로비에만 있는 사람들의 이름
    private Button quickStartButton, createRoomButton, joinByCodeButton;
    private GameObject detailLockChip;
    private TMP_Text detailTitle, detailModeText, detailStatusText, detailMap, detailCount, detailTurn;
    private Image detailModeChip, detailStatusChip;
    private Button detailClose, detailJoin;
    private Image detailJoinImage;
    private TMP_Text detailJoinLabel;
    private GameObject detailProgress;
    private RectTransform gaugeFill;
    private readonly SlotView[] slots = new SlotView[4];

    // 방 만들기 팝업
    private PopupView createPopup;
    private TMP_InputField nameInput, passwordInput;
    private TMP_Text mapNameText;
    private Image mapPreviewImage;
    private Button prevMapButton, nextMapButton, createCancel, createConfirm, createClose;
    private Toggle[] modeToggles, maxToggles;
    private int mapIndex;

    // 코드 입장 팝업
    private PopupView joinPopup;
    private TMP_InputField codeInput;
    private Button joinCancel, joinConfirm, joinClose;
    private Image joinConfirmImage;

    // 설정 팝업
    private SettingsView settingsView;

    // 연결 상태 / 빈 화면 / 알림 메시지(토스트) / 로딩 화면
    private Image connectionDot;
    private TMP_Text connectionText;
    private CanvasGroup listGroup, sideGroup;
    private GameObject emptyState;
    private ToastView toast;
    private LoadingView loading;

    // 비밀번호 입력 팝업
    private const int MaxPasswordFails = 5;
    private const float PasswordLockSeconds = 10f;
    private PopupView passwordPopup;
    private TMP_Text pwRoomText;
    private TMP_InputField pwInput;
    private Button pwCancel, pwConfirm, pwClose;
    private Image pwConfirmImage;
    private RoomData pendingRoom;
    private readonly Dictionary<int, int> pwFails = new Dictionary<int, int>();
    private readonly Dictionary<int, float> pwLockUntil = new Dictionary<int, float>();

    // 입장·방 만들기·게임 시작 요청 처리 중이면 true. 그동안 중복 요청과 다른 입력을 막는다
    private bool entering;

    // ───────────── 초기화 ─────────────

    private void Reset()
    {
        var ui = GameObject.Find("UI");
        if (ui != null) uiRoot = ui.transform;
    }

    private void Awake()
    {
        if (uiRoot == null) Reset();
        if (uiRoot == null) { Debug.LogError("[Lobby] 'UI' 루트를 찾을 수 없습니다.", this); enabled = false; return; }

        // 로비에 있다는 건 보통 어느 방에도 들어가 있지 않다는 뜻이다. 게임을 마치고 같은 방으로 돌아온 경우만 방을 유지한다.
        if (!SceneFlow.IsReturningToRoom) CurrentRoom = null;

        Bind();
        WireEvents();
        SeedRooms();
        SeedIdlers();
        RefreshProfile();
        ShowProfile(false);
        SetConnection(ConnectionState.Connected, false);
        SyncAll();
    }

    private void Start()
    {
        if (simulateLiveUpdates) StartCoroutine(SimulateRoutine());

        // 게임이 끝나 같은 방으로 돌아왔다면 방 설정을 다시 띄운다
        if (SceneFlow.ConsumeReopenRoomSetup() && CurrentRoom != null)
        {
            // 내 준비는 풀린다(방장은 해당 없음). 다른 사람의 준비는 서버가 알려줄 일이라 그대로 둔다.
            var mine = CurrentRoom.Players[CurrentRoom.LocalIndex];
            mine.IsReady = mine.IsHost;
            OpenRoomSetup(CurrentRoom);
        }
    }

    private static T Get<T>(Transform root, string path) where T : Component
    {
        var t = root.Find(path);
        if (t == null) { Debug.LogWarning($"[Lobby] '{root.name}/{path}' 없음"); return null; }
        var c = t.GetComponent<T>();
        if (c == null) Debug.LogWarning($"[Lobby] '{root.name}/{path}'에 {typeof(T).Name} 없음");
        return c;
    }

    private void Bind()
    {
        var lobby = uiRoot.Find("Canvas_Lobby");

        settingsButton = Get<Button>(lobby, "Header/Buttons/SettingsButton");
        exitButton = Get<Button>(lobby, "Header/Buttons/ExitButton");
        refreshButton = Get<Button>(lobby, "RoomListPanel/RefreshButton");
        connectionDot = Get<Image>(lobby, "Header/Brand/ConnectionChip/Dot");
        connectionText = Get<TMP_Text>(lobby, "Header/Brand/ConnectionChip/ConnectionText");
        listGroup = UIFx.EnsureGroup(lobby.Find("RoomListPanel").gameObject);
        sideGroup = UIFx.EnsureGroup(lobby.Find("SidePanel").gameObject);
        emptyState = lobby.Find("RoomListPanel/EmptyState").gameObject;

        toast = uiRoot.Find("Canvas_Toast").GetComponent<ToastView>();
        loading = uiRoot.Find("Canvas_Loading").GetComponent<LoadingView>();

        settingsView = uiRoot.Find("Canvas_Settings").GetComponent<SettingsView>();
        var exitCanvas = uiRoot.Find("Canvas_ExitConfirm");
        if (exitCanvas != null) exitConfirm = exitCanvas.GetComponent<ExitConfirmView>();
        var roomSetupCanvas = uiRoot.Find("Canvas_RoomSetup");
        if (roomSetupCanvas != null) roomSetup = roomSetupCanvas.GetComponent<RoomSetupView>();

        var list = lobby.Find("RoomListPanel");
        filterToggles = list.Find("ModeFilter").GetComponentsInChildren<Toggle>(true);
        joinableOnlyToggle = Get<Toggle>(list, "JoinableOnlyToggle");
        joinableOnlyLabel = Get<TMP_Text>(list, "JoinableOnlyToggle/Label");
        rowContent = (RectTransform)list.Find("RoomScroll/Viewport/Content");

        // 첫 행을 복제용 틀로 쓰고 나머지 샘플 행은 치운다
        rowTemplate = rowContent.Find("RoomRow_1").gameObject;
        foreach (Transform child in rowContent.Cast<Transform>().ToList())
        {
            if (child.gameObject == rowTemplate) continue;
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        rowTemplate.SetActive(false);
        normalStrokeSprite = rowTemplate.transform.Find("Stroke").GetComponent<Image>().sprite;

        var side = lobby.Find("SidePanel");
        profileCard = side.Find("ProfileCard").gameObject;
        recordText = Get<TMP_Text>(side, "ProfileCard/RecordCard/RecordText");
        marbleText = Get<TMP_Text>(side, "ProfileCard/MarbleChip/AmountText");
        playerListCard = side.Find("PlayerListCard").gameObject;
        playerCountText = Get<TMP_Text>(side, "PlayerListCard/CountChip/CountText");
        playerContent = (RectTransform)side.Find("PlayerListCard/PlayerScroll/Viewport/Content");
        // 첫 행을 복제용 틀로 쓴다
        playerRowTemplate = playerContent.Find("PlayerRow_1").gameObject;
        playerRowTemplate.SetActive(false);
        quickStartButton = Get<Button>(side, "QuickStartButton");
        createRoomButton = Get<Button>(side, "CreateRoomButton");
        joinByCodeButton = Get<Button>(side, "JoinByCodeButton");

        // 방 상세 카드
        detailCard = side.Find("RoomDetailCard").gameObject;
        var d = detailCard.transform;
        detailTitle = Get<TMP_Text>(d, "TitleText");
        detailClose = Get<Button>(d, "CloseButton");
        detailLockChip = d.Find("ChipRow/LockChip").gameObject;
        detailModeChip = Get<Image>(d, "ChipRow/ModeChip");
        detailModeText = Get<TMP_Text>(d, "ChipRow/ModeChip/Text");
        detailStatusChip = Get<Image>(d, "ChipRow/StatusChip");
        detailStatusText = Get<TMP_Text>(d, "ChipRow/StatusChip/Text");
        detailMap = Get<TMP_Text>(d, "MapInfo/ValueText");
        detailCount = Get<TMP_Text>(d, "PlayerCountText");
        detailProgress = d.Find("GameProgress").gameObject;
        detailTurn = Get<TMP_Text>(d, "GameProgress/TurnText");
        gaugeFill = (RectTransform)d.Find("GameProgress/GaugeTrack/GaugeFill");
        detailJoin = Get<Button>(d, "JoinButton");
        detailJoinImage = detailJoin.GetComponent<Image>();
        detailJoinLabel = Get<TMP_Text>(d, "JoinButton/Label");
        for (int i = 0; i < slots.Length; i++)
        {
            var s = d.Find($"PlayerSlots/PlayerSlot_{i + 1}");
            slots[i] = new SlotView
            {
                Go = s.gameObject,
                Bg = s.GetComponent<Image>(),
                Stroke = Get<Image>(s, "Stroke"),
                Portrait = Get<Image>(s, "Portrait/PortraitMask/PortraitImage"),
                Ring = Get<Image>(s, "Portrait/Ring"),
                Host = s.Find("Portrait/HostBadge").gameObject,
                Nick = Get<TMP_Text>(s, "NicknameText"),
            };
        }

        // 방 만들기 팝업
        createPopup = uiRoot.Find("Canvas_CreateRoom").GetComponent<PopupView>();
        var cw = createPopup.Window;
        nameInput = Get<TMP_InputField>(cw, "NameSection/NameInput");
        mapNameText = Get<TMP_Text>(cw, "MapSection/MapNameText");
        mapPreviewImage = Get<Image>(cw, "MapSection/MapPreview/MapImage");
        prevMapButton = Get<Button>(cw, "MapSection/PrevMapButton");
        nextMapButton = Get<Button>(cw, "MapSection/NextMapButton");
        modeToggles = cw.Find("ModeSection/ModeToggle").GetComponentsInChildren<Toggle>(true);
        maxToggles = cw.Find("MaxPlayersSection/MaxPlayersToggle").GetComponentsInChildren<Toggle>(true);
        passwordInput = Get<TMP_InputField>(cw, "PasswordSection/PasswordInput");
        createCancel = Get<Button>(cw, "CancelButton");
        createConfirm = Get<Button>(cw, "CreateButton");
        createClose = Get<Button>(cw, "CloseButton");

        // 코드 입장 팝업
        joinPopup = uiRoot.Find("Canvas_JoinRoom").GetComponent<PopupView>();
        var jw = joinPopup.Window;
        codeInput = Get<TMP_InputField>(jw, "CodeInput");
        joinCancel = Get<Button>(jw, "CancelButton");
        joinConfirm = Get<Button>(jw, "JoinButton");
        joinConfirmImage = joinConfirm.GetComponent<Image>();
        joinClose = Get<Button>(jw, "CloseButton");

        // 비밀번호 입력 팝업
        passwordPopup = uiRoot.Find("Canvas_Password").GetComponent<PopupView>();
        var pw = passwordPopup.Window;
        pwRoomText = Get<TMP_Text>(pw, "RoomNameText");
        pwInput = Get<TMP_InputField>(pw, "PasswordInput");
        pwCancel = Get<Button>(pw, "CancelButton");
        pwConfirm = Get<Button>(pw, "ConfirmButton");
        pwConfirmImage = pwConfirm.GetComponent<Image>();
        pwClose = Get<Button>(pw, "CloseButton");
    }

    private void WireEvents()
    {
        // 상단바
        settingsButton.onClick.AddListener(settingsView.Open); // 설정 창은 게임 씬과 같은 SettingsView 프리팹을 쓴다
        exitButton.onClick.AddListener(ExitToLogin);
        refreshButton.onClick.AddListener(RefreshRooms);

        // 필터
        for (int i = 0; i < filterToggles.Length; i++)
        {
            int index = i;
            filterToggles[i].onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;
                filter = index;
                SyncRows();
            });
        }
        joinableOnlyToggle.onValueChanged.AddListener(isOn =>
        {
            UpdateJoinableToggleVisual(isOn);
            SyncRows();
        });
        UpdateJoinableToggleVisual(joinableOnlyToggle.isOn);

        // 오른쪽 열
        quickStartButton.onClick.AddListener(QuickStart);
        createRoomButton.onClick.AddListener(OpenCreatePopup);
        joinByCodeButton.onClick.AddListener(OpenJoinPopup);
        detailClose.onClick.AddListener(Deselect);
        detailJoin.onClick.AddListener(() => { if (selected != null) TryJoin(selected); });

        // 방 만들기
        prevMapButton.onClick.AddListener(() => ChangeMap(-1));
        nextMapButton.onClick.AddListener(() => ChangeMap(1));
        createCancel.onClick.AddListener(createPopup.Close);
        createClose.onClick.AddListener(createPopup.Close);
        createConfirm.onClick.AddListener(ConfirmCreate);

        // 코드 입장
        codeInput.onValueChanged.AddListener(OnCodeChanged);
        codeInput.onSubmit.AddListener(_ => ConfirmJoinByCode());
        joinCancel.onClick.AddListener(joinPopup.Close);
        joinClose.onClick.AddListener(joinPopup.Close);
        joinConfirm.onClick.AddListener(ConfirmJoinByCode);

        // 비밀번호 입력
        pwInput.onValueChanged.AddListener(OnPasswordChanged);
        pwInput.onSubmit.AddListener(_ => ConfirmPassword());
        pwCancel.onClick.AddListener(passwordPopup.Close);
        pwClose.onClick.AddListener(passwordPopup.Close);
        pwConfirm.onClick.AddListener(ConfirmPassword);
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F2))
            SetConnection((ConnectionState)(((int)connection + 1) % 3), true);
#endif

        if (!Input.GetKeyDown(KeyCode.Escape) || entering) return;
        if (settingsView.IsOpen) settingsView.Close();
        else if (roomSetup != null && roomSetup.IsOpen) return; // 방 안에서는 X 버튼으로만 나간다
        else if (passwordPopup.IsOpen) passwordPopup.Close();
        else if (createPopup.IsOpen) createPopup.Close();
        else if (joinPopup.IsOpen) joinPopup.Close();
        else if (selected != null) Deselect();
    }

    // ───────────── 연결 상태 ─────────────

    private void SetConnection(ConnectionState state, bool notify)
    {
        var previous = connection;
        connection = state;

        bool online = state == ConnectionState.Connected;
        connectionDot.color = state == ConnectionState.Connected ? new Color32(140, 220, 184, 255)
            : state == ConnectionState.Reconnecting ? UIPalette.Gold : UIPalette.Red;
        connectionText.text = state == ConnectionState.Connected ? "서버 연결됨"
            : state == ConnectionState.Reconnecting ? "재연결 중..." : "연결 끊김";

        // 연결이 없으면 방 목록과 오른쪽 버튼을 흐리게 하고 누를 수 없게 한다
        listGroup.interactable = online;
        sideGroup.interactable = online;
        listGroup.alpha = online ? 1f : 0.55f;
        sideGroup.alpha = online ? 1f : 0.55f;
        if (!online)
        {
            createPopup.Close();
            joinPopup.Close();
            passwordPopup.Close();
        }

        if (!notify) return;
        if (state == ConnectionState.Disconnected) ShowToast("서버와 연결이 끊겼어요");
        else if (state == ConnectionState.Connected && previous != ConnectionState.Connected) ShowToast("서버에 다시 연결됐어요");
    }

    // ───────────── 알림 메시지(토스트) / 로딩 화면 ─────────────

    private void ShowToast(string message) => toast.Show(message);

    private void ShowLoading(bool show, string message = null) => loading.Show(show, message);

    /// <summary>못 들어가는 이유. 들어갈 수 있으면 null.</summary>
    private string JoinBlockedMessage(RoomData room)
    {
        if (room.State == RoomState.Playing) return "이미 게임이 시작된 방이에요";
        if (room.IsFull) return "방이 가득 찼어요";
        return null;
    }

    /// <summary>나가기: 로그인 화면으로 돌아갈지 묻고, 확인하면 돌아간다.</summary>
    private void ExitToLogin()
    {
        if (entering) return;
        if (exitConfirm != null) exitConfirm.Show(LoadLoginScene, "로그인 화면으로 돌아갈까요?");
        else LoadLoginScene();
    }

    // 로그인 씬으로 돌아간다. 씬이 아직 없으면 안내만 한다.
    private void LoadLoginScene()
    {
        if (entering) return;
        if (SceneFlow.ToLogin()) return;
        ShowToast("로그인 화면이 아직 없어요");
    }

    /// <summary>
    /// 방 정보(CurrentRoom)를 채운다. 방 설정 화면과 게임 씬이 읽는다.
    /// 내가 방장이면 맨 앞, 아니면 빈 색 중 첫 번째 자리로 들어간다. (목업: 방장으로 만든 방은 빈자리를 가짜 인원으로 채우고, 팀전이면 팀을 번갈아 배정한다)
    /// </summary>
    private void FillCurrentRoom(RoomData room, bool asHost)
    {
        // 다른 사람들은 이미 준비를 마쳤다고 본다 (목업)
        var players = room.Players
            .Select(p => new RoomPlayerInfo { Name = p.Name, ColorIndex = p.ColorIndex, IsHost = p.IsHost, IsReady = true })
            .ToList();
        int color = Enumerable.Range(0, 4).FirstOrDefault(c => players.All(p => p.ColorIndex != c));
        var mine = new RoomPlayerInfo { Name = me.Name, ColorIndex = color, IsHost = asHost, IsReady = asHost };
        if (asHost) players.Insert(0, mine); else players.Add(mine);

        // 방금 만든 방은 비어 있으니, 시작 조건을 확인해 볼 수 있게 다른 플레이어가 이미 들어와 있다고 본다 (목업)
        while (asHost && players.Count < room.MaxPlayers)
        {
            string botName = PickFreeName() ?? $"손님{players.Count}";
            int botColor = Enumerable.Range(0, 4).FirstOrDefault(c => players.All(p => p.ColorIndex != c));
            players.Add(new RoomPlayerInfo { Name = botName, ColorIndex = botColor, IsReady = true });
            idlers.Add(botName); // PickFreeName이 같은 이름을 또 고르지 않게
        }

        // 팀전의 기본 팀은 자리 순서대로 레드, 블루, 레드, 블루. (팀 "미선택"은 없다) 방 설정에서 바꿀 수 있다.
        AssignDefaultTeams(players, room.IsTeam);

        CurrentRoom = new RoomInfo
        {
            Name = room.Name,
            Code = room.Code,
            Map = room.Map,
            IsTeam = room.IsTeam,
            MaxPlayers = room.MaxPlayers,
            HasPassword = room.HasPassword,
            Password = room.Password,
            IsHost = asHost,
            LocalIndex = players.IndexOf(mine),
            Players = players,
        };
    }

    // ───────────── 방 정보 (방 설정과 게임 씬이 함께 읽는다) ─────────────

    /// <summary>방에 들어올 때 채우는 방 정보. 방 설정(로비 씬)에서 바뀌고, 게임 씬이 읽어 간다. 서버 연동 후에는 서버가 내려 주는 값으로 대체한다.</summary>
    public class RoomInfo
    {
        public string Name;
        public string Code;
        public string Map;       // 맵 이름. 방 설정에서 방장이 바꾼다
        public bool IsTeam;      // 팀전 여부. 방 설정에서 방장이 바꾼다
        public int MaxPlayers;   // 최대 인원(2~4). 방 설정에서 방장이 바꾼다
        public bool HasPassword;
        public string Password; // 방 안에 있는 사람에게만 보여 주는 값. 서버 연동 후에는 서버가 내려 준다
        public bool IsHost;
        public int LocalIndex;  // Players 중 나의 자리
        public List<RoomPlayerInfo> Players = new List<RoomPlayerInfo>();
    }

    public class RoomPlayerInfo
    {
        public string Name;
        public int ColorIndex;
        public bool IsHost;
        public bool IsReady;
        public int Team; // 0 없음, 1 레드, 2 블루
    }

    /// <summary>입장한 방. 방 설정(로비 씬)과 게임 씬이 함께 읽는다. 로비를 거치지 않고 게임 씬만 실행하면 null.</summary>
    public static RoomInfo CurrentRoom { get; private set; }

    // 에디터에서 도메인 리로드 없이 플레이를 다시 시작해도 이전 플레이의 방 정보가 남지 않게 한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => CurrentRoom = null;

    // ───────────── 가짜 방 데이터 ─────────────

    private void SeedRooms()
    {
        AddSeedRoom("우리들마블방", 0, false, 4, RoomState.Waiting, 0, "주사위요정", "건물부자", "여행가");
        AddSeedRoom("초보 환영방", 0, true, 4, RoomState.Waiting, 0, "땅부자", "황금열쇠");
        AddSeedRoom("프로만 오세요", 1, false, 4, RoomState.Waiting, 0, "코인왕", "보드마스터", "행운아", "달려라");
        AddSeedRoom("주말 마블 한판", 0, true, 4, RoomState.Playing, 12, "새벽별", "루돌프", "별똥별", "다람쥐");
        AddSeedRoom("친구들끼리", 2, false, 4, RoomState.Waiting, 0, "구름");
        AddSeedRoom("건물부자 모여라", 0, true, 4, RoomState.Waiting, 0, "호랑이", "펭귄", "고래");
        AddSeedRoom("둘이서 한판", 1, false, 2, RoomState.Playing, 5, "토끼", "여우");
        AddSeedRoom("2인 대기방", 0, false, 2, RoomState.Waiting, 0, "다이아");
        AddSeedRoom("3인 대기방", 0, false, 3, RoomState.Waiting, 0, "바둑이", "단풍");

        // 테스트용 고정 방 코드. 방 코드로 입장 팝업에 그대로 입력하면 된다. 위 AddSeedRoom 호출 순서와 1:1로 대응하므로, 방을 추가하거나 순서를 바꾸면 이 배열도 맞춘다.
        string[] fixedCodes = { "MARBLE", "NEWBIE", "EXPERT", "WEEKND", "FRIEND", "BUILDS", "ONLY22", "DUO222", "TRIO33" };
        for (int i = 0; i < fixedCodes.Length; i++) rooms[i].Code = fixedCodes[i];

        // 자물쇠 표시 확인용: 초보 환영방, 주말 마블 한판, 친구들끼리에 비밀번호를 건다 (목업이라 값은 의미 없다)
        foreach (int index in new[] { 1, 3, 4 }) rooms[index].Password = "1234";
    }

    private void AddSeedRoom(string roomName, int map, bool team, int max, RoomState state, int turn, params string[] names)
    {
        var room = NewRoom(roomName, Maps[map], team, max, null);
        for (int i = 0; i < names.Length; i++)
        {
            room.Players.Add(new PlayerData
            {
                Name = names[i],
                ColorIndex = i,
                IsHost = i == 0,
                IsReady = i == 0 || UnityEngine.Random.value > 0.4f,
            });
        }
        if (state == RoomState.Playing)
        {
            room.State = RoomState.Playing;
            room.Turn = turn;
        }
        rooms.Add(room);
    }

    private RoomData NewRoom(string roomName, string map, bool team, int max, string password)
    {
        return new RoomData
        {
            Id = nextRoomId++,
            Name = roomName,
            Code = GenerateCode(),
            Map = map,
            IsTeam = team,
            MaxPlayers = max,
            Password = password,
        };
    }

    private string GenerateCode()
    {
        string code;
        do
        {
            var chars = new char[6];
            for (int i = 0; i < chars.Length; i++) chars[i] = CodeLetters[UnityEngine.Random.Range(0, CodeLetters.Length)];
            code = new string(chars);
        } while (rooms.Any(r => r.Code == code));
        return code;
    }

    // ───────────── 방 입장 ─────────────

    private bool CanJoin(RoomData room) =>
        room.State == RoomState.Waiting && !room.IsFull;

    /// <summary>
    /// 입장 요청. 목록, 상세, 방 코드 어디서 들어오든 이 함수를 거친다.
    /// 비밀번호가 있는 방은 입력 팝업을 먼저 띄운다. 서버 응답을 기다리는 동안 로딩 화면을 띄우고, 통과하면 방 설정 화면으로 들어간다.
    /// </summary>
    private void TryJoin(RoomData room)
    {
        if (entering || refreshing || connection != ConnectionState.Connected) return;
        string blocked = JoinBlockedMessage(room);
        if (blocked != null) { ShowToast(blocked); return; }
        if (room.HasPassword) { OpenPasswordPopup(room, false); return; }
        Debug.Log($"[Lobby] 입장 요청: {room.Name} ({room.Code})");
        StartCoroutine(EnterRoutine(room, false, null));
    }

    // ───────────── 비밀번호 입력 ─────────────

    private bool IsPasswordLocked(RoomData room) =>
        pwLockUntil.TryGetValue(room.Id, out var until) && Time.unscaledTime < until;

    private void OpenPasswordPopup(RoomData room, bool shake)
    {
        if (IsPasswordLocked(room)) { ShowToast("잠시 후 다시 시도해 주세요"); return; }
        pendingRoom = room;
        pwRoomText.text = room.Name;
        pwInput.text = "";
        SetPasswordConfirmEnabled(false);
        passwordPopup.Open();
        if (shake) UIFx.Shake(pwInput.transform);
    }

    private void OnPasswordChanged(string value) => SetPasswordConfirmEnabled(value.Length == 4);

    private void SetPasswordConfirmEnabled(bool enabled)
    {
        pwConfirm.interactable = enabled;
        pwConfirmImage.color = enabled ? Mint : Neutral;
    }

    private void ConfirmPassword()
    {
        if (pendingRoom == null) return;
        string password = pwInput.text;
        if (password.Length != 4) { UIFx.Shake(pwInput.transform); return; }

        var room = pendingRoom;
        passwordPopup.Close();
        Debug.Log($"[Lobby] 입장 요청(비밀번호): {room.Name} ({room.Code})");
        StartCoroutine(EnterRoutine(room, false, password));
    }

    /// <param name="room">입장할 방. 방 만들기면 새로 만든 방(asHost = true).</param>
    /// <param name="password">비밀번호 방에 입력한 값. 검증은 서버가 하므로 클라이언트는 그대로 보내기만 한다.</param>
    private IEnumerator EnterRoutine(RoomData room, bool asHost, string password)
    {
        entering = true;
        ShowLoading(true);
        yield return new WaitForSecondsRealtime(serverDelay);

        // 응답을 기다리는 사이 연결이 끊겼으면 입장하지 않는다
        if (connection != ConnectionState.Connected)
        {
            ShowLoading(false);
            entering = false;
            yield break;
        }

        // 응답이 오는 사이 방이 사라졌거나(게임이 끝남) 가득 찼을 수 있다 (새로 만든 방은 해당 없음)
        if (!asHost && !rooms.Contains(room))
        {
            ShowLoading(false);
            entering = false;
            ShowToast("방이 사라졌어요");
            SyncAll();
            yield break;
        }
        string blocked = asHost ? null : JoinBlockedMessage(room);
        if (blocked != null)
        {
            ShowLoading(false);
            entering = false;
            ShowToast(blocked);
            SyncAll();
            yield break;
        }

        // ── 서버 쪽 비밀번호 검증 (목업) ──
        if (!asHost && room.HasPassword && room.Password != password)
        {
            ShowLoading(false);
            entering = false;
            int fails = pwFails.TryGetValue(room.Id, out var count) ? count + 1 : 1;
            if (fails >= MaxPasswordFails)
            {
                // 4자리 숫자는 만 가지뿐이라, 계속 틀리면 잠깐 막는다
                pwFails[room.Id] = 0;
                pwLockUntil[room.Id] = Time.unscaledTime + PasswordLockSeconds;
                ShowToast("여러 번 틀렸어요. 잠시 후 다시 시도해 주세요");
            }
            else
            {
                pwFails[room.Id] = fails;
                ShowToast($"비밀번호가 맞지 않아요 ({fails}/{MaxPasswordFails})");
                OpenPasswordPopup(room, true);
            }
            yield break;
        }
        pwFails.Remove(room.Id);

        // 방에 들어왔다. 방 설정 화면에서 준비하고, 방장이 시작하면 게임 씬으로 넘어간다.
        FillCurrentRoom(room, asHost);
        Debug.Log($"[Lobby] 방 입장: {room.Name} ({room.Code}), 방장 {asHost}");
        ShowLoading(false);
        entering = false;
        OpenRoomSetup(CurrentRoom);
    }

    // ───────────── 방 설정 (방 안, 게임 시작 전) ─────────────

    /// <summary>방 설정 화면에 방 정보를 채워 연다. 모드, 맵, 팀, 준비, 최대 인원을 바꾸면 방 정보(RoomInfo)에 기록한다.</summary>
    private void OpenRoomSetup(RoomInfo room)
    {
        if (roomSetup == null) { Debug.LogError("[Lobby] Canvas_RoomSetup이 로비 씬에 없습니다."); return; }

        roomSetup.SetRoomInfo(room.Name, room.Code, room.HasPassword ? room.Password : null);
        roomSetup.SetMode(room.IsTeam);
        roomSetup.SetMap(room.Map, MapSpriteOf(room.Map));
        roomSetup.SetMapSelectable(room.IsHost); // 맵은 방장만 바꾼다
        roomSetup.SetLocalPlayer(room.LocalIndex);
        roomSetup.SetMaxPlayers(room.MaxPlayers);
        roomSetup.SetHost(room.IsHost);
        ApplyRoomSetupSlots(room);
        roomSetup.SetReady(room.Players[room.LocalIndex].IsReady);

        roomSetup.ModeChanged -= OnRoomModeChanged;
        roomSetup.ModeChanged += OnRoomModeChanged;
        roomSetup.MaxPlayersChanged -= OnRoomMaxPlayersChanged;
        roomSetup.MaxPlayersChanged += OnRoomMaxPlayersChanged;
        roomSetup.ReadyToggled -= OnLocalReadyToggled;
        roomSetup.ReadyToggled += OnLocalReadyToggled;
        roomSetup.LeaveRequested -= OnLeaveRequested;
        roomSetup.LeaveRequested += OnLeaveRequested;

        // 내가 팀을 고르면 방 정보에도 기록한다
        roomSetup.Show(() => ChangeRoomMap(room, -1), () => ChangeRoomMap(room, 1),
            () => { room.Players[room.LocalIndex].Team = 1; ApplyRoomSetupSlots(room); },
            () => { room.Players[room.LocalIndex].Team = 2; ApplyRoomSetupSlots(room); },
            StartGameFromRoom);
    }

    // 방장이 맵을 바꿨을 때. 방 정보에 기록하고 방 설정 화면의 맵 이름과 초상화를 바꾼다.
    private void ChangeRoomMap(RoomInfo room, int step)
    {
        int index = Mathf.Max(0, Array.IndexOf(Maps, room.Map));
        room.Map = Maps[(index + step + Maps.Length) % Maps.Length];
        roomSetup.SetMap(room.Map, MapSpriteOf(room.Map));
    }

    private void ApplyRoomSetupSlots(RoomInfo room)
    {
        for (int i = 0; i < 4; i++)
        {
            if (i >= room.Players.Count) { roomSetup.SetSlot(i, new RoomSetupView.Slot { IsEmpty = true }); continue; }
            var p = room.Players[i];
            roomSetup.SetSlot(i, new RoomSetupView.Slot
            {
                IsEmpty = false,
                Name = p.Name,
                ColorIndex = p.ColorIndex,
                IsHost = p.IsHost,
                IsReady = p.IsReady,
                Team = (RoomSetupView.Team)p.Team,
            });
        }
        roomSetup.RecountTeams();
    }

    // 팀전이면 자리 순서대로 레드, 블루, 레드, 블루로 나누고, 개인전이면 팀을 없앤다. (0 없음, 1 레드, 2 블루)
    private static void AssignDefaultTeams(IList<RoomPlayerInfo> players, bool isTeam)
    {
        for (int i = 0; i < players.Count; i++) players[i].Team = isTeam ? 1 + (i % 2) : 0;
    }

    // 방장이 모드를 바꿨을 때. 팀전으로 바꾸면 기본 팀은 자리 순서대로 레드, 블루, 레드, 블루다. 개인전이면 팀이 없다.
    private void OnRoomModeChanged(bool isTeam)
    {
        var room = CurrentRoom;
        if (room == null) return;
        room.IsTeam = isTeam;
        AssignDefaultTeams(room.Players, isTeam);
        ApplyRoomSetupSlots(room);
    }

    // 방장이 최대 인원을 바꿨을 때. 지금 들어와 있는 인원보다 적게는 못 줄인다(창이 막아 준다).
    private void OnRoomMaxPlayersChanged(int max)
    {
        if (CurrentRoom != null) CurrentRoom.MaxPlayers = max;
    }

    // 방장이 아닌 내가 준비하기/준비 취소를 눌렀을 때
    private void OnLocalReadyToggled(bool ready)
    {
        var room = CurrentRoom;
        if (room == null) return;
        room.Players[room.LocalIndex].IsReady = ready;
        ApplyRoomSetupSlots(room);

        // 목업: 내가 방장이 아니면, 모두 준비된 뒤 방장이 잠시 후 시작한다
        if (mockHostStart != null) { StopCoroutine(mockHostStart); mockHostStart = null; }
        if (ready && !room.IsHost && CanStart(room)) mockHostStart = StartCoroutine(MockHostStartRoutine());
    }

    // 시작 조건: 2명 이상, 방장 외 모두 준비, 팀전이면 두 팀 인원이 같다 (RoomSetupView의 시작 버튼 조건과 같다)
    private static bool CanStart(RoomInfo room)
    {
        if (room.Players.Count < 2) return false;
        int red = 0, blue = 0;
        foreach (var p in room.Players)
        {
            if (!p.IsHost && !p.IsReady) return false;
            if (p.Team == 1) red++; else if (p.Team == 2) blue++;
        }
        return !room.IsTeam || red == blue;
    }

    private IEnumerator MockHostStartRoutine()
    {
        yield return new WaitForSecondsRealtime(2f);
        mockHostStart = null;
        // 기다리는 사이 방을 나갔거나 시작 조건이 깨졌다면 시작하지 않는다
        if (CurrentRoom == null || !CanStart(CurrentRoom)) yield break;
        ShowToast("방장이 게임을 시작해요");
        StartGameFromRoom();
    }

    /// <summary>방 설정 화면의 "게임 시작". 게임 씬으로 넘어간다. 게임 씬의 GameManager가 방 정보(CurrentRoom)를 읽어 게임을 시작한다.</summary>
    private void StartGameFromRoom()
    {
        if (entering || CurrentRoom == null) return;
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        entering = true;
        ShowLoading(true);
        yield return new WaitForSecondsRealtime(serverDelay);

        // 응답을 기다리는 사이 연결이 끊기거나 방을 나갔으면 시작하지 않는다
        if (connection != ConnectionState.Connected || CurrentRoom == null)
        {
            ShowLoading(false);
            entering = false;
            yield break;
        }

        Debug.Log($"[Lobby] 게임 씬으로 이동 (방 {CurrentRoom.Code})");
        if (!SceneFlow.ToGame())
        {
            ShowLoading(false);
            entering = false;
            ShowToast("게임 화면을 불러오지 못했어요");
        }
    }

    // X 버튼: 방에서 나갈지 묻고, 확인하면 방을 떠나 방 목록으로 돌아간다
    private void OnLeaveRequested()
    {
        if (entering) return;
        if (exitConfirm != null) exitConfirm.Show(LeaveRoom, "방에서 나갈까요?");
        else LeaveRoom();
    }

    private void LeaveRoom()
    {
        if (mockHostStart != null) { StopCoroutine(mockHostStart); mockHostStart = null; }
        CurrentRoom = null;
        roomSetup.Close();
        SyncAll();
    }

    private void QuickStart()
    {
        // 비밀번호가 있는 방은 빠른 시작으로 들어가지 않는다
        var target = rooms
            .Where(r => !r.HasPassword && CanJoin(r))
            .OrderByDescending(r => r.Players.Count)
            .FirstOrDefault();
        if (target != null) { TryJoin(target); return; }

        Debug.Log("[Lobby] 들어갈 방이 없어 새 방을 만듭니다.");
        CreateRoom($"{me.Name}의 방", Maps[0], false, 4, null);
    }

    /// <summary>방을 만들고 방장으로 방 설정 화면에 들어간다. password가 비어 있으면 비밀번호 없는 방.</summary>
    private void CreateRoom(string roomName, string map, bool team, int max, string password)
    {
        if (entering || refreshing || connection != ConnectionState.Connected) return;
        var room = NewRoom(roomName, map, team, max, password);
        Debug.Log($"[Lobby] 방 생성 요청: {room.Name} ({room.Code}) {(team ? "팀전" : "개인전")} {max}명 {(room.HasPassword ? "비밀번호 있음" : "비밀번호 없음")}");
        StartCoroutine(EnterRoutine(room, true, null));
    }

    private void Select(RoomData room)
    {
        bool wasShowingDetail = detailCard.activeSelf;
        selected = room;
        RefreshDetail();
        UpdateRowHighlights();
        ShowDetail(!wasShowingDetail);
    }

    private void Deselect()
    {
        if (selected == null) return;
        selected = null;
        UpdateRowHighlights();
        ShowProfile(true);
    }

    // ───────────── 오른쪽 열 전환 ─────────────

    private void ShowDetail(bool animate)
    {
        profileCard.SetActive(false);
        playerListCard.SetActive(false);
        detailCard.SetActive(true);
        if (animate) Fade(detailCard);
    }

    // 평소에는 내 정보 + 접속자 목록, 방을 고르면 방 상세가 이 자리를 통째로 차지한다
    private void ShowProfile(bool animate)
    {
        detailCard.SetActive(false);
        profileCard.SetActive(true);
        playerListCard.SetActive(true);
        if (!animate) return;
        Fade(profileCard);
        Fade(playerListCard);
    }

    private static void Fade(GameObject go)
    {
        var group = UIFx.EnsureGroup(go);
        group.DOKill();
        group.alpha = 0f;
        group.DOFade(1f, 0.2f).SetUpdate(true).SetLink(go);
    }

    // ───────────── 방 목록 ─────────────

    /// <summary>방 목록을 서버에서 다시 받아 온다. 응답을 기다리는 동안 로딩 화면을 띄운다. (목업: 잠깐 기다린 뒤 지금 목록을 다시 그린다)</summary>
    private void RefreshRooms()
    {
        if (refreshing || entering || connection != ConnectionState.Connected) return;
        StartCoroutine(RefreshRoutine());
    }

    private IEnumerator RefreshRoutine()
    {
        refreshing = true;
        ShowLoading(true, ""); // 문구 없이 점만
        yield return new WaitForSecondsRealtime(serverDelay);
        ShowLoading(false);
        refreshing = false;

        // 연결이 끊겼으면 갱신 결과를 보여 주지 않는다
        if (connection != ConnectionState.Connected) yield break;
        SyncAll();
        ShowToast("방 목록을 새로 불러왔어요");
    }

    private void SyncAll()
    {
        // 보고 있던 방이 사라졌으면 내 정보로 돌아간다
        if (selected != null && !rooms.Contains(selected)) selected = null;
        if (selected == null && detailCard.activeSelf) ShowProfile(true);
        SyncRows();
        RefreshPlayerList();
        if (selected != null) RefreshDetail();
    }

    private void UpdateJoinableToggleVisual(bool isOn)
    {
        joinableOnlyToggle.targetGraphic.color = isOn ? ReadyBg : Neutral;
        joinableOnlyLabel.color = isOn ? ReadyText : UIPalette.Ink;
    }

    private bool IsListed(RoomData room)
    {
        // 플레이어가 켜면 입장할 수 없는 방(가득 참, 게임 중)은 숨긴다
        if (joinableOnlyToggle.isOn && !CanJoin(room)) return false;
        return filter == 0 || (filter == 1 && !room.IsTeam) || (filter == 2 && room.IsTeam);
    }

    private void SyncRows()
    {
        var visible = rooms.Where(IsListed).ToList();

        foreach (var gone in rows.Keys.Where(r => !visible.Contains(r)).ToList())
        {
            rows[gone].Go.SetActive(false); // Destroy는 프레임 끝에 처리되므로 먼저 숨긴다
            Destroy(rows[gone].Go);
            rows.Remove(gone);
        }

        for (int i = 0; i < visible.Count; i++)
        {
            var room = visible[i];
            if (!rows.TryGetValue(room, out var row))
            {
                row = CreateRow(room);
                rows[room] = row;
            }
            row.Go.transform.SetSiblingIndex(i + 1); // 0번은 복제용 틀
            UpdateRow(row, room);
        }

        emptyState.SetActive(visible.Count == 0);
        UpdateRowHighlights();
    }

    private RowView CreateRow(RoomData room)
    {
        var go = Instantiate(rowTemplate, rowContent);
        go.name = $"RoomRow_{room.Id}";
        go.SetActive(true);
        var t = go.transform;

        var row = new RowView
        {
            Go = go,
            Row = go.GetComponent<Button>(),
            Bg = go.GetComponent<Image>(),
            Stroke = Get<Image>(t, "Stroke"),
            Thumb = Get<Image>(t, "Thumb"),
            ThumbStroke = Get<Image>(t, "Thumb/Stroke"),
            Lock = t.Find("Thumb/LockBadge").gameObject,
            Number = Get<TMP_Text>(t, "Thumb/NumberText"),
            Name = Get<TMP_Text>(t, "NameText"),
            Info = Get<TMP_Text>(t, "InfoText"),
            ModeChip = Get<Image>(t, "ModeChip"),
            ModeText = Get<TMP_Text>(t, "ModeChip/Text"),
            Count = Get<TMP_Text>(t, "PlayerCountText"),
            StatusChip = Get<Image>(t, "StatusChip"),
            StatusText = Get<TMP_Text>(t, "StatusChip/Text"),
            Join = Get<Button>(t, "JoinButton"),
            JoinImage = Get<Image>(t, "JoinButton"),
            JoinLabel = Get<TMP_Text>(t, "JoinButton/Label"),
            Dots = new Image[4],
        };
        for (int i = 0; i < 4; i++) row.Dots[i] = Get<Image>(t, $"PlayerDots/Dot_{i + 1}");

        row.Row.onClick.RemoveAllListeners();
        row.Row.onClick.AddListener(() => Select(room));
        row.Join.onClick.RemoveAllListeners();
        row.Join.onClick.AddListener(() => TryJoin(room));
        return row;
    }

    private void UpdateRow(RowView row, RoomData room)
    {
        row.Bg.color = room.IsTeam ? TeamRowBg : SoloRowBg;
        row.Thumb.color = Color.white;
        row.ThumbStroke.color = RowStroke;
        row.Lock.SetActive(room.HasPassword);
        row.Number.text = (room.Id % 100).ToString("00"); // 목록 번호: 방 Id의 끝 두 자리
        row.Name.text = room.Name;
        row.Info.text = $"{room.Map} · 방장 {room.Host?.Name}";

        row.ModeChip.color = room.IsTeam ? WarmBg : ReadyBg;
        row.ModeText.text = room.IsTeam ? "팀전" : "개인전";
        row.ModeText.color = room.IsTeam ? WarmText : ReadyText;

        row.Count.text = $"{room.Players.Count} / {room.MaxPlayers}";
        for (int i = 0; i < row.Dots.Length; i++)
        {
            row.Dots[i].gameObject.SetActive(i < room.MaxPlayers);
            row.Dots[i].color = i < room.Players.Count ? UIPalette.PlayerColor(room.Players[i].ColorIndex) : DotEmpty;
        }

        ApplyStatus(room, row.StatusChip, row.StatusText);

        bool canJoin = CanJoin(room);
        row.Join.interactable = canJoin;
        row.JoinImage.color = canJoin ? Mint : Neutral;
        row.JoinLabel.text = canJoin ? "참가" : "입장 불가";
        row.JoinLabel.fontSize = canJoin ? 24 : 18;
        row.JoinLabel.color = canJoin ? Color.white : UIPalette.InkSub;
    }

    private void ApplyStatus(RoomData room, Image chip, TMP_Text text)
    {
        bool playing = room.State == RoomState.Playing;
        bool full = !playing && room.IsFull;
        chip.color = playing ? WarmBg : full ? RedBg : ReadyBg;
        text.text = playing ? "게임 중" : full ? "가득 참" : "대기 중";
        text.color = playing ? WarmText : full ? RedText : ReadyText;
    }

    private void UpdateRowHighlights()
    {
        foreach (var pair in rows)
        {
            bool on = pair.Key == selected;
            bool team = pair.Key.IsTeam;
            var stroke = pair.Value.Stroke;
            stroke.color = on ? (team ? TeamRowStrokeOn : SoloRowStrokeOn) : (team ? TeamRowStroke : SoloRowStroke);
            if (selectedStrokeSprite != null && normalStrokeSprite != null)
                stroke.sprite = on ? selectedStrokeSprite : normalStrokeSprite;
        }
    }

    // ───────────── 방 상세 ─────────────

    private void RefreshDetail()
    {
        var room = selected;
        if (room == null) return;

        detailTitle.text = room.Name;
        detailLockChip.SetActive(room.HasPassword); // 방 코드는 방 안에 있는 사람에게만 보여 주므로 로비에서는 표시하지 않는다
        detailModeChip.color = room.IsTeam ? WarmBg : ReadyBg;
        detailModeText.text = room.IsTeam ? "팀전" : "개인전";
        detailModeText.color = room.IsTeam ? WarmText : ReadyText;
        ApplyStatus(room, detailStatusChip, detailStatusText);
        detailMap.text = room.Map;
        detailCount.text = $"{room.Players.Count} / {room.MaxPlayers}";

        bool playing = room.State == RoomState.Playing;
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            s.Go.SetActive(i < room.MaxPlayers);
            if (i >= room.MaxPlayers) continue;

            if (i >= room.Players.Count) { SetEmptySlot(s); continue; }

            var p = room.Players[i];
            Color color = UIPalette.PlayerColor(p.ColorIndex);
            s.Bg.color = UIPalette.PlayerLightColor(p.ColorIndex);
            s.Stroke.color = color;
            s.Ring.color = color;
            s.Portrait.color = color;
            s.Host.SetActive(p.IsHost);
            s.Nick.text = p.Name;
            s.Nick.color = UIPalette.Ink;
        }

        detailProgress.SetActive(playing);
        if (playing)
        {
            detailTurn.text = $"{room.Turn} <size=75%><color=#C9A770>/ {room.MaxTurn} 턴</color></size>";
            var max = gaugeFill.anchorMax;
            max.x = Mathf.Clamp01(room.Turn / (float)room.MaxTurn);
            gaugeFill.anchorMax = max;
        }

        bool canJoin = CanJoin(room);
        detailJoin.interactable = canJoin;
        detailJoinImage.color = canJoin ? Mint : Neutral;
        detailJoinLabel.text = canJoin ? "참가하기" : "입장 불가";
        detailJoinLabel.color = canJoin ? Color.white : UIPalette.InkSub;
    }

    private static void SetEmptySlot(SlotView s)
    {
        s.Bg.color = Neutral;
        s.Stroke.color = Neutral;
        s.Ring.color = Neutral;
        s.Portrait.color = Neutral;
        s.Host.SetActive(false);
        s.Nick.text = "빈 자리";
        s.Nick.color = UIPalette.InkSub;
    }

    // ───────────── 방 만들기 ─────────────

    private void OpenCreatePopup()
    {
        nameInput.text = $"{me.Name}의 방";
        passwordInput.text = "";
        mapIndex = 0;
        ShowMap();
        modeToggles[0].SetIsOnWithoutNotify(true);
        modeToggles[1].SetIsOnWithoutNotify(false);
        for (int i = 0; i < maxToggles.Length; i++) maxToggles[i].SetIsOnWithoutNotify(i == maxToggles.Length - 1);
        createPopup.Open();
    }

    private void ChangeMap(int step)
    {
        mapIndex = (mapIndex + step + Maps.Length) % Maps.Length;
        ShowMap();
    }

    // 고른 맵의 이름과 초상화를 보여 준다
    private void ShowMap()
    {
        mapNameText.text = Maps[mapIndex];
        if (mapPreviewImage != null) mapPreviewImage.sprite = MapSpriteOf(Maps[mapIndex]);
    }

    /// <summary>맵 이름에 맞는 초상화. 없으면 null.</summary>
    private Sprite MapSpriteOf(string mapName)
    {
        int index = Array.IndexOf(Maps, mapName);
        return mapSprites != null && index >= 0 && index < mapSprites.Length ? mapSprites[index] : null;
    }

    private void ConfirmCreate()
    {
        string roomName = nameInput.text.Trim();
        if (roomName.Length == 0) { UIFx.Shake(nameInput.transform); ShowToast("방 이름을 입력해 주세요"); return; }

        // 비밀번호는 선택이다. 비워 두면 비밀번호 없는 방, 적었다면 4자리여야 한다.
        string password = passwordInput.text;
        if (password.Length > 0 && password.Length != 4) { UIFx.Shake(passwordInput.transform); ShowToast("비밀번호는 4자리로 입력해 주세요"); return; }

        int max = 2 + Mathf.Max(0, Array.FindIndex(maxToggles, t => t.isOn)); // 최대 인원 토글은 2명부터. 아무것도 안 켜져 있으면 2명
        CreateRoom(roomName, Maps[mapIndex], modeToggles[1].isOn, max, password.Length > 0 ? password : null);
        createPopup.Close();
    }

    // ───────────── 코드로 입장 ─────────────

    private void OpenJoinPopup()
    {
        codeInput.text = "";
        SetJoinConfirmEnabled(false);
        joinPopup.Open();
    }

    private void OnCodeChanged(string value)
    {
        string upper = value.ToUpperInvariant();
        if (upper != value) codeInput.SetTextWithoutNotify(upper);
        SetJoinConfirmEnabled(upper.Length == 6);
    }

    private void SetJoinConfirmEnabled(bool enabled)
    {
        joinConfirm.interactable = enabled;
        joinConfirmImage.color = enabled ? Mint : Neutral;
    }

    private void ConfirmJoinByCode()
    {
        string code = codeInput.text.Trim().ToUpperInvariant();
        if (code.Length != 6) { UIFx.Shake(codeInput.transform); return; }

        var room = rooms.FirstOrDefault(r => r.Code == code);
        if (room == null) { UIFx.Shake(codeInput.transform); ShowToast("존재하지 않는 방 코드예요"); return; }
        string blocked = JoinBlockedMessage(room);
        if (blocked != null) { UIFx.Shake(codeInput.transform); ShowToast(blocked); return; }

        // 코드로 들어와도 비밀번호가 있는 방이면 TryJoin이 비밀번호를 묻는다
        TryJoin(room);
        joinPopup.Close();
    }

    // ───────────── 실시간 갱신 흉내 ─────────────

    private IEnumerator SimulateRoutine()
    {
        var wait = new WaitForSeconds(simulateInterval);
        while (true)
        {
            yield return wait;
            if (simulateLiveUpdates && connection == ConnectionState.Connected) Simulate(); // 연결이 끊기면 갱신도 멈춘다
        }
    }

    private void Simulate()
    {
        SimulateIdlers();
        if (rooms.Count == 0) { SyncAll(); return; }
        var room = rooms[UnityEngine.Random.Range(0, rooms.Count)];

        if (room.State == RoomState.Playing)
        {
            room.Turn++;
            if (room.Turn > room.MaxTurn) rooms.Remove(room); // 게임이 끝난 방은 사라진다
        }
        else
        {
            var bots = room.Players.Where(p => !p.IsHost).ToList();
            int roll = UnityEngine.Random.Range(0, 10);
            if (roll < 4 && !room.IsFull) AddBot(room);
            else if (roll < 6 && bots.Count > 0) room.Players.Remove(bots[bots.Count - 1]);
            else if (bots.Count > 0) { var b = bots[UnityEngine.Random.Range(0, bots.Count)]; b.IsReady = !b.IsReady; }

            // 가득 차고 모두 준비되면 가끔 게임이 시작된다
            if (room.IsFull && room.Players.All(p => p.IsReady) && UnityEngine.Random.value < 0.5f)
            {
                room.State = RoomState.Playing;
                room.Turn = 1;
            }
        }
        SyncAll();
    }

    private void AddBot(RoomData room)
    {
        string botName = PickFreeName();
        if (botName == null) return;
        int color = Enumerable.Range(0, 4).FirstOrDefault(c => room.Players.All(p => p.ColorIndex != c));
        room.Players.Add(new PlayerData { Name = botName, ColorIndex = color, IsReady = UnityEngine.Random.value > 0.5f });
    }

    /// <summary>서버에 아직 없는 이름 하나. 다 쓰였으면 null.</summary>
    private string PickFreeName()
    {
        var used = new HashSet<string> { me.Name };
        foreach (var r in rooms) foreach (var p in r.Players) used.Add(p.Name);
        foreach (var name in idlers) used.Add(name);
        var free = NamePool.Where(n => !used.Contains(n)).ToList();
        return free.Count == 0 ? null : free[UnityEngine.Random.Range(0, free.Count)];
    }

    // ───────────── 내 정보 ─────────────

    // 목업 값. 서버 연동 후에는 서버가 내려 주는 내 전적과 보유 마블로 바꾼다
    private const int MockWins = 28;
    private const int MockLosses = 20;
    private const long MockMarble = 12500;

    private void RefreshProfile()
    {
        int games = MockWins + MockLosses;
        int rate = games == 0 ? 0 : Mathf.RoundToInt(MockWins * 100f / games);
        // 승패와 승률을 한 줄로: "28승 20패 (58%)"
        recordText.text = $"{MockWins}승 {MockLosses}패 <size=80%><color=#8C90A8>({rate}%)</color></size>";
        marbleText.text = UIPalette.Money(MockMarble);
    }

    // ───────────── 접속자 목록 ─────────────

    private enum PresenceState { Lobby, Waiting, Playing }

    private class PlayerRowView
    {
        public GameObject Go;
        public Image StatusChip;
        public TMP_Text Name, Sub, StatusText;
    }

    /// <summary>방 안 플레이어와 로비에 있는 플레이어를 합쳐 "서버에 있는 사람" 목록을 만든다. 나도 목록의 한 명으로만 들어간다.</summary>
    private void RefreshPlayerList()
    {
        var entries = new List<(string name, PresenceState state, string roomName)>
        {
            (me.Name, PresenceState.Lobby, null),
        };
        foreach (var name in idlers) entries.Add((name, PresenceState.Lobby, null));
        foreach (var r in rooms)
            foreach (var p in r.Players)
                entries.Add((p.Name, r.State == RoomState.Playing ? PresenceState.Playing : PresenceState.Waiting, r.Name));

        playerCountText.text = $"{entries.Count}명";

        while (playerRows.Count < entries.Count) playerRows.Add(CreatePlayerRow(playerRows.Count));
        for (int i = 0; i < playerRows.Count; i++)
        {
            var row = playerRows[i];
            bool active = i < entries.Count;
            row.Go.SetActive(active);
            if (!active) continue;
            var e = entries[i];

            row.Name.text = e.name;

            // 방에 있는 사람은 상태 칩 바로 왼쪽에 방 이름이 붙는다
            bool hasRoom = e.roomName != null;
            row.Sub.gameObject.SetActive(hasRoom);
            if (hasRoom) row.Sub.text = e.roomName;

            Color bg = e.state == PresenceState.Playing ? WarmBg : e.state == PresenceState.Waiting ? ReadyBg : Neutral;
            Color fg = e.state == PresenceState.Playing ? WarmText : e.state == PresenceState.Waiting ? ReadyText : UIPalette.InkSub;
            row.StatusChip.color = bg;
            row.StatusText.color = fg;
            row.StatusText.text = e.state == PresenceState.Playing ? "게임 중" : e.state == PresenceState.Waiting ? "방 대기" : "로비";
        }
    }

    private PlayerRowView CreatePlayerRow(int index)
    {
        var go = Instantiate(playerRowTemplate, playerContent);
        go.name = $"PlayerRow_{index + 1}";
        var t = go.transform;
        return new PlayerRowView
        {
            Go = go,
            Name = Get<TMP_Text>(t, "NameText"),
            Sub = Get<TMP_Text>(t, "RoomText"),
            StatusChip = Get<Image>(t, "StatusChip"),
            StatusText = Get<TMP_Text>(t, "StatusChip/Text"),
        };
    }

    // 로비에만 있는 플레이어(방에 들어가지 않은 사람). 가끔 들어오고 나간다.
    private void SeedIdlers()
    {
        foreach (var name in new[] { "새싹", "도토리", "푸딩", "머핀", "솜사탕", "별빛" })
            idlers.Add(name);
    }

    private void SimulateIdlers()
    {
        int roll = UnityEngine.Random.Range(0, 10);
        if (roll < 3 && idlers.Count < 10)
        {
            string name = PickFreeName();
            if (name != null) idlers.Add(name);
        }
        else if (roll < 6 && idlers.Count > 2)
        {
            idlers.RemoveAt(UnityEngine.Random.Range(0, idlers.Count));
        }
    }
}
