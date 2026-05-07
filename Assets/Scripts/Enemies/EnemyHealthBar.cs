using UnityEngine;
using Unity.Netcode;

public class EnemyHealthBar : MonoBehaviour
{
    public EnemyBase enemy;

    private SpriteRenderer barRenderer;
    private float originalScaleX;

    void Start()
    {
        barRenderer = GetComponent<SpriteRenderer>();
        originalScaleX = transform.localScale.x;
    }

    void Update()
    {
        // Здесь просто заготовка
    }

    public void UpdateBar(float currentHP, float maxHP)
    {
        float ratio = Mathf.Clamp01(currentHP / maxHP);
        Vector3 scale = transform.localScale;
        scale.x = originalScaleX * ratio;
        transform.localScale = scale;
    }
}