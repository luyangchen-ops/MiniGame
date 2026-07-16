using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class BallSpawn : MonoBehaviour
{
    [Header("Ball")]
    [SerializeField] private GameObject beefBallPrefab;
    [SerializeField] private GameObject fishBallPrefab;
    [SerializeField] private GameObject cornPrefab;
    [SerializeField] private GameObject broccoliPrefab;
    [SerializeField, Min(0)] private int ballCount = 20;
    [SerializeField, Min(0.01f)] private float ballScale = 1f;
    [SerializeField, Min(0.1f)] private float ballSpawnInterval = 60f;
    [SerializeField, Min(0)] private int ballsPerSpawn = 1;

    [Header("Special Items")]
    [SerializeField] private GameObject specialItemPrefab;
    [SerializeField, Min(0)] private int specialItemCount = 3;
    [SerializeField, Min(0.01f)] private float specialItemScale = 1f;
    [SerializeField, Min(0.1f)] private float specialItemSpawnInterval = 20f;

    [Header("Circular Spawn Area (relative to this object)")]
    [SerializeField, Min(0.01f)] private float spawnRadius = 10f;
    [SerializeField] private Vector2 spawnCenterOffset;
    [SerializeField] private float spawnHeight = 0.5f;

    private readonly Queue<CollectibleBall> ballPool = new Queue<CollectibleBall>();
    private int spawnedBallCount;
    private int spawnedSpecialItemCount;
    private bool spawningStarted;

    /// <summary>
    /// Creates the initial balls and special items, then starts their periodic spawning.
    /// Safe to call more than once; only the first call takes effect.
    /// </summary>
    public void InitializeSpawns()
    {
        if (spawningStarted)
        {
            return;
        }

        spawningStarted = true;
        SpawnBalls();
        SpawnSpecialItems();
        StartCoroutine(SpawnBallPeriodically());
        StartCoroutine(SpawnSpecialItemPeriodically());
    }

    /// <summary>
    /// Spawns the configured number of balls. Call this method when the game is ready.
    /// </summary>
    public void SpawnBalls()
    {
        for (int i = 0; i < ballCount; i++)
        {
            SpawnBall(spawnedBallCount);
            spawnedBallCount++;
        }
    }

    private void SpawnBall(int index)
    {
        Vector3 position = GetRandomSpawnPosition();
        CollectibleBall.BallType randomType = GetRandomBallType();
        GameObject ballPrefab = GetPrefab(randomType);

        GameObject ball = ballPrefab != null
            ? Instantiate(ballPrefab, position, Quaternion.identity, transform)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        if (ballPrefab == null)
        {
            ball.transform.SetParent(transform);
            ball.transform.position = position;
        }

        ball.name = $"Ball_{index + 1}";
        ball.transform.localScale *= ballScale;

        CollectibleBall collectibleBall = ball.GetComponent<CollectibleBall>();
        if (collectibleBall == null)
        {
            collectibleBall = ball.AddComponent<CollectibleBall>();
        }

        if (ball.GetComponentInChildren<Collider>(true) == null)
        {
            ball.AddComponent<SphereCollider>();
        }

        collectibleBall.Initialize(this, randomType);
    }

    /// <summary>
    /// Spawns special items at random positions inside the configured map area.
    /// </summary>
    public void SpawnSpecialItems()
    {
        for (int i = 0; i < specialItemCount; i++)
        {
            SpawnSpecialItem(spawnedSpecialItemCount);
            spawnedSpecialItemCount++;
        }
    }

    private IEnumerator SpawnBallPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(ballSpawnInterval);
            for (int i = 0; i < ballsPerSpawn; i++)
            {
                SpawnBall(spawnedBallCount);
                spawnedBallCount++;
            }
        }
    }

    private IEnumerator SpawnSpecialItemPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(specialItemSpawnInterval);
            SpawnSpecialItem(spawnedSpecialItemCount);
            spawnedSpecialItemCount++;
        }
    }

    private void SpawnSpecialItem(int index)
    {
        Vector3 position = GetRandomSpawnPosition();
        GameObject item = specialItemPrefab != null
            ? Instantiate(specialItemPrefab, position, Quaternion.identity, transform)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        if (specialItemPrefab == null)
        {
            item.transform.SetParent(transform);
            item.transform.position = position;
            Renderer itemRenderer = item.GetComponent<Renderer>();
            if (itemRenderer != null)
            {
                itemRenderer.material.color = Color.magenta;
            }
        }

        item.name = $"SpecialItem_{index + 1}";
        item.transform.localScale *= specialItemScale;

        SpecialItem specialItem = item.GetComponent<SpecialItem>();
        if (specialItem == null)
        {
            specialItem = item.AddComponent<SpecialItem>();
        }

        specialItem.Initialize(GetRandomSpecialEffect());

        if (item.GetComponent<SpecialItemGoldenGlow>() == null)
        {
            item.AddComponent<SpecialItemGoldenGlow>();
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // insideUnitCircle 按面积均匀采样，不会让生成物集中在圆心。
        Vector2 point = Random.insideUnitCircle * spawnRadius + spawnCenterOffset;
        return transform.position + new Vector3(point.x, spawnHeight, point.y);
    }

    private static SpecialItem.EffectType GetRandomSpecialEffect()
    {
        int effectCount = System.Enum.GetValues(typeof(SpecialItem.EffectType)).Length;
        return (SpecialItem.EffectType)Random.Range(0, effectCount);
    }

    public bool ContainsPosition(Vector3 worldPosition)
    {
        Vector3 relativePosition = worldPosition - transform.position;
        Vector2 horizontalPosition = new Vector2(relativePosition.x, relativePosition.z);
        return (horizontalPosition - spawnCenterOffset).sqrMagnitude
               <= spawnRadius * spawnRadius;
    }

    public void ReturnToPool(CollectibleBall ball)
    {
        if (ball == null || !ball.gameObject.activeSelf)
        {
            return;
        }

        ball.PrepareForPool();
        ball.transform.SetParent(transform, true);
        ball.gameObject.SetActive(false);
        ballPool.Enqueue(ball);
    }

    public CollectibleBall GetFromPool(Vector3 worldPosition)
    {
        if (ballPool.Count == 0)
        {
            return null;
        }

        CollectibleBall ball = ballPool.Dequeue();
        ball.transform.position = worldPosition;
        ball.gameObject.SetActive(true);

        ball.Initialize(this, GetRandomBallType());
        return ball;
    }

    private static CollectibleBall.BallType GetRandomBallType()
    {
        return (CollectibleBall.BallType)Random.Range(0, 4);
    }

    private GameObject GetPrefab(CollectibleBall.BallType ballType)
    {
        switch (ballType)
        {
            case CollectibleBall.BallType.BeefBall: return beefBallPrefab;
            case CollectibleBall.BallType.FishBall: return fishBallPrefab;
            case CollectibleBall.BallType.Corn: return cornPrefab;
            default: return broccoliPrefab;
        }
    }

    public void ApplyTypeVisual(CollectibleBall ball, CollectibleBall.BallType ballType)
    {
        GameObject sourcePrefab = GetPrefab(ballType);
        if (ball == null || sourcePrefab == null)
        {
            return;
        }

        MeshFilter sourceFilter = sourcePrefab.GetComponentInChildren<MeshFilter>(true);
        MeshFilter targetFilter = ball.GetComponentInChildren<MeshFilter>(true);
        if (sourceFilter != null && targetFilter != null)
        {
            targetFilter.sharedMesh = sourceFilter.sharedMesh;
        }

        MeshRenderer sourceRenderer = sourcePrefab.GetComponentInChildren<MeshRenderer>(true);
        MeshRenderer targetRenderer = ball.GetComponentInChildren<MeshRenderer>(true);
        if (sourceRenderer != null && targetRenderer != null)
        {
            targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position
                         + new Vector3(spawnCenterOffset.x, spawnHeight, spawnCenterOffset.y);
        const int segmentCount = 64;
        Vector3 previousPoint = center + Vector3.right * spawnRadius;
        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / segmentCount;
            Vector3 nextPoint = center
                                + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                                * spawnRadius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
