using System.Collections;
using UnityEngine;

public class TaxCoinBurstAnimation : MonoBehaviour
{
    [SerializeField] private Transform atm;
    [SerializeField] private SpriteRenderer coinTemplate;
    [SerializeField, Min(1)] private int coinCount = 18;
    [SerializeField, Min(0.1f)] private float atmBounceDuration = 0.8f;
    [SerializeField, Min(0f)] private float atmJumpHeight = 0.35f;
    [SerializeField, Range(0f, 0.8f)] private float atmSquashAmount = 0.38f;
    [SerializeField, Range(0f, 45f)] private float atmWobbleAngle = 16f;
    [SerializeField, Min(0f)] private float emitDuration = 0.35f;
    [SerializeField] private Vector2 horizontalSpeedRange = new(-3.2f, 3.2f);
    [SerializeField] private Vector2 upwardSpeedRange = new(3.8f, 6.5f);
    [SerializeField] private Vector2 coinLifetimeRange = new(0.65f, 1.05f);
    [SerializeField] private float gravity = -9f;

    private Vector3 atmOriginPosition;
    private Vector3 atmOriginScale;
    private Quaternion atmOriginRotation;
    private Vector3 coinSpawnPosition;
    private Vector3 coinOriginScale;
    private Quaternion coinOriginRotation;
    private Color coinOriginColor;
    private bool coinTemplateWasEnabled;
    private CoinParticle[] particles;
    private Coroutine animationCoroutine;
    private bool initialized;

    private sealed class CoinParticle
    {
        public GameObject GameObject;
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 Velocity;
        public float Delay;
        public float Lifetime;
        public float RotationSpeed;
        public float Scale;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        ResetEffect();
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        ResetEffect();
    }

    public void PlayAnimation()
    {
        if (!isActiveAndEnabled || atm == null || coinTemplate == null) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        ResetEffect();
        PrepareParticles();
        animationCoroutine = StartCoroutine(Play());
    }

    private void Initialize()
    {
        if (initialized || atm == null || coinTemplate == null) return;

        atmOriginPosition = atm.localPosition;
        atmOriginScale = atm.localScale;
        atmOriginRotation = atm.localRotation;
        coinSpawnPosition = coinTemplate.transform.localPosition;
        coinOriginScale = coinTemplate.transform.localScale;
        coinOriginRotation = coinTemplate.transform.localRotation;
        coinOriginColor = coinTemplate.color;
        coinTemplateWasEnabled = coinTemplate.enabled;

        particles = new CoinParticle[coinCount];
        for (int i = 0; i < particles.Length; i++)
        {
            GameObject coin = new($"CoinParticle_{i}");
            coin.layer = coinTemplate.gameObject.layer;
            coin.transform.SetParent(transform, false);

            SpriteRenderer renderer = coin.AddComponent<SpriteRenderer>();
            renderer.sprite = coinTemplate.sprite;
            renderer.sharedMaterial = coinTemplate.sharedMaterial;
            renderer.sortingLayerID = coinTemplate.sortingLayerID;
            renderer.sortingOrder = coinTemplate.sortingOrder + 1;
            renderer.color = coinOriginColor;

            coin.SetActive(false);
            particles[i] = new CoinParticle
            {
                GameObject = coin,
                Transform = coin.transform,
                Renderer = renderer
            };
        }

        initialized = true;
    }

    private void PrepareParticles()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            CoinParticle particle = particles[i];
            particle.Delay = particles.Length == 1 ? 0f : emitDuration * i / (particles.Length - 1f);
            particle.Lifetime = Random.Range(coinLifetimeRange.x, coinLifetimeRange.y);
            particle.Velocity = new Vector3(
                Random.Range(horizontalSpeedRange.x, horizontalSpeedRange.y),
                Random.Range(upwardSpeedRange.x, upwardSpeedRange.y),
                0f);
            particle.RotationSpeed = Random.Range(-540f, 540f);
            particle.Scale = Random.Range(0.65f, 1.15f);
            particle.Transform.localPosition = coinSpawnPosition;
            particle.Transform.localRotation = coinOriginRotation;
            particle.Transform.localScale = coinOriginScale * particle.Scale;
            particle.Renderer.color = coinOriginColor;
        }
    }

    private IEnumerator Play()
    {
        float longestLifetime = Mathf.Max(coinLifetimeRange.x, coinLifetimeRange.y);
        float totalDuration = Mathf.Max(atmBounceDuration, emitDuration + longestLifetime);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            AnimateAtm(elapsed);
            AnimateCoins(elapsed);
            yield return null;
        }

        ResetEffect();
        animationCoroutine = null;
    }

    private void AnimateAtm(float elapsed)
    {
        float progress = Mathf.Clamp01(elapsed / atmBounceDuration);
        float damping = 1f - progress;
        float pulse = Mathf.Sin(progress * Mathf.PI * 6f) * damping;
        float squash = Mathf.Sin(progress * Mathf.PI * 6f + Mathf.PI * 0.5f) * atmSquashAmount * damping;

        atm.localPosition = atmOriginPosition
            + Vector3.up * (Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 3f)) * atmJumpHeight * damping);
        atm.localScale = new Vector3(
            atmOriginScale.x * Mathf.Max(0.2f, 1f + pulse * 0.42f + squash),
            atmOriginScale.y * Mathf.Max(0.2f, 1f + pulse * 0.42f - squash),
            atmOriginScale.z);
        atm.localRotation = atmOriginRotation * Quaternion.Euler(
            0f,
            0f,
            Mathf.Sin(progress * Mathf.PI * 8f) * atmWobbleAngle * damping);
    }

    private void AnimateCoins(float elapsed)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            CoinParticle particle = particles[i];
            float age = elapsed - particle.Delay;

            if (age < 0f || age >= particle.Lifetime)
            {
                particle.GameObject.SetActive(false);
                continue;
            }

            if (!particle.GameObject.activeSelf)
                particle.GameObject.SetActive(true);

            float progress = age / particle.Lifetime;
            particle.Transform.localPosition = coinSpawnPosition
                + particle.Velocity * age
                + Vector3.up * (0.5f * gravity * age * age);
            particle.Transform.localRotation = coinOriginRotation
                * Quaternion.Euler(0f, 0f, particle.RotationSpeed * age);
            particle.Transform.localScale = coinOriginScale
                * particle.Scale
                * (1f + Mathf.Sin(progress * Mathf.PI * 4f) * 0.12f);

            Color color = coinOriginColor;
            color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, progress));
            particle.Renderer.color = color;
        }
    }

    private void ResetEffect()
    {
        if (!initialized) return;

        atm.localPosition = atmOriginPosition;
        atm.localScale = atmOriginScale;
        atm.localRotation = atmOriginRotation;
        coinTemplate.enabled = coinTemplateWasEnabled;

        for (int i = 0; i < particles.Length; i++)
            particles[i].GameObject.SetActive(false);
    }
}
