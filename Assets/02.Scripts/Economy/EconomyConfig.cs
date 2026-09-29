using UnityEngine;

/// <summary>
/// 공통 경제 수치 틀. 게임 중 바뀌지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "EconomyConfig", menuName = "Economy/EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Tooltip("매각가 비율 (예: 0.5 = 투자금의 50%)")]
    [SerializeField] private float sellRate = 0.5f;

    [Tooltip("인수가 배수 (예: 2 = 투자금의 2배)")]
    [SerializeField] private float takeoverRate = 2f;

    [Tooltip("이 단계까지만 인수 가능")]
    [SerializeField] private BuildingLevel takeoverMaxLevel = BuildingLevel.Building;

    public float SellRate => sellRate;
    public float TakeoverRate => takeoverRate;
    public BuildingLevel TakeoverMaxLevel => takeoverMaxLevel;
}