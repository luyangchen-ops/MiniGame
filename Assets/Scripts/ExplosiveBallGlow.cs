using UnityEngine;

/// <summary>爆炸球武装提示：环绕球体的红色光点和脉冲红光。</summary>
public class ExplosiveBallGlow : MonoBehaviour
{
    private Material glowMaterial;
    private Light glowLight;
    private GameObject particleObject;
    private GameObject lightObject;
    private bool isVisible = true;

    private void Awake()
    {
        CollectibleBall ball = GetComponent<CollectibleBall>();
        float radius = ball != null ? ball.WorldRadius * 1.75f : 1f;

        particleObject = new GameObject("Explosive Red Glow");
        particleObject.transform.SetParent(transform, false);
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.02f, 0.08f, 1f),
            new Color(1f, 0.9f, 0.35f, 1f));
        main.maxParticles = 128;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 65f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = 0.45f;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0f, 0.12f), 0.35f),
                new GradientColorKey(new Color(0.5f, 0f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = fade;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            glowMaterial = new Material(shader) { name = "Runtime Explosive Glow Material" };
            renderer.material = glowMaterial;
        }

        lightObject = new GameObject("Explosive Red Light");
        lightObject.transform.SetParent(transform, false);
        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = new Color(1f, 0.01f, 0.08f);
        glowLight.range = Mathf.Max(3f, radius * 4.5f);
        glowLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        if (isVisible && glowLight != null)
        {
            glowLight.intensity = 3.5f + Mathf.Sin(Time.time * 8f) * 1.4f;
        }
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (particleObject != null)
        {
            particleObject.SetActive(visible);
        }

        if (lightObject != null)
        {
            lightObject.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        if (particleObject != null) Destroy(particleObject);
        if (lightObject != null) Destroy(lightObject);
        if (glowMaterial != null) Destroy(glowMaterial);
    }
}
