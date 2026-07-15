using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Serializable]
    public class CollectedBallData
    {
        [SerializeField] private CollectibleBall.BallColor color;
        [SerializeField] private CollectibleBall ball;

        public CollectibleBall.BallColor Color => color;
        public CollectibleBall Ball => ball;

        public CollectedBallData(CollectibleBall collectedBall)
        {
            color = collectedBall.Color;
            ball = collectedBall;
        }
    }

    [SerializeField] private List<CollectedBallData> collectedBalls = new List<CollectedBallData>();

    public IReadOnlyList<CollectedBallData> CollectedBalls => collectedBalls;
    public int BallCount => collectedBalls.Count;

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
        // contiguous color group containing the projectile.
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

        CollectibleBall.BallColor matchedColor = projectile.Color;
        int first = insertedIndex;
        int last = insertedIndex;

        while (first > 0 && collectedBalls[first - 1].Color == matchedColor)
        {
            first--;
        }

        while (last + 1 < collectedBalls.Count && collectedBalls[last + 1].Color == matchedColor)
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

        collectedBalls.RemoveRange(first, matchCount);
        foreach (CollectibleBall ball in removedBalls)
        {
            if (ball != null)
            {
                ball.ReturnToPool();
            }
        }
    }
}
