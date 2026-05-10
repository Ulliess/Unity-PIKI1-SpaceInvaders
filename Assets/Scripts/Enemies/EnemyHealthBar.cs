using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    private EnemyBase enemy;
    private SpriteRenderer barRenderer;
    private float originalScaleX;

    void Start()
    {
        barRenderer = GetComponent<SpriteRenderer>();
        originalScaleX = transform.localScale.x;

        enemy = GetComponentInParent<EnemyBase>();

        if (enemy != null)
        {
            enemy.CurrentHealth.OnValueChanged += OnHealthChanged;
        }
    }

    void OnDestroy()
    {
        if (enemy != null)
            enemy.CurrentHealth.OnValueChanged -= OnHealthChanged;
    }

    void OnHealthChanged(float oldValue, float newValue)
    {
        UpdateBar(newValue, enemy.MaxHealth);
    }

    void UpdateBar(float currentHP, float maxHP)
    {
        float ratio = Mathf.Clamp01(currentHP / maxHP);
        Vector3 scale = transform.localScale;
        scale.x = originalScaleX * ratio;
        transform.localScale = scale;
    }
}