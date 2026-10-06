using UnityEngine;

/// <summary>
/// 데이터를 직접 다루지 않는 단순 팝업(방 만들기, 코드 입장, 비밀번호 입력 등)의 열기·닫기 연출.
/// 창과 어두운 배경의 경로만 인스펙터에 적어 두면 UIView의 튀어나오는 연출을 그대로 쓴다.
/// 안의 버튼과 입력칸은 이 팝업을 쓰는 매니저가 직접 연결한다.
/// </summary>
public class PopupView : UIView
{
    [SerializeField] private string panelPath;
    [SerializeField] private string dimPath;

    public override string PanelPath => string.IsNullOrEmpty(panelPath) ? null : panelPath;
    public override string DimPath => string.IsNullOrEmpty(dimPath) ? null : dimPath;

    /// <summary>튀어나오는 창. 안의 요소를 찾을 때 기준이 된다.</summary>
    public RectTransform Window => Panel;

    public override void Bind() { }
}
