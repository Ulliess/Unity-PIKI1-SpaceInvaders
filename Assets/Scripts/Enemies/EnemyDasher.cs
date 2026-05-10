using UnityEngine;
using Unity.Netcode;

public class EnemyDasher : EnemyBase
{
    [Header("Dash Settings")]
    [Tooltip("Множитель скорости во время рывка")]
    public float dashSpeedMultiplier = 5f;

    [Tooltip("Длительность рывка (секунды)")]
    public float dashDuration = 0.35f;

    [Tooltip("Пауза между рывками (секунды)")]
    public float dashCooldown = 2.5f;

    [Tooltip("Задержка до первого рывка после спавна")]
    public float initialDelay = 1f;

    private float cooldownTimer;
    private float dashTimer;
    private bool isDashing = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Первый рывок произойдёт через initialDelay
            cooldownTimer = dashCooldown - initialDelay;
        }
    }

    protected override void MoveDown()
    {
        if (!isDashing)
        {
            cooldownTimer += Time.deltaTime;

            if (cooldownTimer >= dashCooldown)
            {
                cooldownTimer = 0f;
                isDashing = true;
                dashTimer = 0f;
                // Запускаем визуальный эффект рывка (пока нет)
                OnDashStartClientRpc();
            }
        }
        else
        {
            dashTimer += Time.deltaTime;
            if (dashTimer >= dashDuration)
            {
                isDashing = false;
            }
        }

        float speed = isDashing ? moveSpeed * dashSpeedMultiplier : moveSpeed;
        transform.position += Vector3.down * speed * Time.deltaTime;
    }

    // Заготовка для визуального эффекта рывка на клиентах.
    // Сюда можно добавить вспышку, след, звук и т.д.
    [ClientRpc]
    private void OnDashStartClientRpc()
    {
        // TODO: добавить визуальный эффект рывка
    }
}