using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadyRoomUI : MonoBehaviour
{
    public GameObject readyRoomPanel;
    public Button circleButton;
    public Button rectButton;
    public Button triangleButton;
    public Button readyButton;
    public TMP_Text statusText;
    
    [Header("Panels to Hide")]
    public GameObject[] panelsToHide; // Сюда закинешь MainPanel, HostPanel и JoinPanel

    private bool isReady = false;
    private TMP_Text readyButtonText;

    private void Start()
    {
        readyRoomPanel.SetActive(false);
        readyButtonText = readyButton.GetComponentInChildren<TMP_Text>();
        readyButton.gameObject.SetActive(false); // Прячем до выбора корабля

        // Привязываем логику лобби
        if (LobbyManager.Instance != null)
        {
            // Для хоста: срабатывает, когда подключился второй игрок
            LobbyManager.Instance.OnBothPlayersConnected += ShowReadyRoom;
            
            // Для клиента: срабатывает, когда он сам успешно подключился к хосту
            LobbyManager.Instance.OnJoinedLobby += ShowReadyRoom;
        }

        // Кнопки выбора кораблей
        circleButton.onClick.AddListener(() => SelectShip(0));
        rectButton.onClick.AddListener(() => SelectShip(1));
        triangleButton.onClick.AddListener(() => SelectShip(2));

        // Кнопка Готов
        readyButton.onClick.AddListener(OnReadyClicked);

        // Подписываемся на изменения статуса от сети
        if (ReadyRoomManager.Instance != null)
        {
            ReadyRoomManager.Instance.OnReadyStateChanged += UpdateStatusText;
        }
    }

    private void ShowReadyRoom()
    {
        if (panelsToHide != null)
        {
            foreach (var panel in panelsToHide)
            {
                if (panel != null) panel.SetActive(false);
            }
        }
        
        readyRoomPanel.SetActive(true);
        
        // Прячем кнопки пока сеть не готова
        circleButton.gameObject.SetActive(false);
        rectButton.gameObject.SetActive(false);
        triangleButton.gameObject.SetActive(false);
        readyButton.gameObject.SetActive(false);
        statusText.text = "Подключение...";
        
        // Проверяем готовность сети каждые 0.2 сек
        InvokeRepeating(nameof(CheckNetworkReady), 0.1f, 0.2f);
    }
    
    private void CheckNetworkReady()
    {
        if (ReadyRoomManager.Instance != null && ReadyRoomManager.Instance.IsSpawned)
        {
            CancelInvoke(nameof(CheckNetworkReady));
            circleButton.gameObject.SetActive(true);
            rectButton.gameObject.SetActive(true);
            triangleButton.gameObject.SetActive(true);
            statusText.text = "Выбери корабль и нажми ГОТОВ";
        }
    }

    private void SelectShip(int index)
    {
        if (ReadyRoomManager.Instance == null || !ReadyRoomManager.Instance.IsSpawned)
        {
            statusText.text = "Подключение... Попробуйте ещё раз";
            return;
        }
        
        ReadyRoomManager.Instance.SelectShip(index);
        
        // Прячем кнопки кораблей, показываем кнопку Готов
        circleButton.gameObject.SetActive(false);
        rectButton.gameObject.SetActive(false);
        triangleButton.gameObject.SetActive(false);
        readyButton.gameObject.SetActive(true);
        
        isReady = false;
        if (readyButtonText != null) readyButtonText.text = "ГОТОВ";
        
        statusText.text = "Корабль выбран. Нажми ГОТОВ.";
    }

    private void OnReadyClicked()
    {
        isReady = !isReady;
        ReadyRoomManager.Instance.SetReady(isReady);

        if (isReady)
        {
            if (readyButtonText != null) readyButtonText.text = "НЕ ГОТОВ";
            statusText.text = "Ожидаем второго игрока...";
        }
        else
        {
            // Отжали готовность: возвращаем выбор кораблей
            circleButton.gameObject.SetActive(true);
            rectButton.gameObject.SetActive(true);
            triangleButton.gameObject.SetActive(true);
            readyButton.gameObject.SetActive(false);
            
            statusText.text = "Выбери корабль и нажми ГОТОВ";
        }
    }

    private void UpdateStatusText()
    {
        // Вызывается когда кто-то нажимает Готов
        statusText.text = "Хост готов! Запуск...";
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnBothPlayersConnected -= ShowReadyRoom;
            LobbyManager.Instance.OnJoinedLobby -= ShowReadyRoom;
        }
            
        if (ReadyRoomManager.Instance != null)
            ReadyRoomManager.Instance.OnReadyStateChanged -= UpdateStatusText;
    }
}
