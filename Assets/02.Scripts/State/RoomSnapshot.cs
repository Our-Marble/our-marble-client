using System;
using System.Collections.Generic;

[Serializable]
public class RoomSnapshot
{
    public string roomId;
    public long hostId;
    public int mapId;
    public RoomStatus status;
    public GameConfig config;
    public List<PlayerSnapshot> Players;
    public int maxPlayers;
    public int botCount;
}

public enum RoomStatus
{
    WAITING,
    PLAYING
}

public class PlayerSnapshot
{
    public long playerId;
    public String nickname;
    public bool ready;
}