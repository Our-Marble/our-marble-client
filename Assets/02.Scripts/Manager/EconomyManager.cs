using System;
using UnityEngine;

/// <summary>
/// 돈 변경을 알린다. 돈 관리(보관, 계산, 이동)는 GameManager가 GameState에서 한다.
/// UI는 OnMoneyChanged를 구독해서 돈 표시와 연출을 갱신한다.
/// 씬에 올리지 않고 바로 쓴다. 구독한 쪽은 OnDestroy에서 반드시 해제(-=)한다.
/// </summary>
public static class EconomyManager
{
    /// <summary>돈 변경 알림 (playerId, 이전 금액, 새 금액).</summary>
    public static event Action<long, long, long> OnMoneyChanged;

    // ───────────── 알림 ─────────────

    /// <summary>GameState의 돈이 바뀐 뒤 GameManager가 호출.</summary>
    public static void NotifyMoneyChanged(long playerId, long before, long after)
    {
        Debug.Log($"[EconomyManager] {playerId} 돈 변경: {before} → {after}");
        OnMoneyChanged?.Invoke(playerId, before, after);
    }
}

/// <summary>건설 선택지 하나. Cost는 현재 단계부터 해당 단계까지의 누적 비용. GameManager.GetBuildOptions 결과.</summary>
public class BuildOption
{
    public BuildingLevel Level;
    public long Cost;
}