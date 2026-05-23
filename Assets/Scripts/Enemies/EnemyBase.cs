using UnityEngine;
using Unity.Netcode;
using System;

public class EnemyBase : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 150f;
    public float moveSpeed = 0.7f;
    public float collisionDamage = 25f; // урон кораблю при столкновении

    [Header("Score")]
    [Tooltip("Сколько очков даёт этот враг тому, кто его убьёт")]
    public int scoreValue = 10;

    [Header("Explosion")]
    public GameObject explosionPrefab;

    // Читать могут все клиенты, писать — только сервер
    protected NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    protected float bottomThresholdY;

    // ClientId последнего, кто нанёс урон — получит очки за убийство
    protected ulong lastAttackerId = ulong.MaxValue;

    public event Action<float, float> OnHealthChanged; // (currentHP, maxHP)
    public static event Action OnEnemyDied; // Вызывается когда враг умирает или доходит до низа

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            CalculateBottomThreshold();
        }

        // Все клиенты подписываются, чтобы обновлять полоску HP локально
        currentHealth.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= HandleHealthChanged;
        
        if (IsServer)
        {
            OnEnemyDied?.Invoke();
        }
    }

    private void HandleHealthChanged(float oldVal, float newVal)
    {
        OnHealthChanged?.Invoke(newVal, maxHealth);
    }

  
    protected virtual void Update()
    {
        if (!IsServer) return;

        MoveDown();
        CheckBoundary();
    }

  
    protected virtual void MoveDown()
    {
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }

    protected virtual void CheckBoundary()
    {
        if (transform.position.y <= bottomThresholdY)
            ReachBottom();
    }

  
    /// <summary>
    /// Нанести урон врагу.
    /// attackerId — ClientId стрелявшего игрока (ulong.MaxValue если неизвестен).
    /// Подпись с optional-параметром обратно совместима: старые вызовы без attackerId работают без изменений.
    /// </summary>
    public virtual void TakeDamage(float damage, ulong attackerId = ulong.MaxValue)
    {
        if (!IsServer) return;

        // Запоминаем последнего атакующего для начисления очков
        if (attackerId != ulong.MaxValue)
            lastAttackerId = attackerId;

        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        // Начисляем очки тому, кто нанёс последний удар
        if (lastAttackerId != ulong.MaxValue && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(lastAttackerId, scoreValue);
        }

        SpawnExplosionClientRpc(transform.position);

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }


    protected virtual void ReachBottom()
    {
        // Враг достиг низа — не даёт очков, триггерит поражение
        SpawnExplosionClientRpc(transform.position);

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver(false); // false = поражение

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }


    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null && enemy == this) return;

        if (!other.CompareTag("Player")) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(collisionDamage);

        // При столкновении враг погибает без начисления очков
        // (lastAttackerId остаётся ulong.MaxValue)
        Die();
    }


    protected virtual void CalculateBottomThreshold()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("[EnemyBase] Camera.main не найдена при расчёте bottomThresholdY");
            bottomThresholdY = -3f; // fallback
            return;
        }

        float camHeight = Camera.main.orthographicSize;
        bottomThresholdY = -camHeight / 3f;
    }

    [ClientRpc]
    protected void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, pos, Quaternion.identity);
    }

    // Удобные геттеры для UI
    public float GetCurrentHealth() => currentHealth.Value;
    public float GetHealthRatio() => Mathf.Clamp01(currentHealth.Value / maxHealth);
}