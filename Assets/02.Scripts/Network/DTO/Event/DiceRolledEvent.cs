using UnityEngine;
using Newtonsoft.Json;

public class DiceRolledEvent
{
    public long PlayerId { get; set; }
    public int Dice1 { get; set; }
    public int Dice2 { get; set; }
}
