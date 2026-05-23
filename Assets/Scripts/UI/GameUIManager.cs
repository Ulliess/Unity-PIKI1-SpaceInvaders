using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel; // Панель паузы
    public GameObject endGamePanel; // Панель конца игры

    [Header("Buttons")]
    public Button resumeButton;
    public Button leaveButton;
    public Button endGameLeaveButton;
    
    [Header("Optional: HUD to hide on game over")]
    public GameObject[] otherPanelsToHide; 

    [Header("Text")]
    public TMPro.TMP_Text statusText;
    public TMPro.TMP_Text endGameResultText; // "ПОБЕДА" или "ПОРАЖЕНИЕ"
    public TMPro.TMP_Text waveCompleteText;  // "Уровень X пройден!"

    private void Start()
    {
        // Прячем панель на старте
        if (pausePanel != null)
            pausePanel.SetActive(false);
            
        if (endGamePanel != null)
            endGamePanel.SetActive(false);
            
        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(false);

        // Привязываем кнопки
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);
            
        if (endGameLeaveButton != null)
            endGameLeaveButton.onClick.AddListener(OnLeaveClicked);

        // Ждём инициализации GameManager
        Invoke(nameof(SubscribeToGameManager), 0.5f);
    }

    private void SubscribeToGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGlobalPauseChanged += UpdatePauseUI;
            GameManager.Instance.OnLocalPauseMenuToggled += UpdatePauseUI;
            GameManager.Instance.OnPlayerLeft += ShowPlayerLeftUI;
            GameManager.Instance.OnGameOver += ShowGameOverUI;
            GameManager.Instance.OnWaveComplete += ShowWaveCompleteUI;
            GameManager.Instance.OnHealWave += ShowHealWaveUI;
        }
    }
    private bool isGameOver = false;

    private void ShowPlayerLeftUI(string message, bool canResume)
    {
        // Если уже показано ПОРАЖЕНИЕ/ПОБЕДА — не показываем ничего сверху
        if (isGameOver) return;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);

            if (statusText != null)
            {
                statusText.text = message;
            }

            if (resumeButton != null)
            {
                resumeButton.gameObject.SetActive(canResume);
            }
        }
    }

    private void ShowGameOverUI(bool isWin)
    {
        isGameOver = true;
        
        // Если открыта пауза - прячем её, чтобы не перекрывала
        if (pausePanel != null) pausePanel.SetActive(false);

        if (endGamePanel != null)
            endGamePanel.SetActive(true);
            
        // Прячем другие панели, чтобы не было наложений
        if (otherPanelsToHide != null)
        {
            foreach (var panel in otherPanelsToHide)
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        if (endGameResultText != null)
        {
            endGameResultText.text = isWin ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
            endGameResultText.color = isWin ? Color.green : Color.red;
        }
    }

    private void UpdatePauseUI(bool isMenuOpenParam)
    {
        if (pausePanel != null)
        {
            // Панель должна быть показана, если включена ЛИБО глобальная пауза, ЛИБО локальная
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
                // Кнопка "Продолжить" работает только если:
                // Мы сервер (сервер управляет всем) ИЛИ глобальной паузы нет
                bool canResume = Unity.Netcode.NetworkManager.Singleton.IsServer || !GameManager.Instance.IsGlobalPaused.Value;
                resumeButton.interactable = canResume;
                resumeButton.gameObject.SetActive(true); // Сама кнопка всегда видима, но может быть некликабельна
            }
        }
    }

    private void OnResumeClicked()
    {
        // Кнопка "Продолжить" симулирует нажатие ESC
        if (GameManager.Instance != null)
        {
            // Поскольку это локальное действие, если мы хост, мы снимаем глобальную паузу
            // Если клиент, просто закрываем локальную менюшку
            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                GameManager.Instance.IsGlobalPaused.Value = false;
            }
            else
            {
                // Правильно переключаем стейт в GameManager, а не просто прячем UI
                if (GameManager.Instance.IsLocalMenuOpen)
                {
                    GameManager.Instance.ToggleLocalPauseMenu();
                }
            }
        }
    }

    private void OnLeaveClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DisconnectAndLeave();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGlobalPauseChanged -= UpdatePauseUI;
            GameManager.Instance.OnLocalPauseMenuToggled -= UpdatePauseUI;
            GameManager.Instance.OnPlayerLeft -= ShowPlayerLeftUI;
            GameManager.Instance.OnGameOver -= ShowGameOverUI;
            GameManager.Instance.OnWaveComplete -= ShowWaveCompleteUI;
            GameManager.Instance.OnHealWave -= ShowHealWaveUI;
        }
    }
    
    private void ShowWaveCompleteUI(int waveNumber)
    {
        if (waveCompleteText != null)
        {
            waveCompleteText.text = $"УРОВЕНЬ {waveNumber} ПРОЙДЕН!";
            waveCompleteText.color = Color.green;
            waveCompleteText.gameObject.SetActive(true);
            
            // Автоматически прячем через 2.5 секунды
            CancelInvoke(nameof(HideWaveCompleteText));
            Invoke(nameof(HideWaveCompleteText), 2.5f);
        }
    }
    
    private void HideWaveCompleteText()
    {
        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(false);
    }
    
    private void ShowHealWaveUI()
    {
        if (waveCompleteText != null)
        {
            waveCompleteText.text += "\n<size=70%>Все игроки вылечены и возрождены!</size>";
            waveCompleteText.color = new Color(0.3f, 1f, 0.5f); // Яркий зелёный
            
            // Даём чуть больше времени прочитать
            CancelInvoke(nameof(HideWaveCompleteText));
            Invoke(nameof(HideWaveCompleteText), 3.5f);
        }
    }
}
