using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>로비 씬의 방 설정 화면. 방에 들어오면 열리고, 방장이 시작하면 게임 씬으로 넘어간다. Canvas_RoomSetup에 붙는다.</summary>
public class RoomSetupView : UIView
{
    public override string PanelPath => "RoomSetupPopup/Window";
    public override string DimPath => "RoomSetupPopup/Dim";

    // 기본값(0)이 "팀 미선택"이 되도록 None을 0으로 둔다
    public enum Team { None = 0, Red = 1, Blue = 2 }

    /// <summary>슬롯 1칸 정보. IsEmpty면 빈 자리로 표시.</summary>
    public struct Slot
    {
        public bool IsEmpty;
        public string Name;
        public Sprite Portrait;
        public int ColorIndex;
        public bool IsHost;
        public bool IsReady;
        public Team Team;
    }

    private static readonly Color RedTeamBg = new Color32(255, 211, 216, 255);
    private static readonly Color RedTeamText = new Color32(214, 90, 110, 255);
    private static readonly Color BlueTeamBg = new Color32(214, 230, 255, 255);
    private static readonly Color BlueTeamText = new Color32(74, 127, 209, 255);
    private static readonly Color SoloSlotBg = new Color32(246, 248, 252, 255);     // 개인전(색 없음)
    private static readonly Color SoloSlotStroke = new Color32(220, 225, 236, 255);

    [Serializable]
    private class SlotRefs
    {
        public Image background;
        public Image stroke;
        public Image portraitImage;
        public Image portraitRing;
        public GameObject hostBadge;
        public Image readyBadge;
        public TMP_Text readyText;
        public Image teamBadge;
        public TMP_Text teamText;
        public TMP_Text nicknameText;
    }

    [SerializeField] private Image mapImage;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private Button prevMapButton;
    [SerializeField] private Button nextMapButton;
    [SerializeField] private Toggle soloToggle;
    [SerializeField] private Toggle teamToggle;
    [SerializeField] private Toggle[] maxPlayersToggles = new Toggle[3]; // 2명, 3명, 4명
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private SlotRefs[] slots = new SlotRefs[4];
    [SerializeField] private GameObject teamSection;
    [SerializeField] private Button redTeamButton;
    [SerializeField] private TMP_Text redTeamLabel;
    [SerializeField] private Button blueTeamButton;
    [SerializeField] private TMP_Text blueTeamLabel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyLabel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text subtitleText;

    /// <summary>모드 토글이 바뀌면 호출. true면 팀전.</summary>
    public event Action<bool> ModeChanged;

    /// <summary>방장이 아닌 플레이어가 준비하기/준비 취소를 누르면 호출. true면 준비 완료.</summary>
    public event Action<bool> ReadyToggled;

    /// <summary>방장이 최대 인원(2~4)을 바꾸면 호출.</summary>
    public event Action<int> MaxPlayersChanged;

    /// <summary>오른쪽 위 X 버튼을 눌렀다. 방에서 나가겠다는 뜻이라, 창을 닫는 건 받는 쪽이 정한다.</summary>
    public event Action LeaveRequested;

    private bool isTeamMode;
    private bool isHost = true;

    public override void Bind()
    {
        const string w = "RoomSetupPopup/Window/";
        mapImage = Find<Image>(w + "MapSection/MapPreview/MapImage");
        mapNameText = Find<TMP_Text>(w + "MapSection/MapSelector/MapNameText");
        prevMapButton = Find<Button>(w + "MapSection/MapSelector/PrevMapButton");
        nextMapButton = Find<Button>(w + "MapSection/MapSelector/NextMapButton");
        soloToggle = Find<Toggle>(w + "ModeSection/ModeToggle/SoloToggle");
        teamToggle = Find<Toggle>(w + "ModeSection/ModeToggle/TeamToggle");
        if (maxPlayersToggles == null || maxPlayersToggles.Length != 3) maxPlayersToggles = new Toggle[3]; // 이미 저장된 프리팹에는 배열이 없다
        for (int i = 0; i < maxPlayersToggles.Length; i++)
            maxPlayersToggles[i] = Find<Toggle>($"{w}MaxPlayersSection/MaxPlayersToggle/Max{i + 2}Toggle");
        playerCountText = Find<TMP_Text>(w + "PlayerCountText");
        for (int i = 0; i < 4; i++)
        {
            string s = $"{w}PlayerSlots/PlayerSlot_{i + 1}/";
            slots[i] = new SlotRefs
            {
                background = Find<Image>(s.TrimEnd('/')),
                stroke = Find<Image>(s + "Stroke"),
                portraitImage = Find<Image>(s + "Portrait/PortraitMask/PortraitImage"),
                portraitRing = Find<Image>(s + "Portrait/Ring"),
                hostBadge = FindObject(s + "Portrait/HostBadge"),
                readyBadge = Find<Image>(s + "Portrait/ReadyBadge"),
                readyText = Find<TMP_Text>(s + "Portrait/ReadyBadge/Text"),
                teamBadge = Find<Image>(s + "TeamBadge"),
                teamText = Find<TMP_Text>(s + "TeamBadge/Text"),
                nicknameText = Find<TMP_Text>(s + "NicknameText"),
            };
        }
        teamSection = FindObject(w + "TeamSection");
        redTeamButton = Find<Button>(w + "TeamSection/TeamButtons/RedTeamButton");
        redTeamLabel = Find<TMP_Text>(w + "TeamSection/TeamButtons/RedTeamButton/Label");
        blueTeamButton = Find<Button>(w + "TeamSection/TeamButtons/BlueTeamButton");
        blueTeamLabel = Find<TMP_Text>(w + "TeamSection/TeamButtons/BlueTeamButton/Label");
        startButton = Find<Button>(w + "StartButton");
        readyButton = Find<Button>(w + "ReadyButton");
        readyLabel = Find<TMP_Text>(w + "ReadyButton/Label");
        hintText = Find<TMP_Text>(w + "HintText");
        closeButton = Find<Button>(w + "CloseButton");
        subtitleText = Find<TMP_Text>(w + "SubtitleText");
    }

    private void Awake()
    {
        if (teamToggle != null) teamToggle.onValueChanged.AddListener(OnTeamToggleChanged);
        SetOnClick(closeButton, () => LeaveRequested?.Invoke());
        SetOnClick(readyButton, OnReadyClicked);
        for (int i = 0; i < maxPlayersToggles.Length; i++)
        {
            int value = i + 2;
            if (maxPlayersToggles[i] != null) maxPlayersToggles[i].onValueChanged.AddListener(isOn => OnMaxPlayersToggled(value, isOn));
        }
        // 팀 버튼은 Show를 거치지 않고 열어도 동작해야 해서 여기서 연결한다
        SetOnClick(redTeamButton, () => ChangeLocalTeam(Team.Red, onRedTeamChanged));
        SetOnClick(blueTeamButton, () => ChangeLocalTeam(Team.Blue, onBlueTeamChanged));
    }

    private Action onRedTeamChanged;
    private Action onBlueTeamChanged;

    /// <summary>제목 옆 줄에 방 이름, 방 코드, (있으면) 비밀번호를 보여 준다.</summary>
    public void SetRoomInfo(string roomName, string roomCode, string password)
    {
        if (subtitleText == null) subtitleText = Find<TMP_Text>("RoomSetupPopup/Window/SubtitleText"); // 이미 만들어진 프리팹에는 연결돼 있지 않을 수 있다
        if (subtitleText == null) return;
        string text = $"{roomName}  ·  방 코드 {roomCode}";
        if (!string.IsNullOrEmpty(password)) text += $"  ·  비밀번호 {password}";
        subtitleText.text = text;
    }

    /// <summary>
    /// onRedTeam / onBlueTeam: 팀 버튼을 눌러 이 화면의 플레이어(SetLocalPlayer) 팀이 실제로 바뀐 뒤 호출된다.
    /// </summary>
    public void Show(Action onPrevMap, Action onNextMap, Action onRedTeam, Action onBlueTeam, Action onStart)
    {
        SetOnClick(prevMapButton, onPrevMap);
        SetOnClick(nextMapButton, onNextMap);
        onRedTeamChanged = onRedTeam;
        onBlueTeamChanged = onBlueTeam;
        SetOnClick(startButton, onStart);
        Open();
    }

    public void SetMap(string mapName, Sprite preview)
    {
        if (mapNameText != null) mapNameText.text = mapName;
        if (preview != null && mapImage != null) { mapImage.sprite = preview; mapImage.color = Color.white; }
    }

    /// <summary>팀전이면 팀 선택 영역과 팀 배지를 보여준다. 토글도 같이 맞춘다.</summary>
    public void SetMode(bool teamMode)
    {
        isTeamMode = teamMode;
        if (teamToggle != null) teamToggle.SetIsOnWithoutNotify(teamMode);
        if (soloToggle != null) soloToggle.SetIsOnWithoutNotify(!teamMode);
        SetActive(teamSection, teamMode);
        foreach (var s in slots)
            if (s != null && s.teamBadge != null) s.teamBadge.gameObject.SetActive(teamMode);

        // 모드에 따라 슬롯 색이 달라지므로 다시 그린다 (팀전: 팀 색, 개인전: 색 없음)
        for (int i = 0; i < slotData.Length; i++) SetSlot(i, slotData[i]);
        UpdateStartState();
    }

    public void SetSlot(int index, Slot slot)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null) return;
        slotData[index] = slot;
        var r = slots[index];

        // 팀전이면 팀 색, 개인전이면 색 없이 담백하게. 빈 자리는 어느 쪽이든 회색.
        Color bgColor, strokeColor;
        if (slot.IsEmpty) { bgColor = UIPalette.Neutral; strokeColor = UIPalette.Neutral; }
        else if (isTeamMode)
        {
            bgColor = slot.Team == Team.Blue ? BlueTeamBg : RedTeamBg;
            strokeColor = slot.Team == Team.Blue ? UIPalette.Player[1] : UIPalette.Player[0];
        }
        else { bgColor = SoloSlotBg; strokeColor = SoloSlotStroke; }

        if (r.background != null) r.background.color = bgColor;
        if (r.stroke != null) r.stroke.color = strokeColor;
        // 초상화 테두리: 팀전은 팀 색, 개인전은 원래대로 플레이어 색을 유지한다
        if (r.portraitRing != null)
            r.portraitRing.color = slot.IsEmpty ? UIPalette.Neutral : isTeamMode ? strokeColor : UIPalette.PlayerColor(slot.ColorIndex);
        SetPortrait(r.portraitImage, slot.IsEmpty ? null : slot.Portrait, Color.white);
        if (r.nicknameText != null) r.nicknameText.text = slot.IsEmpty ? "빈 자리" : slot.Name;

        SetActive(r.hostBadge, !slot.IsEmpty && slot.IsHost);

        // 방장은 준비 표시가 없다
        bool showReady = !slot.IsEmpty && !slot.IsHost;
        if (r.readyBadge != null)
        {
            r.readyBadge.gameObject.SetActive(showReady);
            r.readyBadge.color = slot.IsReady ? UIPalette.ReadyBg : UIPalette.Neutral;
        }
        if (r.readyText != null)
        {
            r.readyText.text = slot.IsReady ? "준비 완료" : "대기 중";
            r.readyText.color = slot.IsReady ? UIPalette.ReadyText : UIPalette.InkSub;
        }

        if (r.teamBadge != null)
        {
            r.teamBadge.gameObject.SetActive(isTeamMode && !slot.IsEmpty);
            r.teamBadge.color = slot.Team == Team.Blue ? BlueTeamBg : RedTeamBg; // 팀은 레드 아니면 블루, "미선택"은 없다
        }
        if (r.teamText != null)
        {
            r.teamText.text = slot.Team == Team.Blue ? "블루팀" : "레드팀";
            r.teamText.color = slot.Team == Team.Blue ? BlueTeamText : RedTeamText;
        }
        UpdateStartState();
    }

    private int maxPlayers = 4;

    /// <summary>
    /// 방의 최대 인원(2~4). 최대 인원 토글을 맞추고, 인원 수 표시를 고치고, 최대 인원을 넘는 자리는 숨긴다.
    /// 지금 들어와 있는 인원보다 적은 값은 고를 수 없다.
    /// </summary>
    public void SetMaxPlayers(int max)
    {
        maxPlayers = Mathf.Clamp(max, 2, 4);
        RefreshMaxPlayers();
    }

    private void RefreshMaxPlayers()
    {
        int current = 0;
        foreach (var slot in slotData) if (!slot.IsEmpty) current++;

        if (playerCountText != null) playerCountText.text = $"{current} / {maxPlayers}";
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].background != null) slots[i].background.gameObject.SetActive(i < maxPlayers);

        for (int i = 0; i < maxPlayersToggles.Length; i++)
        {
            var toggle = maxPlayersToggles[i];
            if (toggle == null) continue;
            int value = i + 2;
            toggle.SetIsOnWithoutNotify(value == maxPlayers);
            toggle.interactable = isHost && value >= current;
        }
    }

    private void OnMaxPlayersToggled(int value, bool isOn)
    {
        if (!isOn || value == maxPlayers) return;
        maxPlayers = value;
        RefreshMaxPlayers();
        MaxPlayersChanged?.Invoke(value);
    }

    /// <summary>팀 인원 표시. 팀을 고를 때 인원 제한은 없다. (시작할 때만 두 팀 인원이 같아야 한다)</summary>
    public void SetTeamCounts(int red, int blue)
    {
        if (redTeamLabel != null) redTeamLabel.text = $"레드팀 <size=75%><color=#E8A0AC>{red}명</color></size>";
        if (blueTeamLabel != null) blueTeamLabel.text = $"블루팀 <size=75%><color=#9DBBE8>{blue}명</color></size>";
    }

    /// <summary>
    /// 이 화면의 플레이어가 방장인지. 방장만 모드를 바꾸고 게임을 시작할 수 있고,
    /// 방장이 아니면 시작 버튼 자리에 준비하기 버튼이 보인다.
    /// </summary>
    public void SetHost(bool host)
    {
        isHost = host;
        if (soloToggle != null) soloToggle.interactable = host;
        if (teamToggle != null) teamToggle.interactable = host;
        RefreshActionButtons();
        RefreshMaxPlayers();
        UpdateStartState();
    }

    private bool localReady;

    private void RefreshActionButtons()
    {
        SetActive(startButton != null ? startButton.gameObject : null, isHost);
        SetActive(readyButton != null ? readyButton.gameObject : null, !isHost);
    }

    /// <summary>이 화면의 플레이어가 준비한 상태인지 보여준다. (준비하기 ↔ 준비 취소)</summary>
    public void SetReady(bool ready)
    {
        localReady = ready;
        // 회색은 비활성처럼 보이므로 두 상태 모두 또렷한 색을 쓴다: 준비하기는 파랑, 준비 취소는 노랑
        if (readyLabel != null) readyLabel.text = ready ? "준비 취소" : "준비하기";
        if (readyButton != null && readyButton.targetGraphic is Image image)
            image.color = ready ? new Color32(255, 208, 130, 255) : new Color32(91, 155, 235, 255);
        if (readyLabel != null) readyLabel.color = ready ? new Color32(150, 100, 30, 255) : Color.white;
    }

    private void OnReadyClicked()
    {
        SetReady(!localReady);
        ReadyToggled?.Invoke(localReady);
    }

    /// <summary>맵 고르기 화살표를 누를 수 있는지. 고를 맵이 하나뿐이면 끈다.</summary>
    public void SetMapSelectable(bool selectable)
    {
        if (prevMapButton != null) prevMapButton.interactable = selectable;
        if (nextMapButton != null) nextMapButton.interactable = selectable;
    }

    /// <summary>
    /// 시작 버튼을 켤 수 있는지 판단한다.
    /// 방장만 시작할 수 있고, 2명 이상이며, 방장 외 모든 플레이어가 준비를 마쳤어야 한다. 팀전이면 두 팀 인원도 같아야 한다.
    /// </summary>
    private void UpdateStartState()
    {
        int total = 0, red = 0, blue = 0;
        bool allReady = true;
        foreach (var slot in slotData)
        {
            if (slot.IsEmpty) continue;
            total++;
            if (!slot.IsHost && !slot.IsReady) allReady = false;
            if (slot.Team == Team.Red) red++;
            else if (slot.Team == Team.Blue) blue++;
        }

        bool ok = isHost && total >= 2 && allReady && (!isTeamMode || red == blue);
        if (startButton != null) startButton.interactable = ok;
        UpdateTeamButtons();
        RefreshMaxPlayers(); // 인원이 바뀌면 고를 수 있는 최대 인원도 달라진다
    }

    // 내가 속한 팀 버튼은 또렷하게, 다른 팀 버튼은 흐리게 해서 어느 팀을 골랐는지 한눈에 보이게 한다
    private void UpdateTeamButtons()
    {
        Team mine = slotData[localSlot].IsEmpty ? Team.None : slotData[localSlot].Team;
        DimTeamButton(redTeamButton, mine != Team.Red);
        DimTeamButton(blueTeamButton, mine != Team.Blue);
    }

    private static void DimTeamButton(Button button, bool dim)
    {
        if (button == null) return;
        float alpha = dim ? 0.5f : 1f;
        if (button.targetGraphic is Image image)
        {
            var c = image.color; c.a = alpha; image.color = c;
        }
        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            var c = label.color; c.a = alpha; label.color = c;
        }
    }

    private void OnTeamToggleChanged(bool isOn)
    {
        SetMode(isOn);
        if (isOn) RecountTeams();
        ModeChanged?.Invoke(isOn);
    }

    // ───────────── 팀 선택 ─────────────

    // 아직 SetSlot 하지 않은 자리는 빈 자리로 센다
    private readonly Slot[] slotData =
    {
        new Slot { IsEmpty = true }, new Slot { IsEmpty = true },
        new Slot { IsEmpty = true }, new Slot { IsEmpty = true },
    };
    private int localSlot;

    /// <summary>이 화면의 플레이어가 몇 번째 슬롯인지(0~3). 팀 버튼은 이 슬롯의 팀을 바꾼다.</summary>
    public void SetLocalPlayer(int slotIndex) => localSlot = Mathf.Clamp(slotIndex, 0, slotData.Length - 1);

    /// <summary>현재 슬롯들의 팀으로 인원 수와 팀 버튼 상태를 다시 계산한다.</summary>
    public void RecountTeams()
    {
        int red = 0, blue = 0;
        foreach (var slot in slotData)
        {
            if (slot.IsEmpty) continue;
            if (slot.Team == Team.Red) red++;
            else if (slot.Team == Team.Blue) blue++;
        }
        SetTeamCounts(red, blue);
    }

    private void ChangeLocalTeam(Team team, Action onChanged)
    {
        if (!isTeamMode) return;
        var slot = slotData[localSlot];
        if (slot.IsEmpty || slot.Team == team) return;

        slot.Team = team; // 팀을 고를 때 인원 제한은 없다. 인원이 같아야 하는 건 게임을 시작할 때뿐이다.
        SetSlot(localSlot, slot);
        RecountTeams();

        // 바뀐 팀 배지가 톡 튄다
        var badge = slots[localSlot] != null && slots[localSlot].teamBadge != null ? slots[localSlot].teamBadge.transform : null;
        if (badge != null)
        {
            badge.DOKill(true);
            badge.DOPunchScale(Vector3.one * 0.25f, 0.3f, 6, 0.6f).SetUpdate(true).SetLink(badge.gameObject);
        }
        onChanged?.Invoke();
    }
}
