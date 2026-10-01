using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>남의 땅 인수 여부 팝업. Canvas_TakeoverPopup에 붙는다.</summary>
public class TakeoverPopupView : UIView
{
    public override string PanelPath => "TakeoverPopup/Window";
    public override string DimPath => "TakeoverPopup/Dim";

    [SerializeField] private Image tileImage;
    [SerializeField] private TMP_Text ownerText;
    [SerializeField] private TMP_Text tileNameText;
    [SerializeField] private StageTrack stageTrack = new StageTrack();
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button takeoverButton;

    public override void Bind()
    {
        const string w = "TakeoverPopup/Window/";
        tileImage = Find<Image>(w + "TilePortrait/TileImage");
        ownerText = Find<TMP_Text>(w + "TilePortrait/OwnerChip/OwnerText");
        tileNameText = Find<TMP_Text>(w + "TileNameText");
        stageTrack.Bind(transform.Find(w + "StageTrack"));
        priceText = Find<TMP_Text>(w + "InfoPanel/TakeoverRow/ValueText");
        cashText = Find<TMP_Text>(w + "InfoPanel/CashRow/ValueText");
        cancelButton = Find<Button>(w + "CancelButton");
        takeoverButton = Find<Button>(w + "TakeoverButton");
    }

    public void Show(string tileName, Sprite image, BuildingLevel level,
                     string ownerName, int ownerColorIndex,
                     long takeoverPrice, long cash, Action onTakeover, Action onCancel)
    {
        if (image != null && tileImage != null) { tileImage.sprite = image; tileImage.color = Color.white; }
        if (tileNameText != null) tileNameText.text = tileName;
        if (ownerText != null)
            ownerText.text = $"<color={UIPalette.Hex(UIPalette.PlayerColor(ownerColorIndex))}>●</color> {ownerName} 님의 땅";
        stageTrack.Set(level);

        if (priceText != null) priceText.text = UIPalette.Money(takeoverPrice);
        if (cashText != null) cashText.text = UIPalette.Money(cash);

        if (takeoverButton != null) takeoverButton.interactable = cash >= takeoverPrice;
        SetOnClick(takeoverButton, () => { Close(); onTakeover?.Invoke(); });
        SetOnClick(cancelButton, () => { Close(); onCancel?.Invoke(); });
        Open();
    }
}
