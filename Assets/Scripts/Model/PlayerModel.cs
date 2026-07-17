using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Serializable]
    public class CollectedBallData
    {
        [SerializeField] private CollectibleBall.BallType ballType;
        [SerializeField] private CollectibleBall ball;

        public CollectibleBall.BallType Type => ball != null ? ball.Type : ballType;
        public CollectibleBall Ball => ball;

        public CollectedBallData(CollectibleBall collectedBall)
        {
            ballType = collectedBall.Type;
            ball = collectedBall;
        }
    }

    [SerializeField] private List<CollectedBallData> collectedBalls = new List<CollectedBallData>();
    [SerializeField, Min(0)] private int eliminationScore;

    public IReadOnlyList<CollectedBallData> CollectedBalls => collectedBalls;
    public int BallCount => collectedBalls.Count;
    public int EliminationScore => eliminationScore;

    // Temporary scoring rule: one point for each currently collected ball.
    // Other systems should read Score instead of BallCount so the scoring
    // implementation can be replaced later without changing its consumers.
    public int Score => BallCount + eliminationScore;

    public void AddBall(CollectibleBall ball)
    {
        if (ball == null)
        {
            return;
        }

        ball.AttachToChain(this);
        collectedBalls.Add(new CollectedBallData(ball));
    }

    public CollectibleBall RemoveFirstBall()
    {
        if (collectedBalls.Count == 0)
        {
            return null;
        }

        CollectibleBall ball = collectedBalls[0].Ball;
        collectedBalls.RemoveAt(0);
        return ball;
    }

    public void ResolveProjectileHit(CollectibleBall hitBall, CollectibleBall projectile)
    {
        if (hitBall == null || projectile == null)
        {
            return;
        }

        int hitIndex = collectedBalls.FindIndex(data => data.Ball == hitBall);
        if (hitIndex < 0)
        {
            projectile.ReturnToPool();
            return;
        }

        // Insert immediately before the struck ball, then inspect the complete
        // contiguous food-type group containing the projectile.
        int insertedIndex = hitIndex;
        projectile.AttachToChain(this);
        collectedBalls.Insert(insertedIndex, new CollectedBallData(projectile));

        ResolveInsertedBall(insertedIndex, projectile);
    }

    public void ResolveProjectileHitAtFront(CollectibleBall projectile)
    {
        if (projectile == null)
        {
            return;
        }

        projectile.AttachToChain(this);
        collectedBalls.Insert(0, new CollectedBallData(projectile));
        ResolveInsertedBall(0, projectile);
    }

    private void ResolveInsertedBall(int insertedIndex, CollectibleBall projectile)
    {
        if (projectile.IsExplosive)
        {
            ResolveExplosion(insertedIndex);
            return;
        }

        CollectibleBall.BallType matchedType = projectile.Type;
        int first = insertedIndex;
        int last = insertedIndex;

        while (first > 0 && collectedBalls[first - 1].Type == matchedType)
        {
            first--;
        }

        while (last + 1 < collectedBalls.Count && collectedBalls[last + 1].Type == matchedType)
        {
            last++;
        }

        int matchCount = last - first + 1;
        if (matchCount < 3)
        {
            return;
        }

        List<CollectibleBall> removedBalls = new List<CollectibleBall>(matchCount);
        for (int i = first; i <= last; i++)
        {
            removedBalls.Add(collectedBalls[i].Ball);
        }

        Vector3 eliminationCenter = GetBallGroupCenter(removedBalls);
        collectedBalls.RemoveRange(first, matchCount);
        AddEliminationScore(matchCount);
        TriggerEliminationFeedback(matchCount, eliminationCenter);
        foreach (CollectibleBall ball in removedBalls)
        {
            if (ball != null)
            {
                ball.ReturnToPool();
            }
        }
    }

    public void InfectBackHalf()
    {
        if (collectedBalls.Count == 0)
        {
            return;
        }

        CollectibleBall.BallType infectedType =
            (CollectibleBall.BallType)UnityEngine.Random.Range(0, 4);
        int firstInfectedIndex = collectedBalls.Count / 2;
        for (int i = firstInfectedIndex; i < collectedBalls.Count; i++)
        {
            CollectibleBall ball = collectedBalls[i].Ball;
            if (ball != null)
            {
                ball.SetType(infectedType);
            }
        }
    }

    private void ResolveExplosion(int centerIndex)
    {
        int first = Mathf.Max(0, centerIndex - 2);
        int last = Mathf.Min(collectedBalls.Count - 1, centerIndex + 2);
        int removeCount = last - first + 1;
        List<CollectibleBall> removedBalls = new List<CollectibleBall>(removeCount);
        for (int i = first; i <= last; i++)
        {
            removedBalls.Add(collectedBalls[i].Ball);
        }

        Vector3 eliminationCenter = GetBallGroupCenter(removedBalls);
        collectedBalls.RemoveRange(first, removeCount);
        AddEliminationScore(removeCount);
        TriggerEliminationFeedback(removeCount, eliminationCenter);
        foreach (CollectibleBall ball in removedBalls)
        {
            if (ball != null)
            {
                ball.ReturnToPool();
            }
        }
    }

    private static Vector3 GetBallGroupCenter(IReadOnlyList<CollectibleBall> balls)
    {
        Vector3 center = Vector3.zero;
        int validBallCount = 0;
        for (int i = 0; i < balls.Count; i++)
        {
            if (balls[i] == null)
            {
                continue;
            }

            center += balls[i].transform.position;
            validBallCount++;
        }

        return validBallCount > 0 ? center / validBallCount : Vector3.zero;
    }

    private void AddEliminationScore(int eliminatedBallCount)
    {
        eliminationScore += Mathf.Max(0, eliminatedBallCount - 2);
    }

    private static void TriggerEliminationFeedback(
        int eliminatedBallCount,
        Vector3 eliminationPosition)
    {
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            cameraController.ShakeForElimination(eliminatedBallCount);
        }

        GESpawner effectSpawner = FindObjectOfType<GESpawner>();
        if (effectSpawner != null)
        {
            effectSpawner.PlayElimination(eliminationPosition);
        }
    }
}
