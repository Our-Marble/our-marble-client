using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private BoardController boardController;
    [Tooltip("GameManager.playerOrder 순서와 같게 연결하세요.")]
    [SerializeField] private List<PlayerPawn> pawns = new();

    [Header("Move")]
    [SerializeField] private float hopDuration = 0.25f;
    [SerializeField] private float slideDuration = 0.6f;

    private readonly Dictionary<long, PlayerPawn> pawnsByPlayerId = new();

    /// <summary>게임 시작 시 GameManager가 호출. playerId와 말을 매핑하고 시작 칸(0)에 배치합니다.</summary>
    public void Initialize(IReadOnlyList<long> playerIds)
    {
        pawnsByPlayerId.Clear();

        for (int i = 0; i < playerIds.Count; i++)
        {
            if (i >= pawns.Count || pawns[i] == null)
            {
                Debug.LogError($"[PlayerManager] playerId {playerIds[i]}에 연결된 PlayerPawn이 없습니다.", this);
                continue;
            }

            pawnsByPlayerId[playerIds[i]] = pawns[i];
            pawns[i].Move(0, boardController.GetTilePosition(0));
        }
    }

    public bool TryGetPawn(long playerId, out PlayerPawn pawn)
    {
        if (pawnsByPlayerId.TryGetValue(playerId, out pawn) && pawn != null)
            return true;

        Debug.LogError($"[PlayerManager] PlayerPawn not found: {playerId}", this);
        return false;
    }

    /// <summary>주사위 결과만큼 한 칸씩 이동. 칸에 도착할 때마다 onTileReached(칸 index), 끝나면 onCompleted.</summary>
    public void MoveBySteps(long playerId, int steps, Action<int> onTileReached, Action onCompleted)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn))
        {
            onCompleted?.Invoke();   // 말이 없어도 게임이 멈추지 않게
            return;
        }
        StartCoroutine(MoveByStepsRoutine(pawn, steps, onTileReached, onCompleted));
    }

    /// <summary>목적지로 직접 이동 (자유여행, 무인도 이동 등)</summary>
    public void MoveToTile(long playerId, int tileIndex, Action onCompleted)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn))
        {
            onCompleted?.Invoke();
            return;
        }
        StartCoroutine(MoveToTileRoutine(pawn, tileIndex, onCompleted));
    }

    /// <summary>파산 시 말 숨기기</summary>
    public void HidePawn(long playerId)
    {
        if (TryGetPawn(playerId, out PlayerPawn pawn))
            pawn.gameObject.SetActive(false);
    }

    private IEnumerator MoveByStepsRoutine(PlayerPawn pawn, int steps, Action<int> onTileReached, Action onCompleted)
    {
        int tileCount = boardController.TileCount;

        for (int i = 0; i < steps; i++)
        {
            int next = (pawn.CurrentTileIndex + 1) % tileCount;
            yield return pawn.HopTo(next, boardController.GetTilePosition(next), hopDuration);
            onTileReached?.Invoke(next);
        }

        onCompleted?.Invoke();
    }

    private IEnumerator MoveToTileRoutine(PlayerPawn pawn, int tileIndex, Action onCompleted)
    {
        yield return pawn.SlideTo(tileIndex, boardController.GetTilePosition(tileIndex), slideDuration);
        onCompleted?.Invoke();
    }
}