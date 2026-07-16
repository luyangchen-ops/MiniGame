using UnityEngine;

/// <summary>爆炸球武装提示：环绕球体的红色光点和脉冲红光。</summary>
public class ExplosiveBallGlow : MonoBehaviour
{
    private Material glowMaterial;
    private Light glowLight;
    private GameObject particleObject;
    private GameObject lightObject;

    private void Awake()
    {
        CollectibleBall ball = GetComponent<CollectibleBall>();
        float radius = ball != null ? ball.WorldRadius * 1.35f : 0.65f;

        particleObject = new GameObject("Explosive Red Glow");
        particleObject.transform.SetParent(transform, false);
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, new Color(1f, 0.3f, 0.01f));
        main.maxParticles = 64;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 28f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = 0.2f;

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
        glowLight.color = Color.red;
        glowLight.range = Mathf.Max(1f, radius * 3f);
        glowLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        if (glowLight != null)
        {
            glowLight.intensity = 1.4f + Mathf.Sin(Time.time * 7f) * 0.55f;
        }
    }

    private void OnDestroy()
    {
        if (particleObject != null) Destroy(particleObject);
        if (lightObject != null) Destroy(lightObject);
        if (glowMaterial != null) Destroy(glowMaterial);
    }
}
