using UnityEngine;
using Unity.Netcode;

public class PlayerShip : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    private float fireCooldown = 0f;
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private float halfWidth;
    private float halfHeight;

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
        if (!IsOwner) return;

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(x, y) * moveSpeed;
        }

        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            ShootServerRpc(rb != null ? (Vector2)rb.position : (Vector2)transform.position);
            fireCooldown = 1f / fireRate;
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

    [ServerRpc]
    void ShootServerRpc(Vector2 spawnPos)
    {
        if (bulletPrefab == null) return;

        // Спавним пулю чуть ВЫШЕ корабля, чтобы она не появлялась прямо внутри него
        Vector2 finalSpawnPos = spawnPos + new Vector2(0, halfHeight + 0.2f);
        GameObject bullet = Instantiate(bulletPrefab, finalSpawnPos, Quaternion.identity);
        
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
        Collider2D shipCollider = GetComponent<Collider2D>();
        if (bulletCollider != null && shipCollider != null)
        {
            Physics2D.IgnoreCollision(bulletCollider, shipCollider);
        }

        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError("На префабе пули нет компонента NetworkObject!");
        }
    }
}