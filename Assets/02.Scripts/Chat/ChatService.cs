using System;
using System.Collections.Generic;

/// <summary>채팅 메시지 한 줄.</summary>
public struct ChatMessage
{
    public string Sender;     // 보낸 사람 닉네임
    public int ColorIndex;    // 보낸 사람의 플레이어 색 번호 (0~3)
    public string Text;
    public bool IsLocal;      // 내가 보낸 메시지인지
    public DateTime Time;     // 보낸 시각
}

/// <summary>
/// 대기방, 로비, 인게임이 함께 쓰는 채팅 창구. 화면은 이 클래스로 보내고 받기만 한다.
/// 서버 채팅이 생기면 SendRequested에 전송 함수를 달고, 서버에서 받은 메시지를 Receive로 넘기면 된다.
/// SendRequested가 비어 있으면 보낸 메시지를 그대로 되돌려 받아(로컬 에코) 혼자서도 동작한다.
/// </summary>
public static class ChatService
{
    public const int MaxLength = 60;      // 한 번에 보낼 수 있는 글자 수
    private const int MaxHistory = 100;   // 다시 열었을 때 보여 줄 지난 메시지 수

    private static readonly List<ChatMessage> history = new List<ChatMessage>();

    /// <summary>메시지가 도착했다(내가 보낸 것 포함). 화면이 로그와 말풍선을 갱신한다.</summary>
    public static event Action<ChatMessage> MessageReceived;

    /// <summary>서버로 보내는 함수. 달려 있으면 로컬 에코 대신 이 함수가 쓰인다.</summary>
    public static Action<ChatMessage> SendRequested;

    public static IReadOnlyList<ChatMessage> History => history;

    /// <summary>내 메시지를 보낸다. 빈 글과 공백뿐인 글은 무시한다.</summary>
    public static void Send(string sender, int colorIndex, string text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length > MaxLength) text = text.Substring(0, MaxLength);

        var message = new ChatMessage { Sender = sender, ColorIndex = colorIndex, Text = text, IsLocal = true };
        if (SendRequested != null) SendRequested(message);
        else Receive(message);
    }

    /// <summary>도착한 메시지를 기록하고 알린다. 서버에서 받은 메시지와 로컬 에코가 여기로 온다.</summary>
    public static void Receive(ChatMessage message)
    {
        if (message.Time == default) message.Time = DateTime.Now;
        history.Add(message);
        if (history.Count > MaxHistory) history.RemoveAt(0);
        MessageReceived?.Invoke(message);
    }

    /// <summary>방을 나갔을 때 지난 메시지를 비운다.</summary>
    public static void Clear() => history.Clear();
}
