using UnityEngine;

/// <summary>
/// 在火锅汤面上生成油滴飞溅特效。把组件挂在火锅中心，并让局部 Y 轴朝上。
/// 不需要预制体或贴图，运行时会自动创建粒子系统。
/// </summary>
public class GESpawner : MonoBehaviour
{
    [Header("触发")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool repeat;
    [SerializeField, Min(0.1f)] private float repeatInterval = 1.2f;

    [Header("飞溅范围")]
    [SerializeField, Min(1)] private int dropletCount = 34;
    [SerializeField, Min(0.01f)] private float surfaceRadius = 0.65f;
    [SerializeField] private Vector2 dropletSpeed = new Vector2(2.2f, 5.2f);
    [SerializeField] private Vector2 dropletSize = new Vector2(0.025f, 0.085f);
    [SerializeField] private Vector2 dropletLifetime = new Vector2(0.45f, 1.15f);
    [SerializeField, Range(0f, 1f)] private float upwardBias = 0.62f;
    [SerializeField, Min(0f)] private float gravity = 2.1f;

    [Header("外观")]
    [SerializeField] private Color hotOilColor = new Color(1f, 0.42f, 0.035f, 1f);
    [SerializeField] private Color darkOilColor = new Color(0.48f, 0.07f, 0.015f, 0.9f);
    [SerializeField] private Color mistColor = new Color(1f, 0.72f, 0.34f, 0.28f);
    [SerializeField] private bool collideWithWorld = true;
    [SerializeField] private LayerMask collisionLayers = ~0;

    [Header("Elimination Audio")]
    [SerializeField] private AudioClip eliminationSound;

    private ParticleSystem droplets;
    private ParticleSystem mist;
    private Material particleMaterial;
    private float nextPlayTime;

    private void Awake()
    {
        BuildEffect();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (repeat && Time.time >= nextPlayTime)
        {
            Play();
        }
    }

    /// <summary>从代码、按钮事件或动画事件中调用，喷发一次油花。</summary>
    public void Play()
    {
        if (droplets == null || mist == null)
        {
            BuildEffect();
        }

        droplets.Emit(dropletCount);
        mist.Emit(Mathf.Max(5, dropletCount / 3));
        nextPlayTime = Time.time + repeatInterval;
    }

    /// <summary>在指定世界坐标播放一次消除特效和消除音效。</summary>
    public void PlayElimination(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        Play();

        if (AudioManager.Instance != null && eliminationSound != null)
        {
            AudioManager.Instance.PlaySoundEffect(eliminationSound);
        }
    }

    public void Stop(bool clearParticles = false)
    {
        repeat = false;
        ParticleSystemStopBehavior behavior = clearParticles
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;
        if (droplets != null) droplets.Stop(true, behavior);
        if (mist != null) mist.Stop(true, behavior);
    }

    [ContextMenu("预览油花 / Play Splash")]
    private void PreviewSplash()
    {
        if (Application.isPlaying) Play();
    }

    private void BuildEffect()
    {
        if (droplets != null) return;

        particleMaterial = CreateParticleMaterial();
        droplets = CreateDroplets();
        mist = CreateMist();
    }

    private ParticleSystem CreateDroplets()
    {
        ParticleSystem system = CreateChildSystem("Oil Droplets");
        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(dropletLifetime.x, dropletLifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(dropletSpeed.x, dropletSpeed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(dropletSize.x, dropletSize.y);
        main.startColor = new ParticleSystem.MinMaxGradient(hotOilColor, darkOilColor);
        main.gravityModifier = gravity;
        main.maxParticles = Mathf.Max(128, dropletCount * 3);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = surfaceRadius;
        shape.radiusThickness = 1f;
        shape.angle = Mathf.Lerp(48f, 10f, upwardBias);
        shape.arc = 360f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve shrink = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.78f, 0.72f),
            new Keyframe(1f, 0f));
        size.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.45f, 0.15f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.72f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystem.CollisionModule collision = system.collision;
        collision.enabled = collideWithWorld;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.collidesWith = collisionLayers;
        collision.dampen = 0.35f;
        collision.bounce = 0.18f;
        collision.lifetimeLoss = 0.35f;

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 1.7f;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        return system;
    }

    private ParticleSystem CreateMist()
    {
        ParticleSystem system = CreateChildSystem("Hot Oil Mist");
        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        main.startColor = mistColor;
        main.gravityModifier = -0.08f;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = surfaceRadius * 0.75f;
        shape.radiusThickness = 1f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.35f, 1f), new Keyframe(1f, 1.5f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return system;
    }

    private ParticleSystem CreateChildSystem(string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = Vector3.zero;
        // ParticleSystem 的锥体沿局部 +Z 发射；转到火锅的局部 +Y（向上）。
        child.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        return child.AddComponent<ParticleSystem>();
    }

    private static Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return shader != null ? new Material(shader) { name = "Runtime Hot Oil Material" } : null;
    }

    private void OnDestroy()
    {
        if (particleMaterial != null)
        {
            Destroy(particleMaterial);
        }
    }

    private void OnValidate()
    {
        dropletSpeed.x = Mathf.Max(0f, dropletSpeed.x);
        dropletSpeed.y = Mathf.Max(dropletSpeed.x, dropletSpeed.y);
        dropletSize.x = Mathf.Max(0.001f, dropletSize.x);
        dropletSize.y = Mathf.Max(dropletSize.x, dropletSize.y);
        dropletLifetime.x = Mathf.Max(0.05f, dropletLifetime.x);
        dropletLifetime.y = Mathf.Max(dropletLifetime.x, dropletLifetime.y);
    }
}
