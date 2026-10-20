using System.Collections.Generic;
using UnityEngine;

public class UIManager_temp : MonoBehaviour
{
    // 자유여행으로 가고싶은 칸을 선택하는 창을 띄워주는 함수입니다.
    // 칸을 선택하면, GameManager.HandleDestinationChosen(int 해당칸인덱스) 를 호출해야합니다.
    public void ShowChooseDestinationPopup()
    {
        
    }
    
    // 주사위 굴리기 버튼이 있는 창을 띄워주는 함수입니다.
    // 굴리기 버튼을 누르면 GameManager.RollDice() 를 호출해야합니다.
    public void ShowRollDicePopup()
    {
        
    }
    
    // 빈 땅에 대하여  사기 & 사지않기 버튼이 있는 창을 띄워주는 함수입니다.
    // 사기 버튼을 누르면 GameManager.PurchaseProperty(playerId, propertyId) 를 호출해야합니다.
    // 사지않기 버튼을 누르면 GameManager.DeclinePropertyPurchase(playerId, propertyId) 를 호출해야합니다.
    public void ShowPurchasePropertyPopup(long playerId, long propertyId, int amount)
    {
        
    }
    
    // 건설하기 & 건설하지않기 버튼이 있는 창을 띄워주는 함수입니다.
    // 건설하기 버튼을 누르면 GameManager.Build(playerId, propertyId) 를 호출해야합니다.
    // 건설하지않기 버튼을 누르면 GameManager.DeclineBuild(playerId, propertyId) 를 호출해야합니다.
    public void ShowBuildPopup(long playerId, int propertyId)
    {
        
    }
    
    // 인수하기 & 인수하지않기 버튼이 있는 창을 띄워주는 함수입니다.
    // 인수하기 버튼을 누르면 GameManager.AcquireProperty(playerId, propertyId) 를 호출해야합니다.
    // 인수하지않기 버튼을 누르면 GameManager.DeclineAcquireProperty(playerId, propertyId) 를 호출해야합니다.
    public void ShowAcquirePropertyPopup(long playerId, int propertyId)
    {
        
    }
    
    // 매각할 자산들을 선택할 수 있는 창을 띄워주는 함수입니다.
    // 선택 완료 버튼을 누르면 GameManager.SellProperties(payerId, receiverId, List<int> propertyIds, long requiredAmount) 를 호출해야합니다.
    // 클라측에서 판단한 '선택한 자산 가치 총합' + 현금값이 requiredAmount보다 작으면 선택 완료 버튼이 활성화되지 않게끔 만들어 주시면 감사하겠습니다..
    public void ShowSellPropertiesPopup(long payerId, long receiverId, long requiredAmount)
    {
        
    }
}
