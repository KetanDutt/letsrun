using UnityEngine;

/// <summary>A fixed, reused sprite-particle pool and projection-only impact shake. No per-impact Instantiate.</summary>
public sealed class RunnerFeedback : MonoBehaviour
{
    private const int Capacity = 48;
    private sealed class Particle
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 velocity;
        public Color color;
        public float remaining, lifetime, scale, spin;
    }

    private readonly Particle[] particles = new Particle[Capacity];
    private Sprite dot;
    private Texture2D dotTexture;
    private Camera view;
    private float dustTimer, shakeRemaining, shakeDuration, shakeStrength;
    private Matrix4x4 restProjection;
    private bool projectionOverridden;

    private bool Reduced { get { return GameManager.instance != null && GameManager.instance.ReducedMotion; } }

    public void Initialize(Camera camera)
    {
        view = camera;
        dotTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        dotTexture.name = "Runner Soft Particle";
        dotTexture.filterMode = FilterMode.Bilinear;
        var colors = new Color32[256];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float distance = new Vector2(x - 7.5f, y - 7.5f).magnitude;
                colors[y * 16 + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01((7.5f - distance) / 2f) * 255f));
            }
        dotTexture.SetPixels32(colors);
        dotTexture.Apply(false, true);
        dot = Sprite.Create(dotTexture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        for (int i = 0; i < Capacity; i++)
        {
            var renderer = new GameObject("Feedback Particle " + i).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.sprite = dot;
            renderer.sortingLayerName = "Main";
            renderer.sortingOrder = 20;
            renderer.gameObject.SetActive(false);
            particles[i] = new Particle { transform = renderer.transform, renderer = renderer };
        }
    }

    private void Update()
    {
        float delta = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        for (int i = 0; i < particles.Length; i++)
        {
            Particle particle = particles[i];
            if (particle == null || particle.remaining <= 0f) continue;
            particle.remaining -= delta;
            if (particle.remaining <= 0f) { particle.renderer.gameObject.SetActive(false); continue; }
            if (!Reduced)
            {
                particle.velocity += Vector3.down * (delta * 1.2f);
                particle.transform.position += particle.velocity * delta;
                particle.transform.Rotate(0f, 0f, particle.spin * delta);
            }
            float life = particle.remaining / particle.lifetime;
            Color color = particle.color;
            color.a *= life;
            particle.renderer.color = color;
            particle.transform.localScale = Vector3.one * (particle.scale * Mathf.Lerp(0.4f, 1f, life));
        }
        GameplayController controller = GameplayController.instance;
        if (Reduced || controller == null || controller.State != RunState.Running || PlayerController.instance == null) return;
        dustTimer -= Time.deltaTime;
        if (dustTimer > 0f) return;
        dustTimer = PlayerController.instance.IsPowered ? 0.06f : 0.13f;
        GameObject shadow = PlayerController.instance.shadow;
        Vector3 position = shadow != null ? shadow.transform.position : PlayerController.instance.transform.position;
        Emit(position, PlayerController.instance.IsPowered ? UIFactory.Teal : new Color(1f, 0.96f, 0.8f, 0.22f), 1, 0.4f, 0.28f, 0.12f);
    }

    private void LateUpdate()
    {
        if (view == null || !projectionOverridden) return;
        if (Reduced || shakeRemaining <= 0f)
        {
            view.ResetProjectionMatrix();
            projectionOverridden = false;
            return;
        }
        shakeRemaining -= Time.unscaledDeltaTime;
        float strength = shakeStrength * Mathf.Clamp01(shakeRemaining / shakeDuration);
        Matrix4x4 projection = restProjection;
        projection.m03 += Mathf.Sin(Time.unscaledTime * 73f) * strength;
        projection.m13 += Mathf.Cos(Time.unscaledTime * 61f) * strength * 0.7f;
        view.projectionMatrix = projection;
    }

    public void Collect(Vector3 position) { Emit(position, UIFactory.Gold, 8, 2.1f, 0.4f, 0.13f); }
    public void PowerUp(Vector3 position) { Emit(position, UIFactory.Teal, 16, 3f, 0.6f, 0.2f); }
    public void Impact(Vector3 position, bool fatal)
    {
        Emit(position, fatal ? UIFactory.Coral : UIFactory.Gold, fatal ? 20 : 12, 3.6f, 0.5f, 0.2f);
        if (Reduced || view == null) return;
        if (!projectionOverridden) restProjection = view.projectionMatrix;
        projectionOverridden = true;
        shakeDuration = shakeRemaining = fatal ? 0.24f : 0.12f;
        shakeStrength = fatal ? 0.012f : 0.006f;
    }

    public void Celebrate()
    {
        if (view == null) return;
        Vector3 position = view.ViewportToWorldPoint(new Vector3(0.5f, 0.67f, 10f));
        Emit(position, UIFactory.Gold, 24, 5f, 1f, 0.2f);
    }

    private void Emit(Vector3 position, Color color, int count, float speed, float lifetime, float scale)
    {
        if (Reduced) { count = Mathf.Min(count, 3); speed = 0f; lifetime = 0.22f; }
        for (int i = 0; i < particles.Length && count > 0; i++)
        {
            Particle particle = particles[i];
            if (particle == null || particle.remaining > 0f) continue;
            Vector2 direction = Random.insideUnitCircle;
            particle.velocity = new Vector3(direction.x, direction.y + 0.25f) * speed;
            particle.color = color;
            particle.remaining = particle.lifetime = lifetime;
            particle.scale = scale * Random.Range(0.75f, 1.3f);
            particle.spin = Reduced ? 0f : Random.Range(-180f, 180f);
            particle.transform.position = position;
            particle.transform.localScale = Vector3.one * particle.scale;
            particle.renderer.color = color;
            particle.renderer.gameObject.SetActive(true);
            count--;
        }
    }

    public void ShiftOrigin(Vector3 offset)
    {
        for (int i = 0; i < particles.Length; i++)
            if (particles[i] != null && particles[i].remaining > 0f) particles[i].transform.position -= offset;
    }

    private void OnDisable()
    {
        if (projectionOverridden && view != null) view.ResetProjectionMatrix();
        projectionOverridden = false;
    }

    private void OnDestroy()
    {
        if (dot != null) Destroy(dot);
        if (dotTexture != null) Destroy(dotTexture);
    }
}
