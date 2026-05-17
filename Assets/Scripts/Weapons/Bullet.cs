using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 25f; // Увеличили урон
    public float aoeRadius = 0f; // 0 = обычная пуля, >0 = AoE-урон в радиусе

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        if (transform.position.y > Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0)).y + 1f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Столкновение пули с врагом.
    /// Обрабатывается ТОЛЬКО на сервере (на клиенте у пули отключен коллайдер).
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

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

        Destroy(gameObject);
    }
}
