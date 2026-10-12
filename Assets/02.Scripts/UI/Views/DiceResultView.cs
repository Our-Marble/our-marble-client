using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// 주사위 결과(두 눈 + 합계 + 더블 표시). Canvas_DiceResult에 붙는다.
/// 열면 주사위가 흔들리며 눈이 바뀌다가 멈추고, 합계와 더블 표시가 차례로 나온다.
/// </summary>
public class DiceResultView : UIView
{
    public override string PanelPath => "DiceResult";

    // 눈 배치 순서: TL, TR, ML, C, MR, BL, BR
    private static readonly string[] PipNames = { "Pip_TL", "Pip_TR", "Pip_ML", "Pip_C", "Pip_MR", "Pip_BL", "Pip_BR" };
    private static readonly bool[][] Faces =
    {
        null,
        new[] { false, false, false, true,  false, false, false }, // 1
        new[] { true,  false, false, false, false, false, true  }, // 2
        new[] { true,  false, false, true,  false, false, true  }, // 3
        new[] { true,  true,  false, false, false, true,  true  }, // 4
        new[] { true,  true,  false, true,  false, true,  true  }, // 5
        new[] { true,  true,  true,  false, true,  true,  true  }, // 6
    };

    [SerializeField] private GameObject[] die1Pips = new GameObject[7];
    [SerializeField] private GameObject[] die2Pips = new GameObject[7];
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private GameObject doubleBadge;
    [SerializeField] private GameObject doubleSubText;
    [SerializeField] private GameObject doubleHighlight;

    [Header("굴림 연출")]
    [Tooltip("눈이 바뀌는 시간(초). 뒤로 갈수록 느려진다.")]
    [SerializeField] private float rollSeconds = 0.8f;
    [SerializeField] private int rollSteps = 10;
    [Tooltip("결과가 다 나온 뒤 창이 닫히기까지의 시간(초). 0이면 자동으로 닫지 않는다.")]
    [SerializeField] private float autoHideSeconds = 1.5f;

    private Sequence sequence;
    private Sequence rollLoop;   // 결과를 기다리는 동안 계속 굴러가는 연출
    private Tween shake1, shake2;
    private bool rolling;

    public override void Bind()
    {
        for (int i = 0; i < PipNames.Length; i++)
        {
            die1Pips[i] = FindObject($"DiceResult/Dice/Die_1/Face/Pips/{PipNames[i]}");
            die2Pips[i] = FindObject($"DiceResult/Dice/Die_2/Face/Pips/{PipNames[i]}");
        }
        totalText = Find<TMP_Text>("DiceResult/TotalText");
        doubleBadge = FindObject("DiceResult/DoubleBadge");
        doubleSubText = FindObject("DiceResult/DoubleSubText");
        doubleHighlight = FindObject("DiceResult/Card/DoubleHighlight");
    }

    /// <summary>
    /// 결과를 기다리는 굴림 상태를 시작한다. 창을 열고, Show()로 결과가 들어오기 전까지 주사위가 계속 구른다.
    /// 굴리기 버튼을 누르는 즉시(결과를 요청하는 시점에) 부른다.
    /// </summary>
    public void StartRolling()
    {
        sequence?.Kill();
        KillRollLoop();
        ResetVisuals();
        SetActive(doubleBadge, false);
        SetActive(doubleSubText, false);
        SetActive(doubleHighlight, false);
        if (totalText != null) totalText.text = "";
        Open();
        rolling = true;

        rollLoop = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).SetLoops(-1);
        rollLoop.AppendCallback(() =>
        {
            SetFace(die1Pips, Random.Range(1, 7));
            SetFace(die2Pips, Random.Range(1, 7));
        });
        rollLoop.AppendInterval(0.07f);

        var die1 = DieOf(die1Pips);
        var die2 = DieOf(die2Pips);
        if (die1 != null) shake1 = die1.DOShakeRotation(0.5f, new Vector3(0f, 0f, 22f), 14, 90f, true).SetUpdate(true).SetLink(gameObject).SetLoops(-1);
        if (die2 != null) shake2 = die2.DOShakeRotation(0.5f, new Vector3(0f, 0f, 22f), 14, 90f, true).SetUpdate(true).SetLink(gameObject).SetLoops(-1);
    }

    /// <summary>결과가 오지 않아 굴림을 취소할 때 부른다. 굴리는 중이던 창을 닫는다.</summary>
    public void CancelRolling()
    {
        if (!rolling) return;
        KillRollLoop();
        ResetVisuals();
        Close();
    }

    private void KillRollLoop()
    {
        rolling = false;
        rollLoop?.Kill();
        shake1?.Kill();
        shake2?.Kill();
        rollLoop = null;
        shake1 = shake2 = null;
    }

    /// <summary>
    /// 두 주사위 값(1~6)을 굴림 연출과 함께 보여준다. onRevealed는 합계·더블 표시까지 끝난 뒤 호출.
    /// 그 뒤 autoHideSeconds(기본 1.5초)가 지나면 창이 스스로 닫힌다.
    /// </summary>
    public void Show(int dice1, int dice2, Action onRevealed = null)
    {
        sequence?.Kill();
        bool wasRolling = rolling; // 이미 굴러가는 중이면 결과가 오는 즉시 멈추는 연출로 이어간다
        KillRollLoop();
        ResetVisuals();

        bool isDouble = dice1 == dice2;
        SetActive(doubleBadge, false);
        SetActive(doubleSubText, false);
        SetActive(doubleHighlight, false);
        if (totalText != null) totalText.text = "";
        if (!wasRolling || !gameObject.activeSelf) Open(); // 굴리는 도중 창이 닫혔다면 다시 연다

        var die1 = DieOf(die1Pips);
        var die2 = DieOf(die2Pips);
        sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        // 1) 굴림: 눈이 바뀌는 간격이 점점 길어진다
        // (이미 굴러가던 중이면 짧게 감속만 한다)
        int steps = wasRolling ? 3 : rollSteps;
        float seconds = wasRolling ? rollSeconds * 0.35f : rollSeconds;
        float rollTime = 0f;
        for (int k = 0; k < steps; k++)
        {
            sequence.AppendCallback(() =>
            {
                SetFace(die1Pips, Random.Range(1, 7));
                SetFace(die2Pips, Random.Range(1, 7));
            });
            float interval = seconds / steps * Mathf.Lerp(0.6f, 1.6f, k / (float)steps);
            sequence.AppendInterval(interval);
            rollTime += interval;
        }
        if (die1 != null) sequence.Insert(0f, die1.DOShakeRotation(rollTime, new Vector3(0f, 0f, 22f), 14, 90f, true));
        if (die2 != null) sequence.Insert(0f, die2.DOShakeRotation(rollTime, new Vector3(0f, 0f, 22f), 14, 90f, true));

        // 2) 멈춤: 최종 눈 + 주사위가 톡 튄다
        sequence.AppendCallback(() =>
        {
            SetFace(die1Pips, dice1);
            SetFace(die2Pips, dice2);
        });
        if (die1 != null) sequence.Append(die1.DOPunchScale(Vector3.one * 0.18f, 0.25f, 6, 0.5f));
        if (die2 != null) sequence.Join(die2.DOPunchScale(Vector3.one * 0.18f, 0.25f, 6, 0.5f));

        // 3) 합계 숫자가 튀어나온다
        if (totalText != null)
        {
            sequence.AppendCallback(() =>
            {
                totalText.text = (dice1 + dice2).ToString();
                totalText.transform.localScale = Vector3.zero;
            });
            sequence.Append(totalText.transform.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 2.5f));
        }

        // 4) 더블: 배지가 통통 튀고 별이 반짝인다
        if (isDouble) AppendDouble();

        sequence.AppendCallback(() => onRevealed?.Invoke());

        // 5) 잠시 보여준 뒤 스스로 닫힌다
        if (autoHideSeconds > 0f)
        {
            sequence.AppendInterval(autoHideSeconds);
            sequence.AppendCallback(Close);
        }
    }

    private void AppendDouble()
    {
        var badge = doubleBadge != null ? doubleBadge.transform : null;
        var sub = doubleSubText != null ? doubleSubText.GetComponent<TMP_Text>() : null;
        var highlight = doubleHighlight != null ? doubleHighlight.GetComponent<Image>() : null;

        sequence.AppendCallback(() =>
        {
            SetActive(doubleBadge, true);
            SetActive(doubleSubText, true);
            SetActive(doubleHighlight, true);
            if (badge != null) badge.localScale = Vector3.zero;
            if (sub != null) sub.alpha = 0f;
            if (highlight != null) SetAlpha(highlight, 0f);
        });
        if (badge != null) sequence.Append(badge.DOScale(1f, 0.4f).SetEase(Ease.OutBack, 3f));
        if (highlight != null) sequence.Join(highlight.DOFade(1f, 0.3f));
        if (sub != null) sequence.Join(DOTween.To(() => sub.alpha, a => sub.alpha = a, 1f, 0.3f));

        if (badge == null) return;
        foreach (var name in new[] { "Star_L", "Star_R" })
        {
            var star = badge.Find(name);
            if (star == null) continue;
            sequence.Join(star.DOPunchScale(Vector3.one * 0.6f, 0.5f, 5, 0.6f));
            sequence.Join(star.DOLocalRotate(new Vector3(0f, 0f, -360f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
        }
    }

    // 이전 연출이 중간에 끊겨도 원래 모양으로 돌아오게 한다
    private void ResetVisuals()
    {
        foreach (var die in new[] { DieOf(die1Pips), DieOf(die2Pips) })
        {
            if (die == null) continue;
            die.localRotation = Quaternion.identity;
            die.localScale = Vector3.one;
        }
        if (totalText != null) totalText.transform.localScale = Vector3.one;
        if (doubleBadge != null)
        {
            doubleBadge.transform.localScale = Vector3.one;
            foreach (Transform star in doubleBadge.transform)
            {
                star.localScale = Vector3.one;
                star.localRotation = Quaternion.identity;
            }
        }
        if (doubleSubText != null && doubleSubText.TryGetComponent(out TMP_Text sub)) sub.alpha = 1f;
        if (doubleHighlight != null && doubleHighlight.TryGetComponent(out Image image)) SetAlpha(image, 1f);
    }

    /// <summary>Pip → Pips → Face → Die_N</summary>
    private static Transform DieOf(GameObject[] pips)
    {
        if (pips == null || pips.Length == 0 || pips[0] == null) return null;
        return pips[0].transform.parent.parent.parent;
    }

    private static void SetFace(GameObject[] pips, int value)
    {
        var face = Faces[Mathf.Clamp(value, 1, 6)];
        for (int i = 0; i < pips.Length; i++) SetActive(pips[i], face[i]);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        var c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }
}
