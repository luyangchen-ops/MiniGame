using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    [SerializeField] private Image playerOneCooldownIcon;
    [SerializeField] private Image playerTwoCooldownIcon;
    [SerializeField] private Color dashCooldownColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color dashReadyColor = Color.white;

    [Header("Result Page")]
    [SerializeField] private Text playerOneScoreText;
    [SerializeField] private Text playerTwoScoreText;
    [SerializeField] private Text winnerText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button returnToMenuButton;

    [Header("Result Image Targets")]
    [SerializeField] private Image playerOneResultImage;
    [SerializeField] private Image playerTwoResultImage;
    [SerializeField] private Image playerOneFrameImage;
    [SerializeField] private Image playerTwoFrameImage;

    [Header("Result Sprites")]
    [SerializeField] private Sprite playerOneWinSprite;
    [SerializeField] private Sprite playerOneLoseSprite;
    [SerializeField] private Sprite playerOneDrawSprite;
    [SerializeField] private Sprite playerTwoWinSprite;
    [SerializeField] private Sprite playerTwoLoseSprite;
    [SerializeField] private Sprite playerTwoDrawSprite;
    [SerializeField] private Sprite winFrameSprite;
    [SerializeField] private Sprite loseFrameSprite;

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
    private Image playerOneCooldownFill;
    private Image playerTwoCooldownFill;
    private bool isPauseMenuOpen;
    private static PlayerSpawner.PlayerJoinType[] savedRestartJoinOrder;

    public bool IsPlaying => isPlaying;
    public float RemainingTime => remainingTime;

    private void Awake()
    {
        Time.timeScale = 1f;

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
            PlayMenuMusic();
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

        if (isPlaying && WasPausePressed())
        {
            if (isPauseMenuOpen)
            {
                ResumeGame();
            }
            else
            {
                ShowPauseMenu();
            }

            return;
        }

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
        PlayMenuMusic();

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

        playerSpawner.enabled = false;
        players.Clear();
        if (!playerSpawner.SpawnBattlePlayers(playerOneBattlePosition, playerTwoBattlePosition))
        {
            playerSpawner.enabled = true;
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        if (cameraController != null)
        {
            cameraController.MoveToMenuPosition(cameraPosition3, true);
        }

        remainingTime = gameDuration;
        isPlaying = true;
        ResetCountdownStyle();

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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        savedRestartJoinOrder = null;
        isPlaying = false;
        isPauseMenuOpen = false;
        Time.timeScale = 1f;
        PlayMenuMusic();

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

    private static void PlayMenuMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }
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
        isPauseMenuOpen = false;
        Time.timeScale = 1f;
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

        SetResultTextVisible(true);
        RefreshResultPage();
        ShowPage(resultPage);
    }

    private void ShowPauseMenu()
    {
        isPauseMenuOpen = true;
        Time.timeScale = 0f;

        foreach (PlayerController player in players)
        {
            if (player != null)
            {
                player.enabled = false;
            }
        }

        SetResultTextVisible(false);
        SetAllResultImagesVisible(false);
        ShowPage(resultPage);
    }

    private void ResumeGame()
    {
        isPauseMenuOpen = false;
        Time.timeScale = 1f;

        foreach (PlayerController player in players)
        {
            if (player != null)
            {
                player.enabled = true;
            }
        }

        ShowPage(hudPage);
        RefreshHud();
    }

    private void SetResultTextVisible(bool visible)
    {
        if (playerOneScoreText != null) playerOneScoreText.gameObject.SetActive(visible);
        if (playerTwoScoreText != null) playerTwoScoreText.gameObject.SetActive(visible);
        if (winnerText != null) winnerText.gameObject.SetActive(visible);
    }

    private static bool WasPausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
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

        RefreshResultImages(playerOneScore, playerTwoScore);
    }

    private void RefreshResultImages(int playerOneScore, int playerTwoScore)
    {
        SetAllResultImagesVisible(false);
        if (playerOneScore == playerTwoScore)
        {
            SetImageSprite(playerOneResultImage, playerOneDrawSprite);
            SetImageSprite(playerTwoResultImage, playerTwoDrawSprite);
            return;
        }

        bool playerOneWins = playerOneScore > playerTwoScore;
        SetImageSprite(
            playerOneResultImage,
            playerOneWins ? playerOneWinSprite : playerOneLoseSprite);
        SetImageSprite(
            playerTwoResultImage,
            playerOneWins ? playerTwoLoseSprite : playerTwoWinSprite);
        SetImageSprite(
            playerOneFrameImage,
            playerOneWins ? winFrameSprite : loseFrameSprite);
        SetImageSprite(
            playerTwoFrameImage,
            playerOneWins ? loseFrameSprite : winFrameSprite);
    }

    private void SetAllResultImagesVisible(bool visible)
    {
        SetImageVisible(playerOneResultImage, visible);
        SetImageVisible(playerTwoResultImage, visible);
        SetImageVisible(playerOneFrameImage, visible);
        SetImageVisible(playerTwoFrameImage, visible);
    }

    private static void SetImageSprite(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.gameObject.SetActive(sprite != null);
    }

    private static void SetImageVisible(Image image, bool visible)
    {
        if (image != null)
        {
            image.gameObject.SetActive(visible);
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

        SetCooldownIcon(playerOneCooldownIcon, ref playerOneCooldownFill, 0);
        SetCooldownIcon(playerTwoCooldownIcon, ref playerTwoCooldownFill, 1);
    }

    private void SetCooldownIcon(Image target, ref Image cooldownFill, int index)
    {
        if (target == null)
        {
            return;
        }

        if (index >= players.Count || players[index] == null)
        {
            target.enabled = false;
            if (cooldownFill != null)
            {
                cooldownFill.enabled = false;
            }
            return;
        }

        EnsureCooldownFill(target, ref cooldownFill);

        PlayerController player = players[index];
        target.enabled = true;
        target.color = dashCooldownColor;

        cooldownFill.enabled = true;
        cooldownFill.sprite = target.sprite;
        cooldownFill.color = dashReadyColor;
        cooldownFill.fillAmount = player.IsDashing ? 0f : player.DashCooldownProgress;
    }

    private static void EnsureCooldownFill(Image target, ref Image cooldownFill)
    {
        if (cooldownFill != null)
        {
            return;
        }

        GameObject fillObject = new GameObject("Cooldown Radial Fill", typeof(RectTransform));
        RectTransform fillTransform = fillObject.GetComponent<RectTransform>();
        fillTransform.SetParent(target.rectTransform, false);
        fillTransform.anchorMin = Vector2.zero;
        fillTransform.anchorMax = Vector2.one;
        fillTransform.offsetMin = Vector2.zero;
        fillTransform.offsetMax = Vector2.zero;

        cooldownFill = fillObject.AddComponent<Image>();
        cooldownFill.raycastTarget = false;
        cooldownFill.preserveAspect = target.preserveAspect;
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillMethod = Image.FillMethod.Radial360;
        cooldownFill.fillOrigin = (int)Image.Origin360.Top;
        cooldownFill.fillClockwise = true;
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
