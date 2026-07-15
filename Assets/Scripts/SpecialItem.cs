using System;
using UnityEngine;

public class SpecialItem : MonoBehaviour
{
    public enum EffectType
    {
        SpeedBoost,
        Shield,
        ExtraBall
    }

    [SerializeField] private EffectType effectType;

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
        // Effect implementations are intentionally left as extension points.
        switch (effectType)
        {
            case EffectType.SpeedBoost:
                break;
            case EffectType.Shield:
                break;
            case EffectType.ExtraBall:
                break;
        }
    }
}
