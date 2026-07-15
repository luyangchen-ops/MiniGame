using System;
using UnityEngine;

public class SpecialItem : MonoBehaviour
{
    public enum EffectType
    {
        SpeedBoost,
        AimGuide,
        Suction,
        ExplosiveBall,
        Infection,
        Slow
    }

    [SerializeField] private EffectType effectType;

    [Header("Timed Effect Settings")]
    [SerializeField, Min(0.01f)] private float speedBoostMultiplier = 1.75f;
    [SerializeField, Min(0f)] private float speedBoostDuration = 5f;
    [SerializeField, Min(0f)] private float aimGuideDuration = 6f;
    [SerializeField, Min(0.1f)] private float aimGuideLength = 12f;
    [SerializeField, Min(0.1f)] private float suctionRadius = 4f;
    [SerializeField, Min(0f)] private float suctionDuration = 6f;
    [SerializeField, Range(0.01f, 1f)] private float slowMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float slowDuration = 5f;

    private bool isCollected;

    public EffectType Effect => effectType;
    public static event Action<PlayerModel, EffectType> Collected;

    public void Initialize(EffectType newEffectType)
    {
        effectType = newEffectType;
        isCollected = false;

        Collider[] itemColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.enabled = true;
            itemCollider.isTrigger = true;
        }

        if (itemColliders.Length == 0)
        {
            SphereCollider itemCollider = gameObject.AddComponent<SphereCollider>();
            itemCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (isCollected)
        {
            return;
        }

        PlayerModel player = other.GetComponentInParent<PlayerModel>();
        if (player == null)
        {
            return;
        }

        isCollected = true;
        ApplyEffect(player);
        Collected?.Invoke(player, effectType);
        Destroy(gameObject);
    }

    private void ApplyEffect(PlayerModel player)
    {
        PlayerController controller = player.GetComponent<PlayerController>();

        switch (effectType)
        {
            case EffectType.SpeedBoost:
                if (controller != null)
                {
                    controller.ApplySpeedBoost(speedBoostMultiplier, speedBoostDuration);
                }
                break;
            case EffectType.AimGuide:
                if (controller != null)
                {
                    controller.ApplyAimGuide(aimGuideDuration, aimGuideLength);
                }
                break;
            case EffectType.Suction:
                if (controller != null)
                {
                    controller.ApplySuction(suctionRadius, suctionDuration);
                }
                break;
            case EffectType.ExplosiveBall:
                if (controller != null)
                {
                    controller.ArmExplosiveBall();
                }
                break;
            case EffectType.Infection:
                player.InfectBackHalf();
                break;
            case EffectType.Slow:
                if (controller != null)
                {
                    controller.ApplySlow(slowMultiplier, slowDuration);
                }
                break;
        }
    }
}
