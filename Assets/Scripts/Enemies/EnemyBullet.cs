using UnityEngine;

/// <summary>
/// Пуля врага. Летит вниз, наносит урон игрокам.
/// НЕ является сетевым объектом (как и обычная пуля игрока).
/// Коллизия обрабатывается только на сервере, но пуля ВИЗУАЛЬНО
/// уничтожается при контакте на всех клиентах.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 6f;
    public float damage = 15f;
    
    /// <summary>
    /// true = серверная пуля (наносит урон), false = клиентская (только визуал)
    /// </summary>
    [HideInInspector]
    public bool canDealDamage = false;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
        
        // Уничтожаем, если улетела за экран
        if (Camera.main != null)
        {
            float bottomY = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;
            if (transform.position.y < bottomY - 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Бьём только игроков
        if (!other.CompareTag("Player")) return;

        // Урон наносится ТОЛЬКО на сервере
        if (canDealDamage)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();
                
            if (damageable != null)
                damageable.TakeDamage(damage);
        }

        // Пуля уничтожается ВСЕГДА (и на сервере, и на клиенте)
        Destroy(gameObject);
    }
}
