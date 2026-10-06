using System;
using DG.Tweening;
using TMPro;

/// <summary>
/// 금액 텍스트를 이전 값에서 새 값까지 촤르륵 굴려서 바꾼다.
/// 처음 값을 넣을 때와 텍스트가 꺼져 있을 때는 바로 바꾼다.
/// </summary>
public class RollingNumber
{
    public const float DefaultDuration = 0.6f;

    private readonly TMP_Text text;
    private readonly Func<long, string> format;
    private readonly Action<long> onValue;
    private long shown;
    private bool initialized;
    private Tween tween;

    /// <param name="onValue">값이 바뀔 때마다 호출. 부호에 따라 색을 바꾸는 등에 쓴다.</param>
    public RollingNumber(TMP_Text text, Func<long, string> format, Action<long> onValue = null)
    {
        this.text = text;
        this.format = format;
        this.onValue = onValue;
    }

    public long Value => shown;

    public void Set(long value, bool animate = true, float delay = 0f, float duration = DefaultDuration)
    {
        tween?.Kill();

        bool canAnimate = animate && initialized && text != null && text.isActiveAndEnabled && value != shown;
        initialized = true;
        if (!canAnimate)
        {
            Apply(value);
            return;
        }

        tween = DOTween.To(() => shown, Apply, value, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetLink(text.gameObject);
    }

    private void Apply(long value)
    {
        shown = value;
        if (text != null) text.text = format(value);
        onValue?.Invoke(value);
    }
}
