using System;

[Serializable]
public class RoomConfigRequest
{
    public string type;
    public GameConfig config;
    public int maxPlayers;
    public int botCount;
}