using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 서버에 요청을 보내고 응답을 기다리는 동안(방 참가, 방 만들기, 게임 시작, 로그인, 방 목록 새로고침) 화면을 덮는 로딩 표시. 점 세 개가 뛴다. Canvas_Loading에 붙는다.
/// 화면 전체를 덮어 그동안 다른 버튼을 누를 수 없다.
/// </summary>
public class LoadingView : MonoBehaviour
{
    // 문구 없이 점만 보일 때의 카드 크기
    private static readonly Vector2 DotsOnlyCardSize = new Vector2(200f, 110f);

    private Transform[] dots;
    private TMP_Text label;
    private string defaultMessage;
    private RectTransform card, dotsRoot;
    private Vector2 cardSize, dotsPosition;

    /// <param name="message">표시할 문구. null이면 프리팹에 적힌 기본 문구("입장 중"), 빈 문장("")이면 문구 없이 점만 보인다.</param>
    public void Show(bool show, string message = null)
    {
        if (dots == null) Cache();

        if (show)
        {
            string text = message ?? defaultMessage;
            bool dotsOnly = string.IsNullOrEmpty(text);
            if (label != null)
            {
                label.gameObject.SetActive(!dotsOnly);
                if (!dotsOnly) label.text = text;
            }
            // 문구가 없으면 카드를 줄이고 점을 가운데로 옮긴다
            if (card != null) card.sizeDelta = dotsOnly ? DotsOnlyCardSize : cardSize;
            if (dotsRoot != null)
            {
                // 기준점(pivot)이 가운데가 아니어도 점이 카드 가운데에 오도록 맞춘다
                float centerY = -DotsOnlyCardSize.y / 2f + (dotsRoot.pivot.y - 0.5f) * dotsRoot.rect.height;
                dotsRoot.anchoredPosition = dotsOnly ? new Vector2(dotsPosition.x, centerY) : dotsPosition;
            }
        }

        gameObject.SetActive(show);
        for (int i = 0; i < dots.Length; i++)
        {
            var dot = dots[i];
            dot.DOKill();
            dot.localScale = Vector3.one;
            if (!show) continue;
            dot.DOScale(1.4f, 0.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetDelay(i * 0.15f).SetUpdate(true).SetLink(dot.gameObject);
        }
    }

    // 꺼져 있던 오브젝트라 처음 쓸 때 참조와 원래 모양을 기억해 둔다
    private void Cache()
    {
        dots = new Transform[3];
        for (int i = 0; i < dots.Length; i++) dots[i] = transform.Find($"Card/Dots/Dot_{i + 1}");

        label = GetComponentInChildren<TMP_Text>(true);
        if (label != null) defaultMessage = label.text;
        card = transform.Find("Card") as RectTransform;
        dotsRoot = transform.Find("Card/Dots") as RectTransform;
        if (card != null) cardSize = card.sizeDelta;
        if (dotsRoot != null) dotsPosition = dotsRoot.anchoredPosition;
    }
}
