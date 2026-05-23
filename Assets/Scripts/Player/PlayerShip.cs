using UnityEngine;
using Unity.Netcode;

public class PlayerShip : NetworkBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool isDead = false;

    [Header("Weapons")]
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    private double nextFireTime = 0;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private float halfWidth;
    private float halfHeight;

    public float GetHealthRatio() => Mathf.Clamp01(currentHealth.Value / maxHealth);

    public void FullHeal()
    {
        if (!IsServer || isDead) return;
        currentHealth.Value = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer || isDead) return;
        currentHealth.Value -= amount;

        OnDamagedClientRpc();

        if (currentHealth.Value <= 0f)
        {
            isDead = true;
            DieClientRpc(transform.position);

            bool anyAlive = false;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                var ship = client.PlayerObject.GetComponent<PlayerShip>();
                if (ship != null && !ship.isDead)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive && GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver(false);
            }

            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    private void OnDamagedClientRpc()
    {
        var flash = GetComponent<PlayerDamageFlash>();
        if (flash != null) flash.Flash();
    }

    [ClientRpc]
    private void DieClientRpc(Vector3 pos)
    {
        Debug.Log("[PlayerShip] Корабль уничтожен!");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            bool isHost = OwnerClientId == NetworkManager.ServerClientId;
            Vector3 startPos = isHost ? new Vector3(-3f, -3f, 0f) : new Vector3(3f, -3f, 0f);
            transform.position = startPos;

            var rBody = GetComponent<Rigidbody2D>();
            if (rBody != null)
            {
                rBody.position = startPos;
                rBody.linearVelocity = Vector2.zero;
            }

            StartCoroutine(ForcePositionNextFrame(startPos));

            double interval = 1.0 / fireRate;
            nextFireTime = System.Math.Ceiling(NetworkManager.Singleton.LocalTime.Time / interval) * interval;
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    private System.Collections.IEnumerator ForcePositionNextFrame(Vector3 pos)
    {
        yield return null;
        transform.position = pos;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = pos;
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

                double localTime = NetworkManager.Singleton.LocalTime.Time;
                if (localTime >= nextFireTime)
                {
                    Vector2 spawnPos = rb != null ? (Vector2)rb.position : (Vector2)transform.position;
                    Vector2 finalSpawnPos = spawnPos + new Vector2(0, halfHeight + 0.2f);

                    // Визуальная пуля у владельца (без shooterClientId — урона не наносит)
                    SpawnLocalBullet(finalSpawnPos, false, ulong.MaxValue);

                    // Отправляем на сервер: там будет физическая пуля с нашим ClientId
                    ShootServerRpc(finalSpawnPos);

                    double interval = 1.0 / fireRate;
                    nextFireTime = localTime + interval;
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

    /// <summary>
    /// Добавлен ServerRpcParams — сервер узнаёт ClientId стрелявшего и передаёт его пуле.
    /// </summary>
    [ServerRpc]
    private void ShootServerRpc(Vector2 pos, ServerRpcParams serverRpcParams = default)
    {
        ulong shooterId = serverRpcParams.Receive.SenderClientId;

        // Серверная пуля: наносит урон и несёт shooterClientId для начисления очков
        SpawnLocalBullet(pos, true, shooterId);

        // Просим остальных клиентов нарисовать пулю
        ShootClientRpc(pos);
    }

    [ClientRpc]
    private void ShootClientRpc(Vector2 pos)
    {
        if (IsOwner) return; // Владелец уже нарисовал себе визуальную пулю
        SpawnLocalBullet(pos, false, ulong.MaxValue);
    }

    private void SpawnLocalBullet(Vector2 pos, bool isServerBullet, ulong shooterId)
    {
        if (bulletPrefab == null) return;
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.canDealDamage = isServerBullet;
            b.shooterClientId = shooterId; // Только у серверной пули значение != ulong.MaxValue
        }

        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
        Collider2D shipCollider = GetComponent<Collider2D>();
        if (bulletCollider != null && shipCollider != null)
        {
            Physics2D.IgnoreCollision(bulletCollider, shipCollider);
        }
    }
}