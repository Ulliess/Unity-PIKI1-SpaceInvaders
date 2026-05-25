using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Хранит и синхронизирует два числа каждого игрока:
///   - LevelScore  — очки за убийства в ТЕКУЩЕМ уровне (сбрасываются в начале каждого уровня)
///   - WinPoints   — суммарное количество уровней, которые игрок «выиграл» по очкам
///
/// Правила конца уровня:
///   • Кто-то погиб В ЭТОМ уровне:
///       выживший > убитый (до смерти) → выживший +2
///       выживший ≤ убитый (до смерти) → выживший +1
///   • Оба живы (в т.ч. если соперник мёртв с предыдущего уровня и LevelScore = 0):
///       больше очков → +1; ничья → оба +1
/// </summary>
public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // Очки за убийства в текущем уровне — видны всем клиентам для HUD
    public NetworkVariable<int> Player0LevelScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Player1LevelScore = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Победные очки (сколько уровней «выиграл» каждый игрок)
    public NetworkVariable<int> Player0WinPoints = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Player1WinPoints = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Вызывается на всех клиентах при любом изменении очков.</summary>
    public event Action OnScoresChanged;

    // Только сервер: флаги смерти и очки на момент гибели за текущий уровень
    private bool player0DiedThisLevel;
    private bool player1DiedThisLevel;
    private int  player0KillsAtDeath;
    private int  player1KillsAtDeath;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Player0LevelScore.OnValueChanged += (_, __) => OnScoresChanged?.Invoke();
        Player1LevelScore.OnValueChanged += (_, __) => OnScoresChanged?.Invoke();
        Player0WinPoints.OnValueChanged  += (_, __) => OnScoresChanged?.Invoke();
        Player1WinPoints.OnValueChanged  += (_, __) => OnScoresChanged?.Invoke();
    }

        public override void OnNetworkDespawn()
    {
        Player0LevelScore.OnValueChanged -= (_, __) => OnScoresChanged?.Invoke();
        Player1LevelScore.OnValueChanged -= (_, __) => OnScoresChanged?.Invoke();
        Player0WinPoints.OnValueChanged  -= (_, __) => OnScoresChanged?.Invoke();
        Player1WinPoints.OnValueChanged  -= (_, __) => OnScoresChanged?.Invoke();
    }

    // ── Серверные методы ──────────────────────────────────────────────────────

    /// <summary>Добавить очки за убийство врага. Вызывать только с сервера (из EnemyBase.Die).</summary>
    public void AddScore(ulong clientId, int points)
    {
        if (!IsServer) return;
        if (clientId == NetworkManager.ServerClientId)
            Player0LevelScore.Value += points;
        else
            Player1LevelScore.Value += points;
    }

    /// <summary>
    /// Зафиксировать гибель игрока в этом уровне.
    /// Вызывать с сервера из PlayerShip.TakeDamage ДО деспавна объекта.
    /// </summary>
    public void NotifyPlayerDied(ulong clientId)
    {
        if (!IsServer) return;
        if (clientId == NetworkManager.ServerClientId)
        {
            player0DiedThisLevel = true;
            player0KillsAtDeath  = Player0LevelScore.Value;
        }
        else
        {
            player1DiedThisLevel = true;
            player1KillsAtDeath  = Player1LevelScore.Value;
        }
    }

    /// <summary>
    /// Сравнить очки за уровень и начислить победные очки.
    /// Вызывать с сервера из GameManager.NotifyWaveComplete ДО сброса.
    /// </summary>
    public void EvaluateLevelEnd()
    {
        if (!IsServer) return;

        if (player0DiedThisLevel && !player1DiedThisLevel)
        {
            // Игрок 0 (хост) погиб в этом уровне, игрок 1 выжил
            int survivorScore = Player1LevelScore.Value;
            int deadScore     = player0KillsAtDeath;
            Player1WinPoints.Value += (survivorScore > deadScore) ? 2 : 1;
        }
        else if (player1DiedThisLevel && !player0DiedThisLevel)
        {
            // Игрок 1 (клиент) погиб в этом уровне, игрок 0 выжил
            int survivorScore = Player0LevelScore.Value;
            int deadScore     = player1KillsAtDeath;
            Player0WinPoints.Value += (survivorScore > deadScore) ? 2 : 1;
        }
        else
        {
            // Оба живы — или соперник мёртв с предыдущего уровня (его LevelScore = 0)
            int p0 = Player0LevelScore.Value;
            int p1 = Player1LevelScore.Value;

            if (p0 > p1)
                Player0WinPoints.Value += 1;
            else if (p1 > p0)
                Player1WinPoints.Value += 1;
            else
            {
                // Ничья — оба получают по одному
                Player0WinPoints.Value += 1;
                Player1WinPoints.Value += 1;
            }
        }
    }

    /// <summary>
    /// Сбросить очки за уровень и флаги смерти для следующего уровня.
    /// Вызывать с сервера из GameManager.NotifyWaveComplete ПОСЛЕ EvaluateLevelEnd.
    /// </summary>
    public void ResetLevelScores()
    {
        if (!IsServer) return;
        Player0LevelScore.Value = 0;
        Player1LevelScore.Value = 0;
        player0DiedThisLevel    = false;
        player1DiedThisLevel    = false;
        player0KillsAtDeath     = 0;
        player1KillsAtDeath     = 0;
    }

    // ── Геттеры для UI ────────────────────────────────────────────────────────

    public int GetMyLevelScore()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsServer ? Player0LevelScore.Value : Player1LevelScore.Value;
    }

    public int GetRivalLevelScore()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsServer ? Player1LevelScore.Value : Player0LevelScore.Value;
    }

    public int GetMyWinPoints()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsServer ? Player0WinPoints.Value : Player1WinPoints.Value;
    }

    public int GetRivalWinPoints()
    {
        if (NetworkManager.Singleton == null) return 0;
        return NetworkManager.Singleton.IsServer ? Player1WinPoints.Value : Player0WinPoints.Value;
    }
}