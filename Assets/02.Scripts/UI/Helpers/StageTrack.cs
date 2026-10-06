using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "건물 + ★★★" 단계 표시 묶음. 구매·인수·타일 정보 팝업이 같이 쓴다.
/// BuildingLevel: Land=건물만, Villa=★1, Building=★2, Hotel=★3.
/// </summary>
[Serializable]
public class StageTrack
{
    [SerializeField] private Image[] stars = new Image[3];
    [SerializeField] private RectTransform newStarGlow;

    /// <summary>track 아래 Star_1~3을 찾는다. 별 이미지는 Star_N 또는 Star_N/Icon에 있다.</summary>
    public void Bind(Transform track)
    {
        if (track == null) return;
        for (int i = 0; i < 3; i++)
        {
            var star = track.Find($"Star_{i + 1}");
            if (star == null) continue;
            var icon = star.Find("Icon");
            stars[i] = (icon != null ? icon : star).GetComponent<Image>();

            var glow = star.Find("Glow");
            if (glow != null) newStarGlow = (RectTransform)glow;
        }
    }

    /// <summary>구매 대상 별 뒤에 붙은 빛. 없으면 null.</summary>
    public RectTransform NewStarGlow => newStarGlow;

    /// <summary>단계의 표시 오브젝트. Land면 "건물" 칩, Villa~Hotel이면 해당 별.</summary>
    public Transform GetStageTransform(BuildingLevel level)
    {
        int index = (int)level - 1;
        if (index >= 0) return index < stars.Length && stars[index] != null ? StarRoot(stars[index]) : null;

        // 건물 칩은 별들과 같은 부모(StageTrack) 아래에 있다
        var first = Array.Find(stars, s => s != null);
        var track = first != null ? StarRoot(first).parent : null;
        return track != null ? track.Find("BuildingStage") : null;
    }

    private static Transform StarRoot(Image star) => star.transform.name == "Icon" ? star.transform.parent : star.transform;

    /// <summary>현재 단계까지 빨간 별. highlightNext면 다음 별(구매 대상)도 빨갛게 하고 빛을 붙인다.</summary>
    public void Set(BuildingLevel level, bool highlightNext = false)
    {
        int owned = (int)level;
        int next = highlightNext ? owned + 1 : -1;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            int starLevel = i + 1;
            stars[i].color = starLevel <= owned || starLevel == next ? UIPalette.Red : UIPalette.StarEmpty;
        }

        if (newStarGlow == null) return;
        bool showGlow = next >= 1 && next <= stars.Length && stars[next - 1] != null;
        newStarGlow.gameObject.SetActive(showGlow);
        if (showGlow)
        {
            // 빛을 구매 대상 별 뒤로 옮긴다
            var starRoot = stars[next - 1].transform.name == "Icon" ? stars[next - 1].transform.parent : stars[next - 1].transform;
            newStarGlow.SetParent(starRoot, false);
            newStarGlow.SetAsFirstSibling();
        }
    }
}
