using System;
using System.Collections.Generic;

[Serializable]
public class RoomListResponse
{
    public string type;
    public List<RoomSummary> rooms;
}
