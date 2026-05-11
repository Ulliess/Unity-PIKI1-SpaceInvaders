// Интерфейс для объектов, которые могут получать урон.
// Корабли игроков должны реализовать этот интерфейс.
// Пример: public class PlayerShip : NetworkBehaviour, IDamageable { ... }

public interface IDamageable
{
    void TakeDamage(float amount);
}