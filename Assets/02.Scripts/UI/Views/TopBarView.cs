using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>상단바: 방 정보, 턴 수, 설정·나가기 버튼. Canvas_TopBar에 붙는다.</summary>
public class TopBarView : UIView
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Button roomSetupButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    public override void Bind()
    {
        roomNameText = Find<TMP_Text>("TopBar/RoomInfo/RoomNameText");
        roomCodeText = Find<TMP_Text>("TopBar/RoomInfo/RoomCodeChip/RoomCodeText");
        playerCountText = Find<TMP_Text>("TopBar/RoomInfo/PlayerCountChip/PlayerCountText");
        turnText = Find<TMP_Text>("TopBar/TurnInfo/TurnText");
        roomSetupButton = Find<Button>("TopBar/Buttons/RoomSetupButton");
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

    public void SetTurn(int turn, int maxTurn)
    {
        if (turnText != null) turnText.text = $"{turn} <size=70%><color=#C9A770>/ {maxTurn}</color></size>";
    }

    public void SetCallbacks(Action onSettings, Action onExit)
    {
        SetOnClick(settingsButton, onSettings);
        SetOnClick(exitButton, onExit);
    }

    /// <summary>방 설정 아이콘 버튼. UIManager가 방 설정 팝업 열기/닫기로 연결한다.</summary>
    public void SetRoomSetupCallback(Action onRoomSetup) => SetOnClick(roomSetupButton, onRoomSetup);
}
