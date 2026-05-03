using UnityEngine;

public class PlayerShip : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(x, y) * moveSpeed;
    }
}
