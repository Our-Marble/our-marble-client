/// <summary>
/// 유저 매각 창의 자동 선택 방식입니다. 금액 기준은 모두 매각가(투자금의 50%)입니다.
/// 자동 선택은 팔 땅을 골라 보여주기만 하고, 실제 매각은 유저가 확인하면 진행합니다.
/// </summary>
public enum SellSelectionMode
{
    CheapestFirst,       // 싼 땅부터: 매각가가 낮은 땅부터 부족분이 찰 때까지
    MostExpensiveFirst,  // 비싼 땅부터: 매각가가 높은 땅부터 부족분이 찰 때까지
    FewestProperties,    // 적게 팔기: 가장 적은 개수로 채우는 조합 (개수가 같으면 매각가 합이 작은 쪽)
    ProtectIncome,       // 수익 지키기: 잃는 기대 통행료가 적은 땅부터 (봇 매각 판단과 동일)
    ProtectTourist,      // 관광지 지키기: 관광지(건설 불가 땅)를 빼고 싼 땅부터, 부족하면 관광지 추가
}