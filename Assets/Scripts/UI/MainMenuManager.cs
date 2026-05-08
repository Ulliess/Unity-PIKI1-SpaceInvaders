using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управляет UI главного меню. Переключает 3 панели:
/// MainPanel → HostPanel (создал лобби, показывает код)
/// MainPanel → JoinPanel (ввод кода и подключение)
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;    // Стартовый экран с двумя кнопками
    public GameObject hostPanel;    // Экран хоста: показывает код лобби
    public GameObject joinPanel;    // Экран клиента: ввод кода

    [Header("Main Panel")]
    public Button createLobbyButton;
    public Button joinLobbyButton;

    [Header("Host Panel")]
    public TMP_Text lobbyCodeDisplay;
    public TMP_Text hostStatusText;

    [Header("Join Panel")]
    public TMP_InputField lobbyCodeInput;
    public Button connectButton;
    public TMP_Text joinStatusText;

    private async void Start()
    {
        // Показываем только главную панель
        ShowPanel(mainPanel);

        // Инициализируем Unity Services
        await LobbyManager.Instance.InitializeAsync();

        // Подписываемся на события
        LobbyManager.Instance.OnLobbyCreated += OnLobbyCreated;
        LobbyManager.Instance.OnJoinedLobby += OnJoinedLobby;
        LobbyManager.Instance.OnError += OnError;

        // Привязываем кнопки
        createLobbyButton.onClick.AddListener(OnCreateClicked);
        joinLobbyButton.onClick.AddListener(OnJoinClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    // --- Кнопки ---

    private async void OnCreateClicked()
    {
        ShowPanel(hostPanel);
        hostStatusText.text = "Создаю лобби...";
        await LobbyManager.Instance.CreateLobby();
    }

    private void OnJoinClicked()
    {
        // Просто переключаем на панель ввода кода
        ShowPanel(joinPanel);
        joinStatusText.text = "";
    }

    private async void OnConnectClicked()
    {
        string code = lobbyCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            joinStatusText.text = "Введи код!";
            return;
        }

        connectButton.interactable = false;
        joinStatusText.text = "Подключаюсь...";
        await LobbyManager.Instance.JoinLobby(code);
    }

    // --- События от LobbyManager ---

    private void OnLobbyCreated(string lobbyCode)
    {
        lobbyCodeDisplay.text = lobbyCode;
        hostStatusText.text = "Ожидание второго игрока...";
    }

    private void OnJoinedLobby()
    {
        joinStatusText.text = "Подключено!";
        // TODO: переход на сцену Game
    }

    private void OnError(string error)
    {
        // Показываем ошибку на текущей панели
        if (hostPanel.activeSelf)
        {
            hostStatusText.text = error;
        }
        else if (joinPanel.activeSelf)
        {
            joinStatusText.text = error;
            connectButton.interactable = true;
        }
    }

    // --- Helpers ---

    private void ShowPanel(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        hostPanel.SetActive(panel == hostPanel);
        joinPanel.SetActive(panel == joinPanel);
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyCreated -= OnLobbyCreated;
            LobbyManager.Instance.OnJoinedLobby -= OnJoinedLobby;
            LobbyManager.Instance.OnError -= OnError;
        }
    }
}
