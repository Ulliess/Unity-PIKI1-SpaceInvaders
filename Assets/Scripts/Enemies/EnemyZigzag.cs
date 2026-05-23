using UnityEngine;
using Unity.Netcode;

public class EnemyZigzag : EnemyBase
{
    [Header("Zigzag Movement")]
    [Tooltip("Скорость горизонтального движения")]
    public float zigzagSpeed = 1.5f;

    [Tooltip("Отступ от края экрана")]
    public float screenMargin = 1.0f;

    private float direction = 1f; // 1 = вправо, -1 = влево
    private float leftBound;
    private float rightBound;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Вычисляем границы экрана
            if (Camera.main != null)
            {
                float camHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
                leftBound = -camHalfWidth + screenMargin;
                rightBound = camHalfWidth - screenMargin;
            }
            else
            {
                leftBound = -5f;
                rightBound = 5f;
            }

            // Случайное начальное направление
            direction = Random.value > 0.5f ? 1f : -1f;
        }
    }

    protected override void MoveDown()
    {
        Vector3 pos = transform.position;

        // Двигаемся вниз
        pos.y -= moveSpeed * Time.deltaTime;

        // Двигаемся горизонтально (DVD-bounce)
        pos.x += direction * zigzagSpeed * Time.deltaTime;

        // Отталкиваемся от краёв экрана
        if (pos.x <= leftBound)
        {
            pos.x = leftBound;
            direction = 1f;
        }
        else if (pos.x >= rightBound)
        {
            pos.x = rightBound;
            direction = -1f;
        }

        transform.position = pos;
    }
}