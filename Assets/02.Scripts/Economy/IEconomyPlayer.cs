using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경제 로직이 플레이어에게 요구하는 기능 목록.
/// 돈과 보유 목록은 Player가 보관하고, 경제 로직은 이 인터페이스로 요청만 한다.
/// Player 클래스가 이 인터페이스를 구현한다.
/// </summary>
public interface IEconomyPlayer
{
    // 표시용
    string PlayerName { get; }
    Color PlayerColor { get; }

    // 돈
    long Money { get; }
    void SpendMoney(long amount);
    void AddMoney(long amount);

    // 보유 부동산 (칸 번호로 관리)
    IReadOnlyList<int> OwnedTileIndices { get; }
    void AddProperty(int tileIndex);
    void RemoveProperty(int tileIndex);
}