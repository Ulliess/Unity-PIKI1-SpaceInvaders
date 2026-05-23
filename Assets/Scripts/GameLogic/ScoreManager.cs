using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Хранит и синхронизирует очки двух игроков по сети.
/// Player0 = Host (ServerClientId), Player1 = клиент.
/// Добавить на пустой GameObject в сцене Game вместе с NetworkObject.
/// </summary>
public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Читают все, пишет только сервер
    public NetworkVariable<int> Player0Score = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Player1Score = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Вызывается на всех клиентах при изменении любого счёта.</summary>
    public event Action OnScoresChanged;

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
        Player0Score.OnValueChanged += (_, __) => OnScoresChanged?.Invoke();
        Player1Score.OnValueChanged += (_, __) => OnScoresChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        Player0Score.OnValueChanged -= (_, __) => OnScoresChanged?.Invoke();
        Player1Score.OnValueChanged -= (_, __) => OnScoresChanged?.Invoke();
    }

    /// <summary>
    /// Сервер начисляет очки игроку по его ClientId.
    /// Вызывать только с сервера (из EnemyBase.Die).
    /// </summary>
    public void AddScore(ulong clientId, int points)
    {
        if (!IsServer) return;

        if (clientId == NetworkManager.ServerClientId)
            Player0Score.Value += points;
        else
            Player1Score.Value += points;
    }

    /// <summary>Очки локального игрока (YOUR score).</summary>
    public int GetMyScore()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsServer ? Player0Score.Value : Player1Score.Value;
    }

    /// <summary>Очки соперника (RIVAL score).</summary>
    public int GetRivalScore()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsServer ? Player1Score.Value : Player0Score.Value;
    }
}