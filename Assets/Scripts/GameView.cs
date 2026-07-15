using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameView : MonoBehaviour
{
    [Header("Game")]
    [SerializeField, Min(1f)] private float gameDuration = 180f;
    [SerializeField] private PlayerController[] players;

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private Text countdownText;
    [SerializeField] private Text playerCooldownText;

    private readonly StringBuilder cooldownBuilder = new StringBuilder();
    private float remainingTime;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;
    public float RemainingTime => remainingTime;

    private void Awake()
    {
        if (players == null || players.Length == 0)
        {
            players = FindObjectsOfType<PlayerController>();
        }

        remainingTime = gameDuration;
        SetPlayersEnabled(false);
        RefreshCountdown();
        RefreshCooldowns();
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }
    }

    private void Update()
    {
        if (isPlaying)
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (remainingTime <= 0f)
            {
                EndGame();
            }
        }

        RefreshCountdown();
        RefreshCooldowns();
    }

    public void StartGame()
    {
        remainingTime = gameDuration;
        isPlaying = true;
        SetPlayersEnabled(true);

        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }
    }

    private void EndGame()
    {
        isPlaying = false;
        SetPlayersEnabled(false);

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
        }
    }

    private void SetPlayersEnabled(bool value)
    {
        if (players == null)
        {
            return;
        }

        foreach (PlayerController player in players)
        {
            if (player != null)
            {
                player.enabled = value;
            }
        }
    }

    private void RefreshCountdown()
    {
        if (countdownText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        countdownText.text = $"{minutes:00}:{seconds:00}";
    }

    private void RefreshCooldowns()
    {
        if (playerCooldownText == null || players == null)
        {
            return;
        }

        cooldownBuilder.Clear();
        foreach (PlayerController player in players)
        {
            if (player == null)
            {
                continue;
            }

            if (cooldownBuilder.Length > 0)
            {
                cooldownBuilder.AppendLine();
            }

            cooldownBuilder.Append(player.name).Append(" Dash: ");
            if (player.IsDashing)
            {
                cooldownBuilder.Append("Dashing");
            }
            else if (player.DashCooldownRemaining > 0f)
            {
                cooldownBuilder.Append(player.DashCooldownRemaining.ToString("0.0")).Append('s');
            }
            else
            {
                cooldownBuilder.Append("Ready");
            }
        }

        playerCooldownText.text = cooldownBuilder.ToString();
    }
}
