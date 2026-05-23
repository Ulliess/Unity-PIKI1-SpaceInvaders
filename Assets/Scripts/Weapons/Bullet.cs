using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 25f;
    public float aoeRadius = 0f; // 0 = обычная пуля, >0 = AoE-урон в радиусе
    
    /// <summary>
    /// true = серверная пуля (наносит урон), false = клиентская (только визуал)
    /// </summary>
    [HideInInspector]
    public bool canDealDamage = false;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        if (Camera.main != null && transform.position.y > Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0)).y + 1f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Столкновение пули с врагом.
    /// Урон наносится ТОЛЬКО на сервере, но пуля УНИЧТОЖАЕТСЯ везде.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        // Урон только на сервере
        if (canDealDamage)
        {
            if (aoeRadius > 0f)
            {
                // AoE-урон: бьём всех врагов в радиусе
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
                foreach (Collider2D hit in hits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        EnemyBase hitEnemy = hit.GetComponent<EnemyBase>();
                        if (hitEnemy != null)
                            hitEnemy.TakeDamage(damage);
                    }
                }
            }
            else
            {
                // Ищем EnemyBase в самом объекте или в родителе
                EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                    enemy.TakeDamage(damage);
            }
        }

        // Пуля уничтожается ВСЕГДА (и на сервере, и на клиенте)
        Destroy(gameObject);
    }
}
