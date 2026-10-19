using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 초상화 고르기 팝업. 로비에서 내 정보의 초상화를 누르면 열린다. Canvas_PortraitPicker에 붙는다.
/// 칸 목록은 PortraitLibrary의 그림 수만큼 견본(ItemTemplate)을 복제해 만든다.
/// </summary>
public class PortraitPickerView : UIView
{
    public override string PanelPath => "PortraitPickerPopup/Window";
    public override string DimPath => "PortraitPickerPopup/Dim";

    private static readonly Color RingOff = new Color32(226, 229, 238, 255);

    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private RectTransform itemTemplate;
    [SerializeField] private Button closeButton;

    private class Item
    {
        public Button Button;
        public Image Ring;
        public GameObject Tag;
    }

    private readonly List<Item> items = new List<Item>();
    private Action<int> onPicked;

    public override void Bind()
    {
        const string w = "PortraitPickerPopup/Window/";
        gridRoot = Find<RectTransform>(w + "Grid");
        itemTemplate = Find<RectTransform>(w + "Grid/ItemTemplate");
        closeButton = Find<Button>(w + "CloseButton");
    }

    private void Awake()
    {
        SetOnClick(closeButton, Close);
        if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
    }

    /// <summary>selected: 지금 고른 번호. onPicked: 칸을 누르면 그 번호로 호출한다. (창은 바로 닫힌다)</summary>
    public void Show(int selected, Action<int> onPicked)
    {
        this.onPicked = onPicked;
        BuildItems();
        Mark(selected);
        Open();
    }

    private void BuildItems()
    {
        if (gridRoot == null || itemTemplate == null) return;
        int count = PortraitLibrary.Count;
        while (items.Count < count)
        {
            int index = items.Count;
            var go = Instantiate(itemTemplate, gridRoot);
            go.gameObject.SetActive(true);
            go.name = "Item_" + index;
            var item = new Item
            {
                Button = go.GetComponent<Button>(),
                Ring = go.Find("Ring").GetComponent<Image>(),
                Tag = go.Find("SelectedTag").gameObject,
            };
            var art = go.Find("PortraitMask/PortraitImage").GetComponent<Image>();
            art.sprite = PortraitLibrary.Get(index);
            art.color = Color.white; // 견본에 남아 있는 분홍 자리표시 색이 그림을 물들이지 않게 한다
            SetOnClick(item.Button, () => Pick(index));
            items.Add(item);
        }
    }

    private void Mark(int selected)
    {
        for (int i = 0; i < items.Count; i++)
        {
            bool on = i == selected;
            items[i].Ring.color = on ? UIPalette.Mint : RingOff;
            items[i].Tag.SetActive(on);
        }
    }

    private void Pick(int index)
    {
        Mark(index);
        onPicked?.Invoke(index);
        Close();
    }
}
