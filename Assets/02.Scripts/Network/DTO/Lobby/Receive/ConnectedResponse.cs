using System;

[Serializable]
public class ConnectedResponse
{
    public string type;
    public long playerId;
    public string nickname;
    public int? portraitId; // 서버에 저장된 내 초상화 번호. 저장한 적이 없으면 없음
}
