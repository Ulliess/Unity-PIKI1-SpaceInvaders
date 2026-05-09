using UnityEngine;
using Unity.Netcode;

public class PlayerShip : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    private double nextFireTime = 0;
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private float halfWidth;
    private float halfHeight;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            // Жёстко ставим позицию при спавне, чтобы клиент не успел 
            // "отклемпиться" от (0,0,0) до верха разрешённой зоны (-2.2)
            Vector3 startPos = IsServer ? new Vector3(-3f, -3f, 0f) : new Vector3(3f, -3f, 0f);
            transform.position = startPos;
            
            var rBody = GetComponent<Rigidbody2D>();
            if (rBody != null) rBody.position = startPos;
        }

        if (IsServer)
        {
            // Синхронизируем первый выстрел по глобальному времени сервера (сетка интервалов)
            double interval = 1.0 / fireRate;
            nextFireTime = System.Math.Ceiling(NetworkManager.Singleton.ServerTime.Time / interval) * interval;
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
            // Блокируем управление, если игра на паузе (глобальной или локальной)
            if (GameManager.Instance != null && 
               (GameManager.Instance.IsGlobalPaused.Value || GameManager.Instance.IsLocalMenuOpen))
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
            else
            {
                float x = Input.GetAxis("Horizontal");
                float y = Input.GetAxis("Vertical");

                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(x, y) * moveSpeed;
                }
            }
        }

        // Авто-стрельбу обрабатывает ТОЛЬКО сервер, чтобы не было задержек пинга
        if (IsServer)
        {
            // Если игра на глобальной паузе
            if (GameManager.Instance != null && GameManager.Instance.IsGlobalPaused.Value)
            {
                // Прокручиваем таймер вхолостую, чтобы пули не накапливались в "долг"
                if (NetworkManager.Singleton.ServerTime.Time >= nextFireTime)
                {
                    nextFireTime += 1.0 / fireRate;
                }
            }
            else
            {
                // Обычная стрельба
                if (NetworkManager.Singleton.ServerTime.Time >= nextFireTime)
                {
                    Shoot(rb != null ? (Vector2)rb.position : (Vector2)transform.position);
                    nextFireTime += 1.0 / fireRate;
                }
            }
        }
    }

    private int framesAlive = 0;

    void FixedUpdate()
    {
        if (!IsOwner || rb == null) return;

        // Ждём несколько кадров, чтобы NetworkTransform успел применить
        // правильную позицию от сервера и не было ложного клемпинга от (0,0,0)
        framesAlive++;
        if (framesAlive < 10) return;

        Vector2 pos = rb.position;
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0.33f, 0));

        pos.x = Mathf.Clamp(pos.x, bottomLeft.x + halfWidth, topRight.x - halfWidth);
        pos.y = Mathf.Clamp(pos.y, bottomLeft.y + halfHeight, topRight.y - halfHeight);
        rb.position = pos;
    }

    void Shoot(Vector2 spawnPos)
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