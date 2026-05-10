using UnityEngine;
using Unity.Netcode;


// Теперь спавнер со временем увеличивает частоту спавна 

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs (порядок: Scout, Drone, Brute, Weaver, Dasher)")]
    public GameObject[] enemyPrefabs = new GameObject[5];

    [Header("Spawn Weights — вероятность каждого типа")]
    [Tooltip("Должен совпадать по длине с enemyPrefabs")]
    public float[] spawnWeights = { 0.35f, 0.25f, 0.15f, 0.15f, 0.10f };

    [Header("Spawn Area")]
    [Tooltip("Оставь 0 — будет рассчитано автоматически по камере")]
    public float spawnY = 0f;
    public float spawnXRange = 4f;

    [Header("Spawn Timing")]
    public float initialSpawnInterval = 2.5f;   // интервал в начале уровня
    public float minSpawnInterval = 0.6f;         // минимальный интервал (предел сложности)

    [Tooltip("На сколько секунд уменьшается интервал каждые 30 секунд игры")]
    public float difficultyRampPerMinute = 0.3f;

    private float timer = 0f;
    private float elapsedTime = 0f;
    private float currentInterval;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        currentInterval = initialSpawnInterval;

        if (Mathf.Approximately(spawnY, 0f) && Camera.main != null)
            spawnY = Camera.main.orthographicSize + 1.5f; 
    }


    void Update()
    {
        if (!IsServer) return;

        elapsedTime += Time.deltaTime;
        timer += Time.deltaTime;

        // Плавно уменьшаем интервал со временем
        float minutesPassed = elapsedTime / 60f;
        currentInterval = Mathf.Max(
            minSpawnInterval,
            initialSpawnInterval - minutesPassed * difficultyRampPerMinute
        );

        if (timer >= currentInterval)
        {
            timer = 0f;
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Не удалось выбрать префаб — проверь настройки в инспекторе.");
            return;
        }

        float x = Random.Range(-spawnXRange, spawnXRange);
        Vector3 spawnPos = new Vector3(x, spawnY, 0f);

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        NetworkObject netObj = enemy.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError($"[EnemySpawner] Префаб '{prefab.name}' не имеет компонента NetworkObject! Добавь его.");
            Destroy(enemy);
        }
    }

    GameObject PickPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;

        int count = Mathf.Min(enemyPrefabs.Length, spawnWeights.Length);

        // Суммируем веса только для непустых слотов
        float totalWeight = 0f;
        for (int i = 0; i < count; i++)
        {
            if (enemyPrefabs[i] != null)
                totalWeight += spawnWeights[i];
        }

        if (Mathf.Approximately(totalWeight, 0f)) return null;

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < count; i++)
        {
            if (enemyPrefabs[i] == null) continue;

            cumulative += spawnWeights[i];
            if (rand <= cumulative)
                return enemyPrefabs[i];
        }

        // Fallback — первый непустой префаб
        foreach (var p in enemyPrefabs)
            if (p != null) return p;

        return null;
    }
}