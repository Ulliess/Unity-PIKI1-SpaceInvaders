using UnityEngine;
using Unity.Netcode;

public class EnemyBase : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2f;

    [Header("Explosion")]
    public GameObject explosionPrefab; // заготовка под взрыв
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    private float bottomThresholdY; // Y-координата нижней трети экрана

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            float camHeight = Camera.main.orthographicSize;
            bottomThresholdY = -camHeight + (camHeight * 2f / 3f);
            bottomThresholdY = -camHeight * (1f / 3f); 
        }
    }

    void Update()
    {
        if (!IsServer) return;
        transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        if (transform.position.y <= bottomThresholdY)
        {
            ExplodeServerRpc();
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        SpawnExplosionClientRpc(transform.position);
        GetComponent<NetworkObject>().Despawn();
    }

    [ServerRpc]
    void ExplodeServerRpc()
    {
        SpawnExplosionClientRpc(transform.position);
        GetComponent<NetworkObject>().Despawn();
        
        // Если враг долетел до низа — команда проигрывает!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver(false); // false = поражение
        }
    }

    [ClientRpc]
    void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, pos, Quaternion.identity);
        // Заготовка для создания эффекта взрыва. В реальной игре здесь будет анимация, звук и т.д.
        Debug.Log($"Explosion at {pos}");
    }
}