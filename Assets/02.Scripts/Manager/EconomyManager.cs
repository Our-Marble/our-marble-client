using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경제 로직의 창구. 이동 쪽·UI는 여기만 호출한다.
/// 순서: 판정 → 유저 쪽 처리(Player) → 필드 쪽 처리(PropertyTile)
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

    /// <summary>플레이어가 칸에 도착했을 때 호출. 칸 상태에 따른 선택지를 돌려준다.</summary>
    public LandingResult OnLanded(IEconomyPlayer player, int tileIndex)
    {
        var result = new LandingResult();

        if (!Properties.TryGetTile(tileIndex, out var tile))
        {
            result.Type = LandingType.NotProperty;
            return result;
        }

        var state = tile.State;

        // 주인 없음 → 구매 선택
        if (!state.IsOwned)
        {
            result.Type = LandingType.Unowned;
            result.Price = tile.Data.LandPrice;
            result.CanPurchase = CanPurchase(player, tileIndex);
            Debug.Log($"[Economy] {player.PlayerName} → {tileIndex}번 주인 없음, 가격 {result.Price}, 구매 가능 {result.CanPurchase}");
            return result;
        }

        // 내 땅 → 건설 선택
        if (state.Owner == player)
        {
            result.Type = LandingType.OwnProperty;
            result.BuildOptions = GetBuildOptions(player, tileIndex);
            Debug.Log($"[Economy] {player.PlayerName} → {tileIndex}번 내 땅, 건설 선택지 {result.BuildOptions.Count}개");
            return result;
        }

        // 남의 땅 → 통행료
        long toll = GetToll(tileIndex);
        result.Toll = toll;
        result.TollReceiver = state.Owner;

        if (player.Money < toll)
        {
            // TODO: 강제매각 → 파산 판정
            result.Type = LandingType.NeedForcedSale;
            Debug.Log($"[Economy] {player.PlayerName} → {tileIndex}번 통행료 {toll}, 돈 부족 (보유 {player.Money})");
            return result;
        }

        player.SpendMoney(toll);
        state.Owner.AddMoney(toll);
        result.Type = LandingType.TollPaid;
        Debug.Log($"[Economy] {player.PlayerName} → {state.Owner.PlayerName} 통행료 {toll} 지불");

        // TODO: 인수 가능 여부
        return result;
    }

    /// <summary>칸 정보 조회. 부동산 칸이 아니면 null.</summary>
    public TileInfo GetTileInfo(int tileIndex)
    {
        if (!Properties.TryGetTile(tileIndex, out var tile)) return null;

        var data = tile.Data;
        return new TileInfo
        {
            CityName = data.CityName,
            Owner = tile.State.Owner,
            Level = tile.State.Level,
            CurrentToll = tile.State.IsOwned ? GetToll(tileIndex) : 0,
            LandPrice = data.LandPrice,
            BuildCosts = new[]
            {
                data.GetBuildCost(BuildingLevel.Villa),
                data.GetBuildCost(BuildingLevel.Building),
                data.GetBuildCost(BuildingLevel.Hotel)
            }
        };
    }

    // ───────────── 행동 ─────────────

    /// <summary>구매. 성공하면 true.</summary>
    public bool Purchase(IEconomyPlayer player, int tileIndex)
    {
        if (!CanPurchase(player, tileIndex))
        {
            Debug.Log($"[Economy] {player.PlayerName} {tileIndex}번 구매 실패");
            return false;
        }
        Properties.TryGetTile(tileIndex, out var tile);
        long price = tile.Data.LandPrice;

        // 유저 쪽
        player.SpendMoney(price);
        player.AddProperty(tileIndex);

        // 필드 쪽
        tile.SetOwner(player);

        Debug.Log($"[Economy] {player.PlayerName} {tileIndex}번 구매 완료, -{price}, 잔액 {player.Money}");
        return true;
    }

    /// <summary>건설. targetLevel까지 한 번에 짓는다. 성공하면 true.</summary>
    public bool Build(IEconomyPlayer player, int tileIndex, BuildingLevel targetLevel)
    {
        var option = GetBuildOptions(player, tileIndex).Find(o => o.Level == targetLevel);
        if (option == null)
        {
            Debug.Log($"[Economy] {player.PlayerName} {tileIndex}번 {targetLevel} 건설 실패");
            return false;
        }
        Properties.TryGetTile(tileIndex, out var tile);

        // 유저 쪽
        player.SpendMoney(option.Cost);

        // 필드 쪽
        tile.SetLevel(targetLevel);

        Debug.Log($"[Economy] {player.PlayerName} {tileIndex}번 {targetLevel} 건설 완료, -{option.Cost}, 잔액 {player.Money}");
        return true;
    }

    // ───────────── 판정 ─────────────
    // TODO: 아래 판정은 Economy/Server/의 Handler로 옮긴다. (Manager는 호출만)

    /// <summary>TODO: PurchaseHandler로 이동</summary>
    public bool CanPurchase(IEconomyPlayer player, int tileIndex)
    {
        if (!Properties.TryGetTile(tileIndex, out var tile)) return false;
        if (tile.State.IsOwned) return false;
        return player.Money >= tile.Data.LandPrice;
    }

    /// <summary>TODO: TollCalculator로 이동</summary>
    public long GetToll(int tileIndex)
    {
        if (!Properties.TryGetTile(tileIndex, out var tile)) return 0;
        return tile.Data.GetToll(tile.State.Level);
    }

    /// <summary>지금 지을 수 있는 단계 목록. Cost는 누적 비용. TODO: BuildHandler로 이동</summary>
    public List<BuildOption> GetBuildOptions(IEconomyPlayer player, int tileIndex)
    {
        var options = new List<BuildOption>();
        if (!Properties.TryGetTile(tileIndex, out var tile)) return options;
        if (tile.State.Owner != player) return options;

        long totalCost = 0;
        for (int lv = (int)tile.State.Level + 1; lv <= (int)BuildingLevel.Hotel; lv++)
        {
            var level = (BuildingLevel)lv;
            totalCost += tile.Data.GetBuildCost(level);
            if (totalCost > player.Money) break;

            options.Add(new BuildOption { Level = level, Cost = totalCost });
        }
        return options;
    }
}