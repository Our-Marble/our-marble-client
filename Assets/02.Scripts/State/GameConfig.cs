using System;

[Serializable]
public class GameConfig
{
    public long startMoney;
    public long salaryAmount;
    public long taxAmount;
    public int maxRound;
    public int islandTurns;
    public int mapId;
    public int limitTimeSeconds;

    public GameConfig() {
        startMoney = 1000000;
        salaryAmount =  150000;
        taxAmount = 100000;
        maxRound = 20;
        islandTurns = 3;
        mapId = 1;
        limitTimeSeconds = 60;
    }
}