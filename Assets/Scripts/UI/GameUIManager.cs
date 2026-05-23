using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject endGamePanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button leaveButton;
    public Button endGameLeaveButton;

    [Header("Optional: HUD to hide on game over")]
    public GameObject[] otherPanelsToHide;

    [Header("Text — Pause / Status")]
    public TMPro.TMP_Text statusText;
    public TMPro.TMP_Text endGameResultText;    // "ПОБЕДА" или "ПОРАЖЕНИЕ"
    public TMPro.TMP_Text waveCompleteText;     // "Уровень X пройден!"

    [Header("Text — End Game Scores")]
    [Tooltip("Текст под результатом на панели EndGame. Формат: ВЫ: 350  |  НАПАРНИК: 420")]
    public TMPro.TMP_Text endGameScoresText;

    [Header("Score HUD (live)")]
    [Tooltip("TMP_Text в левом верхнем углу — очки локального игрока")]
    public TMPro.TMP_Text hudYouScoreText;
    [Tooltip("TMP_Text в правом верхнем углу — очки напарника")]
    public TMPro.TMP_Text hudRivalScoreText;

    // ───────────────────────────────────────────────────────────────────────
    private bool isGameOver = false;

    private void Start()
    {
        if (pausePanel != null)   pausePanel.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (waveCompleteText != null) waveCompleteText.gameObject.SetActive(false);

        // Инициализируем HUD нулями
        // Хост слева — ВЫ слева, клиент справа — ВЫ справа
        bool isHost = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer;
        if (isHost)
        {
            SetHudText(hudYouScoreText,   "ВЫ",       0);
            SetHudText(hudRivalScoreText, "НАПАРНИК",  0);
        }
        else if (hudYouScoreText != null && hudRivalScoreText != null)
        {
            // Меняем местами и текст, и цвета
            var youColor = hudYouScoreText.color;
            var rivalColor = hudRivalScoreText.color;
            
            SetHudText(hudYouScoreText,   "НАПАРНИК",  0);
            SetHudText(hudRivalScoreText, "ВЫ",        0);
            
            hudYouScoreText.color = rivalColor;
            hudRivalScoreText.color = youColor;
        }

        if (resumeButton != null)      resumeButton.onClick.AddListener(OnResumeClicked);
        if (leaveButton != null)       leaveButton.onClick.AddListener(OnLeaveClicked);
        if (endGameLeaveButton != null) endGameLeaveButton.onClick.AddListener(OnLeaveClicked);

        // Ждём инициализации сетевых менеджеров
        Invoke(nameof(SubscribeToManagers), 0.5f);
    }

    // ── Подписки ────────────────────────────────────────────────────────────

    private void SubscribeToManagers()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGlobalPauseChanged   += UpdatePauseUI;
            GameManager.Instance.OnLocalPauseMenuToggled += UpdatePauseUI;
            GameManager.Instance.OnPlayerLeft           += ShowPlayerLeftUI;
            GameManager.Instance.OnGameOver             += ShowGameOverUI;
            GameManager.Instance.OnWaveComplete         += ShowWaveCompleteUI;
            GameManager.Instance.OnHealWave             += ShowHealWaveUI;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoresChanged += UpdateScoreHUD;
            UpdateScoreHUD(); // первоначальный показ (0 : 0)
        }
    }

    // ── Score HUD ───────────────────────────────────────────────────────────

    private void UpdateScoreHUD()
    {
        if (ScoreManager.Instance == null) return;

        bool isHost = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer;
        if (isHost)
        {
            SetHudText(hudYouScoreText,   "ВЫ",       ScoreManager.Instance.GetMyScore());
            SetHudText(hudRivalScoreText, "НАПАРНИК",  ScoreManager.Instance.GetRivalScore());
        }
        else
        {
            SetHudText(hudYouScoreText,   "НАПАРНИК",  ScoreManager.Instance.GetRivalScore());
            SetHudText(hudRivalScoreText, "ВЫ",        ScoreManager.Instance.GetMyScore());
        }
    }

    /// Вспомогательный метод — формирует строку "LABEL\n0000"
    private void SetHudText(TMPro.TMP_Text target, string label, int score)
    {
        if (target == null) return;
        target.text = $"{label}\n{score}";
    }

    // ── Game Over ────────────────────────────────────────────────────────────

    private void ShowGameOverUI(bool isWin)
    {
        isGameOver = true;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (endGamePanel != null) endGamePanel.SetActive(true);

        if (otherPanelsToHide != null)
        {
            foreach (var panel in otherPanelsToHide)
                if (panel != null) panel.SetActive(false);
        }

        // Скрываем HUD очков — они будут показаны в панели итогов
        if (hudYouScoreText != null)   hudYouScoreText.gameObject.SetActive(false);
        if (hudRivalScoreText != null) hudRivalScoreText.gameObject.SetActive(false);

        if (endGameResultText != null)
        {
            endGameResultText.text  = isWin ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
            endGameResultText.color = isWin ? Color.green : Color.red;
        }

        // Финальные очки обоих игроков
        if (endGameScoresText != null && ScoreManager.Instance != null)
        {
            int myScore    = ScoreManager.Instance.GetMyScore();
            int rivalScore = ScoreManager.Instance.GetRivalScore();
            endGameScoresText.text = $"Вы: {myScore}     Напарник: {rivalScore}";
        }
    }

    // ── Pause ────────────────────────────────────────────────────────────────

    private void ShowPlayerLeftUI(string message, bool canResume)
    {
        if (isGameOver) return;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            if (statusText != null) statusText.text = message;
            if (resumeButton != null) resumeButton.gameObject.SetActive(canResume);
        }
    }

    private void UpdatePauseUI(bool isMenuOpenParam)
    {
        if (pausePanel == null) return;

        bool shouldBeOpen = GameManager.Instance.IsGlobalPaused.Value || GameManager.Instance.IsLocalMenuOpen;
        pausePanel.SetActive(shouldBeOpen);

        if (shouldBeOpen && statusText != null)
        {
            if (GameManager.Instance.IsGlobalPaused.Value)
            {
                statusText.text = Unity.Netcode.NetworkManager.Singleton.IsServer
                    ? "ПАУЗА"
                    : "Хост поставил игру на паузу";
            }
            else
            {
                statusText.text = "ПАУЗА (Локально)";
            }
        }

        if (resumeButton != null)
        {
            bool canResume = Unity.Netcode.NetworkManager.Singleton.IsServer || !GameManager.Instance.IsGlobalPaused.Value;
            resumeButton.interactable = canResume;
            resumeButton.gameObject.SetActive(true);
        }
    }

    // ── Wave Complete ────────────────────────────────────────────────────────

    private void ShowWaveCompleteUI(int waveNumber)
    {
        if (waveCompleteText == null) return;
        waveCompleteText.text = $"УРОВЕНЬ {waveNumber} ПРОЙДЕН!";
        waveCompleteText.color = Color.green;
        waveCompleteText.gameObject.SetActive(true);

        CancelInvoke(nameof(HideWaveCompleteText));
        Invoke(nameof(HideWaveCompleteText), 2.5f);
    }

    private void HideWaveCompleteText()
    {
        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(false);
    }

    private void ShowHealWaveUI()
    {
        if (waveCompleteText == null) return;
        waveCompleteText.text += "\n<size=70%>Все игроки вылечены и возрождены!</size>";
        waveCompleteText.color = new Color(0.3f, 1f, 0.5f);

        CancelInvoke(nameof(HideWaveCompleteText));
        Invoke(nameof(HideWaveCompleteText), 3.5f);
    }

    // ── Buttons ──────────────────────────────────────────────────────────────

    private void OnResumeClicked()
    {
        if (GameManager.Instance == null) return;

        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            GameManager.Instance.IsGlobalPaused.Value = false;
        else if (GameManager.Instance.IsLocalMenuOpen)
            GameManager.Instance.ToggleLocalPauseMenu();
    }

    private void OnLeaveClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.DisconnectAndLeave();
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGlobalPauseChanged    -= UpdatePauseUI;
            GameManager.Instance.OnLocalPauseMenuToggled -= UpdatePauseUI;
            GameManager.Instance.OnPlayerLeft            -= ShowPlayerLeftUI;
            GameManager.Instance.OnGameOver              -= ShowGameOverUI;
            GameManager.Instance.OnWaveComplete          -= ShowWaveCompleteUI;
            GameManager.Instance.OnHealWave              -= ShowHealWaveUI;
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoresChanged -= UpdateScoreHUD;
    }
}