using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameView : MonoBehaviour
{
    [Header("Game")]
    [SerializeField, Min(1f)] private float gameDuration = 180f;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private BallSpawn ballSpawn;

    [Header("Pages")]
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject joinPage;
    [SerializeField] private GameObject hudPage;

    [Header("Main Page")]
    [SerializeField] private Button battleModeButton;
    [SerializeField] private Button quitButton;

    [Header("Join Page")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Text keyboardStatusText;
    [SerializeField] private Text gamepadStatusText;

    [Header("Game HUD")]
    [SerializeField] private Text countdownText;
    [SerializeField] private Text playerOneCooldownText;
    [SerializeField] private Text playerTwoCooldownText;

    [Header("Countdown Warning")]
    [SerializeField, Min(0f)] private float redWarningTime = 30f;
    [SerializeField, Min(0f)] private float urgentWarningTime = 10f;
    [SerializeField, Min(1f)] private float urgentFontScale = 1.5f;
    [SerializeField, Min(0f)] private float shakeAmount = 6f;

    private readonly List<PlayerController> players = new List<PlayerController>(2);
    private float remainingTime;
    private bool isPlaying;
    private Color countdownDefaultColor;
    private int countdownDefaultFontSize;
    private Vector2 countdownDefaultPosition;

    public bool IsPlaying => isPlaying;
    public float RemainingTime => remainingTime;

    private void Awake()
    {
        if (playerSpawner == null)
        {
            playerSpawner = FindObjectOfType<PlayerSpawner>();
        }

        if (ballSpawn == null)
        {
            ballSpawn = FindObjectOfType<BallSpawn>();
        }

        CacheCountdownStyle();
        remainingTime = gameDuration;
        if (playerSpawner != null)
        {
            playerSpawner.enabled = false;
        }

        ShowPage(mainPage);
    }

    private void OnEnable()
    {
        if (battleModeButton != null)
        {
            battleModeButton.onClick.AddListener(OpenBattleMode);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }

        if (playerSpawner != null)
        {
            playerSpawner.PlayerSpawned += OnPlayerSpawned;
        }
    }

    private void OnDisable()
    {
        if (battleModeButton != null)
        {
            battleModeButton.onClick.RemoveListener(OpenBattleMode);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }

        if (playerSpawner != null)
        {
            playerSpawner.PlayerSpawned -= OnPlayerSpawned;
        }
    }

    private void Update()
    {
        RefreshJoinPage();

        if (!isPlaying)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        RefreshHud();
        if (remainingTime <= 0f)
        {
            EndGame();
        }
    }

    public void OpenBattleMode()
    {
        ShowPage(joinPage);
        if (playerSpawner != null)
        {
            playerSpawner.enabled = true;
        }

        RefreshJoinPage();
    }

    public void StartGame()
    {
        if (playerSpawner == null || !playerSpawner.BothPlayersJoined)
        {
            return;
        }

        playerSpawner.enabled = false;
        remainingTime = gameDuration;
        isPlaying = true;
        ResetCountdownStyle();
        foreach (PlayerController player in players)
        {
            if (player != null)
            {
                player.enabled = true;
            }
        }

        if (ballSpawn != null)
        {
            ballSpawn.SpawnBalls();
        }

        ShowPage(hudPage);
        RefreshHud();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnPlayerSpawned(PlayerController player)
    {
        if (player != null && !players.Contains(player))
        {
            players.Add(player);
        }

        RefreshJoinPage();
    }

    private void EndGame()
    {
        isPlaying = false;
        foreach (PlayerController player in players)
        {
            if (player != null)
            {
                player.enabled = false;
            }
        }

        if (countdownText != null)
        {
            countdownText.text = "00:00";
            countdownText.rectTransform.anchoredPosition = countdownDefaultPosition;
        }
    }

    private void RefreshJoinPage()
    {
        if (playerSpawner == null || keyboardStatusText == null
            || gamepadStatusText == null || startGameButton == null)
        {
            return;
        }

        keyboardStatusText.text = playerSpawner.KeyboardHasJoined
            ? "Player 1  WASD  READY"
            : "Press SPACE to join (WASD)";
        gamepadStatusText.text = playerSpawner.GamepadHasJoined
            ? "Player 2  GAMEPAD  READY"
            : "Press Xbox A to join (Gamepad)";
        startGameButton.interactable = playerSpawner.BothPlayersJoined;
    }

    private void RefreshHud()
    {
        int totalSeconds = Mathf.CeilToInt(remainingTime);
        if (countdownText != null)
        {
            countdownText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            RefreshCountdownWarning();
        }

        SetCooldownText(playerOneCooldownText, 0, "P1");
        SetCooldownText(playerTwoCooldownText, 1, "P2");
    }

    private void SetCooldownText(Text target, int index, string label)
    {
        if (target == null)
        {
            return;
        }

        if (index >= players.Count || players[index] == null)
        {
            target.text = $"{label} Dash: --";
            return;
        }

        PlayerController player = players[index];
        string state = player.IsDashing
            ? "DASHING"
            : player.DashCooldownRemaining > 0f
                ? $"{player.DashCooldownRemaining:0.0}s"
                : "READY";
        target.text = $"{label} Dash: {state}";
    }

    private void CacheCountdownStyle()
    {
        if (countdownText == null)
        {
            return;
        }

        countdownDefaultColor = countdownText.color;
        countdownDefaultFontSize = countdownText.fontSize;
        countdownDefaultPosition = countdownText.rectTransform.anchoredPosition;
    }

    private void ResetCountdownStyle()
    {
        if (countdownText == null)
        {
            return;
        }

        countdownText.color = countdownDefaultColor;
        countdownText.fontSize = countdownDefaultFontSize;
        countdownText.rectTransform.anchoredPosition = countdownDefaultPosition;
    }

    private void RefreshCountdownWarning()
    {
        bool isRedWarning = remainingTime <= redWarningTime;
        bool isUrgent = remainingTime <= urgentWarningTime;

        countdownText.color = isRedWarning ? Color.red : countdownDefaultColor;
        countdownText.fontSize = isUrgent
            ? Mathf.RoundToInt(countdownDefaultFontSize * urgentFontScale)
            : countdownDefaultFontSize;
        countdownText.rectTransform.anchoredPosition = isUrgent
            ? countdownDefaultPosition + Random.insideUnitCircle * shakeAmount
            : countdownDefaultPosition;
    }

    private void ShowPage(GameObject page)
    {
        if (mainPage != null) mainPage.SetActive(page == mainPage);
        if (joinPage != null) joinPage.SetActive(page == joinPage);
        if (hudPage != null) hudPage.SetActive(page == hudPage);
    }
}
