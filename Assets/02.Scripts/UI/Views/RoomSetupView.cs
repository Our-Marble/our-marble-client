using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>게임 시작 전 방 설정 팝업. Canvas_RoomSetup에 붙는다.</summary>
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
    private static readonly Color NeutralBg = new Color32(238, 241, 247, 255);
    private static readonly Color ReadyBg = new Color32(207, 242, 224, 255);
    private static readonly Color ReadyText = new Color32(63, 168, 119, 255);

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
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private SlotRefs[] slots = new SlotRefs[4];
    [SerializeField] private GameObject teamSection;
    [SerializeField] private Button redTeamButton;
    [SerializeField] private TMP_Text redTeamLabel;
    [SerializeField] private Button blueTeamButton;
    [SerializeField] private TMP_Text blueTeamLabel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;

    /// <summary>모드 토글이 바뀌면 호출. true면 팀전.</summary>
    public event Action<bool> ModeChanged;

    private bool isTeamMode;

    public override void Bind()
    {
        const string w = "RoomSetupPopup/Window/";
        mapImage = Find<Image>(w + "MapSection/MapPreview/MapImage");
        mapNameText = Find<TMP_Text>(w + "MapSection/MapSelector/MapNameText");
        prevMapButton = Find<Button>(w + "MapSection/MapSelector/PrevMapButton");
        nextMapButton = Find<Button>(w + "MapSection/MapSelector/NextMapButton");
        soloToggle = Find<Toggle>(w + "ModeSection/ModeToggle/SoloToggle");
        teamToggle = Find<Toggle>(w + "ModeSection/ModeToggle/TeamToggle");
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
        closeButton = Find<Button>(w + "CloseButton");
    }

    private void Awake()
    {
        if (teamToggle != null) teamToggle.onValueChanged.AddListener(OnTeamToggleChanged);
        SetOnClick(closeButton, Close);
        // 팀 버튼은 Show를 거치지 않고 열어도(상단바 버튼) 동작해야 해서 여기서 연결한다
        SetOnClick(redTeamButton, () => ChangeLocalTeam(Team.Red, onRedTeamChanged));
        SetOnClick(blueTeamButton, () => ChangeLocalTeam(Team.Blue, onBlueTeamChanged));
    }

    private Action onRedTeamChanged;
    private Action onBlueTeamChanged;

    /// <summary>상단바 방 설정 버튼용. 열려 있으면 닫고, 닫혀 있으면 마지막 내용 그대로 다시 연다.</summary>
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
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
    }

    public void SetSlot(int index, Slot slot)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null) return;
        slotData[index] = slot;
        var r = slots[index];

        Color color = UIPalette.PlayerColor(slot.ColorIndex);
        if (r.background != null) r.background.color = slot.IsEmpty ? NeutralBg : UIPalette.PlayerLightColor(slot.ColorIndex);
        if (r.stroke != null) r.stroke.color = slot.IsEmpty ? NeutralBg : color;
        if (r.portraitRing != null) r.portraitRing.color = slot.IsEmpty ? NeutralBg : color;
        SetPortrait(r.portraitImage, slot.IsEmpty ? null : slot.Portrait, Color.white);
        if (r.nicknameText != null) r.nicknameText.text = slot.IsEmpty ? "빈 자리" : slot.Name;

        SetActive(r.hostBadge, !slot.IsEmpty && slot.IsHost);

        // 방장은 준비 표시가 없다
        bool showReady = !slot.IsEmpty && !slot.IsHost;
        if (r.readyBadge != null)
        {
            r.readyBadge.gameObject.SetActive(showReady);
            r.readyBadge.color = slot.IsReady ? ReadyBg : NeutralBg;
        }
        if (r.readyText != null)
        {
            r.readyText.text = slot.IsReady ? "준비 완료" : "대기 중";
            r.readyText.color = slot.IsReady ? ReadyText : UIPalette.InkSub;
        }

        if (r.teamBadge != null)
        {
            r.teamBadge.gameObject.SetActive(isTeamMode && !slot.IsEmpty);
            r.teamBadge.color = slot.Team == Team.Red ? RedTeamBg : slot.Team == Team.Blue ? BlueTeamBg : NeutralBg;
        }
        if (r.teamText != null)
        {
            r.teamText.text = slot.Team == Team.Red ? "레드팀" : slot.Team == Team.Blue ? "블루팀" : "팀 미선택";
            r.teamText.color = slot.Team == Team.Red ? RedTeamText : slot.Team == Team.Blue ? BlueTeamText : UIPalette.InkSub;
        }
    }

    public void SetPlayerCount(int current, int max)
    {
        if (playerCountText != null) playerCountText.text = $"{current} / {max}";
    }

    public void SetTeamCounts(int red, int blue, int maxPerTeam)
    {
        this.maxPerTeam = maxPerTeam;
        if (redTeamLabel != null) redTeamLabel.text = $"레드팀 <size=75%><color=#E8A0AC>{red} / {maxPerTeam}</color></size>";
        if (blueTeamLabel != null) blueTeamLabel.text = $"블루팀 <size=75%><color=#9DBBE8>{blue} / {maxPerTeam}</color></size>";
        if (redTeamButton != null) redTeamButton.interactable = red < maxPerTeam;
        if (blueTeamButton != null) blueTeamButton.interactable = blue < maxPerTeam;
    }

    /// <summary>방장만, 그리고 모두 준비됐을 때만 켠다.</summary>
    public void SetStartInteractable(bool interactable)
    {
        if (startButton != null) startButton.interactable = interactable;
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
    private int maxPerTeam = 2;

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
        SetTeamCounts(red, blue, maxPerTeam);
    }

    private void ChangeLocalTeam(Team team, Action onChanged)
    {
        if (!isTeamMode) return;
        var slot = slotData[localSlot];
        if (slot.IsEmpty || slot.Team == team) return;

        // 옮겨 갈 팀이 이미 꽉 찼으면 바꾸지 않는다
        int members = 0;
        for (int i = 0; i < slotData.Length; i++)
            if (i != localSlot && !slotData[i].IsEmpty && slotData[i].Team == team) members++;
        if (members >= maxPerTeam) return;

        slot.Team = team;
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
