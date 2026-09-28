using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public int TurnNumber { get; set; }
    public long CurrentPlayerId { get; set; }
    
    public long WelfareFund { get; set; }
    
    public List<PlayerState> PlayerStates { get; set; }
    
    public List<PropertyState> PropertyStates{ get; set; }
    
    public GameState()
    {
        TurnNumber = 0;
        CurrentPlayerId = 0;
        WelfareFund = 0;

        PlayerStates = new List<PlayerState>();
        PropertyStates = new List<PropertyState>();
    }
}
