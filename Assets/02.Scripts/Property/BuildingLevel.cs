/// <summary>
/// 부동산 칸의 건물 단계.
/// 숫자는 PropertyData의 단계별 배열 인덱스로 쓴다.
/// </summary>
public enum BuildingLevel
{
    Land = 0,     // 땅만 있음 (구매 직후)
    Villa = 1,    // 별장
    Building = 2, // 빌딩
    Hotel = 3     // 호텔
}