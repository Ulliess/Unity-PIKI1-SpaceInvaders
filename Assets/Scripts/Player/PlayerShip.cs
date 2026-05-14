using UnityEngine;
using Unity.Netcode;

public class PlayerShip : NetworkBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Weapons")]
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    private double nextFireTime = 0;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private float halfWidth;
    private float halfHeight;

    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0f)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            Vector3 startPos = IsServer ? new Vector3(-3f, -3f, 0f) : new Vector3(3f, -3f, 0f);
            transform.position = startPos;
            
            var rBody = GetComponent<Rigidbody2D>();
            if (rBody != null) rBody.position = startPos;
            
            // Таймер стрельбы теперь крутится У ВЛАДЕЛЬЦА
            double interval = 1.0 / fireRate;
            nextFireTime = System.Math.Ceiling(NetworkManager.Singleton.LocalTime.Time / interval) * interval;
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            halfWidth = spriteRenderer.bounds.extents.x;
            halfHeight = spriteRenderer.bounds.extents.y;
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            if (GameManager.Instance != null && 
               (GameManager.Instance.IsGlobalPaused.Value || GameManager.Instance.IsLocalMenuOpen))
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                
                if (NetworkManager.Singleton.LocalTime.Time >= nextFireTime)
                {
                    nextFireTime += 1.0 / fireRate;
                }
            }
            else
            {
                float x = Input.GetAxis("Horizontal");
                float y = Input.GetAxis("Vertical");

                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(x, y) * moveSpeed;
                }

                // Владелец стреляет визуально без задержек (Client-Side Prediction)
                if (NetworkManager.Singleton.LocalTime.Time >= nextFireTime)
                {
                    Vector2 spawnPos = rb != null ? (Vector2)rb.position : (Vector2)transform.position;
                    Vector2 finalSpawnPos = spawnPos + new Vector2(0, halfHeight + 0.2f);
                    
                    // Рисуем пулю себе моментально
                    SpawnLocalBullet(finalSpawnPos, false);
                    
                    // Отправляем на сервер свои координаты для просчёта урона
                    ShootServerRpc(finalSpawnPos);
                    
                    nextFireTime += 1.0 / fireRate;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner || rb == null) return;

        Vector2 pos = rb.position;
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0.33f, 0));

        pos.x = Mathf.Clamp(pos.x, bottomLeft.x + halfWidth, topRight.x - halfWidth);
        pos.y = Mathf.Clamp(pos.y, bottomLeft.y + halfHeight, topRight.y - halfHeight);
        rb.position = pos;
    }

    // Требуем, чтобы RPC вызывал только владелец
    [ServerRpc]
    private void ShootServerRpc(Vector2 pos)
    {
        // Сервер спавнит физическую пулю ровно в координатах клиента
        SpawnLocalBullet(pos, true);
        
        // Сервер просит остальных игроков нарисовать пулю
        ShootClientRpc(pos);
    }

    [ClientRpc]
    private void ShootClientRpc(Vector2 pos)
    {
        // Владелец уже сам себе всё нарисовал
        if (IsOwner) return;

        SpawnLocalBullet(pos, false);
    }

    private void SpawnLocalBullet(Vector2 pos, bool withCollision)
    {
        if (bulletPrefab == null) return;
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);

        if (withCollision)
        {
            Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
            Collider2D shipCollider = GetComponent<Collider2D>();
            if (bulletCollider != null && shipCollider != null)
            {
                Physics2D.IgnoreCollision(bulletCollider, shipCollider);
            }
        }
        else
        {
            Collider2D col = bullet.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }
}