using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 플레이어 초상화 목록과 내가 고른 초상화. 그림은 Resources/Portraits에 portrait_번호_이름.png로 둔다.
/// 앞의 4장(0~3)은 플레이어 색(분홍, 파랑, 민트, 노랑) 순서의 기본 초상화로, 고르지 않은 사람을 보여 줄 때 쓴다.
/// 내가 고른 번호는 이 기기에 저장한다. (서버에는 아직 보내지 않는다)
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

    /// <summary>내가 고른 초상화 번호. 고른 적이 없으면 0.</summary>
    public static int SelectedIndex => Count == 0 ? 0 : Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, Count - 1);

    public static Sprite Selected => Get(SelectedIndex);

    public static Sprite Get(int index) => index >= 0 && index < Count ? All[index] : null;

    /// <summary>고르지 않은(또는 알 수 없는) 사람의 기본 초상화. 플레이어 색 번호(0~3)로 정한다.</summary>
    public static Sprite Default(int colorIndex) => Get(Mathf.Max(0, colorIndex) % Mathf.Max(1, Mathf.Min(4, Count)));

    public static void Select(int index)
    {
        if (index < 0 || index >= Count || index == SelectedIndex) return;
        PlayerPrefs.SetInt(SelectedKey, index);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
