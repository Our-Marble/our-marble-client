using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>나가기 확인 팝업. 게임에서는 로비로, 로비에서는 방 밖이나 로그인 화면으로 나갈지 묻는다. 문구는 부르는 쪽이 정한다. Canvas_ExitConfirm에 붙는다.</summary>
public class ExitConfirmView : UIView
{
    public override string PanelPath => "ExitConfirmPopup/Window";
    public override string DimPath => "ExitConfirmPopup/Dim";

    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text messageText;

    public override void Bind()
    {
        const string w = "ExitConfirmPopup/Window/";
        confirmButton = Find<Button>(w + "ConfirmButton");
        cancelButton = Find<Button>(w + "CancelButton");
        messageText = Find<TMP_Text>(w + "MessageText");
    }

    /// <param name="onConfirm">나가기를 눌렀을 때. 창을 닫은 뒤 호출된다.</param>
    /// <param name="message">안내 문장. 비우면 프리팹에 적힌 문장을 쓴다.</param>
    public void Show(Action onConfirm, string message = null)
    {
        // 이미 만들어진 프리팹에는 이 칸이 연결돼 있지 않을 수 있어 처음 쓸 때 찾는다
        if (messageText == null) messageText = Find<TMP_Text>("ExitConfirmPopup/Window/MessageText");
        if (message != null && messageText != null) messageText.text = message;
        SetOnClick(confirmButton, () => { Close(); onConfirm?.Invoke(); });
        SetOnClick(cancelButton, Close);
        Open();
    }
}
