using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>상단바: 방 정보, 라운드 수, 설정·나가기 버튼. Canvas_TopBar에 붙는다.</summary>
public class TopBarView : UIView
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject passwordChip;
    [SerializeField] private TMP_Text passwordText;
    [SerializeField] private GameObject turnInfo;

    public override void Bind()
    {
        roomNameText = Find<TMP_Text>("TopBar/RoomInfo/RoomNameText");
        roomCodeText = Find<TMP_Text>("TopBar/RoomInfo/RoomCodeChip/RoomCodeText");
        playerCountText = Find<TMP_Text>("TopBar/RoomInfo/PlayerCountChip/PlayerCountText");
        passwordChip = FindObject("TopBar/RoomInfo/PasswordChip");
        passwordText = Find<TMP_Text>("TopBar/RoomInfo/PasswordChip/PasswordText");
        turnInfo = FindObject("TopBar/TurnInfo");
        turnText = Find<TMP_Text>("TopBar/TurnInfo/TurnText");
        settingsButton = Find<Button>("TopBar/Buttons/SettingsButton");
        exitButton = Find<Button>("TopBar/Buttons/ExitButton");
    }

    public void SetRoom(string roomName, string roomCode)
    {
        if (roomNameText != null) roomNameText.text = roomName;
        if (roomCodeText != null) roomCodeText.text = $"방 코드  {roomCode}";
    }

    public void SetPlayerCount(int current, int max)
    {
        if (playerCountText != null) playerCountText.text = $"{current} / {max}명";
    }

    /// <summary>비밀번호 방이면 인원수 옆에 비밀번호를 보여준다. 비어 있으면 칩을 숨긴다.</summary>
    public void SetPassword(string password)
    {
        bool has = !string.IsNullOrEmpty(password);
        SetActive(passwordChip, has);
        if (has && passwordText != null) passwordText.text = $"비밀번호  {password}";
    }

    /// <summary>라운드 표시를 보일지. 게임이 시작되기 전에는 숨긴다.</summary>
    public void SetRoundVisible(bool visible) => SetActive(turnInfo, visible);

    // 현재 라운드 / 최대 라운드를 표시합니다. (turnText는 씬 오브젝트 연결용 이름이라 그대로 둠)
    public void SetRound(int round, int maxRound)
    {
        if (turnText != null) turnText.text = $"{round} <size=70%><color=#C9A770>/ {maxRound}</color></size>";
    }

    public void SetCallbacks(Action onSettings, Action onExit)
    {
        SetOnClick(settingsButton, onSettings);
        SetOnClick(exitButton, onExit);
    }
}
