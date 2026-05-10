using UnityEngine;
using Unity.Netcode;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;

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
        // Проверяем что враг имеет тег "Enemy"
        if (!other.CompareTag("Enemy")) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
