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
        halfWidth = GetComponent<SpriteRenderer>().bounds.extents.x;
        halfHeight = GetComponent<SpriteRenderer>().bounds.extents.y;
    }

    void Update()
    {
        if (!IsOwner) return;

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(x, y) * moveSpeed;
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            Shoot();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector2 pos = rb.position;
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0.33f, 0));

        pos.x = Mathf.Clamp(pos.x, bottomLeft.x + halfWidth, topRight.x - halfWidth);
        pos.y = Mathf.Clamp(pos.y, bottomLeft.y + halfHeight, topRight.y - halfHeight);
        rb.position = pos;

        
    }
    void Shoot()
    {
        Vector3 spawnPos = rb.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Physics2D.IgnoreCollision(bullet.GetComponent<Collider2D>(), GetComponent<Collider2D>());
        fireCooldown = 1f / fireRate;
    }
}