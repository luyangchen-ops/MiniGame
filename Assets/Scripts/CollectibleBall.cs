using UnityEngine;

public class CollectibleBall : MonoBehaviour
{
    public enum BallColor
    {
        Blue,
        Green,
        Yellow
    }

    [SerializeField] private BallColor color;
    [SerializeField, Min(0.001f)] private float worldRadius = 0.5f;

    private BallSpawn ownerPool;
    private bool isLaunched;
    private bool hitResolved;
    private PlayerModel chainOwner;
    private PlayerModel shooter;
    private bool isExplosive;

    public BallColor Color => color;
    public float WorldRadius => worldRadius;
    public bool IsCollected { get; private set; }
    public PlayerModel ChainOwner => chainOwner;
    public bool IsExplosive => isExplosive;

    public void Initialize(BallSpawn pool, BallColor newColor, Color displayColor)
    {
        ownerPool = pool;
        color = newColor;
        IsCollected = false;
        isLaunched = false;
        hitResolved = false;
        chainOwner = null;
        shooter = null;
        isExplosive = false;

        foreach (Collider ballCollider in GetComponentsInChildren<Collider>(true))
        {
            ballCollider.enabled = true;
            ballCollider.isTrigger = false;
        }

        Rigidbody ballBody = GetComponentInChildren<Rigidbody>(true);
        if (ballBody != null)
        {
            ballBody.velocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            ballBody.isKinematic = true;
            ballBody.detectCollisions = true;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer ballRenderer in renderers)
        {
            ballRenderer.material.color = displayColor;
        }

        RefreshWorldRadius();
    }

    private void FixedUpdate()
    {
        if (isLaunched && ownerPool != null && !ownerPool.ContainsPosition(transform.position))
        {
            ownerPool.ReturnToPool(this);
        }
    }

    public void RefreshWorldRadius()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            worldRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return;
        }

        Vector3 scale = transform.lossyScale;
        worldRadius = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.5f;
    }

    public bool TryCollect()
    {
        if (IsCollected)
        {
            return false;
        }

        IsCollected = true;

        foreach (Collider ballCollider in GetComponentsInChildren<Collider>())
        {
            ballCollider.enabled = true;
            ballCollider.isTrigger = true;
        }

        foreach (Rigidbody ballBody in GetComponentsInChildren<Rigidbody>())
        {
            ballBody.isKinematic = true;
            ballBody.detectCollisions = false;
        }

        return true;
    }

    public void AttachToChain(PlayerModel owner)
    {
        chainOwner = owner;
        shooter = null;
        isLaunched = false;
        hitResolved = false;
        IsCollected = true;

        foreach (Collider ballCollider in GetComponentsInChildren<Collider>(true))
        {
            ballCollider.enabled = true;
            ballCollider.isTrigger = true;
        }

        foreach (Rigidbody ballBody in GetComponentsInChildren<Rigidbody>(true))
        {
            ballBody.velocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.isKinematic = true;
            ballBody.detectCollisions = true;
        }
    }

    public void Launch(Vector3 position, Vector3 direction, float speed, PlayerModel launchedBy)
    {
        transform.SetParent(null, true);
        transform.position = position;
        chainOwner = null;
        shooter = launchedBy;
        hitResolved = false;

        foreach (Collider ballCollider in GetComponentsInChildren<Collider>(true))
        {
            ballCollider.enabled = true;
            // Trigger-only projectiles do not apply knockback to player rigidbodies.
            ballCollider.isTrigger = true;
        }

        Rigidbody ballBody = GetComponentInChildren<Rigidbody>(true);
        if (ballBody == null)
        {
            ballBody = gameObject.AddComponent<Rigidbody>();
        }

        ballBody.isKinematic = false;
        ballBody.useGravity = false;
        ballBody.detectCollisions = true;
        ballBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        ballBody.velocity = direction.normalized * speed;
        isLaunched = true;
    }

    public void SetExplosive(bool value)
    {
        isExplosive = value;
    }

    public void SetColor(BallColor newColor)
    {
        color = newColor;
        Color displayColor = ownerPool != null
            ? ownerPool.GetDisplayColor(newColor)
            : newColor == BallColor.Blue
                ? UnityEngine.Color.blue
                : newColor == BallColor.Green ? UnityEngine.Color.green : UnityEngine.Color.yellow;
        foreach (Renderer ballRenderer in GetComponentsInChildren<Renderer>())
        {
            ballRenderer.material.color = displayColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitChain(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHitChain(collision.collider);
    }

    private void TryHitChain(Collider other)
    {
        if (!isLaunched || hitResolved)
        {
            return;
        }

        CollectibleBall hitBall = other.GetComponentInParent<CollectibleBall>();
        if (hitBall != null && hitBall != this && hitBall.chainOwner != null)
        {
            if (hitBall.chainOwner == shooter)
            {
                return;
            }

            hitResolved = true;
            isLaunched = false;
            hitBall.chainOwner.ResolveProjectileHit(hitBall, this);
            return;
        }

        PlayerModel hitPlayer = other.GetComponentInParent<PlayerModel>();
        if (hitPlayer == null || hitPlayer == shooter)
        {
            return;
        }

        hitResolved = true;
        isLaunched = false;
        hitPlayer.ResolveProjectileHitAtFront(this);
    }

    public void ReturnToPool()
    {
        if (ownerPool != null)
        {
            ownerPool.ReturnToPool(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void PrepareForPool()
    {
        isLaunched = false;
        IsCollected = false;
        hitResolved = false;
        chainOwner = null;
        shooter = null;
        isExplosive = false;

        Rigidbody ballBody = GetComponentInChildren<Rigidbody>(true);
        if (ballBody != null)
        {
            ballBody.velocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            ballBody.isKinematic = true;
            ballBody.detectCollisions = false;
        }
    }
}
