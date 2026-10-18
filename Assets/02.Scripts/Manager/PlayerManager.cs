using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    private const int PawnSortingBaseOrder = 100;
    private const int MovingPawnSortingOrder = 104;

    // 게임 씬 전용 매니저입니다. 씬 오브젝트(말, 타일, 보드)를 참조하므로 게임 씬이 로드될 때마다 새로 만들어지게 합니다.
    protected override bool PersistAcrossScenes => false;

    [SerializeField] private BoardController boardController;
    [Tooltip("로비 슬롯 순서(1P, 2P, 3P, 4P)대로 연결하세요. 턴 순서와는 무관합니다.")]
    [SerializeField] private List<PlayerPawn> pawns = new();

    [Header("Move")]
    [SerializeField] private float hopDuration = 0.2f;
    [SerializeField] private float slideDuration = 0.6f;
    [Tooltip("타일을 따라 빠르게 이동할 때(세계여행, 황금열쇠 지정 위치 이동) 한 칸을 지나는 시간(초)")]
    [SerializeField] private float glideStepDuration = 0.07f;

    [Header("말 겹침 방지 (슬롯별 칸 안 위치)")]
    [Tooltip("칸 중심 기준 오프셋입니다. 기본값은 3D 보드(XZ 평면)용이며, 2D 보드라면 Z 대신 Y 값을 쓰세요.")]
    [SerializeField] private Vector3[] slotOffsets =
    {
        new Vector3(-0.15f, 0.15f,  0f), // 1P: 왼쪽 위
        new Vector3( 0.15f, 0.15f,  0f), // 2P: 오른쪽 위
        new Vector3(-0.15f, -0.15f, 0f), // 3P: 왼쪽 아래
        new Vector3( 0.15f, -0.15f, 0f), // 4P: 오른쪽 아래
    };
    
    private readonly Dictionary<long, PlayerPawn> pawnsByPlayerId = new();
    private readonly Dictionary<long, int> slotByPlayerId = new();
    private readonly Dictionary<long, Coroutine> runningMoves = new(); // 말별 진행 중인 이동

    protected override void OnAwake()
    {
        
    }

    private void OnEnable()
    {
        GameManager.OnPlayerArrived += HandlePlayerArrived;
        GameManager.OnPropertyPurchasedSuccessfully += PlayCheer;
        GameManager.OnBuildingBuiltSuccessfully += PlayCheer;
        GameManager.OnIslandEscapeFailed += PlaySad;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerArrived -= HandlePlayerArrived;
        GameManager.OnPropertyPurchasedSuccessfully -= PlayCheer;
        GameManager.OnBuildingBuiltSuccessfully -= PlayCheer;
        GameManager.OnIslandEscapeFailed -= PlaySad;
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
            pawn.SetCharacter(slot);
            pawn.SetTurnFocus(false, false);
        }

        RefreshPawnSorting();

        if (GameManager.Instance.gameState != null)
            SetCurrentTurn(GameManager.Instance.gameState.CurrentPlayerId, false);
    }

    public void SetCurrentTurn(long playerId, bool animate = true)
    {
        foreach (KeyValuePair<long, PlayerPawn> pair in pawnsByPlayerId)
            if (pair.Value != null) pair.Value.SetTurnFocus(pair.Key == playerId, animate);
    }

    public bool TryGetPawn(long playerId, out PlayerPawn pawn)
    {
        if (pawnsByPlayerId.TryGetValue(playerId, out pawn) && pawn != null)
            return true;

        Debug.LogError($"[PlayerManager] PlayerPawn not found: {playerId}", this);
        return false;
    }

    public void PlayCheer(long playerId)
    {
        if (TryGetPawn(playerId, out PlayerPawn pawn))
            pawn.PlayCheer();
    }

    public void PlaySad(long playerId)
    {
        if (TryGetPawn(playerId, out PlayerPawn pawn))
            pawn.PlaySad();
    }

    private void HandlePlayerArrived(long playerId, int tileIndex)
    {
        TileData tile = BoardManager.Instance.GetTileData(tileIndex);
        if (tile == null) return;

        switch (tile.Type)
        {
            case TileType.ISLAND:
                PlaySad(playerId);
                break;

            case TileType.CHARITY:
                if (GameManager.Instance.gameState.WelfareFund > 0)
                    PlayCheer(playerId);
                break;

            case TileType.WORLD_TRAVEL:
                PlayCheer(playerId);
                break;

            case TileType.PROPERTY:
                PropertyState property = GameManager.Instance.GetPropertyState(tile.PropertyId);
                if (property?.OwnerId != null && property.OwnerId.Value != playerId)
                {
                    PlaySad(playerId);
                    PlayCheer(property.OwnerId.Value);
                }
                break;
        }
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
            RefreshPawnSorting();
            onCompleted?.Invoke();
            return;
        }

        StartMove(playerId, pawn, MoveByStepsRoutine(playerId, pawn, from, steps, tileCount, onTileReached, onCompleted));
    }

    /// <summary>이전 버전 호환용입니다. 말이 가진 위치를 기준으로 걷기 때문에 GameState와 어긋날 수 있습니다.</summary>
    [Obsolete("fromIndex를 받는 MoveBySteps(playerId, fromIndex, steps, ...)를 사용하세요.")]
    public void MoveBySteps(long playerId, int steps, Action<int> onTileReached, Action onCompleted)
    {
        int from = TryGetPawn(playerId, out PlayerPawn pawn) ? pawn.CurrentTileIndex : 0;
        MoveBySteps(playerId, from, steps, onTileReached, onCompleted);
    }
    
    /// <summary>
    /// fromIndex에서 toIndex까지 타일을 따라 시계방향으로 빠르게 미끄러져 이동합니다. (점프 없음)
    /// 세계여행, 황금열쇠 지정 위치 이동처럼 출발지를 지나면 월급을 받을 수 있는 지정 위치 이동에 사용합니다.
    /// 칸을 지날 때마다 onTileReached(칸 index), 끝나면 onCompleted.
    /// </summary>
    public void MoveAlongTiles(long playerId, int fromIndex, int toIndex, Action<int> onTileReached, Action onCompleted)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn))
        {
            onCompleted?.Invoke();   // 말이 없어도 게임이 멈추지 않게
            return;
        }

        int tileCount = boardController.TileCount;
        int from = Mod(fromIndex, tileCount);
        int to = Mod(toIndex, tileCount);
        int steps = Mod(to - from, tileCount); // 시계방향 칸 수

        if (steps <= 0)
        {
            StopMove(playerId);
            pawn.Move(from, GetPawnPosition(playerId, from));
            RefreshPawnSorting();
            onCompleted?.Invoke();
            return;
        }

        StartMove(playerId, pawn, MoveAlongTilesRoutine(playerId, pawn, from, steps, tileCount, onTileReached, onCompleted));
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
        StartMove(playerId, pawn, MoveToTileRoutine(playerId, pawn, target, onCompleted));
    }

    /// <summary>연출 없이 즉시 배치합니다. (재접속, 서버 상태 동기화 등)</summary>
    public void SnapToTile(long playerId, int tileIndex)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn)) return;

        StopMove(playerId);
        int target = Mod(tileIndex, boardController.TileCount);
        pawn.Move(target, GetPawnPosition(playerId, target));
        RefreshPawnSorting();
    }
    
    /// <summary>파산 시 말 숨기기</summary>
    public void HidePawn(long playerId)
    {
        if (!TryGetPawn(playerId, out PlayerPawn pawn)) return;

        StopMove(playerId);
        pawn.gameObject.SetActive(false);
        RefreshPawnSorting();
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

    private IEnumerator MoveAlongTilesRoutine(long playerId, PlayerPawn pawn, int from, int steps, int tileCount,
        Action<int> onTileReached, Action onCompleted)
    {
        int current = from;
        pawn.Move(current, GetPawnPosition(playerId, current)); // 시작점을 GameState 기준으로 맞춤

        for (int i = 0; i < steps; i++)
        {
            int next = (current + 1) % tileCount;
            yield return pawn.GlideTo(next, GetPawnPosition(playerId, next), glideStepDuration);
            current = next;
            onTileReached?.Invoke(next);
        }

        pawn.Move(current, GetPawnPosition(playerId, current)); // 도착 위치 스냅 (오차 제거)

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
    private void StartMove(long playerId, PlayerPawn pawn, IEnumerator routine)
    {
        if (StopMove(playerId))
            Debug.LogWarning($"[PlayerManager] {playerId}의 이전 이동이 끝나기 전에 새 이동이 들어와 중단했습니다.", this);

        pawn.SetSortingOrder(MovingPawnSortingOrder);
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
        RefreshPawnSorting();
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

    private void RefreshPawnSorting()
    {
        List<long> sortedPlayerIds = new();

        foreach (KeyValuePair<long, PlayerPawn> pair in pawnsByPlayerId)
        {
            if (pair.Value != null && pair.Value.gameObject.activeSelf && !runningMoves.ContainsKey(pair.Key))
                sortedPlayerIds.Add(pair.Key);
        }

        sortedPlayerIds.Sort((leftPlayerId, rightPlayerId) =>
        {
            PlayerPawn leftPawn = pawnsByPlayerId[leftPlayerId];
            PlayerPawn rightPawn = pawnsByPlayerId[rightPlayerId];
            Vector3 leftPosition = GetPawnPosition(leftPlayerId, leftPawn.CurrentTileIndex);
            Vector3 rightPosition = GetPawnPosition(rightPlayerId, rightPawn.CurrentTileIndex);

            int yComparison = rightPosition.y.CompareTo(leftPosition.y);
            if (yComparison != 0) return yComparison;

            int xComparison = leftPosition.x.CompareTo(rightPosition.x);
            if (xComparison != 0) return xComparison;

            return slotByPlayerId[leftPlayerId].CompareTo(slotByPlayerId[rightPlayerId]);
        });

        for (int i = 0; i < sortedPlayerIds.Count; i++)
            pawnsByPlayerId[sortedPlayerIds[i]].SetSortingOrder(PawnSortingBaseOrder + i);
    }

    private static int Mod(int value, int count)
    {
        return count <= 0 ? 0 : ((value % count) + count) % count;
    }

    public void RefreshPawns(List<PlayerState> playerStates)
    {
        int maxPlayers = playerStates.Count;

        for (int i = 0; i < maxPlayers; i++)
        {
            long playerId =  playerStates[i].PlayerId;
            int tileIndex = playerStates[i].Position;

            SnapToTile(playerId, tileIndex);
        }
    }
}
