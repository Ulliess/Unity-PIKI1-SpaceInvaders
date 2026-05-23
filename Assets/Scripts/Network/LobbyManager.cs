using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Управляет всем жизненным циклом лобби:
/// 1. Инициализация Unity Services + анонимный логин
/// 2. Создание лобби (Host): Relay allocation → join code → lobby с кодом
/// 3. Подключение к лобби (Client): найти лобби по коду → достать relay code → подключиться
/// 4. Heartbeat чтобы лобби не умерло (Unity убивает лобби через 30 сек без пинга)
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // Текущее лобби (null если не в лобби)
    private Lobby currentLobby;

    // Таймер для heartbeat (пинг лобби каждые 15 сек)
    private float heartbeatTimer = 0f;
    private const float HEARTBEAT_INTERVAL = 15f;

    // Ключ в данных лобби, где хранится relay join code
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    // Событие, чтобы UI мог подписаться и узнать о результатах
    public event Action<string> OnLobbyCreated;    // передаёт lobby code
    public event Action OnJoinedLobby;              // клиент подключился
    public event Action<string> OnError;            // ошибка

    private void Awake()
    {
        // Singleton — один LobbyManager на всю игру
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Шаг 0: Инициализация Unity Services и анонимный логин.
    /// Вызывается ОДИН раз при старте игры.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Инициализируем Unity Services (Relay, Lobby, Auth)
            await UnityServices.InitializeAsync();

            // Анонимный логин — каждый игрок получает уникальный ID
            // без необходимости создавать аккаунт
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[LobbyManager] Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] Failed to initialize: {e.Message}");
            OnError?.Invoke($"Ошибка инициализации: {e.Message}");
        }
    }

    /// <summary>
    /// Шаг 1 (HOST): Создать лобби.
    /// 1. Создаёт Relay allocation (резервирует место на relay-сервере)
    /// 2. Получает join code для Relay
    /// 3. Создаёт Lobby в Unity Lobby Service, кладёт relay code в данные лобби
    /// 4. Запускает NetworkManager как Host
    /// </summary>
    public async Task CreateLobby()
    {
        try
        {
            // --- RELAY ---
            // Создаём allocation на 1 подключение (2 игрока = host + 1 client)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);

            // Получаем join code — это строка типа "ABCDEF", 
            // которую второй игрок введёт чтобы подключиться
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"[LobbyManager] Relay join code: {relayJoinCode}");

            // Настраиваем UnityTransport использовать Relay вместо прямого подключения
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            transport.SetRelayServerData(relayServerData);

            // Запускаем как Host (сервер + клиент одновременно)
            NetworkManager.Singleton.StartHost();

            // Подписываемся: когда второй игрок подключится — загружаем Game
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // --- LOBBY ---
            // Создаём лобби на 2 игрока, кладём relay code в данные лобби
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    // Relay join code доступен всем участникам лобби
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("SpaceInvaders", 2, options);

            Debug.Log($"[LobbyManager] Lobby created! Code: {currentLobby.LobbyCode}");

            // Сообщаем UI что лобби создано (передаём код лобби)
            OnLobbyCreated?.Invoke(currentLobby.LobbyCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] Failed to create lobby: {e.Message}");
            OnError?.Invoke($"Не удалось создать лобби: {e.Message}");
        }
    }

    /// <summary>
    /// Шаг 2 (CLIENT): Подключиться к лобби по коду.
    /// 1. Находит лобби по lobby code
    /// 2. Достаёт relay join code из данных лобби
    /// 3. Подключается к Relay серверу
    /// 4. Запускает NetworkManager как Client
    /// </summary>
    public async Task JoinLobby(string lobbyCode)
    {
        try
        {
            // Подключаемся к лобби по коду (тот 6-значный код что показал Host)
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            Debug.Log($"[LobbyManager] Joined lobby: {currentLobby.Name}");

            // Достаём relay join code из данных лобби
            string relayJoinCode = currentLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            // --- RELAY ---
            // Подключаемся к тому же relay серверу что и Host
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // Настраиваем транспорт
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            transport.SetRelayServerData(relayServerData);

            // Запускаем как Client
            NetworkManager.Singleton.StartClient();

            // Сообщаем UI
            OnJoinedLobby?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] Failed to join lobby: {e.Message}");
            OnError?.Invoke($"Не удалось подключиться: {e.Message}");
        }
    }

    private void Update()
    {
        // Heartbeat — пингуем лобби каждые 15 сек, иначе Unity его убьёт
        if (currentLobby != null && IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = HEARTBEAT_INTERVAL;
                LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    /// <summary>
    /// Проверяет, являемся ли мы хостом лобби
    /// </summary>
    private bool IsLobbyHost()
    {
        return currentLobby != null && 
               currentLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    /// <summary>
    /// Покинуть лобби и отключиться
    /// </summary>
    public async Task LeaveLobby()
    {
        try
        {
            if (currentLobby != null)
            {
                if (IsLobbyHost())
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, 
                        AuthenticationService.Instance.PlayerId);
                }
                currentLobby = null;
            }

            NetworkManager.Singleton.Shutdown();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyManager] Failed to leave lobby: {e.Message}");
        }
    }
    public event Action OnBothPlayersConnected;

    /// <summary>
    /// Вызывается когда новый клиент подключается.
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Host = 1 клиент, когда подключается 2-й — их становится 2
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
        {
            Debug.Log("[LobbyManager] 2 игрока подключены! Открываем комнату выбора кораблей.");
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            
            // Сообщаем UI и ReadyRoomManager, что оба игрока на месте
            OnBothPlayersConnected?.Invoke();
        }
    }
}
