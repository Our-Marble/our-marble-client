using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 땅 매각 팝업. Canvas_SellPopup에 붙는다.
/// 땅 선택은 보드에서 하고, 선택이 바뀔 때마다 SetSelectedTotal로 합계를 넘긴다.
/// </summary>
public class SellPopupView : UIView
{
    public override string PanelPath => "SellPopup/Window";

    [SerializeField] private TMP_Text shortfallText;
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private Button autoSelectButton;
    [SerializeField] private Button bankLoanButton;
    [SerializeField] private Button bankruptButton;
    [SerializeField] private Button sellCompleteButton;

    private long requiredAmount;
    private long cash;

    public override void Bind()
    {
        const string w = "SellPopup/Window/";
        shortfallText = Find<TMP_Text>(w + "SummaryPanel/ShortfallRow/ValueText");
        cashText = Find<TMP_Text>(w + "SummaryPanel/CashRow/ValueText");
        autoSelectButton = Find<Button>(w + "AutoSelectButton");
        bankLoanButton = Find<Button>(w + "BankLoanButton");
        bankruptButton = Find<Button>(w + "BankruptButton");
        sellCompleteButton = Find<Button>(w + "SellCompleteButton");
    }

    /// <summary>requiredAmount: 내야 할 금액. loanAvailable: 은행 대출(1회)을 아직 안 썼는지.</summary>
    public void Show(long requiredAmount, long cash, bool loanAvailable,
                     Action onAutoSelect, Action onBankLoan, Action onBankrupt, Action onSellComplete)
    {
        this.requiredAmount = requiredAmount;
        this.cash = cash;
        CashRoll.Set(cash, animate: false);
        if (bankLoanButton != null) bankLoanButton.interactable = loanAvailable;

        SetOnClick(autoSelectButton, onAutoSelect);
        SetOnClick(bankLoanButton, onBankLoan);
        SetOnClick(bankruptButton, onBankrupt);
        SetOnClick(sellCompleteButton, () => { Close(); onSellComplete?.Invoke(); });

        SetSelectedTotal(0, animate: false);
        Open();
    }

    /// <summary>보드에서 고른 땅들의 매각가 합계. 부족한 금액을 굴려서 바꾸고 매각 완료 버튼을 갱신한다.</summary>
    public void SetSelectedTotal(long selectedTotal, bool animate = true)
    {
        long balance = cash + selectedTotal - requiredAmount;
        ShortfallRoll.Set(balance, animate);
        if (sellCompleteButton != null) sellCompleteButton.interactable = balance >= 0;
    }

    /// <summary>대출 후 현금이 늘었을 때.</summary>
    public void SetCash(long newCash, long selectedTotal)
    {
        cash = newCash;
        CashRoll.Set(cash);
        SetSelectedTotal(selectedTotal);
    }

    // 굴러가는 도중 0을 넘는 순간 색도 바뀐다
    private RollingNumber shortfallRoll;
    private RollingNumber cashRoll;
    private RollingNumber ShortfallRoll => shortfallRoll ??= new RollingNumber(shortfallText, UIPalette.SignedMoney,
        v => { if (shortfallText != null) shortfallText.color = v < 0 ? UIPalette.Red : UIPalette.GainText; });
    private RollingNumber CashRoll => cashRoll ??= new RollingNumber(cashText, UIPalette.Money);

    public void SetLoanAvailable(bool available)
    {
        if (bankLoanButton != null) bankLoanButton.interactable = available;
    }
}
