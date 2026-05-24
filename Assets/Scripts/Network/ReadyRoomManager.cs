using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Управляет выбором кораблей в комнате ожидания (MainMenu).
/// Должен висеть на объекте с NetworkObject в сцене MainMenu.
/// </summary>
public class ReadyRoomManager : NetworkBehaviour
{
    public static ReadyRoomManager Instance { get; private set; }

    // Словарь: ClientId -> Ship Index (0=Circle, 1=Square, 2=Triangle)
    public Dictionary<ulong, int> playerShipChoices = new Dictionary<ulong, int>();
    
    // Кто нажал "Готов"
    public Dictionary<ulong, bool> playerReadyStatus = new Dictionary<ulong, bool>();

    public delegate void ReadyStateChanged();
    public event ReadyStateChanged OnReadyStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Не удаляем при загрузке игры, так как серверу из GameScene понадобятся эти данные для спавна
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // Когда мы подключаемся к сети, очищаем словари на всякий случай
        if (IsServer)
        {
            playerShipChoices.Clear();
            playerReadyStatus.Clear();
        }
    }

    // --- Вызывается локальным клиентом при нажатии кнопок в UI ---

    public void SelectShip(int shipIndex)
    {
        SelectShipServerRpc(shipIndex);
    }

    public void SetReady(bool isReady)
    {
        SetReadyServerRpc(isReady);
    }

    // --- RPC на сервер ---

    [Rpc(SendTo.Server)]
    private void SelectShipServerRpc(int shipIndex, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        playerShipChoices[clientId] = shipIndex;
        
        Debug.Log($"[ReadyRoom] Игрок {clientId} выбрал корабль {shipIndex}");
    }

    [Rpc(SendTo.Server)]
    private void SetReadyServerRpc(bool isReady, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        playerReadyStatus[clientId] = isReady;
        
        Debug.Log($"[ReadyRoom] Игрок {clientId} готов: {isReady}");

        // Сообщаем всем клиентам обновить UI (чтобы написать "Хост готов")
        NotifyReadyStateClientRpc();

        if (isReady)
        {
            CheckAllReady();
        }
    }

    // --- RPC клиентам ---

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyReadyStateClientRpc()
    {
        OnReadyStateChanged?.Invoke();
    }

    // --- Логика сервера ---

    private void CheckAllReady()
    {
        if (!IsServer) return;

        // Если подключено 2 игрока и оба готовы
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
        {
            bool allReady = true;
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (!playerReadyStatus.ContainsKey(clientId) || !playerReadyStatus[clientId])
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
            {
                Debug.Log("[ReadyRoom] Все готовы! Загружаем GameScene...");
                NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }
}
