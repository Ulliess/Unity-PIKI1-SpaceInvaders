using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Компонент стрельбы для врагов. Вешается на префаб врага поверх EnemyBase.
/// Враг периодически стреляет вниз. Пуля наносит урон игрокам при попадании.
/// Стрельба управляется только сервером, визуал рассылается через ClientRpc.
/// </summary>
public class EnemyShooter : NetworkBehaviour
{
    [Header("Shooting")]
    public GameObject enemyBulletPrefab; // Префаб вражеской пули
    public float shootInterval = 3f;     // Интервал между выстрелами
    public float shootIntervalRandom = 1f; // Рандомный разброс интервала
    public float bulletSpeed = 6f;
    public float bulletDamage = 15f;

    private float shootTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // Рандомный начальный таймер, чтобы враги не стреляли залпом одновременно
        shootTimer = Random.Range(1f, shootInterval + shootIntervalRandom);
    }

    void Update()
    {
        if (!IsServer) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGlobalPaused.Value) return;

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = shootInterval + Random.Range(-shootIntervalRandom, shootIntervalRandom);
            Shoot();
        }
    }

    void Shoot()
    {
        Vector2 spawnPos = (Vector2)transform.position + Vector2.down * 0.5f;
        
        // Спавним физическую пулю на сервере (с коллайдером)
        SpawnEnemyBullet(spawnPos, true);
        
        // Рассылаем визуальную пулю клиентам
        ShootClientRpc(spawnPos);
    }

    [ClientRpc]
    private void ShootClientRpc(Vector2 pos)
    {
        if (IsServer) return; // Сервер уже создал свою
        SpawnEnemyBullet(pos, false);
    }

    private void SpawnEnemyBullet(Vector2 pos, bool isServerBullet)
    {
        if (enemyBulletPrefab == null) return;
        
        GameObject bullet = Instantiate(enemyBulletPrefab, pos, Quaternion.identity);
        
        EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.speed = bulletSpeed;
            eb.damage = bulletDamage;
            eb.canDealDamage = isServerBullet; // Только серверная пуля наносит урон
        }
    }
}
