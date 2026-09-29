using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 누르고 있는 동안을 알려주는 버튼. 주사위 파워 게이지에 쓴다.
/// 같은 오브젝트의 Button이 비활성이면 반응하지 않는다.
/// </summary>
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public event Action PointerDown;
    public event Action PointerUp;

    public bool IsHolding { get; private set; }

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        IsHolding = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.IsInteractable()) return;
        IsHolding = true;
        PointerDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsHolding) return;
        IsHolding = false;
        PointerUp?.Invoke();
    }
}
