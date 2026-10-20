using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 플레이어 초상화 목록과 내가 고른 초상화. 그림은 Resources/Portraits에 portrait_번호_이름.png로 둔다.
/// 초상화는 파일 이름의 번호(id)로 구분한다. 번호는 서버에 저장되므로 그림을 지우거나 순서를 바꿔도 다른 번호를 다시 쓰지 않는다.
/// 번호는 0번부터 빈틈없이 채운다. 그림을 추가할 때는 마지막 번호 다음 번호를 붙인다.
/// 내가 고른 번호는 서버에 저장하고(SET_PORTRAIT), 접속할 때 CONNECTED로 돌려받는다. 이 기기에도 남겨 서버 응답 전에 바로 보여 준다.
/// 고르지 않은 사람은 모두 같은 기본 초상화(0번, 고양이)를 보여 준다. 자리와 상관없이 어느 화면에서나 같다.
/// </summary>
public static class PortraitLibrary
{
    private const string SelectedKey = "Portrait.Selected";
    private static readonly Regex IdPattern = new Regex(@"^portrait_(\d+)_", RegexOptions.Compiled);

    private static SortedDictionary<int, Sprite> byId;
    private static List<int> ids;

    /// <summary>초상화를 골라서 바뀌었을 때.</summary>
    public static event Action Changed;

    private static void Load()
    {
        if (byId != null) return;
        byId = new SortedDictionary<int, Sprite>();
        foreach (var sprite in Resources.LoadAll<Sprite>("Portraits"))
        {
            var m = IdPattern.Match(sprite.name);
            if (m.Success) byId[int.Parse(m.Groups[1].Value)] = sprite;
        }
        ids = byId.Keys.ToList();
    }

    /// <summary>고를 수 있는 초상화 번호들 (작은 번호부터).</summary>
    public static IReadOnlyList<int> Ids { get { Load(); return ids; } }

    public static int Count => Ids.Count;

    /// <summary>번호가 고를 수 있는 초상화인지.</summary>
    public static bool Has(int id) { Load(); return byId.ContainsKey(id); }

    /// <summary>내가 고른 초상화 번호. 고른 적이 없거나 지금은 없는 번호면 기본 초상화 번호.</summary>
    public static int SelectedId
    {
        get
        {
            if (Count == 0) return 0;
            int saved = PlayerPrefs.GetInt(SelectedKey, -1);
            return Has(saved) ? saved : DefaultId(0);
        }
    }

    public static Sprite Selected => Get(SelectedId);

    /// <summary>번호의 그림. 없으면 null.</summary>
    public static Sprite Get(int id)
    {
        Load();
        Sprite sprite;
        return byId.TryGetValue(id, out sprite) ? sprite : null;
    }

    /// <summary>기본 초상화 번호. 모두 같은 그림(목록의 첫 번째, 0번)이다. colorIndex는 부르는 쪽을 바꾸지 않으려고 남겨 둔 인자로, 쓰지 않는다.</summary>
    public static int DefaultId(int colorIndex = 0)
    {
        return Count == 0 ? 0 : Ids[0];
    }

    /// <summary>고르지 않은(또는 알 수 없는) 사람의 기본 초상화.</summary>
    public static Sprite Default(int colorIndex) => Get(DefaultId(colorIndex));

    private static Sprite bot;

    /// <summary>봇 전용 초상화. 로봇 그림이고, 고르는 목록에는 없어서 플레이어가 고를 수 없다. (Resources/BotPortrait)</summary>
    public static Sprite Bot => bot != null ? bot : bot = Resources.Load<Sprite>("BotPortrait/portrait_bot");

    /// <summary>서버가 알려 준(또는 다른 사람의) 초상화 번호의 그림. 번호가 없거나 지금은 없는 번호면 기본 그림.</summary>
    public static Sprite For(int? portraitId, int colorIndex) =>
        portraitId.HasValue && Get(portraitId.Value) != null ? Get(portraitId.Value) : Default(colorIndex);

    /// <summary>서버에 저장된 내 초상화를 받아 적용한다. 서버로 다시 보내지는 않는다. 지금은 없는 번호(지워진 초상화 등)는 무시한다.</summary>
    public static void ApplyFromServer(int id) => Select(id);

    public static void Select(int id)
    {
        if (!Has(id) || id == PlayerPrefs.GetInt(SelectedKey, -1)) return; // 저장된 값과 같으면 바꿀 게 없다 (기본 초상화를 처음 고르는 경우는 저장한다)
        PlayerPrefs.SetInt(SelectedKey, id);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
