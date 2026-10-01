using System.Collections;
using UnityEngine;

public class PlayerPawn : MonoBehaviour
{
    [SerializeField] private int currentTileIndex = -1;
    [SerializeField] private float hopHeight = 0.25f;

    public int CurrentTileIndex => currentTileIndex;

    /// <summary>즉시 이동 (초기 배치용)</summary>
    public void Move(int tileIndex, Vector3 targetPosition)
    {
        transform.position = targetPosition;
        currentTileIndex = tileIndex;
    }

    /// <summary>한 칸 점프 이동 (주사위 이동용)</summary>
    public IEnumerator HopTo(int tileIndex, Vector3 targetPosition, float duration)
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float height = Mathf.Sin(t * Mathf.PI) * hopHeight;
            transform.position = Vector3.Lerp(start, targetPosition, t) + Vector3.up * height;
            yield return null;
        }
        Move(tileIndex, targetPosition);
    }

    /// <summary>목적지로 한 번에 미끄러지는 이동 (자유여행, 무인도용)</summary>
    public IEnumerator SlideTo(int tileIndex, Vector3 targetPosition, float duration)
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            transform.position = Vector3.Lerp(start, targetPosition, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        Move(tileIndex, targetPosition);
    }
}
