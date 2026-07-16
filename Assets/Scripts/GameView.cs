using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private GameObject resultPage;

    [Header("Main Page")]
    [SerializeField] private Button battleModeButton;
    [SerializeField] private Button quitButton;

    [Header("Camera Positions")]
    [SerializeField] private CameraController cameraController;
    [Tooltip("主菜单镜头机位")]
    [SerializeField] private Transform cameraPosition1;
    [Tooltip("选择/加入玩家页面镜头机位")]
    [SerializeField] private Transform cameraPosition2;
    [Tooltip("正式游戏镜头机位")]
    [SerializeField] private Transform cameraPosition3;

    [Header("Join Page")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Text keyboardStatusText;
    [SerializeField] private Text gamepadStatusText;

    [Header("Battle Player Positions")]
    [Tooltip("玩家 1 进入对战区后的起始位置和朝向")]
    [SerializeField] private Transform playerOneBattlePosition;
    [Tooltip("玩家 2 进入对战区后的起始位置和朝向")]
    [SerializeField] private Transform playerTwoBattlePosition;

    [Header("Game HUD")]
    [SerializeField] private Text countdownText;
    [SerializeField] private Text playerOneCooldownText;
    [SerializeField] private Text playerTwoCooldownText;

    [Header("Result Page")]
    [SerializeField] private Text playerOneScoreText;
    [SerializeField] private Text playerTwoScoreText;
    [SerializeField] private Text winnerText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button returnToMenuButton;

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
    private static PlayerSpawner.PlayerJoinType[] savedRestartJoinOrder;

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

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        if (cameraController != null && cameraPosition1 != null)
        {
            cameraController.SnapToMenuPosition(cameraPosition1);
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

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
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

        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(PlayAgain);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        if (playerSpawner != null)
        {
            playerSpawner.PlayerSpawned -= OnPlayerSpawned;
        }
    }

    private void Start()
    {
        if (savedRestartJoinOrder == null || savedRestartJoinOrder.Length < 2
            || playerSpawner == null)
        {
            return;
        }

        PlayerSpawner.PlayerJoinType[] joinOrder = savedRestartJoinOrder;
        savedRestartJoinOrder = null;
        playerSpawner.SpawnSavedPlayers(joinOrder);
        StartGame();
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
        if (cameraController != null)
        {
            cameraController.MoveToMenuPosition(cameraPosition2);
        }

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

        if (cameraController != null)
        {
            cameraController.MoveToMenuPosition(cameraPosition3, true);
        }

        playerSpawner.enabled = false;
        remainingTime = gameDuration;
        isPlaying = true;
        ResetCountdownStyle();
        for (int i = 0; i < players.Count; i++)
        {
            PlayerController player = players[i];
            if (player != null)
            {
                Transform battlePosition = i == 0
                    ? playerOneBattlePosition
                    : i == 1 ? playerTwoBattlePosition : null;
                if (battlePosition != null)
                {
                    player.TeleportTo(battlePosition);
                }

                player.enabled = true;
            }
        }

        if (ballSpawn != null)
        {
            ballSpawn.InitializeSpawns();
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

    public void PlayAgain()
    {
        if (playerSpawner == null || playerSpawner.JoinOrder.Count < 2)
        {
            return;
        }

        savedRestartJoinOrder = new[]
        {
            playerSpawner.JoinOrder[0],
            playerSpawner.JoinOrder[1]
        };
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        savedRestartJoinOrder = null;
        isPlaying = false;

        if (cameraController != null && cameraPosition1 != null)
        {
            if (returnToMenuButton != null)
            {
                returnToMenuButton.interactable = false;
            }

            cameraController.MoveToMenuPosition(
                cameraPosition1,
                false,
                ReloadCurrentScene);
            return;
        }

        ReloadCurrentScene();
    }

    private static void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        RefreshResultPage();
        ShowPage(resultPage);
    }

    private void RefreshResultPage()
    {
        int playerOneScore = GetPlayerScore(0);
        int playerTwoScore = GetPlayerScore(1);

        if (playerOneScoreText != null)
        {
            playerOneScoreText.text = $"Player 1 Score: {playerOneScore}";
        }

        if (playerTwoScoreText != null)
        {
            playerTwoScoreText.text = $"Player 2 Score: {playerTwoScore}";
        }

        if (winnerText != null)
        {
            winnerText.text = playerOneScore == playerTwoScore
                ? "DRAW"
                : playerOneScore > playerTwoScore ? "PLAYER 1 WINS" : "PLAYER 2 WINS";
        }
    }

    private int GetPlayerScore(int index)
    {
        if (index >= players.Count || players[index] == null)
        {
            return 0;
        }

        PlayerModel model = players[index].GetComponent<PlayerModel>();
        return model != null ? model.Score : 0;
    }

    private void RefreshJoinPage()
    {
        if (playerSpawner == null || keyboardStatusText == null
            || gamepadStatusText == null || startGameButton == null)
        {
            return;
        }

        keyboardStatusText.text = "Press SPACE, ENTER or Xbox A to join";

        string joinedDevices = string.Empty;
        if (playerSpawner.KeyboardHasJoined) joinedDevices += "WASD  ";
        if (playerSpawner.ArrowKeyboardHasJoined) joinedDevices += "ARROWS  ";
        if (playerSpawner.GamepadHasJoined) joinedDevices += "GAMEPAD  ";
        gamepadStatusText.text = $"Players: {playerSpawner.JoinedPlayerCount}/2  {joinedDevices}";
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
        if (resultPage != null) resultPage.SetActive(page == resultPage);
    }
}
