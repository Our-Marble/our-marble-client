using DG.Tweening;
using UnityEngine;

/// <summary>여러 화면이 같이 쓰는 작은 UI 도우미.</summary>
public static class UIFx
{
    /// <summary>입력칸 등을 좌우로 흔든다. 잘못 입력했을 때의 피드백.</summary>
    public static void Shake(Transform target)
    {
        var rect = (RectTransform)target;
        rect.DOKill(true);
        rect.DOShakeAnchorPos(0.3f, new Vector2(14f, 0f), 20, 0f, false, true).SetUpdate(true).SetLink(target.gameObject);
    }

    /// <summary>CanvasGroup이 없으면 붙여서 돌려준다.</summary>
    public static CanvasGroup EnsureGroup(GameObject go)
    {
        var group = go.GetComponent<CanvasGroup>();
        return group != null ? group : go.AddComponent<CanvasGroup>();
    }
}
