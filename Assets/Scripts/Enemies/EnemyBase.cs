using UnityEngine;
using Unity.Netcode;
using System;

public class EnemyBase : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 150f; // +50% (было 100)
    public float moveSpeed = 1.5f; // +25% от 1.2 (было 1.2)
    public float collisionDamage = 25f; // урон кораблю при столкновении

    [Header("Explosion")]
    public GameObject explosionPrefab;

    // Читать могут все клиенты, писать — только сервер
    protected NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    protected float bottomThresholdY;

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

  
    public virtual void TakeDamage(float damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        SpawnExplosionClientRpc(transform.position);

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }


    protected virtual void ReachBottom()
    {
        SpawnExplosionClientRpc(transform.position);

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver(false); // false = поражение

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }


    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // Ищем компонент EnemyBase в объекте или его родителях (на случай, если коллайдер на дочернем объекте)
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        
        if (enemy != null)
        {
            // Если попали в самого себя (например, через триггер), игнорируем
            if (enemy == this) return;
        }

        // Проверяем тег — корабли игроков должны иметь тег "Player"
        if (!other.CompareTag("Player")) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(collisionDamage);

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