using System.Collections;
using UnityEngine;

public class IslandBounceAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Min(0.1f)] private float duration = 0.65f;
    [SerializeField, Min(0f)] private float bounceStrength = 6f;
    [SerializeField, Min(0f)] private float bounceSpeed = 10f;
    [SerializeField, Min(0f)] private float jumpHeight = 0.18f;
    [SerializeField, Range(0f, 0.5f)] private float squashAmount = 0.22f;
    [SerializeField, Range(0f, 30f)] private float wobbleAngle = 8f;

    private Vector3 originPosition;
    private Vector3 originScale;
    private Quaternion originRotation;
    private Color originColor;
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
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        ResetVisual();
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

        originPosition = transform.localPosition;
        originScale = transform.localScale;
        originRotation = transform.localRotation;
        originColor = spriteRenderer.color;
        initialized = true;
    }

    private IEnumerator Play()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float scale = 1f - Mathf.Exp(-bounceStrength * progress) * Mathf.Cos(bounceSpeed * progress);
            float damping = 1f - progress;
            float squash = Mathf.Sin(progress * Mathf.PI * 3f) * squashAmount * damping;
            float wobble = Mathf.Sin(progress * Mathf.PI * 4f) * wobbleAngle * damping;

            transform.localPosition = originPosition + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * jumpHeight);
            transform.localScale = new Vector3(
                originScale.x * scale * (1f + squash),
                originScale.y * scale * (1f - squash),
                originScale.z * scale);
            transform.localRotation = originRotation * Quaternion.Euler(0f, 0f, wobble);

            Color color = originColor;
            color.a *= Mathf.Clamp01(progress * 4f);
            spriteRenderer.color = color;

            yield return null;
        }

        ResetVisual();
        animationCoroutine = null;
    }

    private void ResetVisual()
    {
        if (!initialized) return;

        transform.localPosition = originPosition;
        transform.localScale = originScale;
        transform.localRotation = originRotation;
        spriteRenderer.color = originColor;
    }
}
