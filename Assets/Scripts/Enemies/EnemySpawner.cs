using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Enemy Prefabs (порядок: Scout, Drone, Brute, Weaver, Dasher)")]
    public GameObject[] enemyPrefabs = new GameObject[5];

    [Header("Spawn Weights — вероятность каждого типа")]
    public float[] spawnWeights = { 0.35f, 0.25f, 0.15f, 0.15f, 0.10f };

    [Header("Enemy Costs (must match prefabs)")]
    public int[] enemyCosts = { 1, 2, 5, 3, 2 };

    [Header("Level Settings")]
    public int currentLevel = 1;
    public int baseBudget = 20; // +100% (было 10)
    public int budgetPerLevel = 10; // +100% (было 5)

    public float spawnInterval = 2.0f;
    public float minSpawnInterval = 0.5f;

    private int remainingBudget;
    private int activeEnemiesCount = 0;
    private float timer = 0f;
    private bool levelInProgress = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        EnemyBase.OnEnemyDied += HandleEnemyDied;
        
        // Немного ждем, пока все игроки прогрузятся, и стартуем
        Invoke(nameof(StartLevel), 2f);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        EnemyBase.OnEnemyDied -= HandleEnemyDied;
    }

    public void StartLevel()
    {
        if (!IsServer) return;
        
        remainingBudget = baseBudget + (currentLevel - 1) * budgetPerLevel;
        activeEnemiesCount = 0;
        timer = 0f;
        levelInProgress = true;
        
        // С каждым уровнем враги спавнятся быстрее
        spawnInterval = Mathf.Max(minSpawnInterval, 2.5f - (currentLevel * 0.2f));

        Debug.Log($"[EnemySpawner] Уровень {currentLevel} начат! Бюджет: {remainingBudget}");
    }

    void Update()
    {
        if (!IsServer || !levelInProgress) return;
        
        // Если игра на паузе (например, открыто меню) - не спавним
        if (GameManager.Instance != null && GameManager.Instance.IsGlobalPaused.Value) return;

        if (remainingBudget > 0)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnEnemy();
            }
        }
    }

    void SpawnEnemy()
    {
        int prefabIndex = PickPrefabWithinBudget(remainingBudget);
        if (prefabIndex == -1)
        {
            // Не можем купить даже самого дешёвого врага - остаток бюджета сгорает
            remainingBudget = 0;
            return;
        }

        remainingBudget -= enemyCosts[prefabIndex];
        activeEnemiesCount++;

        GameObject prefab = enemyPrefabs[prefabIndex];
        float spawnY = Camera.main != null ? Camera.main.orthographicSize + 1.5f : 6f;
        float x = Random.Range(-4f, 4f);
        Vector3 spawnPos = new Vector3(x, spawnY, 0f);

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError($"[EnemySpawner] Префаб '{prefab.name}' не имеет NetworkObject!");
            Destroy(enemy);
            activeEnemiesCount--;
        }
    }

    private void HandleEnemyDied()
    {
        if (!IsServer || !levelInProgress) return;

        activeEnemiesCount--;
        
        // Проверка победы на уровне: деньги кончились и всех добили
        if (remainingBudget <= 0 && activeEnemiesCount <= 0)
        {
            levelInProgress = false;
            StartCoroutine(LevelCompleteRoutine());
        }
    }

    private IEnumerator LevelCompleteRoutine()
    {
        Debug.Log($"[EnemySpawner] Уровень {currentLevel} ПРОЙДЕН!");
        
        // TODO: Передать GameManager информацию о победе, начислить 1 очко

        yield return new WaitForSeconds(3f); // Передышка 3 секунды между волнами
        
        currentLevel++;
        StartLevel();
    }

    int PickPrefabWithinBudget(int budget)
    {
        // Ищем все префабы, которые мы можем себе позволить
        float totalWeight = 0f;
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyPrefabs[i] != null && enemyCosts[i] <= budget)
            {
                totalWeight += spawnWeights[i];
            }
        }

        if (totalWeight <= 0f) return -1;

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyPrefabs[i] != null && enemyCosts[i] <= budget)
            {
                cumulative += spawnWeights[i];
                if (rand <= cumulative) return i;
            }
        }

        return -1;
    }
}