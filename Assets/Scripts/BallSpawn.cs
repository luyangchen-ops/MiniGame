using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class BallSpawn : MonoBehaviour
{
    [Header("Ball")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField, Min(0)] private int ballCount = 20;
    [SerializeField, Min(0.01f)] private float ballScale = 1f;
    [SerializeField, Min(0.1f)] private float ballSpawnInterval = 60f;

    [Header("Special Items")]
    [SerializeField] private GameObject specialItemPrefab;
    [SerializeField, Min(0)] private int specialItemCount = 3;
    [SerializeField, Min(0.01f)] private float specialItemScale = 1f;
    [SerializeField, Min(0.1f)] private float specialItemSpawnInterval = 20f;

    [Header("Spawn Area (relative to this object)")]
    [SerializeField] private Vector2 xRange = new Vector2(-10f, 10f);
    [SerializeField] private Vector2 zRange = new Vector2(-10f, 10f);
    [SerializeField] private float spawnHeight = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color blue = Color.blue;
    [SerializeField] private Color green = Color.green;
    [SerializeField] private Color yellow = Color.yellow;

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

        CollectibleBall.BallColor randomColor = GetRandomColor();
        collectibleBall.Initialize(this, randomColor, GetDisplayColor(randomColor));
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
            SpawnBall(spawnedBallCount);
            spawnedBallCount++;
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
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return transform.position + new Vector3(
            Random.Range(Mathf.Min(xRange.x, xRange.y), Mathf.Max(xRange.x, xRange.y)),
            spawnHeight,
            Random.Range(Mathf.Min(zRange.x, zRange.y), Mathf.Max(zRange.x, zRange.y)));
    }

    private static SpecialItem.EffectType GetRandomSpecialEffect()
    {
        int effectCount = System.Enum.GetValues(typeof(SpecialItem.EffectType)).Length;
        return (SpecialItem.EffectType)Random.Range(0, effectCount);
    }

    public bool ContainsPosition(Vector3 worldPosition)
    {
        Vector3 relativePosition = worldPosition - transform.position;
        float minX = Mathf.Min(xRange.x, xRange.y);
        float maxX = Mathf.Max(xRange.x, xRange.y);
        float minZ = Mathf.Min(zRange.x, zRange.y);
        float maxZ = Mathf.Max(zRange.x, zRange.y);

        return relativePosition.x >= minX && relativePosition.x <= maxX
               && relativePosition.z >= minZ && relativePosition.z <= maxZ;
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

        CollectibleBall.BallColor randomColor = GetRandomColor();
        ball.Initialize(this, randomColor, GetDisplayColor(randomColor));
        return ball;
    }

    private CollectibleBall.BallColor GetRandomColor()
    {
        switch (Random.Range(0, 3))
        {
            case 0: return CollectibleBall.BallColor.Blue;
            case 1: return CollectibleBall.BallColor.Green;
            default: return CollectibleBall.BallColor.Yellow;
        }
    }

    public Color GetDisplayColor(CollectibleBall.BallColor ballColor)
    {
        switch (ballColor)
        {
            case CollectibleBall.BallColor.Blue: return blue;
            case CollectibleBall.BallColor.Green: return green;
            default: return yellow;
        }
    }

    private void OnDrawGizmosSelected()
    {
        float minX = Mathf.Min(xRange.x, xRange.y);
        float maxX = Mathf.Max(xRange.x, xRange.y);
        float minZ = Mathf.Min(zRange.x, zRange.y);
        float maxZ = Mathf.Max(zRange.x, zRange.y);

        Vector3 center = transform.position + new Vector3(
            (minX + maxX) * 0.5f,
            spawnHeight,
            (minZ + maxZ) * 0.5f);
        Vector3 size = new Vector3(maxX - minX, 0.05f, maxZ - minZ);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
}
