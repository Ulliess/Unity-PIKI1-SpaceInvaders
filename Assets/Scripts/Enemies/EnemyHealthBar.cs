using UnityEngine;


// Полоска хп должна работать на всех клиентах через событие OnHealthChanged в EnemyBase

public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Оставь пустым — найдёт автоматически на родительском объекте")]
    public EnemyBase enemy;

    private SpriteRenderer barRenderer;
    private float originalScaleX;

    void Start()
    {
        barRenderer = GetComponent<SpriteRenderer>();

        if (barRenderer != null)
            originalScaleX = transform.localScale.x;

        if (enemy == null)
            enemy = GetComponentInParent<EnemyBase>();

        if (enemy != null)
        {
            enemy.OnHealthChanged += UpdateBar;
            UpdateBar(enemy.GetCurrentHealth(), enemy.GetCurrentHealth());
        }
        else
        {
            Debug.LogWarning("[EnemyHealthBar] EnemyBase не найден! Укажи вручную или поставь как дочерний объект врага.");
        }
    }

    void OnDestroy()
    {
        if (enemy != null)
            enemy.OnHealthChanged -= UpdateBar;
    }

    public void UpdateBar(float currentHP, float maxHP)
    {
        if (barRenderer == null) return;

        float ratio = Mathf.Clamp01(currentHP / maxHP);
        Vector3 scale = transform.localScale;
        scale.x = originalScaleX * ratio;
        transform.localScale = scale;

        barRenderer.color = Color.Lerp(Color.red, Color.green, ratio);
    }
}