using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    [SerializeField] private BoardController boardController;
    [Tooltip("로비 슬롯 순서(1P, 2P, 3P, 4P)대로 연결하세요. 턴 순서와는 무관합니다.")]
    [SerializeField] private List<PlayerPawn> pawns = new();

    [Header("Move")]
    [SerializeField] private float hopDuration = 0.25f;
    [SerializeField] private float slideDuration = 0.6f;

    [Header("말 겹침 방지 (슬롯별 칸 안 위치)")]
    [Tooltip("칸 중심 기준 오프셋입니다. 기본값은 3D 보드(XZ 평면)용이며, 2D 보드라면 Z 대신 Y 값을 쓰세요.")]
    [SerializeField] private Vector3[] slotOffsets =
    {
        new Vector3(-0.25f, 0.25f,  0f), // 1P: 왼쪽 위
        new Vector3( 0.25f, 0.25f,  0f), // 2P: 오른쪽 위
        new Vector3(-0.25f, -0.25f, 0f), // 3P: 왼쪽 아래
        new Vector3( 0.25f, -0.25f, 0f), // 4P: 오른쪽 아래
    };
    
    private readonly Dictionary<long, PlayerPawn> pawnsByPlayerId = new();
    private readonly Dictionary<long, int> slotByPlayerId = new();
    private readonly Dictionary<long, Coroutine> runningMoves = new(); // 말별 진행 중인 이동

    protected override void OnAwake()
    {
        
    }

    /// <summary>게임 시작 시 GameManager가 호출. playerId와 말을 매핑하고 시작 칸(0)에 배치합니다.</summary>
    public void Initialize(IReadOnlyList<long> playerIds)
    {
        StopAllMoves();
        pawnsByPlayerId.Clear();
        slotByPlayerId.Clear();

        if (playerIds.Count > pawns.Count)
            Debug.LogError($"[PlayerManager] 플레이어 {playerIds.Count}명인데 말은 {pawns.Count}개뿐입니다.", this);

        for (int slot = 0; slot < pawns.Count; slot++)
        {
            PlayerPawn pawn = pawns[slot];
            bool used = slot < playerIds.Count;

            if (pawn == null)
            {
                if (used)
                    Debug.LogError($"[PlayerManager] {slot + 1}P 슬롯에 PlayerPawn이 연결되지 않았습니다.", this);
                continue;
            }

            // 2인 게임이면 3P, 4P 말은 숨깁니다.
            pawn.gameObject.SetActive(used);
            if (!used) continue;

            long playerId = playerIds[slot];
            if (pawnsByPlayerId.ContainsKey(playerId))
            {
                Debug.LogError($"[PlayerManager] playerId {playerId}가 중복되었습니다.", this);
                pawn.gameObject.SetActive(false);
                continue;
            }

            pawnsByPlayerId[playerId] = pawn;
            slotByPlayerId[playerId] = slot;
            pawn.Move(0, GetPawnPosition(playerId, 0));
        }
    }

    public bool TryGetPawn(long playerId, out PlayerPawn pawn)
    {
        if (pawnsByPlayerId.TryGetValue(playerId, out pawn) && pawn != null)
            return true;

        Debug.LogError($"[PlayerManager] PlayerPawn not found: {playerId}", this);
        return false;
    }

    // ────────────────────────── 이동 ──────────────────────────

    /// <summary>
    /// fromIndex에서 steps칸 앞으로 한 칸씩 이동합니다. (주사위, 앞으로 이동 카드)
    /// 시작 위치를 GameState 기준(fromIndex)으로 맞춘 뒤 걷기 때문에 말 위치가 어긋나지 않습니다.
    /// 칸에 도착할 때마다 onTileReached(칸 index), 끝나면 onCompleted.
    /// </summary>
    public void MoveBySteps(long playerId, int fromIndex, int steps, Action<int> onTileReached, Action onCompleted)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn))
        {
            onCompleted?.Invoke();   // 말이 없어도 게임이 멈추지 않게
            return;
        }
        
        int tileCount = boardController.TileCount;
        int from = Mod(fromIndex, tileCount);

        if (steps <= 0)
        {
            // 이동할 칸이 없으면 위치만 맞추고 바로 완료
            StopMove(playerId);
            pawn.Move(from, GetPawnPosition(playerId, from));
            onCompleted?.Invoke();
            return;
        }

        StartMove(playerId, MoveByStepsRoutine(playerId, pawn, from, steps, tileCount, onTileReached, onCompleted));
    }

    /// <summary>이전 버전 호환용입니다. 말이 가진 위치를 기준으로 걷기 때문에 GameState와 어긋날 수 있습니다.</summary>
    [Obsolete("fromIndex를 받는 MoveBySteps(playerId, fromIndex, steps, ...)를 사용하세요.")]
    public void MoveBySteps(long playerId, int steps, Action<int> onTileReached, Action onCompleted)
    {
        int from = TryGetPawn(playerId, out PlayerPawn pawn) ? pawn.CurrentTileIndex : 0;
        MoveBySteps(playerId, from, steps, onTileReached, onCompleted);
    }
    
    /// <summary>목적지로 직접 이동 (자유여행, 무인도 이동 등)</summary>
    public void MoveToTile(long playerId, int tileIndex, Action onCompleted)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn))
        {
            onCompleted?.Invoke();
            return;
        }

        int target = Mod(tileIndex, boardController.TileCount);
        StartMove(playerId, MoveToTileRoutine(playerId, pawn, target, onCompleted));
    }

    /// <summary>연출 없이 즉시 배치합니다. (재접속, 서버 상태 동기화 등)</summary>
    public void SnapToTile(long playerId, int tileIndex)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn)) return;

        StopMove(playerId);
        int target = Mod(tileIndex, boardController.TileCount);
        pawn.Move(target, GetPawnPosition(playerId, target));
    }
    
    /// <summary>파산 시 말 숨기기</summary>
    public void HidePawn(long playerId)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn)) return;

        StopMove(playerId);
        pawn.gameObject.SetActive(false);
    }

    // ────────────────────────── 코루틴 ──────────────────────────

    private IEnumerator MoveByStepsRoutine(long playerId, PlayerPawn pawn, int from, int steps, int tileCount,
        Action<int> onTileReached, Action onCompleted)
    {
        int current = from;
        pawn.Move(current, GetPawnPosition(playerId, current)); // 시작점을 GameState 기준으로 맞춤

        for (int i = 0; i < steps; i++)
        {
            int next = (current + 1) % tileCount;
            yield return pawn.HopTo(next, GetPawnPosition(playerId, next), hopDuration);
            current = next;
            onTileReached?.Invoke(next);
        }

        pawn.Move(current, GetPawnPosition(playerId, current)); // 도착 위치 스냅 (오차 제거)

        // 콜백 안에서 같은 말의 새 이동이 시작될 수 있으므로, 기록을 먼저 지우고 콜백을 호출합니다.
        FinishMove(playerId);
        onCompleted?.Invoke();
    }

    private IEnumerator MoveToTileRoutine(long playerId, PlayerPawn pawn, int target, Action onCompleted)
    {
        yield return pawn.SlideTo(target, GetPawnPosition(playerId, target), slideDuration);
        pawn.Move(target, GetPawnPosition(playerId, target));

        FinishMove(playerId);
        onCompleted?.Invoke();
    }

    // ────────────────────────── 내부 도우미 ──────────────────────────

    /// <summary>같은 말의 이동이 겹치지 않도록, 진행 중인 이동을 멈추고 새 이동을 시작합니다.</summary>
    private void StartMove(long playerId, IEnumerator routine)
    {
        if (StopMove(playerId))
            Debug.LogWarning($"[PlayerManager] {playerId}의 이전 이동이 끝나기 전에 새 이동이 들어와 중단했습니다.", this);

        runningMoves[playerId] = StartCoroutine(routine);
    }

    /// <summary>진행 중인 이동이 있으면 멈춥니다. 멈춘 이동의 onCompleted는 호출되지 않습니다.</summary>
    private bool StopMove(long playerId)
    {
        if (runningMoves.TryGetValue(playerId, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
            runningMoves.Remove(playerId);
            return true;
        }
        return false;
    }

    private void FinishMove(long playerId)
    {
        runningMoves.Remove(playerId);
    }

    private void StopAllMoves()
    {
        foreach (Coroutine running in runningMoves.Values)
            if (running != null) StopCoroutine(running);
        runningMoves.Clear();
    }

    /// <summary>칸 중심 + 슬롯 오프셋. 같은 칸에 여러 명이 있어도 말이 겹치지 않습니다.</summary>
    private Vector3 GetPawnPosition(long playerId, int tileIndex)
    {
        Vector3 center = boardController.GetTilePosition(tileIndex);

        if (slotByPlayerId.TryGetValue(playerId, out int slot) && slot < slotOffsets.Length)
            return center + slotOffsets[slot];

        return center;
    }

    private static int Mod(int value, int count)
    {
        return count <= 0 ? 0 : ((value % count) + count) % count;
    }
}