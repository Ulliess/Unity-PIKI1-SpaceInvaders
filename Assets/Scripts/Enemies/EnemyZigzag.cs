using UnityEngine;
using Unity.Netcode;

public class EnemyZigzag : EnemyBase
{
    [Header("Zigzag Movement")]
    [Tooltip("Максимальное отклонение по X от стартовой позиции")]
    public float zigzagAmplitude = 2f;

    [Tooltip("Количество полных колебаний в секунду")]
    public float zigzagFrequency = 1.5f;

    private float startX;
    private float spawnTime;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            startX = transform.position.x;
            spawnTime = Time.time;
        }
    }

    protected override void MoveDown()
    {
        float elapsed = Time.time - spawnTime;

        float xOffset = Mathf.Sin(elapsed * zigzagFrequency * Mathf.PI * 2f) * zigzagAmplitude;

        Vector3 pos = transform.position;
        pos.y -= moveSpeed * Time.deltaTime;
        pos.x = startX + xOffset;
        transform.position = pos;
    }
}