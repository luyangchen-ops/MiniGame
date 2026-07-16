using UnityEngine;

/// <summary>为特殊物品创建环绕金色光点和轻微脉冲点光源。</summary>
public class SpecialItemGoldenGlow : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(1f, 0.68f, 0.08f, 1f);
    [SerializeField, Min(0.01f)] private float radius = 0.75f;
    [SerializeField, Min(1)] private int particleCount = 22;
    [SerializeField, Min(0f)] private float lightIntensity = 1.4f;
    [SerializeField, Min(0f)] private float pulseSpeed = 2.5f;

    private Light glowLight;
    private ParticleSystem glowParticles;
    private Material glowMaterial;
    private float pulseOffset;

    private void Awake()
    {
        pulseOffset = Random.value * Mathf.PI * 2f;
        CreateParticles();
        CreateLight();
    }

    private void Update()
    {
        if (glowLight != null)
        {
            float pulse = 0.75f + Mathf.Sin(Time.time * pulseSpeed + pulseOffset) * 0.25f;
            glowLight.intensity = lightIntensity * pulse;
        }
    }

    private void CreateParticles()
    {
        GameObject particleObject = new GameObject("Golden Glow Particles");
        particleObject.transform.SetParent(transform, false);
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        glowParticles = particles;

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            glowColor,
            new Color(1f, 0.95f, 0.45f, 0.85f));
        main.maxParticles = Mathf.Max(32, particleCount * 2);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = particleCount;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = 0.18f;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(glowColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = fade;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            glowMaterial = new Material(shader) { name = "Runtime Golden Glow Material" };
            renderer.material = glowMaterial;
        }
    }

    private void CreateLight()
    {
        GameObject lightObject = new GameObject("Golden Glow Light");
        lightObject.transform.SetParent(transform, false);
        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = radius * 2.5f;
        glowLight.intensity = lightIntensity;
        glowLight.shadows = LightShadows.None;
    }

    private void OnEnable()
    {
        if (glowParticles != null)
        {
            glowParticles.Clear(true);
            glowParticles.Play(true);
        }

        if (glowLight != null)
        {
            glowLight.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (glowParticles != null)
        {
            glowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (glowLight != null)
        {
            glowLight.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (glowMaterial != null)
        {
            Destroy(glowMaterial);
        }
    }
}
