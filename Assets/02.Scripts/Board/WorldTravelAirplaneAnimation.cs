using System.Collections;
using UnityEngine;

public class WorldTravelAirplaneAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Vector3 startOffset = new(-1.7f, -0.45f, 0f);
    [SerializeField] private Vector3 endOffset = new(1.7f, 0.55f, 0f);
    [SerializeField, Min(0.1f)] private float duration = 0.9f;
    [SerializeField, Range(0.1f, 1f)] private float startScale = 0.7f;
    [SerializeField] private Transform[] clouds;
    [SerializeField, Min(0f)] private float cloudFloatHeight = 0.25f;
    [SerializeField, Min(0f)] private float cloudFloatSpeed = 1.2f;

    private Vector3 originPosition;
    private Vector3 originScale;
    private Color originColor;
    private Vector3[] cloudOriginPositions;
    private Transform planeTransform;
    private bool initialized;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        ResetVisual();
        ResetClouds();
    }

    private void Update()
    {
        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null) continue;

            float phase = i * Mathf.PI;
            float offset = Mathf.Sin(Time.time * cloudFloatSpeed + phase) * cloudFloatHeight;
            clouds[i].localPosition = cloudOriginPositions[i] + Vector3.up * offset;
        }
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        ResetVisual();
        ResetClouds();
    }

    public void PlayAnimation()
    {
        if (!isActiveAndEnabled) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        ResetVisual();
        animationCoroutine = StartCoroutine(Play());
    }

    private void Initialize()
    {
        if (initialized) return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        planeTransform = spriteRenderer.transform;
        originPosition = planeTransform.localPosition;
        originScale = planeTransform.localScale;
        originColor = spriteRenderer.color;

        cloudOriginPositions = new Vector3[clouds.Length];
        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] != null)
                cloudOriginPositions[i] = clouds[i].localPosition;
        }

        initialized = true;
    }

    private IEnumerator Play()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            planeTransform.localPosition = originPosition + Vector3.LerpUnclamped(startOffset, endOffset, easedProgress);
            planeTransform.localScale = originScale * Mathf.Lerp(startScale, 1f, easedProgress);

            Color color = originColor;
            color.a *= Mathf.Sin(progress * Mathf.PI);
            spriteRenderer.color = color;

            yield return null;
        }

        ResetVisual();
        animationCoroutine = null;
    }

    private void ResetVisual()
    {
        if (!initialized) return;

        planeTransform.localPosition = originPosition;
        planeTransform.localScale = originScale;
        spriteRenderer.color = originColor;
    }

    private void ResetClouds()
    {
        if (!initialized) return;

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] != null)
                clouds[i].localPosition = cloudOriginPositions[i];
        }
    }
}
