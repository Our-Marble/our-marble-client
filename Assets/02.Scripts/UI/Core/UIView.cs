using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 UI 뷰의 공통 부모.
/// 참조는 인스펙터에 저장하고, 컴포넌트를 붙일 때(Reset) 오브젝트 이름으로 자동 연결한다.
/// 꺼진 오브젝트는 Awake가 돌지 않으므로 런타임 탐색에 기대지 않는다.
/// PanelPath가 있으면 열 때 창이 튀어나오고, 닫을 때 줄어들며 사라진다.
/// </summary>
public abstract class UIView : MonoBehaviour
{
    private const float OpenDuration = 0.28f;
    private const float CloseDuration = 0.16f;
    private const float OpenFromScale = 0.9f;
    private const float CloseToScale = 0.95f;

    [Header("열기·닫기 연출")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private CanvasGroup dimGroup;

    private Sequence transition;
    private bool closing;

    /// <summary>튀어나오는 창의 경로. null이면 연출 없이 켜고 끈다.</summary>
    public virtual string PanelPath => null;

    /// <summary>뒤를 어둡게 덮는 배경의 경로. 없으면 null.</summary>
    public virtual string DimPath => null;

    /// <summary>켜져 있고 닫힘 연출 중이 아닐 때 true. 닫히는 중이면 false.</summary>
    public bool IsOpen => gameObject.activeSelf && !closing;

    /// <summary>창이 완전히 닫혔을 때(닫힘 연출이 끝난 뒤) 호출된다.</summary>
    public event Action Closed;

    /// <summary>하위 뷰가 자체 연출(예: 내용 교체)에 쓰는 창과 CanvasGroup.</summary>
    protected RectTransform Panel => panel;
    protected CanvasGroup PanelGroup => panelGroup;

    public virtual void Open()
    {
        closing = false;
        gameObject.SetActive(true);
        PlayOpen();
    }

    /// <summary>닫힘 연출을 재생한 뒤 끈다. 연출이 없는 뷰는 바로 끈다.</summary>
    public virtual void Close()
    {
        if (!gameObject.activeSelf) return;
        if (panel == null && dimGroup == null)
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
            return;
        }
        PlayClose();
    }

    /// <summary>연출 없이 바로 끈다. 게임 시작 시 정리용.</summary>
    public void CloseImmediate()
    {
        transition?.Kill();
        bool wasActive = gameObject.activeSelf;
        closing = false;
        ResetTransitionVisuals();
        gameObject.SetActive(false);
        if (wasActive) Closed?.Invoke();
    }

    /// <summary>하위 오브젝트 이름으로 참조를 채운다.</summary>
    public abstract void Bind();

    /// <summary>PanelPath / DimPath로 연출 대상을 찾는다. CanvasGroup은 셋업 메뉴가 붙여 둔다.</summary>
    public void BindTransitions()
    {
        panel = PanelPath != null ? transform.Find(PanelPath) as RectTransform : null;
        panelGroup = panel != null ? panel.GetComponent<CanvasGroup>() : null;
        var dim = DimPath != null ? transform.Find(DimPath) : null;
        dimGroup = dim != null ? dim.GetComponent<CanvasGroup>() : null;
    }

    protected virtual void Reset()
    {
        Bind();
        BindTransitions();
    }

    [ContextMenu("Bind (이름으로 다시 연결)")]
    private void BindFromMenu() => Reset();

    // ───────────── 열기·닫기 연출 ─────────────

    private void PlayOpen()
    {
        transition?.Kill();
        if (panel == null && dimGroup == null) return;

        transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        if (panel != null)
        {
            panel.localScale = Vector3.one * OpenFromScale;
            transition.Join(panel.DOScale(1f, OpenDuration).SetEase(Ease.OutBack));
        }
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = true;
            transition.Join(panelGroup.DOFade(1f, OpenDuration * 0.6f));
        }
        if (dimGroup != null)
        {
            dimGroup.alpha = 0f;
            transition.Join(dimGroup.DOFade(1f, OpenDuration * 0.7f));
        }
    }

    private void PlayClose()
    {
        transition?.Kill();
        closing = true;
        // 닫히는 동안 다시 눌리지 않게 막는다
        if (panelGroup != null) panelGroup.blocksRaycasts = false;

        transition = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        if (panel != null) transition.Join(panel.DOScale(CloseToScale, CloseDuration).SetEase(Ease.InQuad));
        if (panelGroup != null) transition.Join(panelGroup.DOFade(0f, CloseDuration));
        if (dimGroup != null) transition.Join(dimGroup.DOFade(0f, CloseDuration));
        transition.OnComplete(() =>
        {
            closing = false;
            ResetTransitionVisuals();
            gameObject.SetActive(false);
            Closed?.Invoke();
        });
    }

    private void ResetTransitionVisuals()
    {
        if (panel != null) panel.localScale = Vector3.one;
        if (panelGroup != null) { panelGroup.alpha = 1f; panelGroup.blocksRaycasts = true; }
        if (dimGroup != null) dimGroup.alpha = 1f;
    }

    // ───────────── 연결 헬퍼 ─────────────

    protected T Find<T>(string path) where T : Component
    {
        var target = string.IsNullOrEmpty(path) ? transform : transform.Find(path);
        if (target == null)
        {
            Debug.LogWarning($"[{GetType().Name}] '{name}/{path}' 오브젝트 없음", this);
            return null;
        }

        var component = target.GetComponent<T>();
        if (component == null)
            Debug.LogWarning($"[{GetType().Name}] '{name}/{path}'에 {typeof(T).Name} 없음", this);
        return component;
    }

    protected GameObject FindObject(string path)
    {
        var target = transform.Find(path);
        if (target == null) Debug.LogWarning($"[{GetType().Name}] '{name}/{path}' 오브젝트 없음", this);
        return target != null ? target.gameObject : null;
    }

    /// <summary>버튼의 런타임 리스너를 교체한다. 열 때마다 새 콜백을 넣기 위함.</summary>
    protected static void SetOnClick(Button button, Action action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action?.Invoke());
    }

    protected static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    /// <summary>초상화가 있으면 원래 색으로, 없으면 플레이어 색 자리표시로 둔다.</summary>
    protected static void SetPortrait(Image image, Sprite portrait, Color placeholder)
    {
        if (image == null) return;
        image.sprite = portrait;
        image.color = portrait != null ? Color.white : placeholder;
    }
}
