using System.Collections;
using UnityEngine;

/// <summary>
/// BallSpawn 的测试版本：进入 Play Mode 后自动生成初始球和特殊道具。
/// 直接把本组件挂到测试场景中，不需要 GameView 调用 InitializeSpawns。
/// </summary>
public class BallSpawnT : BallSpawn
{
    [Header("Test Startup")]
    [SerializeField, Min(0f)] private float startupDelay;

    private void Awake()
    {
        if (startupDelay <= 0f)
        {
            InitializeSpawns();
            return;
        }

        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(startupDelay);
        InitializeSpawns();
    }
}
