using System;

[Serializable]
public class ChatBroadcast
{
    public string type;
    public long playerId;   // 서버가 "1"처럼 문자열로 보내도 숫자로 읽힌다
    public string content;
}
