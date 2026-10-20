using System;

[Serializable]
public class RoomUpdatedBroadcast
{
    public string type;
    public RoomSnapshot room;
}
