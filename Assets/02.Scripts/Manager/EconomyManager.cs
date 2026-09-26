using UnityEngine;

/// <summary>
/// 경제 로직의 창구. 이동 쪽·UI는 여기만 호출한다.
/// 순서: 판정(Handler) → 유저 쪽 처리(Player) → 필드 쪽 처리(PropertyTile)
/// 직접 값을 바꾸지 않고 Player와 PropertyTile에 시킨다.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    private PropertyManager Properties => PropertyManager.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ───────────── 입구 ─────────────
    // TODO: 타입 추가 후 구현
    //
    // 플레이어가 칸에 도착했을 때. 칸 상태에 따른 선택지를 돌려준다.
    //   주인 없음 → 구매 가능 여부와 가격
    //   내 땅     → 건설 가능 단계와 각 비용
    //   남의 땅   → 통행료 자동 지불 (돈 부족 시 강제매각 → 파산)
    // public LandingResult OnLanded(IEconomyPlayer player, int tileIndex)
    //
    // 칸 정보 조회. 소유자, 단계, 통행료, 땅값, 건설비.
    // public TileInfo GetTileInfo(int tileIndex)

    // ───────────── 행동 ─────────────
    // TODO: 타입 추가 후 구현
    //
    // 구매: 판정 → Player 돈 차감·목록 추가 → Tile 소유자 변경
    // public bool Purchase(IEconomyPlayer player, int tileIndex)
    //
    // 건설: 판정 → Player 돈 차감 → Tile 단계 상승
    // public bool Build(IEconomyPlayer player, int tileIndex, BuildingLevel targetLevel)

    // ───────────── 판정 (Handler 호출) ─────────────
    // TODO: Handler 추가 후 구현
    //
    // public bool CanPurchase(IEconomyPlayer player, int tileIndex)      → PurchaseHandler
    // public long GetToll(int tileIndex)                                 → TollCalculator
    // public List<BuildOption> GetBuildOptions(IEconomyPlayer player, int tileIndex) → BuildHandler
}