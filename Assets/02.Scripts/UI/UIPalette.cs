using UnityEngine;

/// <summary>
/// UI 공통 색상과 금액 표기. 씬에 만들어 둔 UI와 같은 값을 쓴다.
/// </summary>
public static class UIPalette
{
    // 플레이어 색 (1~4번: 핑크, 파랑, 민트, 노랑)
    public static readonly Color[] Player =
    {
        new Color32(255, 163, 175, 255), new Color32(150, 196, 255, 255),
        new Color32(140, 220, 184, 255), new Color32(255, 208, 130, 255)
    };

    // 플레이어 색의 옅은 버전 (카드 배경, 초상화 자리)
    public static readonly Color[] PlayerLight =
    {
        new Color32(255, 236, 239, 255), new Color32(230, 241, 255, 255),
        new Color32(226, 247, 237, 255), new Color32(255, 244, 224, 255)
    };

    // 등수 배지 (1위 금, 2위 은, 3위 동, 4위 회색)
    public static readonly Color[] Rank =
    {
        new Color32(255, 214, 102, 255), new Color32(208, 218, 232, 255),
        new Color32(244, 190, 158, 255), new Color32(226, 226, 236, 255)
    };

    public static readonly Color Ink = new Color32(74, 78, 105, 255);
    public static readonly Color InkSub = new Color32(140, 144, 168, 255);
    public static readonly Color Red = new Color32(255, 90, 110, 255);
    public static readonly Color StarEmpty = new Color32(226, 229, 238, 255);
    public static readonly Color Gold = new Color32(255, 209, 102, 255);

    // 돈 증가 / 감소
    public static readonly Color GainBg = new Color32(227, 246, 236, 255);
    public static readonly Color GainStroke = new Color32(180, 228, 203, 255);
    public static readonly Color GainText = new Color32(47, 158, 106, 255);
    public static readonly Color LossBg = new Color32(255, 232, 236, 255);
    public static readonly Color LossStroke = new Color32(255, 196, 204, 255);
    public static readonly Color LossText = new Color32(229, 72, 96, 255);

    public static Color PlayerColor(int index) => Player[Mathf.Clamp(index, 0, Player.Length - 1)];
    public static Color PlayerLightColor(int index) => PlayerLight[Mathf.Clamp(index, 0, PlayerLight.Length - 1)];

    /// <summary>1,234,000</summary>
    public static string Money(long amount) => amount.ToString("N0");

    /// <summary>+1,234,000 / -1,234,000</summary>
    public static string SignedMoney(long amount) => (amount >= 0 ? "+" : "-") + System.Math.Abs(amount).ToString("N0");

    public static string Hex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);
}
