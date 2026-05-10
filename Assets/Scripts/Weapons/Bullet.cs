using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float aoeRadius = 0f; 

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        if (transform.position.y > Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0)).y + 1f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (aoeRadius > 0f)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
                foreach (Collider2D hit in hits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        Debug.Log("AoE попал в: " + hit.name);
                    }
                }
            }
            Destroy(gameObject);
        }
    }
}