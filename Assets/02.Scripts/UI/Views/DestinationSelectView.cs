using TMPro;
using UnityEngine;

/// <summary>
/// 목적지 선택 안내 배너. Canvas_DestinationSelect에 붙는다.
/// 칸 선택은 보드에서 하고, 선택되면 Close를 부른다.
/// </summary>
public class DestinationSelectView : UIView
{
    public override string PanelPath => "DestinationSelectPopup/Window";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;

    public override void Bind()
    {
        titleText = Find<TMP_Text>("DestinationSelectPopup/Window/TitleText");
        hintText = Find<TMP_Text>("DestinationSelectPopup/Window/HintText");
    }

    public void Show(string title = "세계여행", string hint = "보드에서 이동할 칸을 선택하세요")
    {
        if (titleText != null) titleText.text = title;
        if (hintText != null) hintText.text = hint;
        Open();
    }
}
