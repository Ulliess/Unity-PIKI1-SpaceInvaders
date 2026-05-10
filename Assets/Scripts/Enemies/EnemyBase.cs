using UnityEngine;
using Unity.Netcode;

public class EnemyBase : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2f;

    [Header("Explosion")]
    public GameObject explosionPrefab;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private float bottomThresholdY;
    private bool isDead = false;

    public NetworkVariable<float> CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        if (Camera.main != null)
        {
            float camHeight = Camera.main.orthographicSize;
            bottomThresholdY = -camHeight * (1f / 3f);
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (isDead) return;

        transform.position += Vector3.down * moveSpeed * Time.deltaTime;

        if (transform.position.y <= bottomThresholdY)
        {
            TriggerExplosion();
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        if (isDead) return;

        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    void TriggerExplosion()
    {
        isDead = true;
        SpawnExplosionClientRpc(transform.position);
        GetComponent<NetworkObject>().Despawn();
    }

    void Die()
    {
        isDead = true;
        SpawnExplosionClientRpc(transform.position);
        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, pos, Quaternion.identity);

        Debug.Log($"Explosion at {pos}");
    }
}