using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 플레이어 초상화 목록과 내가 고른 초상화. 그림은 Resources/Portraits에 portrait_번호_이름.png로 둔다.
/// 앞의 4장(0~3)은 플레이어 색(분홍, 파랑, 민트, 노랑) 순서의 기본 초상화로, 고르지 않은 사람을 보여 줄 때 쓴다.
/// 내가 고른 번호는 서버에 저장하고(SET_PORTRAIT), 접속할 때 CONNECTED로 돌려받는다. 이 기기에도 남겨 서버 응답 전에 바로 보여 준다.
/// </summary>
public static class PortraitLibrary
{
    private const string SelectedKey = "Portrait.Selected";

    private static List<Sprite> sprites;

    /// <summary>초상화를 골라서 바뀌었을 때.</summary>
    public static event Action Changed;

    public static IReadOnlyList<Sprite> All
    {
        get
        {
            if (sprites == null)
                sprites = Resources.LoadAll<Sprite>("Portraits").OrderBy(s => s.name, StringComparer.Ordinal).ToList();
            return sprites;
        }
    }

    public static int Count => All.Count;

    private static Sprite bot;

    /// <summary>봇 전용 초상화. 로봇 그림이고, 고르는 목록(All)에는 없어서 플레이어가 고를 수 없다. (Resources/BotPortrait)</summary>
    public static Sprite Bot => bot != null ? bot : bot = Resources.Load<Sprite>("BotPortrait/portrait_bot");

    /// <summary>내가 고른 초상화 번호. 고른 적이 없으면 0.</summary>
    public static int SelectedIndex => Count == 0 ? 0 : Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, Count - 1);

    public static Sprite Selected => Get(SelectedIndex);

    public static Sprite Get(int index) => index >= 0 && index < Count ? All[index] : null;

    /// <summary>고르지 않은(또는 알 수 없는) 사람의 기본 초상화. 플레이어 색 번호(0~3)로 정한다.</summary>
    public static Sprite Default(int colorIndex) => Get(Mathf.Max(0, colorIndex) % Mathf.Max(1, Mathf.Min(4, Count)));

    /// <summary>서버가 알려 준(또는 다른 사람의) 초상화 번호의 그림. 번호가 없거나 범위 밖이면 자리 색의 기본 그림.</summary>
    public static Sprite For(int? portraitId, int colorIndex) =>
        portraitId.HasValue && Get(portraitId.Value) != null ? Get(portraitId.Value) : Default(colorIndex);

    /// <summary>서버에 저장된 내 초상화를 받아 적용한다. 서버로 다시 보내지는 않는다.</summary>
    public static void ApplyFromServer(int index) => Select(index);

    public static void Select(int index)
    {
        if (index < 0 || index >= Count || index == SelectedIndex) return;
        PlayerPrefs.SetInt(SelectedKey, index);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
