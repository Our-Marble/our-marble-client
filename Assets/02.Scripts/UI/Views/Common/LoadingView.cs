using DG.Tweening;
using UnityEngine;

/// <summary>
/// 서버에 요청을 보내고 응답을 기다리는 동안(방 참가, 방 만들기, 로그인) 화면을 덮는 로딩 표시. 점 세 개가 뛴다. Canvas_Loading에 붙는다.
/// 화면 전체를 덮어 그동안 다른 버튼을 누를 수 없다.
/// </summary>
public class LoadingView : MonoBehaviour
{
    private Transform[] dots;

    public void Show(bool show)
    {
        if (dots == null)
        {
            dots = new Transform[3];
            for (int i = 0; i < dots.Length; i++) dots[i] = transform.Find($"Card/Dots/Dot_{i + 1}");
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
}
