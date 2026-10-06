using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 팝업. Canvas_Settings에 붙는다. 로비와 게임 씬이 같은 프리팹을 쓴다.
/// 값은 PlayerPrefs에 저장만 하고, 실제 오디오 연결은 사운드 시스템이 생기면 아래 정적 값을 읽어 쓰면 된다.
/// </summary>
public class SettingsView : UIView
{
    public override string PanelPath => "SettingsPopup/Window";
    public override string DimPath => "SettingsPopup/Dim";

    private const string BgmKey = "Settings.Bgm";
    private const string SfxKey = "Settings.Sfx";
    private const string BgmOnKey = "Settings.BgmOn";
    private const string SfxOnKey = "Settings.SfxOn";

    private static readonly Color SwitchOn = UIPalette.Mint;
    private static readonly Color SwitchOff = new Color32(205, 210, 224, 255);

    /// <summary>스위치와 슬라이더 한 줄(배경음 또는 효과음).</summary>
    [System.Serializable]
    private class Channel
    {
        public Slider slider;
        public TMP_Text valueText;
        public Toggle toggle;
        public Image switchTrack;
        public RectTransform switchKnob;
        public CanvasGroup sliderGroup;
        public CanvasGroup valueGroup;
    }

    [SerializeField] private Channel bgm = new Channel();
    [SerializeField] private Channel sfx = new Channel();
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    /// <summary>소리 설정. 사운드 시스템이 읽어 가는 값이다. 꺼져 있으면 볼륨은 0으로 본다.</summary>
    public static float BgmVolume => PlayerPrefs.GetInt(BgmOnKey, 1) == 1 ? PlayerPrefs.GetFloat(BgmKey, 0.7f) : 0f;
    public static float SfxVolume => PlayerPrefs.GetInt(SfxOnKey, 1) == 1 ? PlayerPrefs.GetFloat(SfxKey, 0.7f) : 0f;

    public override void Bind()
    {
        const string w = "SettingsPopup/Window/";
        BindChannel(bgm, w + "BgmSection/", "BgmSlider", "BgmToggle");
        BindChannel(sfx, w + "SfxSection/", "SfxSlider", "SfxToggle");
        confirmButton = Find<Button>(w + "ConfirmButton");
        closeButton = Find<Button>(w + "CloseButton");
    }

    private void BindChannel(Channel c, string section, string sliderName, string toggleName)
    {
        c.slider = Find<Slider>(section + sliderName);
        c.valueText = Find<TMP_Text>(section + "ValueChip/ValueText");
        c.toggle = Find<Toggle>(section + toggleName);
        c.switchTrack = c.toggle != null ? c.toggle.GetComponent<Image>() : null;
        c.switchKnob = c.toggle != null ? c.toggle.transform.Find("Knob") as RectTransform : null;
        c.sliderGroup = c.slider != null ? UIFx.EnsureGroup(c.slider.gameObject) : null;
        var chip = transform.Find(section + "ValueChip");
        c.valueGroup = chip != null ? UIFx.EnsureGroup(chip.gameObject) : null;
    }

    private void Awake()
    {
        SetupChannel(bgm, BgmKey, BgmOnKey);
        SetupChannel(sfx, SfxKey, SfxOnKey);
        SetOnClick(confirmButton, Close);
        SetOnClick(closeButton, Close);

        // 창 밖(어두운 배경)을 눌러도 닫힌다. 배경이 상단바를 덮고 있어서, 열려 있을 때 설정 버튼을 다시 눌러도 이 배경이 받아 닫힌다.
        var dim = transform.Find(DimPath);
        if (dim != null)
        {
            var dimButton = dim.GetComponent<Button>();
            if (dimButton == null) dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Close);
        }
    }

    private void SetupChannel(Channel c, string volumeKey, string onKey)
    {
        if (c.slider == null || c.toggle == null) return;

        c.slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(volumeKey, 0.7f));
        UpdateValueText(c);
        c.slider.onValueChanged.RemoveAllListeners();
        c.slider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(volumeKey, v);
            UpdateValueText(c);
        });

        c.toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(onKey, 1) == 1);
        ApplySwitch(c, c.toggle.isOn, false);
        c.toggle.onValueChanged.RemoveAllListeners();
        c.toggle.onValueChanged.AddListener(on =>
        {
            PlayerPrefs.SetInt(onKey, on ? 1 : 0);
            ApplySwitch(c, on, true);
        });
    }

    private static void UpdateValueText(Channel c)
    {
        if (c.valueText != null) c.valueText.text = $"{Mathf.RoundToInt(c.slider.value * 100)}%";
    }

    /// <summary>스위치를 켜면 소리가 나고, 끄면 음소거 + 슬라이더와 값이 흐려져 못 만진다.</summary>
    private static void ApplySwitch(Channel c, bool on, bool animate)
    {
        if (c.switchTrack != null) c.switchTrack.color = on ? SwitchOn : SwitchOff;
        if (c.switchKnob != null)
        {
            float knobX = on ? 46f : 18f;
            c.switchKnob.DOKill();
            if (animate)
                c.switchKnob.DOAnchorPosX(knobX, 0.15f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(c.switchKnob.gameObject);
            else
                c.switchKnob.anchoredPosition = new Vector2(knobX, c.switchKnob.anchoredPosition.y);
        }
        foreach (var group in new[] { c.sliderGroup, c.valueGroup })
        {
            if (group == null) continue;
            group.interactable = on;
            group.alpha = on ? 1f : 0.45f;
        }
    }

    /// <summary>열려 있으면 닫고, 닫혀 있으면 연다. 상단바 설정 버튼용.</summary>
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }
}
