using System;
using UnityEngine;

/// <summary>
/// 돈 변경을 알린다. 돈 관리(보관, 계산, 이동)는 GameManager가 GameState에서 한다.
/// UI는 OnMoneyChanged를 구독해서 돈 표시와 연출을 갱신한다.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    /// <summary>돈 변경 알림 (playerId, 이전 금액, 새 금액).</summary>
    public event Action<long, long, long> OnMoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ───────────── 알림 ─────────────

    /// <summary>GameState의 돈이 바뀐 뒤 GameManager가 호출.</summary>
    public void NotifyMoneyChanged(long playerId, long before, long after)
    {
        Debug.Log($"[EconomyManager] {playerId} 돈 변경: {before} → {after}");
        OnMoneyChanged?.Invoke(playerId, before, after);
    }
}