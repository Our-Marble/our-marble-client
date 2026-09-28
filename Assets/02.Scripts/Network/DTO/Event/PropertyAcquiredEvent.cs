using UnityEngine;

public class PropertyAcquiredEvent
{
    public int PropertyId { get; set; }
    public long PreviousOwnerId { get; set; }
    public long NewOwnerId { get; set; }
    public long Amount { get; set; }
}
