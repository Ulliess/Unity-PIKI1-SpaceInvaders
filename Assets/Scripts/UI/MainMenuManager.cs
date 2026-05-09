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
    public Button quitButton; // Кнопка выхода из игры

    [Header("Host Panel")]
    public TMP_Text lobbyCodeDisplay;
    public TMP_Text hostStatusText;
    public Button copyCodeButton; // Кнопка копирования кода
    public Button hostBackButton; // Кнопка Назад (Отмена создания)

    [Header("Join Panel")]
    public TMP_InputField lobbyCodeInput;
    public Button connectButton;
    public TMP_Text joinStatusText;
    public Button joinBackButton; // Кнопка Назад (Отмена подключения)

    private async void Start()
    {
        // Показываем только главную панель
        ShowPanel(mainPanel);

        // Привязываем кнопки ДО любых асинхронных сетевых операций, 
        // чтобы они точно работали даже если сеть долго грузится
        createLobbyButton.onClick.AddListener(OnCreateClicked);
        joinLobbyButton.onClick.AddListener(OnJoinClicked);
        connectButton.onClick.AddListener(OnConnectClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (copyCodeButton != null)
            copyCodeButton.onClick.AddListener(OnCopyCodeClicked);

        if (hostBackButton != null)
            hostBackButton.onClick.AddListener(OnHostBackClicked);

        if (joinBackButton != null)
            joinBackButton.onClick.AddListener(OnJoinBackClicked);

        // Фильтр для InputField (только латиница и цифры, принудительно заглавные)
        if (lobbyCodeInput != null)
        {
            lobbyCodeInput.onValidateInput += ValidateLobbyCodeInput;
        }

        // Инициализируем Unity Services
        await LobbyManager.Instance.InitializeAsync();

        // Подписываемся на события
        LobbyManager.Instance.OnLobbyCreated += OnLobbyCreated;
        LobbyManager.Instance.OnJoinedLobby += OnJoinedLobby;
        LobbyManager.Instance.OnError += OnError;
    }

    // --- Кнопки ---

    private async void OnCreateClicked()
    {
        ShowPanel(hostPanel);
        hostStatusText.text = "Создаю лобби...";
        
        if (lobbyCodeDisplay != null)
            lobbyCodeDisplay.text = ""; // Очищаем старый код перед созданием нового
            
        if (copyCodeButton != null)
            copyCodeButton.gameObject.SetActive(false); // Прячем кнопку копирования, пока код не появится
            
        if (hostBackButton != null)
            hostBackButton.gameObject.SetActive(false); // Прячем кнопку Назад (чтобы не сломать асинхронную логику и для эстетики)

        await LobbyManager.Instance.CreateLobby();
    }

    private void OnCopyCodeClicked()
    {
        if (lobbyCodeDisplay != null && !string.IsNullOrEmpty(lobbyCodeDisplay.text))
        {
            GUIUtility.systemCopyBuffer = lobbyCodeDisplay.text;
            hostStatusText.text = "Код скопирован!";
        }
    }

    private void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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

    private async void OnHostBackClicked()
    {
        hostStatusText.text = "Отменяю...";
        if (copyCodeButton != null) copyCodeButton.gameObject.SetActive(false);
        await LobbyManager.Instance.LeaveLobby(); // Удаляем лобби из сервисов
        ShowPanel(mainPanel);
    }

    private async void OnJoinBackClicked()
    {
        joinStatusText.text = "Отменяю...";
        connectButton.interactable = true; // Разблокируем кнопку, если она зависла
        await LobbyManager.Instance.LeaveLobby(); // Разрываем сеть, если успели подключиться
        ShowPanel(mainPanel);
    }

    // --- События от LobbyManager ---

    private void OnLobbyCreated(string lobbyCode)
    {
        lobbyCodeDisplay.text = lobbyCode;
        hostStatusText.text = "Ожидание второго игрока...";
        
        if (copyCodeButton != null)
            copyCodeButton.gameObject.SetActive(true); // Показываем кнопку, когда код появился

        if (hostBackButton != null)
            hostBackButton.gameObject.SetActive(true); // Показываем кнопку Назад
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
            if (hostBackButton != null) 
                hostBackButton.gameObject.SetActive(true); // Если ошибка, разрешаем вернуться
        }
        else if (joinPanel.activeSelf)
        {
            joinStatusText.text = error;
            connectButton.interactable = true;
        }
    }

    // --- Валидация инпута ---
    private char ValidateLobbyCodeInput(string text, int charIndex, char addedChar)
    {
        // Relay код может содержать и английские буквы, и цифры.
        // Оставляем только A-Z, a-z и 0-9, остальное блокируем ('\0').
        if ((addedChar >= 'A' && addedChar <= 'Z') || 
            (addedChar >= 'a' && addedChar <= 'z') || 
            (addedChar >= '0' && addedChar <= '9'))
        {
            return char.ToUpper(addedChar); // Принудительно делаем заглавной
        }
        return '\0';
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
