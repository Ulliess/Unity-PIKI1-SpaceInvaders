using UnityEngine;
using Unity.Netcode;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    // Сетевая переменная для глобальной паузы (меняет только сервер/хост)
    public NetworkVariable<bool> IsGlobalPaused = new NetworkVariable<bool>(false);

    // События, чтобы UI мог реагировать на изменения
    public event Action<bool> OnGlobalPauseChanged;
    public event Action<bool> OnLocalPauseMenuToggled;
    public event Action<string, bool> OnPlayerLeft; // <сообщение, можно_ли_продолжить>
    public event Action<bool> OnGameOver; // true = победа, false = поражение
    public event Action<int> OnWaveComplete; // номер пройденного уровня

    public bool IsLocalMenuOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Подписываемся на изменение переменной по сети
        IsGlobalPaused.OnValueChanged += (oldValue, newValue) =>
        {
            ApplyGlobalPause(newValue);
        };

        // Подписка на отключение игроков
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Применяем сразу при спавне (вдруг мы подключились, а игра уже на паузе)
        if (IsClient)
        {
            ApplyGlobalPause(IsGlobalPaused.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            // Мы сервер, и кто-то отключился. Это точно клиент.
            if (clientId != NetworkManager.ServerClientId)
            {
                OnPlayerLeft?.Invoke("Игрок вышел из игры", true); 
            }
        }
        else
        {
            // Мы клиент, и нас отключило. Значит, сервер упал (хост вышел).
            // При потере сервера клиент получает дисконнект либо с ID 0, либо со своим собственным ID.
            if (clientId == NetworkManager.LocalClientId || clientId == NetworkManager.ServerClientId)
            {
                OnPlayerLeft?.Invoke("Хост вышел из игры", false);
            }
        }
    }

    private void Update()
    {
        // Хост и клиент нажимают ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsServer)
            {
                // Хост переключает глобальную паузу для всех
                IsGlobalPaused.Value = !IsGlobalPaused.Value;
            }
            else if (!IsGlobalPaused.Value)
            {
                // Клиент открывает локальное меню ТОЛЬКО если хост не поставил глобальную
                ToggleLocalPauseMenu();
            }
        }
    }

    private void ApplyGlobalPause(bool isPaused)
    {
        // В Unity остановка времени делается через timeScale
        Time.timeScale = isPaused ? 0f : 1f;
        OnGlobalPauseChanged?.Invoke(isPaused);
    }

    public void ToggleLocalPauseMenu()
    {
        IsLocalMenuOpen = !IsLocalMenuOpen;
        OnLocalPauseMenuToggled?.Invoke(IsLocalMenuOpen);
    }

    // Метод для кнопки "Выйти"
    public void DisconnectAndLeave()
    {
        Time.timeScale = 1f; // Обязательно возвращаем время в норму перед выходом
        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void TriggerGameOver(bool isWin)
    {
        if (IsServer)
        {
            TriggerGameOverClientRpc(isWin);
        }
    }

    [ClientRpc]
    private void TriggerGameOverClientRpc(bool isWin)
    {
        Time.timeScale = 0f; // Останавливаем игру
        OnGameOver?.Invoke(isWin);
    }

    public void NotifyWaveComplete(int waveNumber)
    {
        if (!IsServer) return;
        
        bool isHealWave = (waveNumber % 3 == 0);
        
        if (isHealWave)
        {
            HealAndReviveAllPlayers();
        }
        
        NotifyWaveCompleteClientRpc(waveNumber, isHealWave);
    }

    [ClientRpc]
    private void NotifyWaveCompleteClientRpc(int waveNumber, bool wasHealWave)
    {
        OnWaveComplete?.Invoke(waveNumber);
        
        // Дополнительное сообщение о хиле (UI подхватит через отдельное событие)
        if (wasHealWave)
        {
            OnHealWave?.Invoke();
        }
    }
    
    public event Action OnHealWave;
    
    /// <summary>
    /// Хилит всех живых игроков до максимума и воскрешает мёртвых.
    /// Вызывается только на сервере.
    /// </summary>
    private void HealAndReviveAllPlayers()
    {
        if (!IsServer) return;
        
        // 1. Хилим живых
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var ship = client.PlayerObject.GetComponent<PlayerShip>();
                if (ship != null && !ship.isDead)
                {
                    ship.FullHeal();
                }
            }
        }
        
        // 2. Воскрешаем мёртвых (тех, у кого нет PlayerObject)
        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    spawner.RespawnPlayer(client.ClientId);
                }
            }
        }
        
        Debug.Log("[GameManager] Все игроки вылечены и воскрешены!");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Time.timeScale = 1f; // Защита от зависания времени при удалении сцены
    }
}
