using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>보드 칸을 눌렀을 때의 정보 팝업. Canvas_TileInfo에 붙는다.</summary>
public class TileInfoView : UIView
{
    public override string PanelPath => "TileInfoPopup";

    [SerializeField] private Image tileImage;
    [SerializeField] private GameObject ownerChip;
    [SerializeField] private TMP_Text ownerText;
    [SerializeField] private TMP_Text tileNameText;
    [SerializeField] private StageTrack stageTrack = new StageTrack();
    [SerializeField] private GameObject[] rowHighlights = new GameObject[4];
    [SerializeField] private TMP_Text[] tollTexts = new TMP_Text[4];
    [SerializeField] private RectTransform currentTag;
    [SerializeField] private Button closeButton;

    public override void Bind()
    {
        const string w = "TileInfoPopup/";
        tileImage = Find<Image>(w + "TilePortrait/TileImage");
        ownerChip = FindObject(w + "TilePortrait/OwnerChip");
        ownerText = Find<TMP_Text>(w + "TilePortrait/OwnerChip/OwnerText");
        tileNameText = Find<TMP_Text>(w + "TileNameText");
        stageTrack.Bind(transform.Find(w + "StageTrack"));
        for (int i = 0; i < 4; i++)
        {
            rowHighlights[i] = FindObject($"{w}TollTable/Row_{i}/CurrentHighlight");
            tollTexts[i] = Find<TMP_Text>($"{w}TollTable/Row_{i}/TollText");
            var tag = transform.Find($"{w}TollTable/Row_{i}/CurrentTag");
            if (tag != null) currentTag = (RectTransform)tag;
        }
        closeButton = Find<Button>(w + "CloseButton");
    }

    private void Awake()
    {
        SetOnClick(closeButton, Close);
    }

    // 창 안의 텍스트가 쓰는 Bold / Light 폰트를 찾아 쓴다 (따로 연결하지 않아도 되게)
    private TMP_FontAsset boldFont, lightFont;
    private TMP_FontAsset BoldFont { get { FindFonts(); return boldFont; } }
    private TMP_FontAsset LightFont { get { FindFonts(); return lightFont; } }

    private void FindFonts()
    {
        if (boldFont != null && lightFont != null) return;
        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.font == null) continue;
            if (boldFont == null && t.font.name.Contains("Bold")) boldFont = t.font;
            if (lightFont == null && t.font.name.Contains("Light")) lightFont = t.font;
        }
    }

    /// <summary>
    /// ownerName이 null이면 주인 없는 땅(칩 숨김, 현재 단계 강조 없음).
    /// tolls: 건물·★·★★·★★★ 단계별 이용료 4개.
    /// </summary>
    public void Show(string tileName, Sprite image, string ownerName, int ownerColorIndex,
                     BuildingLevel level, long[] tolls)
    {
        if (image != null && tileImage != null) { tileImage.sprite = image; tileImage.color = Color.white; }
        if (tileNameText != null) tileNameText.text = tileName;

        bool owned = !string.IsNullOrEmpty(ownerName);
        SetActive(ownerChip, owned);
        if (owned && ownerText != null)
            ownerText.text = $"<color={UIPalette.Hex(UIPalette.PlayerColor(ownerColorIndex))}>●</color> {ownerName} 님의 땅";
        stageTrack.Set(owned ? level : BuildingLevel.Land);

        int current = owned ? (int)level : -1;
        for (int i = 0; i < 4; i++)
        {
            bool isCurrent = i == current;
            SetActive(rowHighlights[i], isCurrent);
            if (tollTexts[i] == null) continue;
            tollTexts[i].text = tolls != null && i < tolls.Length ? UIPalette.Money(tolls[i]) : "-";
            tollTexts[i].color = isCurrent ? UIPalette.Red : UIPalette.Ink;
            tollTexts[i].fontSize = isCurrent ? 22 : 20;
            // 현재 단계는 굵게, 나머지는 얇게
            var font = isCurrent ? BoldFont : LightFont;
            if (font != null) tollTexts[i].font = font;
        }

        // "현재" 태그를 현재 단계 줄로 옮긴다
        if (currentTag != null)
        {
            currentTag.gameObject.SetActive(current >= 0);
            if (current >= 0 && rowHighlights[current] != null)
            {
                Vector2 pos = currentTag.anchoredPosition;
                currentTag.SetParent(rowHighlights[current].transform.parent, false);
                currentTag.anchoredPosition = pos;
            }
        }
        Open();
    }
}
