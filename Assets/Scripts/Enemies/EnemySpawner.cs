using UnityEngine;
using Unity.Netcode;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawning")]
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public float spawnXRange = 4f;

    private float timer;
    private float spawnY;

    public override void OnNetworkSpawn()
    {
        if (IsServer && Camera.main != null)
        {
            spawnY = Camera.main.orthographicSize + 1f;
        }
    }

    void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0;
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        float x = Random.Range(-spawnXRange, spawnXRange);
        Vector3 pos = new Vector3(x, spawnY, 0);

        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        enemy.GetComponent<NetworkObject>().Spawn();
    }
}