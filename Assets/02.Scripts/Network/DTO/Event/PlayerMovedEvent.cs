using UnityEngine;

public class PlayerMovedEvent
{
    public long PlayerId { get; set; }
    public int FromPosition { get; set; }
    public int ToPosition { get; set; }   
}
