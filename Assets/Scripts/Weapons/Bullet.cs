using UnityEngine;

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
}
