using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class GameState
{
    public int TurnNumber { get; set; }   // 지금까지 진행된 차례 수 (플레이어 한 명의 차례마다 1 증가)
    public int RoundNumber { get; set; }  // 현재 라운드 수. 살아 있는 플레이어가 모두 한 번씩 하면 1 증가
    public long CurrentPlayerId { get; set; }
    
    public long WelfareFund { get; set; }
    
    [JsonProperty("phase")]
    public GameMessageType? WaitingType { get; set; }
    
    public bool IsDouble { get; set; }
    
    public int ConsecutiveDoubleCount { get; set; }
    
    public bool IsGameOver { get; set; }
    
    public List<long> PlayerOrder { get; set; }
    
    public List<PlayerState> PlayerStates { get; set; }
    
    public List<PropertyState> PropertyStates{ get; set; }
    
    public GameConfig Config { get; set; }
}
