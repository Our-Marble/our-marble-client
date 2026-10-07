using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 누르는 동안 버튼을 살짝 줄였다가 놓으면 튕기듯 돌아오게 한다.
/// 피벗이 가운데가 아닌 버튼도 가운데를 기준으로 줄어들도록 위치를 보정한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.2f;

    private RectTransform rect;
    private Selectable selectable;
    private Vector2 basePosition;
    private float scale = 1f;
    private bool pressed;
    private Tween tween;

    private void Awake()
    {
        rect = (RectTransform)transform;
        selectable = GetComponent<Selectable>();
    }

    private void OnDisable()
    {
        tween?.Kill();
        // 누르는 중이거나, 놓은 뒤 원래 크기로 돌아오는 도중에 꺼져도 크기가 줄어든 채 남지 않게 되돌린다
        if (pressed || !Mathf.Approximately(scale, 1f)) Apply(1f);
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.IsInteractable()) return;
        // 완전히 돌아온 상태에서만 기준 위치를 잡는다 (연타 중 중간 위치를 잡지 않도록)
        if (!pressed && Mathf.Approximately(scale, 1f)) basePosition = rect.anchoredPosition;
        pressed = true;
        AnimateTo(pressedScale, pressDuration, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData) => Release();

    public void OnPointerExit(PointerEventData eventData) => Release();

    private void Release()
    {
        if (!pressed) return;
        pressed = false;
        AnimateTo(1f, releaseDuration, Ease.OutBack);
    }

    private void AnimateTo(float target, float duration, Ease ease)
    {
        tween?.Kill();
        tween = DOTween.To(() => scale, Apply, target, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void Apply(float value)
    {
        scale = value;
        rect.localScale = new Vector3(value, value, 1f);

        // 피벗에서 가운데까지의 거리만큼, 줄어든 비율에 맞춰 옮겨서 가운데를 고정한다
        Vector2 size = rect.rect.size;
        var centerFromPivot = new Vector2((0.5f - rect.pivot.x) * size.x, (0.5f - rect.pivot.y) * size.y);
        rect.anchoredPosition = basePosition + centerFromPivot * (1f - value);
    }
}
